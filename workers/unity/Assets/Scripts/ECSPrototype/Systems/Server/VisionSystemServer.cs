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
public class VisionSystemServer : JobComponentSystem
{
    ILogDispatcher logger;
    CommandSystem m_CommandSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    EntityQuery m_CellData;
    EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();
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

        Entities.ForEach((in GameState.Component gameState, in ObstructVisionClusters.Component clusters, in WorldIndexShared worldIndex) =>
        {
            if(gameState.CurrentState != GameStateEnum.planning && gameState.CurrentState != GameStateEnum.waiting_for_players && gameState.CurrentState != GameStateEnum.game_over)
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

                    p_Vision = UpdatePlayerVision(p_Vision, p_Faction.Faction, p_windex.Value);

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

    private void UpdateUnitVision(WorldIndexShared worldIndex, List<Vector3fList> obstructVisionClusters)
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
                u_Vision = GenerateUnitVision(u_OccupiedCell, u_Vision, u_Faction, obstructVisionClusters, new List<ObstructVisionCluster>());
                SetPlayerReqUpdate(u_Faction.Faction, worldIndex, u_OccupiedCell.CubeCoordinate);
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

    private UnitVision GenerateUnitVision(CubeCoordinate.Component coor, UnitVision inVision, FactionComponent.Component inFaction, List<Vector3fList> fixClusters, List<ObstructVisionCluster> relevantClusters)
    {
        var sightHash = CellGridMethods.CircleDrawHash(coor.CubeCoordinate, inVision.VisionRange);

        foreach (Vector3fList fixCluster in fixClusters)
        {
            foreach (Vector3f fixClusterCell in fixCluster.Coordinates)
            {
                if (CellGridMethods.GetDistance(fixClusterCell, coor.CubeCoordinate) <= inVision.VisionRange)
                {
                    relevantClusters.Add(new ObstructVisionCluster(fixCluster));
                    break;
                }
            }
        }

        if (relevantClusters.Count > 0)
        {
            List<Vector3f> Ring = CellGridMethods.RingDraw(coor.CubeCoordinate, inVision.VisionRange);
            List<List<Vector3f>> Lines = new List<List<Vector3f>>();

            foreach (Vector3f c in Ring)
            {
                Lines.Add(CellGridMethods.LineDrawWhitoutOrigin(new List<Vector3f>(), coor.CubeCoordinate, c));
            }

            /*
            foreach (ObstructVisionCluster o in relevantClusters)
            {
                foreach(Vector3f c in o.cluster)
                    Lines.Add(CellGridMethods.ExtendLineToLength(new List<Vector3f>(), coor.CubeCoordinate, c, inVision.VisionRange));
            }
            */

            foreach (List<Vector3f> l in Lines)
            {
                bool visible = true;
                for (int i = 0; i < l.Count; i++)
                {
                    if (visible)
                    {
                        foreach (ObstructVisionCluster o in relevantClusters)
                        {
                            if (o.cluster.Contains(l[i]))
                            {
                                visible = false;
                            }
                        }
                    }
                    else
                    {
                        sightHash.Remove(l[i]);
                        /*
                        Debug.Log("RemoveCoordinateFromVision " + inVision.Vision.RemoveSwapBack(CellGridMethods.CubeToAxial(l[i])));
                        inVision.Vision.RemoveSwapBack(CellGridMethods.CubeToAxial(l[i]));
                        */
                    }
                }
            }
        }

        inVision.Vision.Clear();

        foreach (Vector3f v in sightHash)
            inVision.Vision.Add(CellGridMethods.CubeToAxial(v));

        //Debug.Log("VisionRange = " + inVision.VisionRange + ", VisonCoordinatesCount = " + inVision.Vision.Length);

        return inVision;
    }

    private Vision.Component RevealMap(uint worldIndex, Vision.Component inVision)
    {
        inVision.CellsInVisionrange.Clear();

        Entities.WithSharedComponentFilter(new WorldIndexShared { Value = worldIndex }).ForEach((in MapData.Component mapData) =>
        {
            inVision.CellsInVisionrange.AddRange(mapData.CoordinateCellDictionary.Keys);
        })
        .WithoutBurst()
        .Run();

        return inVision;
    }

    private Vision.Component UpdatePlayerVision(Vision.Component inVision, uint faction, uint worldIndex)
    {
        inVision.CellsInVisionrange.Clear();

        Entities.WithSharedComponentFilter(new WorldIndexShared {  Value = worldIndex }).WithAll<CubeCoordinate.Component>().ForEach((in UnitVision unitVision, in FactionComponent.Component unitFaction) =>
        {
            if (faction == unitFaction.Faction)
            {
                inVision.CellsInVisionrange.AddRange(unitVision.Vision.ToArray());
            }
        })
        .WithoutBurst()
        .Run();

        return inVision;
    }
}

public struct ObstructVisionCluster
{
    public HashSet<Vector3f> cluster;

    public ObstructVisionCluster(HashSet<Vector3f> inCluster)
    {
        cluster = inCluster;
    }
    
    public ObstructVisionCluster(Vector3fList inCluster)
    {
        cluster = new HashSet<Vector3f>(inCluster.Coordinates);
    }
    
    public ObstructVisionCluster(Vector3f start)
    {
        cluster = new HashSet<Vector3f>
        {
            start
        };
    }
}

