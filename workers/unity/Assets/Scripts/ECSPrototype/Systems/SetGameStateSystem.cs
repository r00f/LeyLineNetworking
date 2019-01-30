using UnityEngine;
using System.Collections;
using Unity.Entities;

namespace LeyLineHybridECS
{
    public class SetGameStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentArray<IsIdle> IsIdleData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
        }

        [Inject] private Data m_Data;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            GameStateSystem.CurrentState = GameStateSystem.State.Spawning;
        }

        protected override void OnUpdate()
        {
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
        }

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

        private bool AllUnitsIdle()
        {
            
            //loop through all Units to check if idle
            for (int i = 0; i < m_Data.Length; i++)
            {
                bool isIdle = m_Data.IsIdleData[i].Value;
                if (isIdle == false)
                    return false;
            }

            return true;

        }
    }


}


