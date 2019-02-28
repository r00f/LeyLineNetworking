using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.PlayerLifecycle;
using Unit;

namespace LeyLineHybridECS
{
    public class GameStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public ComponentDataArray<Generic.GameState.Component> GameStateData;
        }

        [Inject] private Data m_Data;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Player.PlayerState.Component> PlayerStateData;
        }

        [Inject] private PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<ServerPath.Component> Paths;
        }

        [Inject] UnitData m_UnitData;

        protected override void OnUpdate()
        {

            if (m_PlayerData.Length == 0)
                return;

            var gameState = m_Data.GameStateData[0];

            switch (m_Data.GameStateData[0].CurrentState)
            {
                case Generic.GameStateEnum.calculate_energy:
                    gameState.CurrentState = Generic.GameStateEnum.planning;
                    m_Data.GameStateData[0] = gameState;
                    break;
                case Generic.GameStateEnum.planning:
                    if (AllPlayersReady())
                    {
                        gameState.CurrentState = Generic.GameStateEnum.spawning;
                        m_Data.GameStateData[0] = gameState;
                    }
                    break;
                case Generic.GameStateEnum.spawning:
                    gameState.CurrentState = Generic.GameStateEnum.attacking;
                    m_Data.GameStateData[0] = gameState;
                    break;
                case Generic.GameStateEnum.attacking:
                    gameState.CurrentState = Generic.GameStateEnum.moving;
                    m_Data.GameStateData[0] = gameState;
                    break;
                case Generic.GameStateEnum.moving:
                    if (AllUnitsIdle())
                    {
                        gameState.CurrentState = Generic.GameStateEnum.calculate_energy;
                        m_Data.GameStateData[0] = gameState;
                    }
                    break;
                case Generic.GameStateEnum.game_over:

                    break;
            }
        }

        private bool AllPlayersReady()
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var playerState = m_PlayerData.PlayerStateData[i].CurrentState;
                if (playerState != Player.PlayerStateEnum.ready)
                    return false;
            }
            return true;
        }

        private bool AllUnitsIdle()
        {
            //loop through all Units to check if idle
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var path = m_UnitData.Paths[i];
                if (path.Path.CellAttributes.Count != 0)
                    return false;
            }

            return true;

        }

    }


}


