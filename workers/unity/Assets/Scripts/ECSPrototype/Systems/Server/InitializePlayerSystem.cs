using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Player;
using Unity.Jobs;
using Unity.Entities;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class InitializePlayerSystem : JobComponentSystem
    {
        public struct PlayerStateData : ISystemStateComponentData
        {
            public WorldIndex.Component WorldIndexState;
        }

        //EntityQuery m_PlayerAddedData;
        //EntityQuery m_PlayerRemovedData;
        //EntityQuery m_GameStateData;
        //EntityQuery m_PlayerData;

        Settings settings;

        protected override void OnCreate()
        {
            base.OnCreate();
            settings = Resources.Load<Settings>("Settings");

            /*
            var playerAddedDesc = new EntityQueryDesc
            {
                None = new ComponentType[] { typeof(PlayerStateData) },
                All = new ComponentType[] 
                {
                    ComponentType.ReadOnly<PlayerAttributes.Component>(),
                    ComponentType.ReadOnly<PlayerAttributes.HasAuthority>(),
                    ComponentType.ReadWrite<Position.Component>(),
                    ComponentType.ReadWrite<WorldIndex.Component>(),
                    ComponentType.ReadWrite<FactionComponent.Component>()
                }
            };

            m_PlayerAddedData = GetEntityQuery(playerAddedDesc);

            var playerRemovedDesc = new EntityQueryDesc
            {
                None = new ComponentType[] 
                {
                    ComponentType.ReadOnly<PlayerAttributes.Component>(),
                    ComponentType.ReadWrite<Position.Component>(),
                    ComponentType.ReadWrite<WorldIndex.Component>(),
                    ComponentType.ReadWrite<FactionComponent.Component>()
                },
                All = new ComponentType[]
                {
                     typeof(PlayerStateData)
                }
            };

            m_PlayerRemovedData = GetEntityQuery(playerRemovedDesc);

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadWrite<GameState.Component>()
            );

            m_PlayerData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<PlayerStateData>(),
                ComponentType.ReadWrite<PlayerAttributes.Component>(),
                ComponentType.ReadWrite<FactionComponent.Component>()
            );
            */
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Entities.WithNone<PlayerStateData>().ForEach((Entity entity, ref WorldIndex.Component worldIndex, ref FactionComponent.Component factionComp, ref Position.Component pos, in PlayerAttributes.Component playerAddedPlayerAttributes) =>
            {
                if (worldIndex.Value == 0)
                {
                    uint wIndex = SetPlayerWorld();
                    worldIndex.Value = wIndex;
                    var c = new Position.Component();

                    Entities.ForEach((in WorldIndex.Component gameStateWorldIndex, in GameState.Component gameState, in Position.Component gameStatePos) =>
                    {
                        if (wIndex == gameStateWorldIndex.Value)
                            c.Coords = gameStatePos.Coords;
                    })
                    .WithoutBurst()
                    .Run();

                    if (settings.ForcePlayerFaction == 0)
                        factionComp = SetPlayerFaction(wIndex);
                    else
                        factionComp = new FactionComponent.Component
                        {
                            Faction = settings.ForcePlayerFaction
                        };
                    pos.Coords = c.Coords;
                }

                EntityManager.AddComponentData(entity, new PlayerStateData { WorldIndexState = worldIndex });
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            Entities.WithNone<PlayerAttributes.Component>().ForEach((Entity entity, ref PlayerStateData worldIndex) =>
            {
                var wI = worldIndex;
                Entities.ForEach((ref GameState.Component gameState, in WorldIndex.Component gameStateWorldIndex) =>
                {
                    if (gameStateWorldIndex.Value == wI.WorldIndexState.Value)
                    {
                        gameState.PlayersOnMapCount--;
                    }
                })
                .WithoutBurst()
                .Run();
                EntityManager.RemoveComponent<PlayerStateData>(entity);
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            return inputDeps;
        }

        public uint SetPlayerWorld()
        {
            uint sortIndex = 1;
            uint wIndex = 0;

            Entities.ForEach((ref GameState.Component gameState, in WorldIndex.Component mapWorldIndex) =>
            {
                if (mapWorldIndex.Value == sortIndex)
                {
                    if (gameState.PlayersOnMapCount < 2)
                    {
                        gameState.PlayersOnMapCount++;
                        wIndex = mapWorldIndex.Value;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            return wIndex;
        }

        public FactionComponent.Component SetPlayerFaction(uint worldIndex)
        {
            FactionComponent.Component factionComponent = new FactionComponent.Component();

            Entities.ForEach((in WorldIndex.Component mapWorldIndex, in GameState.Component gameState) =>
            {
                var mWorldIndex = mapWorldIndex.Value;
                if (mapWorldIndex.Value == worldIndex)
                {
                    if (gameState.PlayersOnMapCount == 1)
                    {
                        factionComponent.Faction = (mapWorldIndex.Value - 1) * 2 + 1;
                    }
                    else
                    {
                        factionComponent.Faction = FindFactionWithPlayers(mapWorldIndex.Value, factionComponent);
                    }
                }
            })
            .WithoutBurst()
            .Run();

            if (factionComponent.Faction % 2 == 1)
            {
                factionComponent.TeamColor = TeamColorEnum.blue;
            }
            else if (factionComponent.Faction % 2 == 0)
            {
                factionComponent.TeamColor = TeamColorEnum.red;
            }

            return factionComponent;
        }


        public uint FindFactionWithPlayers(uint mWorldIndex, FactionComponent.Component factionComponent)
        {
            Entities.WithAll<PlayerAttributes.Component>().ForEach((in FactionComponent.Component factionC, in WorldIndex.Component pWorldIndex) =>
            {
                if (pWorldIndex.Value == mWorldIndex && factionC.Faction != 0)
                {
                    if (factionC.Faction % 2 == 1)
                    {
                        factionComponent.Faction = (mWorldIndex - 1) * 2 + 2;
                    }
                    else if (factionC.Faction % 2 == 0)
                    {
                        factionComponent.Faction = (mWorldIndex - 1) * 2 + 1;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            return factionComponent.Faction;
        }
    }
}
