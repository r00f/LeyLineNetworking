using Unity.Entities;
using Improbable.Gdk.Core;
using System.Collections.Generic;
using Generic;
using Cell;
using Player;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class VisionSystem_Server : JobComponentSystem
{
    //HandleCellGridRequestsSystem m_GridSystem;
    ILogDispatcher logger;
    CommandSystem m_CommandSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    //EntityQuery m_UnitData;
    //EntityQuery m_PlayerData;
    EntityQuery m_CellData;
    EntityQuery m_GameStateData;
    EntityQuery m_EntityWithoutWorldIndexSharedData;

    bool init = false;
    int mapSize = 631;
    private List<RawCluster> FixClusters = new List<RawCluster>();

    protected override void OnCreate()
    {
        base.OnCreate();

        /*
        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<Vision.Component>()
            );

        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<PlayerAttributes.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<Vision.Component>()
            );
         */
        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>()
            );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<WorldIndexShared>()
            );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var revealVisionRequests = m_CommandSystem.GetRequests<Vision.RevealVisionCommand.ReceivedRequest>();

        for (int i = 0; i < revealVisionRequests.Count; i++)
        {
            var revealVisionRequest = revealVisionRequests[i];

            Entities.WithAll<WorldIndexShared>().ForEach((ref Vision.Component p_Vision, in SpatialEntityId p_id) =>
            {
                if(p_id.EntityId == revealVisionRequest.EntityId)
                {
                    p_Vision.RevealVision = !p_Vision.RevealVision;
                    p_Vision.RequireUpdate = true;
                }
            })
            .WithoutBurst()
            .Run();
        }

        mapSize = m_GameStateData.CalculateEntityCount() * 631;

        //Debug.Log("VisionSystemServer init bool = " + init + ", mapsize = " + mapSize + ", CellDataCount = " + m_CellData.CalculateEntityCount());

        //if any unit requires an update, update player aswell
        if (!init)
        {
            /*
            logger.HandleLog(LogType.Warning,
            new LogEvent("MapNotInitialized")
            .WithField("CellDataCount", m_CellData.CalculateEntityCount()));
            */

            if (m_CellData.CalculateEntityCount() == mapSize)
            {
                //Debug.Log("m_CellData  = Mapsize - init Clusters");
                BuildRawClusters();
            }
        }
        else
        {
            Entities.ForEach((ref Vision.Component u_Vision, in CubeCoordinate.Component u_OccupiedCell, in FactionComponent.Component u_Faction, in SpatialEntityId id, in WorldIndexShared worldIndex) =>
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

                if (u_Vision.InitialWaitTime > 0)
                {
                    u_Vision.InitialWaitTime -= Time.DeltaTime;
                    if (u_Vision.InitialWaitTime <= 0)
                        u_Vision.RequireUpdate = true;
                }

                var unitFaction = u_Faction.Faction;
                var coord = u_OccupiedCell.CubeCoordinate;

                if (u_Vision.RequireUpdate == true)
                {
                    /*
                    logger.HandleLog(LogType.Warning,
                    new LogEvent("u_Vision.ReqUpdate = true")
                    .WithField("unitId", id.EntityId.Id));
                    */
                    var v = UpdateUnitVision(u_OccupiedCell, u_Vision, u_Faction);

                    if (v.CellsInVisionrange.Count != 0)
                    {
                        Entities.WithSharedComponentFilter(worldIndex).WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component p_Vision, in FactionComponent.Component p_Faction, in SpatialEntityId p_id) =>
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
                            else if (p_Vision.CellsInVisionrange.ContainsKey(coord))
                            {
                                m_ComponentUpdateSystem.SendEvent(
                                new Vision.UpdateClientVisionEvent.Event(),
                                p_id.EntityId);
                            }
                        })
                        .WithoutBurst()
                        .Run();

                        u_Vision = v;
                        u_Vision.RequireUpdate = false;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            Entities.WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component p_Vision, in FactionComponent.Component p_Faction, in SpatialEntityId p_id, in WorldIndex.Component p_windex) =>
            {
                logger.HandleLog(LogType.Warning,
                new LogEvent("playerVision.ReqUpdate")
                .WithField("playerVision.ReqUpdate", p_Vision.RequireUpdate)
                .WithField("PlayerId", p_id.EntityId.Id));

                if (p_Vision.RequireUpdate)
                {
                    /*
                    logger.HandleLog(LogType.Warning,
                    new LogEvent("playerVision.ReqUpdate")
                    .WithField("playerVision.ReqUpdate", p_Vision.RequireUpdate));
                    */

                    if (!p_Vision.RevealVision)
                    {
                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("playerVision.RevealVision")
                        .WithField("playerVision.RevealVision", p_Vision.RevealVision));
                        */

                        p_Vision = UpdatePlayerVision(p_Vision, p_Faction.Faction, p_windex.Value);

                        p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;
                        p_Vision.Positives = p_Vision.Positives;
                        p_Vision.Negatives = p_Vision.Negatives;
                        p_Vision.Lastvisible = p_Vision.Lastvisible;

                        m_ComponentUpdateSystem.SendEvent(
                        new Vision.UpdateClientVisionEvent.Event(),
                        p_id.EntityId);

                        p_Vision.RequireUpdate = false;
                    }
                    else
                    {
                        p_Vision = RevealMap(p_windex.Value, p_Vision);

                        p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;
                        p_Vision.Positives = p_Vision.Positives;
                        p_Vision.Negatives = p_Vision.Negatives;
                        p_Vision.Lastvisible = p_Vision.Lastvisible;

                        m_ComponentUpdateSystem.SendEvent(
                        new Vision.UpdateClientVisionEvent.Event(),
                        p_id.EntityId);

                        p_Vision.RequireUpdate = false;
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        return inputDeps;
    }

    private Vision.Component UpdateUnitVision(CubeCoordinate.Component coor, Vision.Component inVision, FactionComponent.Component inFaction)
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
                Lines.Add(CellGridMethods.LineDraw(new List<Vector3f>(), coor.CubeCoordinate, c));
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
            //sight = new List<Vector3f>(sightHash);
        }

        //inVision.CellsInVisionrange.Clear();
        inVision.CellsInVisionrange = sightHash.ToDictionary(x => x, x => (uint)x.X);

        return inVision;
    }

    private Vision.Component RevealMap(uint worldIndex, Vision.Component inVision)
    {
        inVision.CellsInVisionrange.Clear();

        //Add all coordinates to Vision TODO: Store all mapCoords in a dict on Gamestate
        Entities.WithSharedComponentFilter(new WorldIndexShared { Value = worldIndex }).ForEach((in CubeCoordinate.Component coord, in CellAttributesComponent.Component cellAtt) =>
        {
            inVision.CellsInVisionrange.Add(coord.CubeCoordinate, 0);
        })
        .WithoutBurst()
        .Run();

        return inVision;
    }

    private Vision.Component UpdatePlayerVision(Vision.Component inVision, uint faction, uint worldIndex)
    {
        //Debug.Log("UpdatePlayerVision: " + faction);
        /*
        logger.HandleLog(LogType.Warning,
        new LogEvent("UpdatePlayerVision.")
        .WithField("Faction", faction));
        */

        inVision.Lastvisible.Clear();
        inVision.Lastvisible.AddRange(inVision.CellsInVisionrange.Keys);

        inVision.Positives.Clear();
        inVision.Negatives.Clear();
        inVision.CellsInVisionrange.Clear();

        var lastVision = new HashSet<Vector3f>();

        foreach (Vector3f v in inVision.Lastvisible)
        {
            lastVision.Add(v);
        }

        var currentVision = new HashSet<Vector3f>();

        Entities.WithSharedComponentFilter(new WorldIndexShared {  Value = worldIndex }).WithAll<CubeCoordinate.Component>().ForEach((in Vision.Component unitVision, in FactionComponent.Component unitFaction) =>
        {
            if (faction == unitFaction.Faction)
            {
                //use a hashSet to increase performance of contains calls (O(1) vs. O(n))
                foreach (Vector3f v in unitVision.CellsInVisionrange.Keys)
                {
                    currentVision.Add(v);
                }
            }
        })
        .WithoutBurst()
        .Run();

        inVision.CellsInVisionrange = currentVision.ToDictionary(item => item, item => (uint)0);

        foreach (Vector3f v in lastVision)
        {
            if (!currentVision.Contains(v))
            {
                inVision.Negatives.Add(v);
            }
        }

        foreach (Vector3f v in currentVision)
        {
            if (!lastVision.Contains(v))
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
        Entities.ForEach((in CellAttributesComponent.Component cellAttributes) =>
        {
            if (cellAttributes.CellAttributes.Cell.ObstructVision)
            {
                obstructed.Add(cellAttributes);
            }
        })
        .WithoutBurst()
        .Run();

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

        init = true;
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
