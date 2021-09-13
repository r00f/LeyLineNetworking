using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(MouseStateSystem))]
    public class PlayerStateSystem : JobComponentSystem
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
                //ComponentType.ReadOnly<Health.Component>(),
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


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_GameStateData.CalculateEntityCount() == 0)
                return inputDeps;

            var gameState = GetSingleton<GameState.Component>();

            Entities.WithStoreEntityQueryInField(ref m_PlayerData).ForEach((Moba_Camera playerCam, ref PlayerState.Component playerState, ref HighlightingDataComponent playerHigh, ref Vision.Component playerVision, ref FactionComponent.Component playerFaction, ref PlayerEnergy.Component playerEnergy) =>
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
                                    //Debug.Log("SetPlayerReady");
                                    playerState.CurrentState = PlayerStateEnum.ready;
                                    //Debug.Log((int) playerState.CurrentState);
                                }
                            }
                            else
                            {
                                playerState = UpdateSelectedUnit(playerCam, playerState, playerHigh, playerVision, playerFaction, playerEnergy);

                                if (playerHigh.CancelTime < UIRef.TurnStatePnl.CacelGraceTime)
                                    playerHigh.CancelTime += Time.DeltaTime * UIRef.TurnStatePnl.SlidersOpenMultiplier;
                            }

                            UIRef.TurnStatePnl.GOButtonAnimator.SetFloat("CancelTimer", 1 - (playerHigh.CancelTime / UIRef.TurnStatePnl.CacelGraceTime));
                        }
                        else
                        {
                            if (playerState.SelectedUnitId != 0)
                                playerState.SelectedUnitId = 0;
                        }
                    }
                    else
                    {
                        m_HighlightingSystem.ResetHighlightsNoIn();
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
            })
            .WithoutBurst()
            .Run();

            //gameStates.Dispose();

            return inputDeps;
        }

        public PlayerState.Component UpdateSelectedUnit(Moba_Camera playerCam, PlayerState.Component playerState, HighlightingDataComponent playerHigh, Vision.Component playerVision, FactionComponent.Component playerFaction, PlayerEnergy.Component playerEnergy)
        {
            Entities.WithStoreEntityQueryInField(ref m_UnitData).ForEach((Entity e, UnitComponentReferences unitComponentReferences, ref FactionComponent.Component faction, ref SpatialEntityId unitId, ref CubeCoordinate.Component unitCoord, ref Actions.Component actions, ref MouseState mouseState) =>
            {
                if (unitId.EntityId.Id == playerState.SelectedUnitId)
                {
                    if (Vector3fext.ToUnityVector(playerState.SelectedUnitCoordinate) != Vector3fext.ToUnityVector(unitCoord.CubeCoordinate))
                        playerState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;

                    playerCam.SetTargetTransform(unitComponentReferences.transform);

                    unitComponentReferences.SelectionCircleGO.SetActive(true);

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
                            m_HighlightingSystem.ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id);
                            //m_UISystem.FillUnitButtons(actions, unitComponentReferences.BaseDataSetComp, faction.Faction, playerFaction.Faction, unitId.EntityId.Id, playerEnergy);
                            //playerState.UnitTargets.Remove(unitId.EntityId.Id);
                            //m_UISystem.ClearSelectedActionToolTip();
                        }
                    }
                    else if (playerState.CurrentState != PlayerStateEnum.unit_selected && !playerHigh.CancelState)
                    {
                        playerState.CurrentState = PlayerStateEnum.unit_selected;
                    }
                }
                else
                {
                    unitComponentReferences.SelectionCircleGO.SetActive(false);

                    if (mouseState.ClickEvent == 1)
                    {
                        if ((EntityManager.HasComponent<ManalithUnit.Component>(e) || playerVision.CellsInVisionrange.ContainsKey(unitCoord.CubeCoordinate)) && playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                        {
                            if (playerState.CurrentState != PlayerStateEnum.unit_selected && !playerHigh.CancelState)
                            {
                                playerState.CurrentState = PlayerStateEnum.unit_selected;
                            }
                            //Debug.Log("Player Selects A New Unit with id: " + unitId.EntityId.Id);
                            m_UISystem.FillUnitButtons(actions, unitComponentReferences.BaseDataSetComp, faction.Faction, playerFaction.Faction, unitId.EntityId.Id, playerEnergy);
                            //m_UISystem.InitializeSelectedActionTooltip(actions.LockedAction.Index, unitComponentReferences.BaseDataSetComp.Actions.Count);

                            playerState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                            playerState.SelectedUnitId = unitId.EntityId.Id;

                        }
                    }
                }


            })
            .WithoutBurst()
            .Run();

            return playerState;
        }

        public void ResetInputCoolDown(float coolDown)
        {
            Entities.WithStoreEntityQueryInField(ref m_PlayerData).ForEach((Entity e, ref HighlightingDataComponent highlightingData) =>
            {
                highlightingData.InputCooldown = coolDown;
            })
            .WithoutBurst()
            .Run();
        }

        public void SetHoveredCoordinates(Vector3f cubeCoord, Vector3 pos)
        {
            Entities.WithStoreEntityQueryInField(ref m_PlayerData).ForEach((Entity e, ref HighlightingDataComponent highlightingData) => 
            {
                highlightingData.HoveredCoordinate = cubeCoord;
                highlightingData.HoveredPosition = pos;
            })
            .WithoutBurst()
            .Run();
        }

        public void ResetCancelTimer(float timeToCancel)
        {
            var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
            var playerHigh = m_PlayerData.GetSingleton<HighlightingDataComponent>();

            if (playerState.CurrentState != PlayerStateEnum.ready)
            {
                if (!playerHigh.CancelState)
                    playerHigh.CancelTime = timeToCancel;
                playerHigh.CancelState = !playerHigh.CancelState;
            }

            m_PlayerData.SetSingleton(playerHigh);
        }

        public void SetPlayerState(PlayerStateEnum state)
        {
            Entities.WithStoreEntityQueryInField(ref m_PlayerData).ForEach((ref PlayerState.Component playerState) =>
            {
                if (playerState.CurrentState != state && playerState.CurrentState != PlayerStateEnum.ready)
                {
                    playerState.CurrentState = state;

                    if (state == PlayerStateEnum.waiting)
                    {
                        playerState.SelectedUnitId = 0;
                        playerState.SelectedUnitCoordinate = new Vector3f(999, 999, 999);
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }
    
        private bool AnyUnitClicked()
        {
            bool b = false;

            Entities.WithStoreEntityQueryInField(ref m_UnitData).ForEach((ref MouseState mouseState) =>
            {
                if (mouseState.CurrentState == MouseState.State.Clicked)
                    b = true;
            })
            .WithoutBurst()
            .Run();

            return b;
        }
    }
}


