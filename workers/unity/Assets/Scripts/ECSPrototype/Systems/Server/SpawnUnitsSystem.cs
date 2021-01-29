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
        }
        
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_CommandSystem = World.GetExistingSystem<CommandSystem>();
            m_GameStateSystem = World.GetExistingSystem<GameStateSystem>();
        }

        protected override void OnUpdate()
        {

        }

        public void SpawnUnit(uint worldIndex, string unitName, uint unitFaction, Vector3f cubeCoord)
        {
            Entities.With(m_PlayerData).ForEach((ref FactionComponent.Component refPlayerFaction, ref OwningWorker.Component refOwningWorker) =>
            {
                var owningWorker = refOwningWorker;
                var playerFaction = refPlayerFaction;

                //Debug.Log(playerFaction.Faction);

                if (playerFaction.Faction == unitFaction)
                {
                    Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component cCord, ref Position.Component position, ref CellAttributesComponent.Component cell, ref WorldIndex.Component cellWorldIndex) =>
                    {
                        var coord = cCord.CubeCoordinate;

                        if (Vector3fext.ToUnityVector(coord) == Vector3fext.ToUnityVector(cubeCoord))
                        {
                            //Debug.Log("CreateEntityRequest");
                            var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitName).GetComponent<Unit_BaseDataSet>();
                            var entity = LeyLineEntityTemplates.Unit(owningWorker.WorkerId, unitName, position, coord, playerFaction, worldIndex, Stats);

                            var createEntitiyRequest = new WorldCommands.CreateEntity.Request(entity);

                            m_CommandSystem.SendCommand(createEntitiyRequest);
                        }
                    });
                }
            });
        }
    }
}
