using Cell;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Player;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class InitializePlayerSystem : ComponentSystem
    {
        public struct PlayerStateData : ISystemStateComponentData
        {
            public WorldIndex.Component WorldIndexState;
        }

        EntityQuery m_PlayerAddedData;
        EntityQuery m_PlayerRemovedData;
        EntityQuery m_GameStateData;

        EntityQuery m_PlayerData;

        protected override void OnCreate()
        {
            base.OnCreate();
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
        }

        protected override void OnUpdate()
        {
            
            Entities.With(m_PlayerAddedData).ForEach((Entity entity, ref WorldIndex.Component worldIndex, ref FactionComponent.Component factionComp, ref Position.Component pos, ref FactionComponent.Component playerAddedFaction, ref PlayerAttributes.Component playerAddedPlayerAttributes) =>
            {
                if (worldIndex.Value == 0)
                {
                    uint wIndex = SetPlayerWorld();
                    worldIndex.Value = wIndex;
                    var c = new Position.Component();

                    Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState, ref Position.Component gameStatePos) =>
                    {
                        if (wIndex == gameStateWorldIndex.Value)
                            c.Coords = gameStatePos.Coords;
                    });

                    factionComp = SetPlayerFaction(wIndex);
                    pos.Coords = c.Coords;
                }

                PostUpdateCommands.AddComponent(entity, new PlayerStateData { WorldIndexState = worldIndex });
            });

            Entities.With(m_PlayerRemovedData).ForEach((Entity entity, ref PlayerStateData worldIndex) =>
            {
                var wI = worldIndex;
                Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
                {
                    if (gameStateWorldIndex.Value == wI.WorldIndexState.Value)
                    {
                        gameState.PlayersOnMapCount--;
                    }
                });
                PostUpdateCommands.RemoveComponent<PlayerStateData>(entity);
            });
        }

        public uint SetPlayerWorld()
        {
            uint sortIndex = 1;
            uint wIndex = 0;

            Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component mapWorldIndex, ref GameState.Component gameState) =>
            {
                if (mapWorldIndex.Value == sortIndex)
                {
                    if (gameState.PlayersOnMapCount < 2)
                    {
                        gameState.PlayersOnMapCount++;
                        wIndex = mapWorldIndex.Value;
                    }
                    /*
                    else
                    {
                        sortIndex++;
                        i = -1;
                    }
                    */
                }

            });

            return wIndex;
        }

        public FactionComponent.Component SetPlayerFaction(uint worldIndex)
        {
            FactionComponent.Component factionComponent = new FactionComponent.Component();

            Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component mapWorldIndex, ref GameState.Component gameState) =>
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
                        Entities.With(m_PlayerData).ForEach((ref FactionComponent.Component factionC, ref WorldIndex.Component pWorldIndex) =>
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
                        });
                    }
                }
            });
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
    }
}
