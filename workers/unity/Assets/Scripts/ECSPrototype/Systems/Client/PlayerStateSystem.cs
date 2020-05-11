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
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(MouseStateSystem))]
    public class PlayerStateSystem : ComponentSystem
    {
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;
        SendActionRequestSystem m_ActionRequestSystem;
        HighlightingSystem m_HighlightingSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>()
            );

            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Vision.Component>(),
            ComponentType.ReadOnly<HighlightingDataComponent>(),
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


        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_ActionRequestSystem = World.GetExistingSystem<SendActionRequestSystem>();
            m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
        }


        protected override void OnUpdate()
        {

            if (m_PlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() == 0)
                return;

            var gameStateData = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

            #region PlayerData
            var playersWorldID = m_PlayerData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playersCamera = m_PlayerData.ToComponentArray<Moba_Camera>();
            var playersState = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            var playerHighs = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
            var playerVisions = m_PlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
            #endregion

            var gameState = gameStateData[0];

            var playerHigh = playerHighs[0];
            var playerVision = playerVisions[0];
            var playerState = playersState[0];
            var playerWorldIndex = playersWorldID[0].Value;
            var playerCam = playersCamera[0];


            if (gameState.CurrentState == GameStateEnum.planning)
            {
                if (playerState.CurrentState != PlayerStateEnum.ready)
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        playerState.CurrentState = PlayerStateEnum.ready;
                    }

                    HashSet<Vector3f> visionCoordsHash = new HashSet<Vector3f>(playerVision.CellsInVisionrange);

                    Entities.With(m_UnitData).ForEach((Entity e, UnitComponentReferences unitComponentReferences, ref SpatialEntityId unitId, ref CubeCoordinate.Component unitCoord, ref Actions.Component actions, ref MouseState mouseState, ref CellsToMark.Component unitCellsToMark) =>
                    {
                        if (unitId.EntityId.Id == playerState.SelectedUnitId)
                        {
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
                                }
                            }
                            else
                            {
                                if (playerState.CurrentState != PlayerStateEnum.unit_selected)
                                {
                                    playerState.CurrentState = PlayerStateEnum.unit_selected;
                                }
                            }
                        }
                        else
                        {

                            if (visionCoordsHash.Contains(unitCoord.CubeCoordinate) && playerState.CurrentState != PlayerStateEnum.waiting_for_target && mouseState.ClickEvent == 1)
                            {
                                playerState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                                playerState.SelectedUnitId = unitId.EntityId.Id;
                            }
                        }
                    });
                }
                else if (playerState.SelectedUnitId != 0)
                {
                    playerState.SelectedUnitId = 0;
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

            #region Dispose
            playersState[0] = playerState;
            m_PlayerData.CopyFromComponentDataArray(playersState);
            gameStateData.Dispose();
            playerVisions.Dispose();
            playersWorldID.Dispose();
            playersState.Dispose();
            playerHighs.Dispose();
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


