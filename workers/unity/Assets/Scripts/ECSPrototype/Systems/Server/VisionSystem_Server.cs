using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;
using System;
using Generic;
using Cells;
using Player;
using Unit;

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
        public readonly ComponentDataArray<PlayerAttributes.Component> PlayerAttributes;
        public readonly ComponentDataArray<FactionComponent.Component> FactionComponent;
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
        public readonly ComponentDataArray<ServerPath.Component> ServerPath;
        public ComponentDataArray<Vision.Component> VisionComponent;
    }
    [Inject]
    private UnitData m_UnitData;

    [Inject]
    private HandleCellGridRequestsSystem GridSys;

    bool Init = true;
    bool firstTime = true;
    private List<RawCluster> FixClusters = new List<RawCluster>();

    protected override void OnUpdate()
    {
        if (firstTime) firstTime = false;
        else {
            if (Init) BuildRawClusters();

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
                }

            }

            for (int i = m_PlayerData.Length - 1; i >= 0; i--)
            {
                var p_Vision = m_PlayerData.VisionComponent[i];
                var p_Faction = m_PlayerData.FactionComponent[i];

                if (p_Vision.RequireUpdate == true)
                {
                    p_Vision = UpdatePlayerVision(p_Vision, p_Faction);
                    p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;
                    p_Vision.RequireUpdate = p_Vision.RequireUpdate;
                    m_PlayerData.VisionComponent[i] = p_Vision;
                }
            }
        }
    }

    private Vision.Component UpdateUnitVision(CubeCoordinate.Component coor, Vision.Component inVision, FactionComponent.Component inFaction, uint inWorldIndex)
    {
        List<CellAttributes> sight = GridSys.GetRadius(coor.CubeCoordinate, inVision.VisionRange, inWorldIndex);
        List<CellAttributes> Obstructive = new List<CellAttributes>();
        List<List<CellAttributesComponent.Component>> RelevantClusters = new List<List<CellAttributesComponent.Component>>();

        foreach (CellAttributes c in sight)
        {
            
            if (c.Cell.ObstructVision)
            {
                Obstructive.Add(c);
            }
            
        }


        RelevantClusters.Clear();

        while (Obstructive.Count > 0)
        {
            int i = Obstructive.Count - 1;
            if (i >= 0)
            {
                List<CellAttributesComponent.Component> Cluster = new List<CellAttributesComponent.Component>();
                foreach (RawCluster lc in FixClusters)
                {
                    for (int e = lc.cluster.Count - 1; e >= 0; e--)
                    {
                        if (lc.cluster[e].CellAttributes.Cell.CubeCoordinate == Obstructive[i].Cell.CubeCoordinate)
                        {
                            Cluster = lc.cluster;
                        }
                    }
                }
                if (Cluster.Count > 0)
                 {
                        List<CellAttributes> toRemove = new List<CellAttributes>();
                        foreach (CellAttributesComponent.Component c in Cluster)
                        {
                            foreach (CellAttributes ca in sight)
                            {
                                if (ca.Cell.CubeCoordinate == c.CellAttributes.Cell.CubeCoordinate) toRemove.Add(ca);
                            }
                        }
                        foreach (CellAttributes c in toRemove)
                        {
                            sight.Remove(c);
                            Obstructive.Remove(c);
                        }
                        RelevantClusters.Add(Cluster);
                    }
                
            }

        }
        //Debug.Log(RelevantClusters.Count);

        //Fetch watcher out of all the cells
        CellAttributesComponent.Component Watcher = new CellAttributesComponent.Component();
        bool isSet = false;
        for(int i = m_CellData.Length-1; i>=0; i--)
        {
            if(m_CellData.CellCoordinateData[i].CubeCoordinate == coor.CubeCoordinate)
            {
                Watcher = m_CellData.CellsData[i];
                isSet = true;
            }
        }
        if (isSet)
        {
            for (int i = RelevantClusters.Count - 1; i >= 0; i--)
            {

                ObstructVisionCluster OVC = new ObstructVisionCluster(RelevantClusters[i], Watcher);
                Cluster_DetermineAngles(OVC);
                sight = Cluster_UseAngles(OVC, sight);
            }
        }
        //Debug.Log("Sightlength after subtraction:" + sight.Count);
        
        inVision.CellsInVisionrange = sight;
        
        inVision.RequireUpdate = false;
    
        
        for (int i = m_PlayerData.Length - 1; i >= 0; i--)
        {
            var Factiondata = m_PlayerData.FactionComponent[i];
            var Visiondata = m_PlayerData.VisionComponent[i];
            if (Factiondata.Faction == inFaction.Faction)
            {
                Visiondata.RequireUpdate = true;
                Visiondata.RequireUpdate = Visiondata.RequireUpdate;
                m_PlayerData.VisionComponent[i] = Visiondata;
            }
        }
        
        return inVision;
    }

    private Vision.Component UpdatePlayerVision(Vision.Component inVision, FactionComponent.Component inFaction)
    {

            inVision.CellsInVisionrange.Clear();

            for(int e = m_UnitData.Length-1; e>=0; e--)
            {
                var UnitVision = m_UnitData.VisionComponent[e];
                var UnitFaction = m_UnitData.FactionComponent[e];

                if(inFaction.Faction == UnitFaction.Faction)
                {
                    foreach(CellAttributes c in UnitVision.CellsInVisionrange)
                    {
                        if (!inVision.CellsInVisionrange.Contains(c)) inVision.CellsInVisionrange.Add(c);
                    }
                }

            }
        inVision.RequireUpdate = false;
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
    {
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

    private List<CellAttributes> Cluster_UseAngles(ObstructVisionCluster inCluster, List<CellAttributes> watching)
    {
        int count = inCluster.RelevantAngles.Count;
        Angle largest;
        Angle smallest;
        List<CellAttributes> Cone = new List<CellAttributes>();
        //if count = 1 (solve the broblem of only one angle by making it into 2 angles based on distance to watcher
        inCluster.RelevantAngles.Sort((x, y) => x.angle_float.CompareTo(y.angle_float));
        largest = inCluster.RelevantAngles[count - 1];
        smallest = inCluster.RelevantAngles[0];
        //Debug.Log("Largest: " + largest.angle_float + " , " + "Smallest: " + smallest.angle_float);



        #region logic for checking if something is behind an obstructed cell
        bool specialcase = false;

        for (int i = count - 1; i >= 0; i--)
        {
            if (i == 0)
                continue;
            else
            {
                if (Mathf.Abs(inCluster.RelevantAngles[i].angle_float - inCluster.RelevantAngles[i - 1].angle_float) > 60)
                {
                    specialcase = true;
                }
            }
        }

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

            for (int i = count - 1; i >= 0; i--)
            {
                if (i == 0)
                    continue;
                else
                {
                    Vector3f watcherCubeCoordinate = inCluster.watcher.CellAttributes.Cell.CubeCoordinate;
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
            }
            #endregion
            return watching;
        }
        #region SpecialCase
        else
        {
            Debug.Log("SPECIAL CASE!! DOES IT WORK?");
            List<Angle> Positives = new List<Angle>();
            List<Angle> Negatives = new List<Angle>();
            for (int i = count - 1; i >= 0; i--)
            {
                if (inCluster.RelevantAngles[i].angle_float >= 0)
                {
                    Positives.Add(inCluster.RelevantAngles[i]);
                }
                else {
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
    }

}



#region ClusterDef
public struct ObstructVisionCluster
{
    public List<CellAttributesComponent.Component> cluster;
    public List<Angle> RelevantAngles;
    public CellAttributesComponent.Component watcher;
    public ObstructVisionCluster(List<CellAttributesComponent.Component> inCluster, CellAttributesComponent.Component inWatcher)

    {
        watcher = inWatcher;
        cluster = inCluster;
        RelevantAngles = new List<Angle>();
    }

    public ObstructVisionCluster(CellAttributesComponent.Component start, CellAttributesComponent.Component inWatcher)
    {
        watcher = inWatcher;
        cluster = new List<CellAttributesComponent.Component>();
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
