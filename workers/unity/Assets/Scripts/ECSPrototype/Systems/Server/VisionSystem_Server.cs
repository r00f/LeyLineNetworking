using Unity.Entities;
using Improbable.Gdk.Core;
using System.Collections.Generic;
using Generic;
using Cell;
using Player;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class VisionSystem_Server : JobComponentSystem
{
    ILogDispatcher logger;
    CommandSystem m_CommandSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    EntityQuery m_CellData;
    EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();

        /*
        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            );

        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<PlayerAttributes.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
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

        Entities.WithAll<GameState.Component>().ForEach((in ObstructVisionClusters.Component clusters, in WorldIndexShared worldIndex) =>
        {
            UpdateUnitVision(worldIndex, clusters.RawClusters);
        })
        .WithoutBurst()
        .Run();

        Entities.WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component p_Vision, in FactionComponent.Component p_Faction, in SpatialEntityId p_id, in WorldIndex.Component p_windex) =>
        {
            /*
            logger.HandleLog(LogType.Warning,
            new LogEvent("playerVision.ReqUpdate")
            .WithField("playerVision.ReqUpdate", p_Vision.RequireUpdate)
            .WithField("PlayerId", p_id.EntityId.Id));
            */
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

                    p_Vision = UpdatePlayerVision(p_Vision, p_Faction.Faction, p_windex.Value, new HashSet<Vector2i>());

                    p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;

                    m_ComponentUpdateSystem.SendEvent(
                    new Vision.UpdateClientVisionEvent.Event(),
                    p_id.EntityId);

                    p_Vision.RequireUpdate = false;
                }
                else
                {
                    p_Vision = RevealMap(p_windex.Value, p_Vision);

                    p_Vision.CellsInVisionrange = p_Vision.CellsInVisionrange;

                    m_ComponentUpdateSystem.SendEvent(
                    new Vision.UpdateClientVisionEvent.Event(),
                    p_id.EntityId);

                    p_Vision.RequireUpdate = false;
                }
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }

    private void UpdateUnitVision(WorldIndexShared worldIndex, List<CellAttributesList> obstructVisionClusters)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref UnitVision u_Vision, in CubeCoordinate.Component u_OccupiedCell, in FactionComponent.Component u_Faction, in SpatialEntityId id) =>
        {
            if (u_Vision.InitialWaitTime > 0)
            {
                u_Vision.InitialWaitTime -= Time.DeltaTime;
                if (u_Vision.InitialWaitTime <= 0)
                    u_Vision.RequireUpdate = true;
            }
            
            if (u_Vision.RequireUpdate == true)
            {
                /*
                logger.HandleLog(LogType.Warning,
                new LogEvent("u_Vision.ReqUpdate = true")
                .WithField("unitId", id.EntityId.Id));
                */
                var v = GenerateUnitVision(u_OccupiedCell, u_Vision, u_Faction, obstructVisionClusters);
                SetPlayerReqUpdate(u_Faction.Faction, worldIndex, u_OccupiedCell.CubeCoordinate);
                u_Vision = v;
                u_Vision.RequireUpdate = false;
            }
        })
        .WithoutBurst()
        .Run();
    }

    private void SetPlayerReqUpdate(uint faction, WorldIndexShared worldIndex, Vector3f coord)
    {
        Entities.WithSharedComponentFilter(worldIndex).WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component p_Vision, in FactionComponent.Component p_Faction, in SpatialEntityId p_id) =>
        {
            if (p_Faction.Faction == faction)
            {
                /*
                logger.HandleLog(LogType.Warning,
                new LogEvent("UnitLoopSetPlayerVisionReq")
                .WithField("UnitFaction", unitFaction));
                */
                p_Vision.RequireUpdate = true;
            }
            else if (p_Vision.CellsInVisionrange.Contains(CellGridMethods.CubeToAxial(coord)))
            {
                m_ComponentUpdateSystem.SendEvent(
                new Vision.UpdateClientVisionEvent.Event(),
                p_id.EntityId);
            }
        })
        .WithoutBurst()
        .Run();
    }

    private UnitVision GenerateUnitVision(CubeCoordinate.Component coor, UnitVision inVision, FactionComponent.Component inFaction, List<CellAttributesList> FixClusters)
    {
        List<Vector3f> sight = CellGridMethods.CircleDraw(coor.CubeCoordinate, inVision.VisionRange);
        var sightHash = new HashSet<Vector3f>();

        foreach (Vector3f v in sight)
        {
            sightHash.Add(v);
        }

        List<ObstructVisionCluster> RelevantClusters = new List<ObstructVisionCluster>();

        foreach (CellAttributesList c in FixClusters)
        {
            bool isRelevant = false;
            HashSet<Vector3f> set = new HashSet<Vector3f>();
            foreach (CellAttributes a in c.CellAttributes)
            {
                if (sightHash.Contains(a.Cell.CubeCoordinate)) isRelevant = true;
                set.Add(a.Cell.CubeCoordinate);
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

        inVision.Vision.Clear();

        foreach (Vector3f v in sightHash)
        {
            inVision.Vision.Add(CellGridMethods.CubeToAxial(v));
        }

        return inVision;
    }

    private Vision.Component RevealMap(uint worldIndex, Vision.Component inVision)
    {
        inVision.CellsInVisionrange.Clear();

        //Add all coordinates to Vision TODO: Store all mapCoords in a dict on Gamestate
        Entities.WithSharedComponentFilter(new WorldIndexShared { Value = worldIndex }).ForEach((in CubeCoordinate.Component coord, in CellAttributesComponent.Component cellAtt) =>
        {
            inVision.CellsInVisionrange.Add(CellGridMethods.CubeToAxial(coord.CubeCoordinate));
        })
        .WithoutBurst()
        .Run();

        return inVision;
    }

    private Vision.Component UpdatePlayerVision(Vision.Component inVision, uint faction, uint worldIndex, HashSet<Vector2i> currentVision)
    {
        Entities.WithSharedComponentFilter(new WorldIndexShared {  Value = worldIndex }).WithAll<CubeCoordinate.Component>().ForEach((in UnitVision unitVision, in FactionComponent.Component unitFaction) =>
        {
            if (faction == unitFaction.Faction)
            {
                foreach (Vector2i v in unitVision.Vision)
                {
                    currentVision.Add(v);
                }
            }
        })
        .WithoutBurst()
        .Run();

        inVision.CellsInVisionrange = currentVision.ToList();

        return inVision;
    }

    /*
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

            foreach (CellAttributes c in cluster.cluster.CellAttributes)
            {
                if (Vector3fext.ToUnityVector(c.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate)) contains = true;
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
                    cluster.cluster.CellAttributes.Add(toRemove.CellAttributes);
                    //Debug.Log("added " + i + " to" + cluster);
                    BuildCluster(toRemove, cluster, obstructed, out obstructed);
                }
                }
            }
        newObstructed = obstructed;
    }
    */
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
        cluster = new HashSet<Vector3f>
        {
            start
        };
        RelevantAngles = new List<Angle>();
    }
}

public struct RawCluster
{
    public CellAttributesList cluster;
    
    public RawCluster(CellAttributesList inCluster)
    {
        cluster = inCluster;
    }
    public RawCluster(CellAttributes inStart)
    {
        cluster = new CellAttributesList
        {
            CellAttributes = new List<CellAttributes>()
        };
        cluster.CellAttributes.Add(inStart);
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
