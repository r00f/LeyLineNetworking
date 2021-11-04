using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Player;
using Unity.Jobs;
using Unity.Entities;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializeWorldsSystem))]
    public class InitializePlayerSystem : JobComponentSystem
    {
        public struct PlayerStateData : ISystemStateComponentData
        {
            public uint WorldIndexState;
        }

        Settings settings;
        private BeginSimulationEntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            settings = Resources.Load<Settings>("Settings");
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityCommandBuffer ECBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            Entities.WithNone<WorldIndexShared>().ForEach((Entity entity, ref FactionComponent.Component factionComp, ref Position.Component pos, in PlayerAttributes.Component playerAddedPlayerAttributes) =>
            {
                var wIndex = new WorldIndexShared { Value = SetPlayerWorld(factionComp.Faction) };

                //Debug.Log("PlayerWorldIndexValue = " + wIndex.Value);

                //Prevent PlayerFaction being initialized with WorldIndex 0 because gamestate has not had its WorldIndexShared added
                if(wIndex.Value != 0)
                {
                    //if (settings.ForcePlayerFaction == 0)

                    factionComp.Faction = SetPlayerFaction(wIndex);

                    /*else
                        factionComp = new FactionComponent.Component
                        {
                            Faction = settings.ForcePlayerFaction
                        };
                    */

                    Debug.Log("PlayerFaction after SetPlayerFaction(WorldIndex) = " + factionComp.Faction);

                    pos.Coords = SetPlayerPosition(wIndex, factionComp.Faction);

                    ECBuffer.AddComponent(entity, new PlayerStateData { WorldIndexState = wIndex.Value });
                    ECBuffer.AddSharedComponent(entity, wIndex);
                }
            })
            .WithoutBurst()
            .Run();

            Entities.WithNone<PlayerAttributes.Component>().ForEach((Entity entity, in PlayerStateData worldIndex) =>
            {
                SubstractPlayerOnMapCount(worldIndex);
                EntityManager.RemoveComponent<PlayerStateData>(entity);
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            return inputDeps;
        }

        uint SetPlayerWorld(uint faction)
        {
            uint wIndex = 0;
            //Debug.Log("SetPlayerWorld");

            Entities.ForEach((ref GameState.Component gameState, in WorldIndexShared mapWorldIndex) =>
            {
                //Debug.Log("MapWorldIndexShared in SetPlayerWorld() = " + mapWorldIndex.Value);
                if (mapWorldIndex.Value == 2 && gameState.PlayersOnMapCount < 2)
                {
                    //check if this is not an old player from last editor session before incrementing playersOnMapCount
                    if (faction == 0)
                        gameState.PlayersOnMapCount++;
                    wIndex = mapWorldIndex.Value;
                }
            })
            .WithoutBurst()
            .Run();

            return wIndex;
        }

        void SubstractPlayerOnMapCount(PlayerStateData worldIndex)
        {
            var wI = new WorldIndexShared { Value = worldIndex.WorldIndexState };
            Entities.WithSharedComponentFilter(wI).ForEach((ref GameState.Component gameState) =>
            {
                gameState.PlayersOnMapCount--;
            })
            .WithoutBurst()
            .Run();
        }

        Coordinates SetPlayerPosition(WorldIndexShared worldIndex, uint faction)
        {
            var c = new Coordinates();

            var xOffsetPos = faction * 2;

            Entities.WithSharedComponentFilter(worldIndex).WithAll<GameState.Component>().ForEach((in Position.Component gameStatePos) =>
            {
                c.X = gameStatePos.Coords.X + xOffsetPos;
                c.Y = gameStatePos.Coords.Y;
                c.Z = gameStatePos.Coords.Z;
            })
            .WithoutBurst()
            .Run();

            return c;
        }

        public uint SetPlayerFaction(WorldIndexShared worldIndex)
        {
            uint faction = 0;

            Entities.WithSharedComponentFilter(worldIndex).ForEach((in GameState.Component gameState) =>
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
