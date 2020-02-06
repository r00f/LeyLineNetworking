﻿using UnityEngine;
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
using Unity.Collections;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class VisionSystem_Server : ComponentSystem
{
    //HandleCellGridRequestsSystem m_GridSystem;
    ILogDispatcher logger;

    EntityQuery m_UnitData;
    EntityQuery m_PlayerData;
    EntityQuery m_CellData;

    bool Init = false;
    int mapSize = 631;
    private List<RawCluster> FixClusters = new List<RawCluster>();

    protected override void OnCreate()
    {
        base.OnCreate();
        //m_ClientVisionSystem = World.GetExistingSystem<VisionSystem_Client>();
        //m_GridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();


        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Vision.Component>()
            );

        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<PlayerAttributes.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<Vision.Component>()
            );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>()
            );

    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;

    }

    protected override void OnUpdate()
    {
        
        if (m_CellData.CalculateEntityCount() == mapSize)
        {
            //if any unit requires an update, update player aswell
            if (!Init)
            {
                BuildRawClusters();
            }
            else {

                Entities.With(m_UnitData).ForEach((ref SpatialEntityId id, ref WorldIndex.Component u_worldIndex, ref Vision.Component u_Vision, ref CubeCoordinate.Component u_OccupiedCell, ref FactionComponent.Component u_Faction) =>
                {
                    /*
                    if (u_Vision.VisionRange > 0)
                    {
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("initialize UnitVision")
                        .WithField("unitId", id.EntityId.Id));
                        u_Vision.RequireUpdate = true;
                    }
                    */

                    if(u_Vision.InitialWaitTime > 0)
                    {
                        u_Vision.InitialWaitTime -= Time.deltaTime;
                        if (u_Vision.InitialWaitTime <= 0)
                            u_Vision.RequireUpdate = true;
                    }

                    var unitFaction = u_Faction.Faction;


                    if (u_Vision.RequireUpdate == true)
                    {
                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("u_Vision.ReqUpdate = true")
                        .WithField("unitId", id.EntityId.Id));
                        */
                        var v = UpdateUnitVision(u_OccupiedCell, u_Vision, u_Faction, u_worldIndex.Value);

                        if (v.CellsInVisionrange.Count != 0)
                        {
                            Entities.With(m_PlayerData).ForEach((ref Vision.Component p_Vision, ref FactionComponent.Component p_Faction) =>
                            {
                                if (p_Faction.Faction == unitFaction)
                                {
                                    /*
                                    logger.HandleLog(LogType.Warning,
                                    new LogEvent("UnitLoopSetPlayerVisionReq")
                                    .WithField("UnitFaction", unitFaction));
                                    */
                                    p_Vision.RequireUpdate = true;
                                }
                            });

                            u_Vision = v;
                            u_Vision.RequireUpdate = false;
                        }
                    }
                });

                Entities.With(m_PlayerData).ForEach((ref Vision.Component p_Vision, ref FactionComponent.Component p_Faction) =>
                {
                    if (p_Vision.RequireUpdate)
                    {
                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("playerVision.ReqUpdate = true")
                        .WithField("playerVision.ReqUpdate", p_Vision.RequireUpdate));
                        */
                        

                        p_Vision = UpdatePlayerVision(p_Vision, p_Faction.Faction);
                        
                        p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;
                        p_Vision.Positives = p_Vision.Positives;
                        p_Vision.Negatives = p_Vision.Negatives;
                        p_Vision.Lastvisible = p_Vision.Lastvisible;

                        p_Vision.RequireUpdate = false;

                        //Debug.Log("UpdatePlayerVision");

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
                });
            }
        }
    }

    private Vision.Component UpdateUnitVision(CubeCoordinate.Component coor, Vision.Component inVision, FactionComponent.Component inFaction, uint inWorldIndex)
    {
        List<Vector3f> sight = CellGridMethods.CircleDraw(coor.CubeCoordinate, inVision.VisionRange);
        
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
            List<Vector3f> Ring = CellGridMethods.RingDraw(coor.CubeCoordinate, inVision.VisionRange);
            List<List<Vector3f>> Lines = new List<List<Vector3f>>();

            foreach (Vector3f c in Ring)
            {
                Lines.Add(CellGridMethods.LineDraw(coor.CubeCoordinate, c));
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
                                //first tree is visible if not removed from hash
                                //sightHash.Remove(l[i]);
                                //Debug.Log("contains removed Coord: " + l[i].X + "," + l[i].Y + "," + l[i].Z);
                            }
                        }
                    }
                    else
                    {
                        sightHash.Remove(l[i]);
                        //Debug.Log("else removed Coord: " + l[i].X + "," + l[i].Y + "," + l[i].Z);
                    }
                }
            }
            sight = new List<Vector3f>(sightHash);
        }

        inVision.CellsInVisionrange = sight;

        return inVision;
    }

    private Vision.Component UpdatePlayerVision(Vision.Component inVision, uint faction)
    {
        //Debug.Log("UpdatePlayerVision: " + faction);
        /*
        logger.HandleLog(LogType.Warning,
        new LogEvent("UpdatePlayerVision.")
        .WithField("Faction", faction));
        */
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

        Entities.With(m_UnitData).ForEach((ref Vision.Component unitVision, ref FactionComponent.Component unitFaction) =>
        {
            if (faction == unitFaction.Faction)
            {
                //use a hashSet to increase performance of contains calls (O(1) vs. O(n))
                foreach (Vector3f v in unitVision.CellsInVisionrange)
                {
                    currentVision.Add(v);
                }
            }
        });

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

        //Debug.Log("cell entity count" + m_CellData.CalculateEntityCount());
        Entities.With(m_CellData).ForEach((ref CellAttributesComponent.Component cellAttributes) =>
        {
            if (cellAttributes.CellAttributes.Cell.ObstructVision)
            {
                obstructed.Add(cellAttributes);
            }
        });
        //Debug.Log("obstructed:" + obstructed.Count);
        List<RawCluster> raw = new List<RawCluster>();
        while (obstructed.Count > 0)
        {
            CellAttributesComponent.Component c = obstructed[0];
            RawCluster go = new RawCluster(c);
            obstructed.Remove(c);
            BuildCluster(c, go, obstructed, out obstructed);
            raw.Add(go);
            //Debug.Log("Cluster:" + go.cluster.Count);
        }

        for (int i = raw.Count - 1; i >= 0; i--)
        {
            if (raw[i].cluster.Count > 0)
            {
                FixClusters.Add(raw[i]);
            }
        }
        //Debug.Log("NumberofClusters" + FixClusters.Count());

        Init = true;
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
                    if (Vector3fext.ToUnityVector(c.CellAttributes.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate))
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
                if (Vector3fext.ToUnityVector(c.CellAttributes.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate)) contains = true;
            }

            if (!contains)
            {
                bool isSet = false;
                CellAttributesComponent.Component toRemove = new CellAttributesComponent.Component();
                foreach (CellAttributesComponent.Component c in obstructed)
                {
                    if (Vector3fext.ToUnityVector(c.CellAttributes.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate))
                    {
                        toRemove = c;
                        isSet = true;
                    }
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
