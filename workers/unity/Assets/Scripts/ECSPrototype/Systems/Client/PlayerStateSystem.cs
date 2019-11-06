using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;
using UnityEngine;
using Unity.Collections;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(MouseStateSystem))]
    public class PlayerStateSystem : ComponentSystem
    {
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>()
            );

            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<PlayerState.ComponentAuthority>(),
            ComponentType.ReadWrite<PlayerState.Component>(),
            ComponentType.ReadWrite<Moba_Camera>()
            );

            m_PlayerData.SetFilter(PlayerState.ComponentAuthority.Authoritative);

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<MouseState>(),
                ComponentType.ReadOnly<Health.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadWrite<UnitComponentReferences>()
            );

        }

        

        protected override void OnUpdate()
        {


            if (m_PlayerData.CalculateEntityCount() == 0)
                return;

            #region PlayerData
            var playersWorldID = m_PlayerData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playersCamera = m_PlayerData.ToComponentArray<Moba_Camera>();
            var playersState = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            #endregion

            var playerState = playersState[0];
            var playerWorldIndex = playersWorldID[0].Value;
            var playerCam = playersCamera[0];

            Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
            {
                var gWi = gameStateWorldIndex.Value;
                var g = gameState;

                Entities.With(m_UnitData).ForEach((UnitComponentReferences unitComponentReferences, ref SpatialEntityId unitId, ref CubeCoordinate.Component unitCoord, ref Actions.Component actions, ref MouseState mouseState) =>
                {
                    if (playerWorldIndex == gWi)
                    {
                        if (g.CurrentState == GameStateEnum.planning)
                        {
                            if (playerState.CurrentState != PlayerStateEnum.ready)
                            {
                                if (unitId.EntityId.Id == playerState.SelectedUnitId)
                                {
                                    playerCam.SetTargetTransform(unitComponentReferences.transform);

                                    if (actions.LockedAction.Index == -3 && actions.CurrentSelected.Index != -3)
                                    {
                                        if (playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                                        {
                                            playerState.CurrentState = PlayerStateEnum.waiting_for_target;
                                        }
                                    }
                                    else
                                    {
                                        if (playerState.CurrentState != PlayerStateEnum.unit_selected)
                                        {
                                            playerState.CurrentState = PlayerStateEnum.unit_selected;
                                        }
                                    }

                                    if (!unitComponentReferences.SelectionCircleGO.activeSelf)
                                        unitComponentReferences.SelectionCircleGO.SetActive(true);

                                }
                                else
                                {
                                    unitComponentReferences.SelectionCircleGO.SetActive(false);
                                    if (playerState.CurrentState != PlayerStateEnum.waiting_for_target && mouseState.ClickEvent == 1)
                                    {
                                        playerState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                                        playerState.SelectedUnitId = unitId.EntityId.Id;
                                    }
                                }
                            }
                            else if (playerState.SelectedUnitId != 0)
                            {
                                
                                playerState.SelectedUnitId = 0;
                            }
                        }
                        else
                        {
                            unitComponentReferences.SelectionCircleGO.SetActive(false);
                            if (playerState.CurrentState != PlayerStateEnum.waiting)
                            {
                                if (playerState.UnitTargets.Count != 0)
                                {
                                    playerState.UnitTargets.Clear();
                                    playerState.UnitTargets = playerState.UnitTargets;
                                }
                                playerState.CurrentState = PlayerStateEnum.waiting;
                            }
                        }
                    }
                });
            });

            #region PlayerData
            playersState[0] = playerState;
            m_PlayerData.CopyFromComponentDataArray(playersState);
            playersWorldID.Dispose();
            playersState.Dispose();
            #endregion
        }
        

        public void SetPlayerState(PlayerStateEnum state)
        {
            //Debug.Log("SetPlayerStateMethodCall");
            #region PlayerData
            var playersWorldID = m_PlayerData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playersState = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            #endregion

            if (playersState.Length == 0)
            {
                return;
            }

            var playerState = playersState[0];
            
            if(playerState.CurrentState != state)
            {
                playerState.CurrentState = state;
            }

            playersState[0] = playerState;
            #region PlayerData
            m_PlayerData.CopyFromComponentDataArray(playersState);
            playersWorldID.Dispose();
            playersState.Dispose();
            #endregion
            
    }
    
        private bool AnyUnitClicked()
        {
            bool b = false;

            Entities.With(m_UnitData).ForEach((ref MouseState mouseState) =>
            {
                if (mouseState.CurrentState == MouseState.State.Clicked)
                    b = true;
            });

            return b;
        }
    }
}


