using Unity.Entities;
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
            public readonly ComponentDataArray<SpatialEntityId> EntityIdData;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Health.Component> HealthData;
            public ComponentArray<UnitComponentReferences> UnitCompReferences;
        }

        [Inject] private UnitData m_UnitData;

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
                            var actions = m_UnitData.ActionsData[ui];
                            var unitComponentReferences = m_UnitData.UnitCompReferences[ui];
                            var mouseState = m_UnitData.MouseStateData[ui];

                            if (playerState.CurrentState != PlayerStateEnum.ready)
                            {
                                if (unitId == playerState.SelectedUnitId)
                                {
                                    //if selected unit is waiting for target set playerstate to waitingForTarget
                                    if (actions.CurrentSelected.Targets.Count != 0 && actions.LockedAction.Targets.Count == 0)
                                    {
                                        SetPlayerState(PlayerStateEnum.waiting_for_target);
                                    }
                                    else
                                    {
                                        SetPlayerState(PlayerStateEnum.unit_selected);
                                    }

                                    if (!unitComponentReferences.SelectionCircleGO.activeSelf)
                                        unitComponentReferences.SelectionCircleGO.SetActive(true);
                                    /*
                                    else if (!AnyUnitClicked() && playerState.CurrentState != PlayerStateEnum.waiting)
                                    {
                                        SetPlayerState(PlayerStateEnum.waiting);
                                    }
                                    */
                                }
                                else if (playerState.CurrentState != PlayerStateEnum.waiting_for_target && mouseState.ClickEvent == 1)
                                {
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


