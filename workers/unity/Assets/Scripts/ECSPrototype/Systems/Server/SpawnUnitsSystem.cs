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

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateAfter(typeof(ExecuteActionsSystem))]
    public class SpawnUnitsSystem : ComponentSystem
    {
        GameStateSystem m_GameStateSystem;
        CommandSystem m_CommandSystem;
        EntityQuery m_PlayerData;
        EntityQuery m_CellData;
        EntityQuery m_GameStateData;
        EntityQuery m_SpawnCellData;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        WorkerSystem m_WorkerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<OwningWorker.Component>(),
            ComponentType.ReadOnly<PlayerState.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );
            m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>()
            );
            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>()
            );

            m_SpawnCellData = GetEntityQuery(
                //ComponentType.ReadOnly<CellAttributesComponent.HasAuthority>(),
                ComponentType.ReadOnly<CellAttributesComponent.Component>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<WorldIndex.Component>(),
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
        }

        protected override void OnUpdate()
        {
            var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.InitializeMapEvent.Event>();
            
            if (initMapEvents.Count > 0)
            {
                Entities.With(m_SpawnCellData).ForEach((ref WorldIndex.Component cellWorldIndex, ref UnitToSpawn.Component unitToSpawn, ref CubeCoordinate.Component coord) =>
                {
                    if (unitToSpawn.Faction == 0)
                    {
                        SpawnNeutralUnit(cellWorldIndex.Value, unitToSpawn.UnitName, unitToSpawn.Faction, coord.CubeCoordinate, m_WorkerSystem.WorkerId, unitToSpawn.StartRotation, unitToSpawn.ManalithUnit);
                    }
                });

                Entities.With(m_PlayerData).ForEach((Entity entity, ref FactionComponent.Component faction, ref WorldIndex.Component worldIndex, ref PlayerAttributes.Component playerAttribute, ref OwningWorker.Component owningWorker) =>
                {
                    if (worldIndex.Value != 0)
                    {
                        var playerAtt = playerAttribute;
                        var playerWIndex = worldIndex.Value;
                        var f = faction.Faction;
                        var heroName = playerAttribute.HeroName;
                        var owningW = owningWorker;

                        Entities.With(m_SpawnCellData).ForEach((ref WorldIndex.Component cellWorldIndex, ref UnitToSpawn.Component unitToSpawn, ref CubeCoordinate.Component coord) =>
                        {
                            if (cellWorldIndex.Value == playerWIndex)
                            {
                                if (unitToSpawn.Faction == f)
                                {
                                    SpawnUnit(cellWorldIndex.Value, heroName, unitToSpawn.Faction, coord.CubeCoordinate, owningW.WorkerId, unitToSpawn.StartRotation);

                                    for (int i = 0; i < playerAtt.StartingUnitNames.Count; i++)
                                    {
                                        SpawnUnit(cellWorldIndex.Value, playerAtt.StartingUnitNames[i], unitToSpawn.Faction, CellGridMethods.LineDraw(new List<Vector3f>(), coord.CubeCoordinate, new Vector3f(0, 0, 0))[i + 1], owningW.WorkerId, unitToSpawn.StartRotation);
                                    }
                                }
                            }
                        });
                    }
                });
            }
        }

        public void SpawnUnit(uint worldIndex, string unitName, uint unitFaction, Vector3f cubeCoord, string owningWorkerId, uint startRotation = 0)
        {
            Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component cCord, ref Position.Component position, ref CellAttributesComponent.Component cell, ref WorldIndex.Component cellWorldIndex) =>
            {
                var coord = cCord.CubeCoordinate;

                if (Vector3fext.ToUnityVector(coord) == Vector3fext.ToUnityVector(cubeCoord))
                {
                    //Debug.Log("CreateEntityRequest");
                    var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitName).GetComponent<Unit_BaseDataSet>();
                    var entity = LeyLineEntityTemplates.Unit(owningWorkerId, unitName, position, coord, unitFaction, worldIndex, Stats, startRotation);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                }
            });
        }

        public void SpawnNeutralUnit(uint worldIndex, string unitName, uint unitFaction, Vector3f cubeCoord, string owningWorkerId, uint startRotation = 0, bool isManalithUnit = false)
        {
            Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component cCord, ref Position.Component position, ref CellAttributesComponent.Component cell, ref WorldIndex.Component cellWorldIndex) =>
            {
                var coord = cCord.CubeCoordinate;

                if (Vector3fext.ToUnityVector(coord) == Vector3fext.ToUnityVector(cubeCoord))
                {
                    var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitName).GetComponent<Unit_BaseDataSet>();
                    var entity = LeyLineEntityTemplates.NeutralUnit(owningWorkerId, unitName, position, coord, unitFaction, worldIndex, Stats, startRotation, isManalithUnit);
                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);
                    m_CommandSystem.SendCommand(createEntitiyRequest);
                }
            });
        }
    }
}
