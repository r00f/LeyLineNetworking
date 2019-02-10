using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.PlayerLifecycle;

namespace LeyLineHybridECS
{
    public class SetGameStateSystem : ComponentSystem
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


        /*
        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            GameStateSystem.CurrentState = GameStateSystem.State.Spawning;
        }
        */
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
                    Debug.Log("Spawning");
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
                    //display gameOver screen
                    break;

            }

            /*
            if (GameStateSystem.CurrentState == GameStateSystem.State.CalculateEnergy)
            {
                GameStateSystem.CurrentState = GameStateSystem.State.WaitingForInput;
            }
            else if (GameStateSystem.CurrentState == GameStateSystem.State.WaitingForInput)
            {
                if(AnyUnitClicked())
                {
                    GameStateSystem.CurrentState = GameStateSystem.State.UnitClicked;
                }
            }
            else if(GameStateSystem.CurrentState == GameStateSystem.State.UnitClicked)
            {
                if (!AnyUnitClicked())
                {
                    GameStateSystem.CurrentState = GameStateSystem.State.WaitingForInput;
                }
            }
            else if (GameStateSystem.CurrentState == GameStateSystem.State.Attacking)
            {
                GameStateSystem.CurrentState = GameStateSystem.State.Moving;
            }
            else if (GameStateSystem.CurrentState == GameStateSystem.State.Moving)
            {
                //if all Units are Idle, set GameState to Planning
                if (AllUnitsIdle() && m_Data.Length != 0)
                {
                    GameStateSystem.CurrentState = GameStateSystem.State.CalculateEnergy;
                }
            }

            */
        }

        /*
        private bool AnyUnitClicked()
        {

            //loop through all Units to check if clicked
            for (int i = 0; i < m_Data.Length; i++)
            {
                var mouseState = m_Data.MouseStateData[i].CurrentState;
                if (mouseState == MouseState.State.Clicked)
                    return true;
            }
            return false;
        }
        */
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
            /*
            //loop through all Units to check if idle
            for (int i = 0; i < m_Data.Length; i++)
            {
                bool isIdle = m_Data.IsIdleData[i].Value;
                if (isIdle == false)
                    return false;
            }

            return true;
            */
            return true;
        }

    }


}


