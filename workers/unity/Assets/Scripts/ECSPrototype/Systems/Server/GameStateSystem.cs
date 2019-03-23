using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.PlayerLifecycle;
using Unit;
using Improbable.Gdk.Core;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem))]
    public class GameStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public ComponentDataArray<Generic.GameState.Component> GameStateData;
            public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        }

        [Inject] private Data m_Data;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Player.PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        }

        [Inject] private PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<ServerPath.Component> Paths;
            public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        }

        [Inject] UnitData m_UnitData;

        protected override void OnUpdate()
        {
            for(int i = 0; i < m_Data.Length; i++)
            {
                var gameState = m_Data.GameStateData[i];
                var gameStateWorldIndex = m_Data.WorldIndexData[i].Value;

                if (gameState.PlayersOnMapCount == 2)
                {
                    switch (gameState.CurrentState)
                    {
                        case Generic.GameStateEnum.waiting_for_players:
                            gameState.CurrentState = Generic.GameStateEnum.spawning;
                            m_Data.GameStateData[i] = gameState;
                            break;
                        case Generic.GameStateEnum.calculate_energy:
                            gameState.CurrentState = Generic.GameStateEnum.planning;
                            m_Data.GameStateData[i] = gameState;
                            break;
                        case Generic.GameStateEnum.planning:
                            if (AllPlayersReady(gameStateWorldIndex))
                            {
                                gameState.CurrentState = Generic.GameStateEnum.spawning;
                                m_Data.GameStateData[i] = gameState;
                            }
                            break;
                        case Generic.GameStateEnum.spawning:
                            gameState.CurrentState = Generic.GameStateEnum.attacking;
                            m_Data.GameStateData[i] = gameState;
                            break;
                        case Generic.GameStateEnum.attacking:
                            gameState.CurrentState = Generic.GameStateEnum.moving;
                            m_Data.GameStateData[i] = gameState;
                            break;
                        case Generic.GameStateEnum.moving:
                            if (AllUnitsIdle(gameStateWorldIndex))
                            {
                                gameState.CurrentState = Generic.GameStateEnum.calculate_energy;
                                m_Data.GameStateData[i] = gameState;
                            }
                            break;
                        case Generic.GameStateEnum.game_over:

                            break;
                    }
                }
                else if (gameState.CurrentState != Generic.GameStateEnum.waiting_for_players)
                {
                    gameState.CurrentState = Generic.GameStateEnum.waiting_for_players;
                    m_Data.GameStateData[i] = gameState;
                }
            }

        }

        private bool AllPlayersReady(uint gameStateWorldIndex)
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var playerWorldIndex = m_PlayerData.WorldIndexData[i].Value;

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    var playerState = m_PlayerData.PlayerStateData[i].CurrentState;
                    if (playerState != Player.PlayerStateEnum.ready)
                        return false;
                }
            }
            return true;
        }

        private bool AllUnitsIdle(uint gameStateWorldIndex)
        {
            //loop through all Units to check if idle
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;

                if (unitWorldIndex == gameStateWorldIndex)
                {
                    var path = m_UnitData.Paths[i];
                    if (path.Path.CellAttributes.Count != 0)
                        return false;
                }
            }
            return true;

        }

    }


}


