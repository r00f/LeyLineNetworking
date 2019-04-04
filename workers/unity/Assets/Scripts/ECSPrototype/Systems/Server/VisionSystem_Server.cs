using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;
using System;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class VisionSystem_Server : ComponentSystem
    {
    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CellCoordinateData;
        public ComponentDataArray<Cells.CellAttributesComponent.Component> CellsData;
    }

    [Inject]
    private CellData m_CellData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Player.PlayerAttributes.Component> PlayerAttributes;
        public ComponentDataArray<Generic.FactionComponent.Component> FactionComponent;
        public ComponentDataArray<Generic.Vision.Component> VisionComponent;
    }

    [Inject]
    private PlayerData m_PlayerData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> UnitCoordinateData;
        public ComponentDataArray<Generic.Vision.Component> VisionComponent;
        public ComponentDataArray<Generic.FactionComponent.Component> FactionComponent;
        public readonly ComponentDataArray<Unit.CurrentPath.Component> Path;
    }
    [Inject]
    private UnitData m_UnitData;

    [Inject]
    private HandleCellGridRequestsSystem GridSys;

    bool Init = true;
    bool firstTime = true;
    private List<RawCluster> FixClusters = new List<RawCluster>();

    protected override void OnStartRunning()
        {
            base.OnStartRunning();
         //BuildRawClusters();
        }
    /*protected override void OnCreateManager()
    {
        base.OnCreateManager();
        BuildRawClusters();
    }*/
    protected override void OnUpdate()
        {
        if (firstTime) firstTime = false;
        else {
            if (Init) BuildRawClusters();
            //Debug.Log("CellCount2: " + m_CellData.Length);
            for (int i = m_UnitData.Length - 1; i >= 0; i--)
            {
                var u_Vision = m_UnitData.VisionComponent[i];
                var u_OccupiedCell = m_UnitData.UnitCoordinateData[i];
                var u_Faction = m_UnitData.FactionComponent[i];

                if (u_Vision.RequireUpdate == true)
                {
                    u_Vision = UpdateUnitVision(u_OccupiedCell, u_Vision, u_Faction);
                    Debug.Log("uVisionbool: " + u_Vision.RequireUpdate);
                    u_Vision.CellsInVisionrange = u_Vision.CellsInVisionrange;
                    u_Vision.RequireUpdate = u_Vision.RequireUpdate;
                    m_UnitData.VisionComponent[i] = u_Vision;
                    //Debug.Log("appliedCount: " + m_UnitData.VisionComponent[i].CellsInVisionrange.Count);
                }
            }
            for (int i = m_PlayerData.Length - 1; i >= 0; i--)
            {
                var p_Vision = m_PlayerData.VisionComponent[i];
                var p_Faction = m_PlayerData.FactionComponent[i];

                if (p_Vision.RequireUpdate == true)
                {
                    p_Vision =  UpdatePlayerVision(p_Vision, p_Faction);
                    p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;
                    p_Vision.RequireUpdate = p_Vision.RequireUpdate;
                    m_PlayerData.VisionComponent[i] = p_Vision;
                }
            }
        }
    }
    private Generic.Vision.Component UpdateUnitVision(Generic.CubeCoordinate.Component coor, Generic.Vision.Component inVision, Generic.FactionComponent.Component inFaction)
    {
        List<Cells.CellAttributes> sight = new List<Cells.CellAttributes>();
        
        foreach (Cells.CellAttributes c in GridSys.GetRadius(coor.CubeCoordinate, inVision.VisionRange))
        {
            sight.Add(c);
        }
        //Debug.Log("Sightlength before subtraction:" + sight.Count);

        List<Cells.CellAttributes> Obstructive = new List<Cells.CellAttributes>();
        List<List<Cells.CellAttributesComponent.Component>> RelevantClusters = new List<List<Cells.CellAttributesComponent.Component>>();
        foreach (Cells.CellAttributes c in sight)
        {
            
            if (c.Cell.ObstructVision)
            {
                Obstructive.Add(c);
            }
            
        }


        RelevantClusters.Clear();
        //RelevantClusters = FixClusters;

        while (Obstructive.Count > 0)
        {
            int i = Obstructive.Count - 1;
            if (i >= 0)
            {
                List<Cells.CellAttributesComponent.Component> Cluster = new List<Cells.CellAttributesComponent.Component>();
                foreach (RawCluster lc in FixClusters)
                {
                    //Debug.Log("clustercount" + lc.cluster.Count);
                    /*if (lc.Contains(Obstructive[i]))
                    {
                        Cluster = lc;
                    }*/
                    //foreach (Cells.CellAttributesComponent.Component c in lc.cluster)
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
                        List<Cells.CellAttributes> toRemove = new List<Cells.CellAttributes>();
                        foreach (Cells.CellAttributesComponent.Component c in Cluster)
                        {
                            foreach (Cells.CellAttributes ca in sight)
                            {
                                if (ca.Cell.CubeCoordinate == c.CellAttributes.Cell.CubeCoordinate) toRemove.Add(ca);
                            }
                        }
                        foreach (Cells.CellAttributes c in toRemove)
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
        Cells.CellAttributesComponent.Component Watcher = new Cells.CellAttributesComponent.Component();
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

    private Generic.Vision.Component UpdatePlayerVision(Generic.Vision.Component inVision, Generic.FactionComponent.Component inFaction)
    {

            inVision.CellsInVisionrange.Clear();

            for(int e = m_UnitData.Length-1; e>=0; e--)
            {
                var UnitVision = m_UnitData.VisionComponent[e];
                var UnitFaction = m_UnitData.FactionComponent[e];

                if(inFaction.Faction == UnitFaction.Faction)
                {
                    foreach(Cells.CellAttributes c in UnitVision.CellsInVisionrange)
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
        
        List<Cells.CellAttributesComponent.Component> obstructed = new List<Cells.CellAttributesComponent.Component>();

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
            Cells.CellAttributesComponent.Component c = obstructed[0];
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

    private void BuildCluster(Cells.CellAttributesComponent.Component cell, RawCluster cluster, List<Cells.CellAttributesComponent.Component> obstructed, out List<Cells.CellAttributesComponent.Component> newObstructed)
    {
        List<Cells.CellAttribute> neighbours = cell.CellAttributes.Neighbours;
        for (int i = neighbours.Count - 1; i >= 0; i--)
        {
            bool contains = false;
            {
                foreach (Cells.CellAttributesComponent.Component c in obstructed)
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

            foreach (Cells.CellAttributesComponent.Component c in cluster.cluster)
            {
                if (c.CellAttributes.Cell.CubeCoordinate == neighbours[i].CubeCoordinate) contains = true;
            }

            if (!contains)
            {
                bool isSet = false;
                Cells.CellAttributesComponent.Component toRemove = new Cells.CellAttributesComponent.Component();
                foreach (Cells.CellAttributesComponent.Component c in obstructed)
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
        foreach (Cells.CellAttributesComponent.Component c in inCluster.cluster)
        {

            Angle angle = new Angle(c, GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.CellAttributes.Cell.Position));

            if (!inCluster.RelevantAngles.Contains(angle))
            {
                inCluster.RelevantAngles.Add(angle);
            }
            
        }
        //print("Relevant angles Count: " + inCluster.RelevantAngles.Count());

    }
    private List<Cells.CellAttributes> Cluster_UseAngles(ObstructVisionCluster inCluster, List<Cells.CellAttributes> watching)
    {
        int count = inCluster.RelevantAngles.Count;
        Angle largest;
        Angle smallest;
        List<Cells.CellAttributes> Cone = new List<Cells.CellAttributes>();
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
            foreach (Cells.CellAttributes c in watching)
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

                    foreach (Cells.CellAttributes c in Cone)
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
            Debug.Log(Positives[0].angle_float + " ; " + Positives[Positives.Count - 1].angle_float);
            foreach (Cells.CellAttributes c in watching)
            {
                float Angle = GridSys.GetAngle(inCluster.watcher.CellAttributes.Cell.Position, c.Cell.Position);
                if ((Positives[Positives.Count - 1].angle_float <= Angle && Angle >= 0) || (Angle <= Negatives[0].angle_float && Angle <= 0))
                {
                    Cone.Add(c);
                }
            }
            Debug.Log("clustercount" + Cone.Count);

            Vector3f watcherCubeCoordinate = inCluster.watcher.CellAttributes.Cell.CubeCoordinate;
            Vector3f largestCubeCoordinate = largest.cell.CellAttributes.Cell.CubeCoordinate;
            Vector3f smallestCubeCoordinate = smallest.cell.CellAttributes.Cell.CubeCoordinate;

            foreach (Cells.CellAttributes c in Cone)
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

                foreach (Cells.CellAttributes c in Cone)
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

                foreach (Cells.CellAttributes c in Cone)
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
    public List<Cells.CellAttributesComponent.Component> cluster;
    public List<Angle> RelevantAngles;
    public Cells.CellAttributesComponent.Component watcher;
    public ObstructVisionCluster(List<Cells.CellAttributesComponent.Component> inCluster, Cells.CellAttributesComponent.Component inWatcher)

    {
        watcher = inWatcher;
        cluster = inCluster;
        RelevantAngles = new List<Angle>();
    }

    public ObstructVisionCluster(Cells.CellAttributesComponent.Component start, Cells.CellAttributesComponent.Component inWatcher)
    {
        watcher = inWatcher;
        cluster = new List<Cells.CellAttributesComponent.Component>();
        cluster.Add(start);
        RelevantAngles = new List<Angle>();
    }
}
public struct RawCluster
{
    public List<Cells.CellAttributesComponent.Component> cluster;
    public RawCluster(List<Cells.CellAttributesComponent.Component> inCluster)
    {
        cluster = inCluster;
    }
    public RawCluster(Cells.CellAttributesComponent.Component inStart)
    {
        cluster = new List<Cells.CellAttributesComponent.Component>();
        cluster.Add(inStart);
    }
}
public struct Angle
{
    public Cells.CellAttributesComponent.Component cell;
    public float angle_float;
    public Angle(Cells.CellAttributesComponent.Component inCell, float inAngle)
    {
        cell = inCell;
        angle_float = inAngle;
    }
}
#endregion
