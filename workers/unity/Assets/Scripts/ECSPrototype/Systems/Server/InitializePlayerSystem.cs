using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Player;
using Unity.Jobs;
using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.PlayerLifecycle;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class InitializePlayerSystem : JobComponentSystem
    {
        public struct PlayerStateData : ISystemStateComponentData
        {
            public uint WorldIndexState;
        }

        EntityQuery m_GameStateData;
        EntityQuery m_GameStateSharedData;
        Settings settings;
        private EndSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        ILogDispatcher logger;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>()
            );

            m_GameStateSharedData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>(),
                ComponentType.ReadOnly<WorldIndexShared>()
            );

            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            settings = Resources.Load<Settings>("Settings");
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityCommandBuffer ecb = entityCommandBufferSystem.CreateCommandBuffer();
            /*
            logger.HandleLog(LogType.Warning,
            new LogEvent("GameStates with worldIndexShared Count")
            .WithField("Count", m_GameStateData.CalculateEntityCount()));
            */

            Entities.WithNone<WorldIndexShared, PlayerStateData, NewlyAddedSpatialOSEntity>().ForEach((Entity entity, ref FactionComponent.Component factionComp, ref Position.Component pos, ref WorldIndex.Component spatialWorldIndex, in PlayerAttributes.Component playerAddedPlayerAttributes, in SpatialEntityId id, in OwningWorker.Component owningWorker) =>
            {
                //Prevent PlayerFaction being initialized with WorldIndex 0 because gamestate has not had its WorldIndexShared added
                if(spatialWorldIndex.Value == 0)
                {
                    spatialWorldIndex.Value = SetPlayerWorld(1, owningWorker.WorkerId);
                    //Debug.Log("Newly added player spatialWorldIndex after SetPlayerWorld: " + spatialWorldIndex.Value);
                    /*
                    logger.HandleLog(LogType.Warning,
                    new LogEvent("Newly added player spatialWorldIndex after SetPlayerWorld")
                    .WithField("SpatialWorldIndex", spatialWorldIndex.Value));
                    */

                    if (spatialWorldIndex.Value != 0)
                    {
                        factionComp.Faction = SetPlayerFaction(spatialWorldIndex.Value);
                        //Debug.Log("Newly added player faction after SetPlayerFaction: " + factionComp.Faction);
                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("Newly added player faction after SetPlayerFaction")
                        .WithField("faction", factionComp.Faction));
                        */

                        pos.Coords = SetPlayerPosition(spatialWorldIndex.Value, factionComp.Faction);
                        ecb.AddSharedComponent(entity, new WorldIndexShared { Value = spatialWorldIndex.Value });
                        ecb.AddComponent(entity, new PlayerStateData { WorldIndexState = spatialWorldIndex.Value });
                    }
                }
                else
                {
                    //Debug.Log("Reinitialize old player with worldindex: " + spatialWorldIndex.Value);
                    /*
                    logger.HandleLog(LogType.Warning,
                    new LogEvent("Reinitialize old player with worldindex")
                    .WithField("WorldIndex", spatialWorldIndex.Value));
                    */
                    ecb.AddSharedComponent(entity, new WorldIndexShared { Value = spatialWorldIndex.Value });
                    ecb.AddComponent(entity, new PlayerStateData { WorldIndexState = spatialWorldIndex.Value });
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            Entities.WithNone<PlayerAttributes.Component>().ForEach((Entity entity, in PlayerStateData worldIndex) =>
            {
                SubstractPlayerOnMapCount(worldIndex);
                ecb.RemoveComponent<PlayerStateData>(entity);
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            return inputDeps;
        }

        uint SetPlayerWorld(uint index, string clientWorkerId)
        {
            if (m_GameStateData.CalculateEntityCount() < index)
            {
                return 0;
            }

            uint wIndex = 0;

            Entities.WithSharedComponentFilter(new WorldIndexShared { Value = index }).ForEach((ref GameState.Component gameState, ref ClientWorkerIds.Component clientWorkerIds) =>
            {
                if (gameState.PlayersOnMapCount < 2)
                {
                    wIndex = index;
                    gameState.PlayersOnMapCount++;

                    if (gameState.PlayersOnMapCount == 1)
                        clientWorkerIds.ClientWorkerId1 = clientWorkerId;
                    else
                        clientWorkerIds.ClientWorkerId2 = clientWorkerId;
                }
            })
            .WithoutBurst()
            .Run();

            if (wIndex == 0)
                return SetPlayerWorld(index + 1, clientWorkerId);
            else
                return wIndex;
        }

        void SubstractPlayerOnMapCount(PlayerStateData worldIndex)
        {
            var wI = new WorldIndexShared { Value = worldIndex.WorldIndexState };

            //Debug.Log("Substract player from gamestate: " + worldIndex.WorldIndexState);

            Entities.WithSharedComponentFilter(wI).ForEach((ref GameState.Component gameState) =>
            {
                gameState.PlayersOnMapCount--;
            })
            .WithoutBurst()
            .Run();
        }

        Coordinates SetPlayerPosition(uint worldIndex, uint faction)
        {
            var c = new Coordinates();

            var xOffsetPos = faction * 2;

            var worldIndexShared = new WorldIndexShared { Value = worldIndex };

            Entities.WithSharedComponentFilter(worldIndexShared).WithAll<GameState.Component>().ForEach((in Position.Component gameStatePos) =>
            {
                c.X = gameStatePos.Coords.X + xOffsetPos;
                c.Y = gameStatePos.Coords.Y;
                c.Z = gameStatePos.Coords.Z;
            })
            .WithoutBurst()
            .Run();

            return c;
        }

        public uint SetPlayerFaction(uint worldIndex)
        {
            uint faction = 0;
            var worldIndexShared = new WorldIndexShared { Value = worldIndex };

            Entities.WithSharedComponentFilter(worldIndexShared).ForEach((in GameState.Component gameState) =>
            {
                faction = gameState.PlayersOnMapCount;
            })
            .WithoutBurst()
            .Run();

            return faction;
        }

        public uint FindFactionWithPlayers(WorldIndexShared mWorldIndex, FactionComponent.Component factionComponent)
        {
            Entities.WithSharedComponentFilter(mWorldIndex).WithAll<PlayerAttributes.Component>().ForEach((in FactionComponent.Component factionC) =>
            {
                if (factionC.Faction != 0)
                {
                    if (factionC.Faction % 2 == 1)
                    {
                        factionComponent.Faction = (mWorldIndex.Value - 1) * 2 + 2;
                    }
                    else if (factionC.Faction % 2 == 0)
                    {
                        factionComponent.Faction = (mWorldIndex.Value - 1) * 2 + 1;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            return factionComponent.Faction;
        }
    }
}
