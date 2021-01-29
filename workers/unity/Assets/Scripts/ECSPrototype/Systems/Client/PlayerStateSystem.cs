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

            if (m_PlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() == 0)
                return;

            var gameStateData = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

            /*
            #region PlayerData
            var playersWorldID = m_PlayerData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playersCamera = m_PlayerData.ToComponentArray<Moba_Camera>();
            var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            var playerHighs = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
            var playerVisions = m_PlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
            var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            #endregion
            

            var playerFact = playerFactions[0];
            var playerHigh = playerHighs[0];
            var playerVision = playerVisions[0];
            var playerState = playerStates[0];
            var playerWorldIndex = playersWorldID[0].Value;
            var playerCam = playersCamera[0];
            */

            var gameState = gameStateData[0];

            Entities.With(m_PlayerData).ForEach((Moba_Camera playerCam, ref PlayerState.Component playerState, ref HighlightingDataComponent playerHigh, ref Vision.Component playerVision, ref FactionComponent.Component playerFaction) =>
            {
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
                                HashSet<Vector3f> visionCoordsHash = new HashSet<Vector3f>(playerVision.CellsInVisionrange);

                                var pS = playerState;
                                var pH = playerHigh;
                                
                                Entities.With(m_UnitData).ForEach((Entity e, UnitComponentReferences unitComponentReferences, ref SpatialEntityId unitId, ref CubeCoordinate.Component unitCoord, ref Actions.Component actions, ref MouseState mouseState, ref CellsToMark.Component unitCellsToMark) =>
                                {
                                    var teamColorMeshes = EntityManager.GetComponentObject<TeamColorMeshes>(e);

                                    if (unitId.EntityId.Id == pS.SelectedUnitId)
                                    {
                                        foreach (ParticleSystem p in teamColorMeshes.ParticleSystems)
                                        {
                                            if (!p.isPlaying)
                                                p.Play();
                                        }

                                        if (Vector3fext.ToUnityVector(pS.SelectedUnitCoordinate) != Vector3fext.ToUnityVector(unitCoord.CubeCoordinate))
                                            pS.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                                            
                                            playerCam.SetTargetTransform(unitComponentReferences.transform);

                                        if (actions.LockedAction.Index == -3 && actions.CurrentSelected.Index != -3)
                                        {
                                            if (pS.CurrentState != PlayerStateEnum.waiting_for_target)
                                            {
                                                pS.CurrentState = PlayerStateEnum.waiting_for_target;
                                            }
                                            //if playerState is WaitingForTarget and rightMouseButton is pressed or we click the same unit
                                            if (Input.GetButtonDown("Fire2"))
                                            {
                                                //Debug.Log("ClearSelectedUnitActions from PlayerStateSys");
                                                m_ActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                                                //Call methods so line/target gets disabled instantly
                                                m_HighlightingSystem.ResetUnitHighLights(e, pS, unitId.EntityId.Id);
                                                pS.UnitTargets.Remove(unitId.EntityId.Id);
                                                m_UISystem.ClearSelectedActionToolTip();
                                            }
                                        }
                                        else if (pS.CurrentState != PlayerStateEnum.unit_selected && !pH.CancelState)
                                        {
                                            pS.CurrentState = PlayerStateEnum.unit_selected;
                                        }
                                    }
                                    else
                                    {
                                        foreach (ParticleSystem p in teamColorMeshes.ParticleSystems)
                                        {
                                            if (p.isPlaying)
                                                p.Stop();
                                        }
                                        if (visionCoordsHash.Contains(unitCoord.CubeCoordinate) && pS.CurrentState != PlayerStateEnum.waiting_for_target && mouseState.ClickEvent == 1)
                                        {
                                            pS.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                                            pS.SelectedUnitId = unitId.EntityId.Id;
                                            //Clear ActionTooltip on UnitSelect
                                            m_UISystem.ClearSelectedActionToolTip();
                                        }
                                    }
                                });

                                playerState = pS;
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

            #region Dispose
            //playerHighs[0] = playerHigh;
            //m_PlayerData.CopyFromComponentDataArray(playerHighs);
            //playerStates[0] = playerState;
            //m_PlayerData.CopyFromComponentDataArray(playerStates);
            //Debug.Log(playerStates[0].CurrentState);
            gameStateData.Dispose();
            //playerVisions.Dispose();
            //playersWorldID.Dispose();
            //playerStates.Dispose();
            //playerHighs.Dispose();
            //playerFactions.Dispose();
            #endregion
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
            var playerHighs = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
            var playerHigh = playerHighs[0];
            var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            var playerState = playerStates[0];

            if(playerState.CurrentState != PlayerStateEnum.ready)
            {
                playerHigh.CancelTime = timeToCancel;
                playerHigh.CancelState = !playerHigh.CancelState;
            }

            playerHighs[0] = playerHigh;
            m_PlayerData.CopyFromComponentDataArray(playerHighs);
            playerHighs.Dispose();
            playerStates.Dispose();
        }

        public void SetPlayerState(PlayerStateEnum state)
        {

            #region PlayerData
            var playersWorldID = m_PlayerData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playersState = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            #endregion

            if (playersState.Length == 0)
            {
                return;
            }
            var playerState = playersState[0];
            
            if(playerState.CurrentState != state && playerState.CurrentState != PlayerStateEnum.ready)
            {
                playerState.CurrentState = state;
            }

            //Debug.Log("SetPlayerStateMethodCall: " + playerState.CurrentState);

            playerState.CurrentState = playerState.CurrentState;
            playersState[0] = playerState;
            //Debug.Log("SetPlayerStateMethodCallApplied: " + playersState[0].CurrentState);
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


