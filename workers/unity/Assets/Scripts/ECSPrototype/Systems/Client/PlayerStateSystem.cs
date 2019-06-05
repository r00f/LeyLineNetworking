using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;
using Improbable.Gdk.ReactiveComponents;

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

        [Inject] GameStateData m_GameStateData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public ComponentDataArray<PlayerState.Component> PlayerStateData;
        }

        [Inject] PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<SpatialEntityId> EntityIdData;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Health.Component> HealthData;
            public ComponentArray<UnitComponentReferences> UnitCompReferences;
        }

        [Inject] UnitData m_UnitData;

        protected override void OnUpdate()
        {
            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var playerState = m_PlayerData.PlayerStateData[0];
                var playerWorldIndex = m_PlayerData.WorldIndexData[0].Value;
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;
                var gameState = m_GameStateData.GameState[gi];

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        for (int ui = 0; ui < m_UnitData.Length; ui++)
                        {
                            var unitId = m_UnitData.EntityIdData[ui].EntityId.Id;
                            var unitCoord = m_UnitData.CoordinateData[ui].CubeCoordinate;
                            var actions = m_UnitData.ActionsData[ui];
                            var unitComponentReferences = m_UnitData.UnitCompReferences[ui];
                            var mouseState = m_UnitData.MouseStateData[ui];

                            if (playerState.CurrentState != PlayerStateEnum.ready)
                            {
                                if (unitId == playerState.SelectedUnitId)
                                {
                                    if(actions.LockedAction.Index == -3 && actions.CurrentSelected.Index != -3)
                                    {
                                        if(playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                                            SetPlayerState(PlayerStateEnum.waiting_for_target);
                                    }
                                    else
                                    {
                                        if(playerState.CurrentState != PlayerStateEnum.unit_selected)
                                            SetPlayerState(PlayerStateEnum.unit_selected);
                                    }
                                    
                                    if (!unitComponentReferences.SelectionCircleGO.activeSelf)
                                        unitComponentReferences.SelectionCircleGO.SetActive(true);
                                }
                                else if (playerState.CurrentState != PlayerStateEnum.waiting_for_target && mouseState.ClickEvent == 1)
                                {
                                    playerState.SelectedUnitCoordinate = unitCoord;
                                    playerState.SelectedUnitId = unitId;
                                    m_PlayerData.PlayerStateData[0] = playerState;
                                }
                                else
                                {
                                    if (unitComponentReferences.SelectionCircleGO.activeSelf)
                                        unitComponentReferences.SelectionCircleGO.SetActive(false);
                                }
                            }
                            else if(playerState.SelectedUnitId != 0)
                            {
                                playerState.SelectedUnitId = 0;
                                m_PlayerData.PlayerStateData[0] = playerState;
                                if (unitComponentReferences.SelectionCircleGO.activeSelf)
                                    unitComponentReferences.SelectionCircleGO.SetActive(false);
                            }
                        }
                    }
                    else if (gameState.CurrentState == GameStateEnum.calculate_energy)
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


