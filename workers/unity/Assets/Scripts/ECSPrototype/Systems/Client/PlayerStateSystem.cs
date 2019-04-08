using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;


namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class PlayerStateSystem : ComponentSystem
    {
        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public ComponentDataArray<PlayerState.Component> PlayerStateData;
        }

        [Inject] private PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<UnitAttributes.Component> UnitAttributeData;
        }

        [Inject] private UnitData m_UnitData;

        UIReferences uiRef;

        //Button readyButton;
        //GameObject startUpPanel;

        protected override void OnStartRunning()
        {

        }

        protected override void OnUpdate()
        {
            if(uiRef == null)
            {
                uiRef = Object.FindObjectOfType<UIReferences>();
                uiRef.ReadyButton.onClick.AddListener(delegate { SetPlayerState(PlayerStateEnum.ready); });
            }

            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var playerState = m_PlayerData.PlayerStateData[0];
                var playerWorldIndex = m_PlayerData.WorldIndexData[0].Value;
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;
                var gameState = m_GameStateData.GameState[gi];

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    if(uiRef != null)
                    {
                        if(gameState.CurrentState == GameStateEnum.waiting_for_players)
                        {
                            //if (!uiRef.StartupPanel.activeSelf)
                                //uiRef.StartupPanel.SetActive(true);
                        }
                        else
                        {
                            if (uiRef.StartupPanel.activeSelf)
                            {
                                if(!uiRef.MatchReadyPanel.activeSelf)
                                {
                                    uiRef.MatchReadyPanel.SetActive(true);
                                }
                                if(uiRef.StartUpWaitTime > 0)
                                {
                                    uiRef.StartUpWaitTime -= Time.deltaTime;
                                }
                                else
                                {
                                    uiRef.StartupPanel.SetActive(false);
                                }
                            }
                                
                        }
                    }

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (playerState.CurrentState != PlayerStateEnum.ready)
                        {
                            if (AnyUnitClicked() && playerState.CurrentState != PlayerStateEnum.unit_selected)
                            {
                                SetPlayerState(PlayerStateEnum.unit_selected);
                            }
                            else if (playerState.CurrentState != PlayerStateEnum.waiting)
                            {
                                SetPlayerState(PlayerStateEnum.waiting);
                            }
                        }
                    }
                    else
                    {
                        SetPlayerState(PlayerStateEnum.waiting);
                        return;
                    }
                }
            }
        }

        public void SetPlayerState(PlayerStateEnum state)
        {
            UpdateInjectedComponentGroups();
            var playerState = m_PlayerData.PlayerStateData[0];
            playerState.CurrentState = state;
            m_PlayerData.PlayerStateData[0] = playerState;
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


