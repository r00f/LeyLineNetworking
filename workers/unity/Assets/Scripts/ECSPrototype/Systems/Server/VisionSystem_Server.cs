using UnityEngine;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using Generic;
using Cell;
using Player;
using Unit;
using System.Linq;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(ResourceSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class VisionSystem_Server : ComponentSystem
{
    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<CubeCoordinate.Component> CellCoordinateData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellsData;
    }

    [Inject]
    private CellData m_CellData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<PlayerAttributes.Component> PlayerAttributes;
        public readonly ComponentDataArray<FactionComponent.Component> FactionComponent;
        public ComponentDataArray<Vision.CommandSenders.UpdateClientVisionCommand> UpdateClientVisionCommands;
        public ComponentDataArray<Vision.Component> VisionComponent;
    }

    [Inject]
    private PlayerData m_PlayerData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WordIndexData;
        public readonly ComponentDataArray<CubeCoordinate.Component> UnitCoordinateData;
        public readonly ComponentDataArray<FactionComponent.Component> FactionComponent;
        public readonly ComponentDataArray<Health.Component> HealthData;
        public ComponentDataArray<Vision.Component> VisionComponent;
    }
    [Inject]
    private UnitData m_UnitData;

    [Inject]
    private HandleCellGridRequestsSystem GridSys;

    //[Inject]
    //private VisionSystem_Client m_VisionSystem_Client;


    bool Init = true;
    bool firstTime = true;
    private List<RawCluster> FixClusters = new List<RawCluster>();

    protected override void OnUpdate()
    {
        if (firstTime) firstTime = false;
        else {
            if (Init) BuildRawClusters();

            //if any unit requires an update, update player aswell
            bool anyUnitReqUpdate = false;

            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var u_worldIndex = m_UnitData.WordIndexData[i].Value;
                var u_Vision = m_UnitData.VisionComponent[i];
                var u_OccupiedCell = m_UnitData.UnitCoordinateData[i];
                var u_Faction = m_UnitData.FactionComponent[i];

                if (u_Vision.RequireUpdate == true)
                {
                    u_Vision = UpdateUnitVision(u_OccupiedCell, u_Vision, u_Faction, u_worldIndex);
                    m_UnitData.VisionComponent[i] = u_Vision;
                    anyUnitReqUpdate = true;
                }
            }

            for (int i = m_PlayerData.Length - 1; i >= 0; i--)
            {
                var p_Vision = m_PlayerData.VisionComponent[i];
                var p_Faction = m_PlayerData.FactionComponent[i];
                //var p_id = m_PlayerData.EntityIds[i].EntityId;
                //var updateClientVisionRequest = m_PlayerData.UpdateClientVisionCommands[i];

                if (anyUnitReqUpdate)
                {
                    //Debug.Log("UpdatePlayerVision");
                    p_Vision = UpdatePlayerVision(p_Vision, p_Faction.Faction);
                    p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;
                    //p_Vision.RequireUpdate = p_Vision.RequireUpdate;
                    p_Vision.Positives = p_Vision.Positives;
                    p_Vision.Negatives = p_Vision.Negatives;
                    p_Vision.Lastvisible = p_Vision.Lastvisible;
                    m_PlayerData.VisionComponent[i] = p_Vision;

                    //Send clientSide updateVision command
                    /*
                    
                    var request = new Vision.UpdateClientVisionCommand.Request
                    (
                        p_id,
                        new UpdateClientVisionRequest()
                    );

                    updateClientVisionRequest.RequestsToSend.Add(request);
                    m_PlayerData.UpdateClientVisionCommands[i] = updateClientVisionRequest;

                    */
                    
                    //Debug.Log("SendUpdateClientVisionRequest, count = " + updateClientVisionRequest.RequestsToSend.Count + ", " + m_PlayerData.UpdateClientVisionCommands[i].RequestsToSend.Count);
  
                }
            
            }
            
        }
    }

    private Vision.Component UpdateUnitVision(CubeCoordinate.Component coor, Vision.Component inVision, FactionComponent.Component inFaction, uint inWorldIndex)
    {
        List<Vector3f> sight = GridSys.CircleDraw(coor.CubeCoordinate, inVision.VisionRange);
        
        var sightHash = new HashSet<Vector3f>();
        foreach (Vector3f v in sight)
        {
            sightHash.Add(v);
        }


        List<ObstructVisionCluster> RelevantClusters = new List<ObstructVisionCluster>();



        foreach(RawCluster c in FixClusters)
        {
            bool isRelevant = false;
            HashSet<Vector3f> set = new HashSet<Vector3f>();
            foreach (CellAttributesComponent.Component a in c.cluster)
            {
                if (sightHash.Contains(a.CellAttributes.Cell.CubeCoordinate)) isRelevant = true;
                set.Add(a.CellAttributes.Cell.CubeCoordinate);
            }

            if (isRelevant)
            {
                RelevantClusters.Add(new ObstructVisionCluster(set, coor.CubeCoordinate));            
            }
        }

        if (RelevantClusters.Count != 0)
        {
            List<Vector3f> Ring = GridSys.RingDraw(coor.CubeCoordinate, inVision.VisionRange);
            List<List<Vector3f>> Lines = new List<List<Vector3f>>();

            foreach (Vector3f c in Ring)
            {
                Lines.Add(GridSys.LineDraw(coor.CubeCoordinate, c));
            }
            foreach (List<Vector3f> l in Lines)
            {
                bool visible = true;
                for (int i = 0; i < l.Count; i++)
                {
                    if (visible)
                    {
                        foreach (ObstructVisionCluster o in RelevantClusters)
                        {
                            if (o.cluster.Contains(l[i]))
                            {
                                visible = false;
                                sightHash.Remove(l[i]);
                            }
                        }
                    }
                    else
                    {
                        sightHash.Remove(l[i]);
                    }
                }
            }
            sight = new List<Vector3f>(sightHash);
        }

        inVision.CellsInVisionrange = sight;
        inVision.RequireUpdate = false;

        return inVision;
    }

    private Vision.Component UpdatePlayerVision(Vision.Component inVision, uint faction)
    {
        //Debug.Log(inVision.CellsInVisionrange.Count);
        inVision.Lastvisible.Clear();
        inVision.Lastvisible.AddRange(inVision.CellsInVisionrange);

        inVision.Positives.Clear();
        inVision.Negatives.Clear();
        inVision.CellsInVisionrange.Clear();

        var lastVision = new HashSet<Vector3f>();

        foreach(Vector3f v in inVision.Lastvisible)
        {
            lastVision.Add(v);
        }

        var currentVision = new HashSet<Vector3f>();

        for (int e = m_UnitData.Length - 1; e >= 0; e--)
        {
            var UnitVision = m_UnitData.VisionComponent[e];
            var UnitFaction = m_UnitData.FactionComponent[e];

            if (faction == UnitFaction.Faction)
            {
                //use a hashSet to increase performance of contains calls (O(1) vs. O(n))
                foreach (Vector3f v in UnitVision.CellsInVisionrange)
                {
                    currentVision.Add(v);
                }
            }
        }

        inVision.CellsInVisionrange = currentVision.ToList();

        foreach (Vector3f v in lastVision)
        {
            if(!currentVision.Contains(v))
            {
                inVision.Negatives.Add(v);
            }
        }

        foreach(Vector3f v in currentVision)
        {
            if(!lastVision.Contains(v))
            {
                inVision.Positives.Add(v);
            }
        }

        return inVision;
    }

    private void BuildRawClusters()
    {
        List<CellAttributesComponent.Component> obstructed = new List<CellAttributesComponent.Component>();

        //Debug.Log(m_CellData.Length);
        for (int i = m_CellData.Length - 1; i >= 0; i--)
        {
            var cell = m_CellData.CellsData[i];
            var cor = m_CellData.CellsData[i];

            if (cell.CellAttributes.Cell.ObstructVision)
            {
                obstructed.Add(cell);
            }

        }
        //Debug.Log("ObstructedCount:" + obstructed.Count);

        List<RawCluster> raw = new List<RawCluster>();
        while (obstructed.Count > 0)
        {
            CellAttributesComponent.Component c = obstructed[0];
            RawCluster go = new RawCluster(c);
            obstructed.Remove(c);
            BuildCluster(c, go, obstructed, out obstructed);
            raw.Add(go);
        }

        for (int i = raw.Count - 1; i >= 0; i--)
        {
            if (raw[i].cluster.Count > 0)
            {
                FixClusters.Add(raw[i]);
            }
        }
        //Debug.Log("NumberofClusters" + FixClusters.Count());
        Init = false;
    }

    private void BuildCluster(CellAttributesComponent.Component cell, RawCluster cluster, List<CellAttributesComponent.Component> obstructed, out List<CellAttributesComponent.Component> newObstructed)
    {
        List<CellAttribute> neighbours = cell.CellAttributes.Neighbours.CellAttributes;
        for (int i = neighbours.Count - 1; i >= 0; i--)
        {
            bool contains = false;
            {
                foreach (CellAttributesComponent.Component c in obstructed)
                {
                    if (c.CellAttributes.Cell.CubeCoordinate == neighbours[i].CubeCoordinate)
                    {
                        contains = true;
                    }
                }
            }
            if (!contains) neighbours.Remove(neighbours[i]);
        }
        
        for (int i = neighbours.Count - 1; i >= 0; i--)
        {
            bool contains = false;

            foreach (CellAttributesComponent.Component c in cluster.cluster)
            {
                if (c.CellAttributes.Cell.CubeCoordinate == neighbours[i].CubeCoordinate) contains = true;
            }

            if (!contains)
            {
                bool isSet = false;
                CellAttributesComponent.Component toRemove = new CellAttributesComponent.Component();
                foreach (CellAttributesComponent.Component c in obstructed)
                {
                    if (c.CellAttributes.Cell.CubeCoordinate == neighbours[i].CubeCoordinate) toRemove = c;
                    isSet = true;
                }

                if (isSet)
                {
                    obstructed.Remove(toRemove);
                    cluster.cluster.Add(toRemove);
                    //Debug.Log("added " + i + " to" + cluster);
                    BuildCluster(toRemove, cluster, obstructed, out obstructed);
                }
                }
            }
        newObstructed = obstructed;
    }

    private void Cluster_DetermineAngles(ObstructVisionCluster inCluster)
    {/*
        foreach (CellAttributesComponent.Component c in inCluster.cluster)
        {

            Angle angle = new Angle(c, GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.CellAttributes.Cell.Position));

            if (!inCluster.RelevantAngles.Contains(angle))
            {
                inCluster.RelevantAngles.Add(angle);
            }
            
        }
        //print("Relevant angles Count: " + inCluster.RelevantAngles.Count());

    }
    */
    }
    /*private List<CellAttributes> Cluster_UseAngles(ObstructVisionCluster inCluster, List<CellAttributes> watching)
    {
        int count = inCluster.RelevantAngles.Count;
        //Debug.Log(count);
        Angle largest;
        Angle smallest;
        List<CellAttributes> Cone = new List<CellAttributes>();
        //if count = 1 (solve the broblem of only one angle by making it into 2 angles based on distance to watcher
        inCluster.RelevantAngles.Sort((x, y) => x.angle_float.CompareTo(y.angle_float));
        largest = inCluster.RelevantAngles[count - 1];
        smallest = inCluster.RelevantAngles[0];
        //Debug.Log("Largest: " + largest.angle_float + " , " + "Smallest: " + smallest.angle_float);

        bool specialcase = false;
        
        for (int i = 1; i < count; i++)
        {
            if (Mathf.Abs(inCluster.RelevantAngles[i].angle_float - inCluster.RelevantAngles[i - 1].angle_float) > 60)
            {
                specialcase = true;
            }
        }

        //Debug.Log(specialcase);

        #region normal case
        if (!specialcase)
        {
            foreach (CellAttributes c in watching)
            {
                float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                if ((largest.angle_float >= Angle) && (Angle >= smallest.angle_float))
                {
                    Cone.Add(c);
                }
            }

            for (int i = 1; i < count; i++)
            {
                Vector3f watcherCubeCoordinate = inCluster.watcher.CellAttributes.Cell.CubeCoordinate;
                Vector3f currentCubeCoordinate = inCluster.RelevantAngles[i - 1].cell.CellAttributes.Cell.CubeCoordinate;
                Vector3f nextCubeCoordinate = inCluster.RelevantAngles[i].cell.CellAttributes.Cell.CubeCoordinate;

                foreach (CellAttributes c in Cone)
                {
                    float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                    Vector3f coneCellCubeCoordinate = c.Cell.CubeCoordinate;

                    if (inCluster.RelevantAngles[i].angle_float >= Angle && Angle >= inCluster.RelevantAngles[i - 1].angle_float)
                    {
                        if (GridSys.GetDistance(watcherCubeCoordinate, currentCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                        {
                            if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, currentCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                        else
                        {
                            if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                    }
                }
            }
            return watching;
        }
        #endregion
        
        #region SpecialCase
        else
        {
            List<Angle> Positives = new List<Angle>();
            List<Angle> Negatives = new List<Angle>();
            for (int i = count - 1; i >= 0; i--)
            {
                if (inCluster.RelevantAngles[i].angle_float >= 0)
                {
                    Positives.Add(inCluster.RelevantAngles[i]);
                }
                else
                {
                    Negatives.Add(inCluster.RelevantAngles[i]);
                }
            }
            //Debug.Log(Positives[0].angle_float + " ; " + Positives[Positives.Count - 1].angle_float);
            foreach (CellAttributes c in watching)
            {
                float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                if ((Positives[Positives.Count - 1].angle_float <= Angle && Angle >= 0) || (Angle <= Negatives[0].angle_float && Angle <= 0))
                {
                    Cone.Add(c);
                }
            }
            //Debug.Log("clustercount" + Cone.Count);

            Vector3f watcherCubeCoordinate = inCluster.watcher.CellAttributes.Cell.CubeCoordinate;
            Vector3f largestCubeCoordinate = largest.cell.CellAttributes.Cell.CubeCoordinate;
            Vector3f smallestCubeCoordinate = smallest.cell.CellAttributes.Cell.CubeCoordinate;

            foreach (CellAttributes c in Cone)
            {
                float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                Vector3f coneCellCubeCoordinate = c.Cell.CubeCoordinate;

                if (largest.angle_float <= Angle || Angle <= smallest.angle_float)
                {
                    if (GridSys.GetDistance(watcherCubeCoordinate, largestCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, smallestCubeCoordinate))
                    {
                        if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, largestCubeCoordinate))
                        {
                            if (watching.Contains(c)) watching.Remove(c);
                        }
                    }
                    else
                    {
                        if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, smallestCubeCoordinate))
                        {
                            if (watching.Contains(c)) watching.Remove(c);
                        }
                    }
                }
            }
            for (int i = 0; i < Positives.Count - 1; i++)
            {
                Vector3f currentCubeCoordinate = inCluster.RelevantAngles[i].cell.CellAttributes.Cell.CubeCoordinate;
                Vector3f nextCubeCoordinate = inCluster.RelevantAngles[i + 1].cell.CellAttributes.Cell.CubeCoordinate;

                foreach (CellAttributes c in Cone)
                {
                    float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                    Vector3f coneCellCubeCoordinate = c.Cell.CubeCoordinate;

                    if (inCluster.RelevantAngles[i].angle_float >= Angle && Angle >= inCluster.RelevantAngles[i + 1].angle_float)
                    {
                        if (GridSys.GetDistance(watcherCubeCoordinate, currentCubeCoordinate) <= GridSys.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                        {
                            if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, currentCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                        else
                        {
                            if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                    }
                }
            }
            for (int i = Negatives.Count; i > 0; i--)
            {
                Vector3f currentCubeCoordinate = inCluster.RelevantAngles[i].cell.CellAttributes.Cell.CubeCoordinate;
                Vector3f nextCubeCoordinate = inCluster.RelevantAngles[i - 1].cell.CellAttributes.Cell.CubeCoordinate;

                foreach (CellAttributes c in Cone)
                {
                    float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                    Vector3f coneCellCubeCoordinate = c.Cell.CubeCoordinate;

                    if (inCluster.RelevantAngles[i].angle_float >= Angle && Angle >= inCluster.RelevantAngles[i - 1].angle_float)
                    {
                        if (GridSys.GetDistance(watcherCubeCoordinate, currentCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                        {
                            if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, currentCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                        else
                        {
                            if (GridSys.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= GridSys.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                    }
                }
            }
            return watching;
        }
        #endregion
        
    }*/

}


#region ClusterDef
public struct ObstructVisionCluster
{
    public HashSet<Vector3f> cluster;
    public List<Angle> RelevantAngles;
    public Vector3f watcherCoor;
    public ObstructVisionCluster(HashSet<Vector3f> inCluster, Vector3f inWatcher)

    {
        watcherCoor = inWatcher;
        cluster = inCluster;
        RelevantAngles = new List<Angle>();
    }

    public ObstructVisionCluster(Vector3f start, Vector3f inWatcher)
    {
        watcherCoor = inWatcher;
        cluster = new HashSet<Vector3f>();
        cluster.Add(start);
        RelevantAngles = new List<Angle>();
    }
}
public struct RawCluster
{
    public List<CellAttributesComponent.Component> cluster;
    
    public RawCluster(List<CellAttributesComponent.Component> inCluster)
    {
        cluster = inCluster;
    }
    public RawCluster(CellAttributesComponent.Component inStart)
    {
        cluster = new List<CellAttributesComponent.Component>();
        cluster.Add(inStart);
    }
}
public struct Angle
{
    public CellAttributesComponent.Component cell;
    public float angle_float;
    public Angle(CellAttributesComponent.Component inCell, float inAngle)
    {
        cell = inCell;
        angle_float = inAngle;
    }
}
#endregion
