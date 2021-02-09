using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(MouseStateSystem))]
    public class PlayerStateSystem : ComponentSystem
    {
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;
        SendActionRequestSystem m_ActionRequestSystem;
        HighlightingSystem m_HighlightingSystem;
        UISystem m_UISystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        public UIReferences UIRef { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>(),
                ComponentType.ReadOnly<WorldIndex.Component>()
            );

            m_PlayerData = GetEntityQuery(
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<Vision.Component>(),
                ComponentType.ReadWrite<HighlightingDataComponent>(),
                ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                ComponentType.ReadWrite<PlayerState.Component>(),
                ComponentType.ReadWrite<Moba_Camera>()
            );


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


        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            UIRef = Object.FindObjectOfType<UIReferences>();
            m_UISystem = World.GetExistingSystem<UISystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_ActionRequestSystem = World.GetExistingSystem<SendActionRequestSystem>();
            m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
        }


        protected override void OnUpdate()
        {
            Entities.With(m_GameStateData).ForEach((ref GameState.Component gState) =>
            {
                var gameState = gState;

                Entities.With(m_PlayerData).ForEach((Moba_Camera playerCam, ref PlayerState.Component playerState, ref HighlightingDataComponent playerHigh, ref Vision.Component playerVision, ref FactionComponent.Component playerFaction) =>
                {
                    if (playerVision.RevealVision)
                    {
                        return;
                    }

                    var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();
                    var ropeEndEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.RopeEndEvent.Event>();

                    if (playerCam.PlayerMapTileInstance)
                    {
                        playerCam.PlayerMapTileInstance.TileRect.anchoredPosition = (new Vector2(gameState.MapCenter.X, gameState.MapCenter.Y) - new Vector2(playerCam.transform.position.x, playerCam.transform.position.z)) * -5.8f;
                        //Equalize rot with player rot
                        Vector3 compassRotation = playerCam.PlayerMapTileInstance.TileRect.eulerAngles;
                        compassRotation.z = -playerCam.transform.eulerAngles.y;
                        playerCam.PlayerMapTileInstance.TileRect.eulerAngles = compassRotation;
                    }

                    if (playerCam.playerFaction != playerFaction.Faction)
                        playerCam.playerFaction = playerFaction.Faction;

                    if (cleanUpStateEvents.Count > 0)
                    {
                        playerHigh.CancelState = false;
                    }

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (ropeEndEvents.Count == 0)
                        {
                            if (playerState.CurrentState != PlayerStateEnum.ready)
                            {
                                if (playerHigh.CancelState)
                                {
                                    if (playerHigh.CancelTime >= 0)
                                        playerHigh.CancelTime -= Time.DeltaTime;
                                    else
                                    {
                                        Debug.Log("SetPlayerReady");
                                        playerState.CurrentState = PlayerStateEnum.ready;
                                        Debug.Log((int) playerState.CurrentState);
                                    }
                                    UIRef.GOButtonAnimator.SetFloat("CancelTimer", 1 - playerHigh.CancelTime / 3);
                                }
                                else
                                {
                                    playerState = UpdateSelectedUnit(playerCam, playerState, playerHigh, playerVision);
                                }
                            }
                            else
                            {
                                if (playerState.SelectedUnitId != 0)
                                    playerState.SelectedUnitId = 0;
                            }
                        }
                        else
                        {
                            m_HighlightingSystem.ResetHighlights();
                            playerState.CurrentState = PlayerStateEnum.ready;
                        }

                    }
                    else
                    {
                        if (playerState.CurrentState != PlayerStateEnum.waiting && gameState.CurrentState != GameStateEnum.interrupt)
                        {
                            if (playerState.UnitTargets.Count != 0)
                            {
                                playerState.UnitTargets.Clear();
                                playerState.UnitTargets = playerState.UnitTargets;
                            }
                            playerState.CurrentState = PlayerStateEnum.waiting;
                        }
                    }

                });
            });
        }

        public PlayerState.Component UpdateSelectedUnit(Moba_Camera playerCam, PlayerState.Component playerState, HighlightingDataComponent playerHigh, Vision.Component playerVision)
        {
            Entities.With(m_UnitData).ForEach((Entity e, UnitComponentReferences unitComponentReferences, ref SpatialEntityId unitId, ref CubeCoordinate.Component unitCoord, ref Actions.Component actions, ref MouseState mouseState, ref CellsToMark.Component unitCellsToMark) =>
            {
                if (unitId.EntityId.Id == playerState.SelectedUnitId)
                {
                    if (!unitComponentReferences.TeamColorMeshesComp.ParticleSystems[0].isPlaying)
                        unitComponentReferences.TeamColorMeshesComp.ParticleSystems[0].Play();

                    if (Vector3fext.ToUnityVector(playerState.SelectedUnitCoordinate) != Vector3fext.ToUnityVector(unitCoord.CubeCoordinate))
                        playerState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;

                    playerCam.SetTargetTransform(unitComponentReferences.transform);

                    if (actions.LockedAction.Index == -3 && actions.CurrentSelected.Index != -3)
                    {
                        if (playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                        {
                            playerState.CurrentState = PlayerStateEnum.waiting_for_target;
                        }
                        
                        //if playerState is WaitingForTarget and rightMouseButton is pressed or we click the same unit
                        if (Input.GetButtonDown("Fire2"))
                        {
                            //Debug.Log("ClearSelectedUnitActions from PlayerStateSys");
                            m_ActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                            //Call methods so line/target gets disabled instantly
                            m_HighlightingSystem.ResetUnitHighLights(e, playerState, unitId.EntityId.Id);
                            playerState.UnitTargets.Remove(unitId.EntityId.Id);
                            m_UISystem.ClearSelectedActionToolTip();
                        }
                        
                    }
                    else if (playerState.CurrentState != PlayerStateEnum.unit_selected && !playerHigh.CancelState)
                    {
                        playerState.CurrentState = PlayerStateEnum.unit_selected;
                    }
                }
                else
                {
                    if (unitComponentReferences.TeamColorMeshesComp.ParticleSystems[0].isPlaying)
                        unitComponentReferences.TeamColorMeshesComp.ParticleSystems[0].Stop();

                    if(mouseState.ClickEvent == 1)
                    {
                        if (playerVision.CellsInVisionrange.ContainsKey(unitCoord.CubeCoordinate) && playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                        {
                            playerState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                            playerState.SelectedUnitId = unitId.EntityId.Id;
                            //Clear ActionTooltip on UnitSelect
                            m_UISystem.ClearSelectedActionToolTip();
                        }
                    }
                }
            });

            return playerState;
        }

        public void ResetInputCoolDown(float coolDown)
        {
            Entities.With(m_PlayerData).ForEach((Entity e, ref HighlightingDataComponent highlightingData) =>
            {
                highlightingData.InputCooldown = coolDown;
            });
        }

        public void SetHoveredCoordinates(Vector3f cubeCoord, Vector3 pos)
        {
            Entities.With(m_PlayerData).ForEach((Entity e, ref HighlightingDataComponent highlightingData) => 
            {
                highlightingData.HoveredCoordinate = cubeCoord;
                highlightingData.HoveredPosition = pos;
            });
        }

        public void ResetCancelTimer(float timeToCancel)
        {
            Entities.With(m_PlayerData).ForEach((ref PlayerState.Component playerState, ref HighlightingDataComponent playerHigh) =>
            {
                if (playerState.CurrentState != PlayerStateEnum.ready)
                {
                    playerHigh.CancelTime = timeToCancel;
                    playerHigh.CancelState = !playerHigh.CancelState;
                }
            });

            /*
                var playerHighs = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
            var playerHigh = playerHighs[0];
            var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            var playerState = playerStates[0];
            playerHighs[0] = playerHigh;
            m_PlayerData.CopyFromComponentDataArray(playerHighs);
            playerHighs.Dispose();
            playerStates.Dispose();
            */
        }

        public void SetPlayerState(PlayerStateEnum state)
        {
            Entities.With(m_PlayerData).ForEach((ref PlayerState.Component playerState) =>
            {
                if (playerState.CurrentState != state && playerState.CurrentState != PlayerStateEnum.ready)
                {
                    playerState.CurrentState = state;
                }
            });

            /*
                #region PlayerData
                var playersWorldID = m_PlayerData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playersState = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            #endregion

            if (playersState.Length == 0)
            {
                playersWorldID.Dispose();
                playersState.Dispose();
                return;
            }
           
            var playerState = playersState[0];
             */



            //Debug.Log("SetPlayerStateMethodCall: " + playerState.CurrentState);

            /*
            playerState.CurrentState = playerState.CurrentState;
            playersState[0] = playerState;
            //Debug.Log("SetPlayerStateMethodCallApplied: " + playersState[0].CurrentState);
            #region PlayerData
            m_PlayerData.CopyFromComponentDataArray(playersState);
            playersWorldID.Dispose();
            playersState.Dispose();
            #endregion
            */
            
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


