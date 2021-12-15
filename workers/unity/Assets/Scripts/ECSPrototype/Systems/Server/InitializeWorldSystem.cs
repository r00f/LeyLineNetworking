using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Core;
using Generic;
using Cell;
using Improbable;
using Player;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Collections;
using System.Collections.Generic;
using Unit;
using Unity.Jobs;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateAfter(typeof(ExecuteActionsSystem))]
    public class InitializeWorldSystem : JobComponentSystem
    {
        GameStateSystem m_GameStateSystem;
        CommandSystem m_CommandSystem;
        EntityQuery m_PlayerData;
        EntityQuery m_CellData;
        EntityQuery m_GameStateData;
        EntityQuery m_SpawnCellData;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        WorkerSystem m_WorkerSystem;
        ManalithSystem m_ManalithSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<OwningWorker.Component>(),
            ComponentType.ReadOnly<PlayerState.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );
            m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>()
            );
            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>()
            );

            m_SpawnCellData = GetEntityQuery(
                //ComponentType.ReadOnly<CellAttributesComponent.HasAuthority>(),
                ComponentType.ReadOnly<CellAttributesComponent.Component>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<IsSpawn.Component>(),
                ComponentType.ReadWrite<UnitToSpawn.Component>()
            );

        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CommandSystem = World.GetExistingSystem<CommandSystem>();
            m_GameStateSystem = World.GetExistingSystem<GameStateSystem>();
            m_WorkerSystem = World.GetExistingSystem<WorkerSystem>();
            m_ManalithSystem = World.GetExistingSystem<ManalithSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.InitializeMapEvent.Event>();
            var spawnUnitsEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.SpawnUnitsEvent.Event>();

            Entities.WithNone<WorldIndexShared, PlayerAttributes.Component, NewlyAddedSpatialOSEntity>().ForEach((Entity e, in WorldIndex.Component entityWorldIndex) =>
            {
                EntityManager.AddSharedComponentData(e, new WorldIndexShared { Value = entityWorldIndex.Value });
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            for (int i = 0; i < initMapEvents.Count; i++)
            {
                var worldIndex = new WorldIndexShared { Value = initMapEvents[i].Event.Payload.WorldIndex };
                var worldOffset = initMapEvents[i].Event.Payload.WorldOffset;

                //replicate mapArchetype obstructVisionClusters
                Entities.WithSharedComponentFilter(worldIndex).ForEach((Entity e, ref ObstructVisionClusters.Component obstructVisionClusters) =>
                {
                    obstructVisionClusters.RawClusters = GetMapArchetypeObstructVisionClusters(0);
                })
                .WithoutBurst()
                .Run();

                //replicate cells to gameState position
                Entities.WithSharedComponentFilter(new WorldIndexShared { Value = 0 }).ForEach((Entity e, in CubeCoordinate.Component coord, in Position.Component position, in CellAttributesComponent.Component cellAtts, in UnitToSpawn.Component unitToSpawn) =>
                {
                    bool isCircleCell = false;
                    bool isSpawn = false;
                    bool staticTaken = false;

                    if (EntityManager.HasComponent<IsCircleCell.Component>(e))
                        isCircleCell = true;
                    if (EntityManager.HasComponent<IsSpawn.Component>(e))
                        isSpawn = true;
                    if (EntityManager.HasComponent<StaticTaken.Component>(e))
                        staticTaken = true;

                    var neighbours = new CellAttributeList
                    {
                        CellAttributes = new List<CellAttribute>()
                    };

                    foreach (CellAttribute n in cellAtts.CellAttributes.Neighbours.CellAttributes)
                    {
                        neighbours.CellAttributes.Add(new CellAttribute
                        {
                            Position = new Vector3f(n.Position.X + worldOffset.X + 400, n.Position.Y, n.Position.Z + worldOffset.Y),
                            CubeCoordinate = n.CubeCoordinate,
                            IsTaken = n.IsTaken,
                            MovementCost = n.MovementCost,
                            ObstructVision = n.ObstructVision
                        });
                    }

                    var entity = LeyLineEntityTemplates.Cell(coord.CubeCoordinate, new Vector3f((float) position.Coords.X + worldOffset.X + 400, (float) position.Coords.Y, (float) position.Coords.Z + worldOffset.Y), staticTaken, isCircleCell, unitToSpawn.UnitName, isSpawn, unitToSpawn.Faction, neighbours, worldIndex.Value, cellAtts.CellAttributes.Cell.ObstructVision, cellAtts.CellAttributes.CellMapColorIndex, unitToSpawn.StartingUnitIndex, unitToSpawn.StartRotation);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                })
                .WithoutBurst()
                .Run();

                //replicate manaliths
                Entities.WithSharedComponentFilter(new WorldIndexShared { Value = 0 }).ForEach((Entity e, in CubeCoordinate.Component coord, in Metadata.Component metaData, in FactionComponent.Component faction, in Position.Component position, in Manalith.Component manalith, in StartRotation.Component startRot) =>
                {
                    var unitGO = Resources.Load<GameObject>("Prefabs/UnityClient/" + metaData.EntityType);
                    var Stats = unitGO.GetComponent<UnitDataSet>();

                    var entity = LeyLineEntityTemplates.ReplicateManalithUnit(metaData.EntityType, new Vector3f((float) position.Coords.X + worldOffset.X + 400, (float) position.Coords.Y, (float) position.Coords.Z + worldOffset.Y), coord.CubeCoordinate, faction.Faction, worldIndex.Value, Stats, startRot.Value, manalith);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                })
                .WithoutBurst()
                .Run();
            }

            for (int i = 0; i < spawnUnitsEvents.Count; i++)
            {
                var worldIndex = new WorldIndexShared { Value = spawnUnitsEvents[i].Event.Payload.WorldIndex };

                Entities.WithSharedComponentFilter(worldIndex).WithAll<IsSpawn.Component>().ForEach((in UnitToSpawn.Component unitToSpawn, in CubeCoordinate.Component coord, in Position.Component position) =>
                {
                    if (unitToSpawn.Faction == 0)
                    {
                        var unitGO = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName);
                        var Stats = unitGO.GetComponent<UnitDataSet>();
                        var AIStats = unitGO.GetComponent<AIUnitDataSet>();
                        var entity = LeyLineEntityTemplates.NeutralUnit(m_WorkerSystem.WorkerId, unitToSpawn.UnitName, position, coord.CubeCoordinate, unitToSpawn.Faction, worldIndex.Value, Stats, AIStats, unitToSpawn.StartRotation, unitToSpawn.ManalithUnit);
                        var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                        m_CommandSystem.SendCommand(createEntitiyRequest);
                    }
                })
                .WithoutBurst()
                .Run();

                Entities.WithSharedComponentFilter(worldIndex).ForEach((Entity entity, in FactionComponent.Component faction, in PlayerAttributes.Component playerAttribute, in OwningWorker.Component owningWorker) =>
                {
                    SpawnPlayerUnits(owningWorker.WorkerId, worldIndex, faction.Faction, playerAttribute);
                })
                .WithoutBurst()
                .Run();

            }
            return inputDeps;
        }

        public List<CellAttributesList> GetMapArchetypeObstructVisionClusters(uint mapArchetypeIndex)
        {
            var rawClusters = new List<CellAttributesList>();

            Entities.WithSharedComponentFilter(new WorldIndexShared { Value = mapArchetypeIndex }).ForEach((in ObstructVisionClusters.Component obstructVisionClusters) =>
            {
                rawClusters = obstructVisionClusters.RawClusters;
            })
            .WithoutBurst()
            .Run();

            return rawClusters;
        }

        public void SpawnPlayerUnits(string workerId, WorldIndexShared worldIndex, uint unitFaction, PlayerAttributes.Component playerAttributes)
        {
            Entities.WithAll<IsSpawn.Component>().WithSharedComponentFilter(worldIndex).ForEach((ref UnitToSpawn.Component unitToSpawn, ref CubeCoordinate.Component coord) =>
            {
                if (unitToSpawn.Faction == unitFaction)
                {
                    if (unitToSpawn.StartingUnitIndex == 0)
                    {
                        SpawnUnit(workerId, worldIndex, playerAttributes.HeroName, unitToSpawn.Faction, coord.CubeCoordinate, unitToSpawn.StartRotation);
                    }
                    else if (unitToSpawn.StartingUnitIndex <= playerAttributes.StartingUnitNames.Count)
                    {
                        SpawnUnit(workerId, worldIndex, playerAttributes.StartingUnitNames[(int) unitToSpawn.StartingUnitIndex - 1], unitToSpawn.Faction, coord.CubeCoordinate, unitToSpawn.StartRotation);
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        public void SpawnUnit(string workerId, WorldIndexShared worldIndex, string unitName, uint unitFaction, Vector3f cubeCoord, uint startRotation = 0)
        {
            Entities.WithSharedComponentFilter(worldIndex).ForEach((in CubeCoordinate.Component cCord, in Position.Component position, in CellAttributesComponent.Component cell) =>
            {
                if (Vector3fext.ToUnityVector(cCord.CubeCoordinate) == Vector3fext.ToUnityVector(cubeCoord))
                {
                    var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitName).GetComponent<UnitDataSet>();
                    var entity = LeyLineEntityTemplates.Unit(workerId, unitName, position, cCord.CubeCoordinate, unitFaction, worldIndex.Value, Stats, startRotation);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                }
            })
            .WithoutBurst()
            .Run();
        }

        public void SpawnNeutralUnit(WorldIndexShared worldIndex, string unitName, uint unitFaction, Vector3f cubeCoord, string owningWorkerId, uint startRotation = 0, bool isManalithUnit = false)
        {
            Entities.WithSharedComponentFilter(worldIndex).ForEach((in CubeCoordinate.Component cCord, in Position.Component position, in CellAttributesComponent.Component cell) =>
            {
                if (Vector3fext.ToUnityVector(cCord.CubeCoordinate) == Vector3fext.ToUnityVector(cubeCoord) && !isManalithUnit)
                {
                    var unitGO = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitName);
                    Debug.Log(unitGO.name);
                    var Stats = unitGO.GetComponent<UnitDataSet>();
                    var AIStats = unitGO.GetComponent<AIUnitDataSet>();
                    var entity = LeyLineEntityTemplates.NeutralUnit(owningWorkerId, unitName, position, cCord.CubeCoordinate, unitFaction, worldIndex.Value, Stats, AIStats, startRotation, isManalithUnit);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                }
            })
            .WithoutBurst()
            .Run();
        }
    }
}
