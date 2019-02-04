using UnityEngine;
using System.Collections;
using Unity.Entities;
using UnityEngine.UI;
using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;
using Improbable;
using Unity.Collections;


namespace LeyLineHybridECS
{
    public class PlayerStateSystem : ComponentSystem
    {
        public struct GameStateData
        {
            public readonly int Length;
            public ComponentDataArray<Generic.GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<Player.PlayerState.Component>> AuthorativeData;
            public ComponentDataArray<Player.PlayerState.Component> PlayerStateData;
        }

        [Inject] private PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Improbable.Gdk.Health.HealthComponent.Component> HealthData;
        }

        [Inject] private UnitData m_UnitData;

        Button readyButton;
        
        protected override void OnUpdate()
        {
            if(readyButton == null)
            {
                readyButton = GameObject.Find("ReadyButton").GetComponent<Button>();
                readyButton.onClick.AddListener(delegate { SetPlayerState(Player.PlayerStateEnum.ready); });
            }

            if (m_GameStateData.Length > 0)
            {
                var gameState = m_GameStateData.GameState[0];
                if (gameState.CurrentState != Generic.GameStateEnum.planning)
                {
                    SetPlayerState(Player.PlayerStateEnum.waiting);
                    return;
                }
            }

            var playerState = m_PlayerData.PlayerStateData[0];

            if (playerState.CurrentState != Player.PlayerStateEnum.ready)
            {
                if (AnyUnitClicked())
                {
                    SetPlayerState(Player.PlayerStateEnum.unit_selected);
                }
                else
                {
                    SetPlayerState(Player.PlayerStateEnum.waiting);
                }
            }
        }

        public void SetPlayerState(Player.PlayerStateEnum state)
        {
            var playerState = m_PlayerData.PlayerStateData[0];
            if (playerState.CurrentState != state)
            {
                playerState.CurrentState = state;
                m_PlayerData.PlayerStateData[0] = playerState;
            }
        }

        private bool AnyUnitClicked()
        {
            //loop through all Units to check if clicked
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var mouseState = m_UnitData.MouseStateData[i].CurrentState;
                if (mouseState == MouseState.State.Clicked)
                    return true;
            }
            return false;
        }
    }
}


