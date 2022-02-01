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
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class InitializeWorldSystem : JobComponentSystem
    {
        CommandSystem m_CommandSystem;
        EntityQuery m_CellData;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        WorkerSystem m_WorkerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CommandSystem = World.GetExistingSystem<CommandSystem>();
            m_WorkerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<InitMapEvent.InitializeMapEvent.Event>();

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

                Entities.WithSharedComponentFilter(new WorldIndexShared { Value = 0 }).ForEach((Entity e, in CubeCoordinate.Component coord, in Metadata.Component metaData, in FactionComponent.Component faction, in Position.Component position, in Manalith.Component manalith, in StartRotation.Component startRot) =>
                {
                    var manalithGO = Resources.Load<GameObject>("Prefabs/UnityClient/" + metaData.EntityType);
                    var Stats = manalithGO.GetComponent<UnitDataSet>();
                    var entity = LeyLineEntityTemplates.ReplicateManalithUnit(metaData.EntityType, new Vector3f((float) position.Coords.X + worldOffset.X + 450, (float) position.Coords.Y, (float) position.Coords.Z + worldOffset.Y), coord.CubeCoordinate, faction.Faction, worldIndex.Value, Stats, startRot.Value, manalith);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                })
                .WithoutBurst()
                .Run();

                Entities.WithSharedComponentFilter(worldIndex).ForEach((Entity e, ref ClientWorkerIds.Component clientWorkerIds, ref ObstructVisionClusters.Component obstructVisionClusters, in MapData.Component mapData, in WorldIndexShared worldIndexShared, in SpatialEntityId id) =>
                {
                    obstructVisionClusters.RawClusters = GetMapArchetypeObstructVisionClusters(0);

                    foreach (UnitSpawn unitSpawn in mapData.UnitsSpawnList)
                    {
                        if (unitSpawn.Faction == 0)
                        {
                            var unitGO = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitSpawn.UnitName);
                            var Stats = unitGO.GetComponent<UnitDataSet>();
                            var AIStats = unitGO.GetComponent<AIUnitDataSet>();
                            var entity = LeyLineEntityTemplates.NeutralUnit(m_WorkerSystem.WorkerId, unitSpawn.UnitName, mapData.CoordinateCellDictionary[unitSpawn.AxialCoordinate].Position, CellGridMethods.AxialToCube(unitSpawn.AxialCoordinate), unitSpawn.Faction, worldIndexShared.Value, Stats, AIStats, unitSpawn.StartRotation);
                            var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                            m_CommandSystem.SendCommand(createEntitiyRequest);
                        }
                        else
                        {
                            SpawnUnit(clientWorkerIds.ClientWorkerIds[(int) unitSpawn.Faction - 1], worldIndexShared, clientWorkerIds.PlayerAttributes[(int) unitSpawn.Faction - 1].StartingUnitNames[(int) unitSpawn.StartingUnitIndex], unitSpawn.Faction, CellGridMethods.AxialToCube(unitSpawn.AxialCoordinate), mapData.CoordinateCellDictionary[unitSpawn.AxialCoordinate].Position, unitSpawn.StartRotation);
                        }
                    }

                    m_ComponentUpdateSystem.SendEvent(
                    new ClientWorkerIds.MapInitializedEvent.Event(new MapInitialized(worldIndex.Value)),
                    id.EntityId
                    );
                })
                .WithoutBurst()
                .Run();

                //replicate cells to gameState position
                /*
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
                */
            }
            return inputDeps;
        }
        
        public List<Vector3fList> GetMapArchetypeObstructVisionClusters(uint mapArchetypeIndex)
        {
            var rawClusters = new List<Vector3fList>();

            Entities.WithSharedComponentFilter(new WorldIndexShared { Value = mapArchetypeIndex }).ForEach((in ObstructVisionClusters.Component obstructVisionClusters) =>
            {
                //Debug.Log("ArchetypeClustersFound");
                rawClusters = obstructVisionClusters.RawClusters;
            })
            .WithoutBurst()
            .Run();

            return rawClusters;
        }

        public void SpawnUnit(string workerId, WorldIndexShared worldIndex, string unitName, uint unitFaction, Vector3f cubeCoord, Vector3f position, uint startRotation = 0)
        {
            var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitName).GetComponent<UnitDataSet>();
            var entity = LeyLineEntityTemplates.Unit(workerId, unitName, position, cubeCoord, unitFaction, worldIndex.Value, Stats, startRotation);
            var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
            m_CommandSystem.SendCommand(createEntitiyRequest);
        }

        /*
        private uint InitializedEntityCount(WorldIndexShared gameStateWorldIndex)
        {
            Entities.WithStoreEntityQueryInField(ref m_CellData).WithSharedComponentFilter(gameStateWorldIndex).ForEach((Entity e) =>
            {

            })
            .Run();
            //Debug.Log(InitializedCellCount(gameStateWorldIndex));
            return (uint) m_CellData.CalculateEntityCount();
        }
        */
    }
}
