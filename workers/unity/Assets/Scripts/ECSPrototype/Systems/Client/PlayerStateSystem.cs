using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Cell;

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
                ComponentType.ReadOnly<GameState.Component>()
            );

            m_PlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Vision.Component>(),
                ComponentType.ReadWrite<HighlightingDataComponent>(),
                ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                ComponentType.ReadWrite<PlayerState.Component>(),
                ComponentType.ReadWrite<Moba_Camera>()
            );

            /*
            m_UnitData = GetEntityQuery(

                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<MouseState>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadWrite<UnitComponentReferences>()
            );
            */
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
            if (m_GameStateData.CalculateEntityCount() != 1 || m_PlayerData.CalculateEntityCount() == 0)
                return inputDeps;

            var gameState = m_GameStateData.GetSingleton<GameState.Component>();

            var playerEntity = m_PlayerData.GetSingletonEntity();
            var playerCam = EntityManager.GetComponentObject<Moba_Camera>(playerEntity);

            var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
            var playerHigh = m_PlayerData.GetSingleton<HighlightingDataComponent>();
            var playerVision = m_PlayerData.GetSingleton<Vision.Component>();
            var playerFaction = m_PlayerData.GetSingleton<FactionComponent.Component>();
            var playerEnergy = m_PlayerData.GetSingleton<PlayerEnergy.Component>();

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
                UpdateSelectedUnit(playerCam, ref playerState, ref playerHigh, playerVision, playerFaction, playerEnergy);

                if (ropeEndEvents.Count == 0)
                {
                    if (playerState.CurrentState != PlayerStateEnum.ready && playerState.CurrentState != PlayerStateEnum.conceded)
                    {
                        if (playerHigh.CancelState)
                        {
                            if (playerState.SelectedUnitId != 0)
                            {
                                playerState.SelectedUnitId = 0;
                            }

                            if (playerHigh.CancelTime >= 0)
                                playerHigh.CancelTime -= Time.DeltaTime;
                            else
                            {
                                playerState.CurrentState = PlayerStateEnum.ready;
                            }
                        }
                        else if (playerHigh.CancelTime < UIRef.TurnStatePnl.CacelGraceTime)
                        {
                            playerHigh.CancelTime += Time.DeltaTime * UIRef.TurnStatePnl.SlidersOpenMultiplier;
                        }
                        UIRef.TurnStatePnl.GOButtonAnimator.SetFloat("CancelTimer", 1 - (playerHigh.CancelTime / UIRef.TurnStatePnl.CacelGraceTime));
                    }
                }
                else
                {
                    playerState.SelectedUnitId = 0;
                    m_HighlightingSystem.ResetHighlightsNoIn();
                    playerState.CurrentState = PlayerStateEnum.ready;
                }
            }
            else if (playerState.CurrentState != PlayerStateEnum.waiting)
            {
                if (playerState.UnitTargets.Count != 0)
                {
                    playerState.UnitTargets.Clear();
                    playerState.UnitTargets = playerState.UnitTargets;
                }

                //reset CancelStae variables to prevent players from getting set to ready instantly when entering planning
                playerHigh.CancelTime = 0.1f;
                playerHigh.CancelState = false;
                playerState.CurrentState = PlayerStateEnum.waiting;
            }

            m_PlayerData.SetSingleton(playerState);
            m_PlayerData.SetSingleton(playerHigh);

            return inputDeps;
        }

        public void UpdateSelectedUnit(Moba_Camera playerCam, ref PlayerState.Component playerState, ref HighlightingDataComponent playerHigh, Vision.Component playerVision, FactionComponent.Component playerFaction, PlayerEnergy.Component playerEnergy)
        {
            var pHigh = playerHigh;
            var pState = playerState;
            var pFact = playerFaction;

            Entities.ForEach((Entity e, UnitComponentReferences unitComponentReferences,  ref SpatialEntityId unitId, ref CubeCoordinate.Component unitCoord, ref MouseState mouseState, in Actions.Component actions, in FactionComponent.Component faction, in ClientActionRequest.Component clientActionRequest) =>
            {
                if (unitId.EntityId.Id == pState.SelectedUnitId)
                {
                    if (Vector3fext.ToUnityVector(pState.SelectedUnitCoordinate) != Vector3fext.ToUnityVector(unitCoord.CubeCoordinate))
                        pState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;

                    playerCam.SetTargetTransform(unitComponentReferences.transform);

                    unitComponentReferences.SelectionCircleGO.SetActive(true);

                    if (clientActionRequest.ActionId != -3 && Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) == Vector3.zero)
                    {
                        if (pState.CurrentState != PlayerStateEnum.waiting_for_target && playerFaction.Faction == faction.Faction)
                        {
                            pState.CurrentState = PlayerStateEnum.waiting_for_target;
                        }
                        
                        //if playerState is WaitingForTarget and rightMouseButton is pressed or we click the same unit
                        if (Input.GetButtonDown("Fire2"))
                        {
                            m_ActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                            m_HighlightingSystem.ResetUnitHighLights(e, ref pState, unitId.EntityId.Id);
                        }
                    }
                    else if (pState.CurrentState != PlayerStateEnum.unit_selected && !pHigh.CancelState)
                    {
                        pState.CurrentState = PlayerStateEnum.unit_selected;
                    }
                }
                else
                {
                    unitComponentReferences.SelectionCircleGO.SetActive(false);

                    if(pHigh.CancelState && clientActionRequest.ActionId != -3 && Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) == Vector3.zero)
                    {
                        m_ActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                        m_HighlightingSystem.ResetUnitHighLights(e, ref pState, unitId.EntityId.Id);
                    }

                    if (!pHigh.CancelState && mouseState.ClickEvent == 1)
                    {
                        if ((EntityManager.HasComponent<Manalith.Component>(e) || playerVision.CellsInVisionrange.Contains(CellGridMethods.CubeToAxial(unitCoord.CubeCoordinate))) && pState.CurrentState != PlayerStateEnum.waiting_for_target)
                        {
                            if (pState.CurrentState != PlayerStateEnum.unit_selected && !pHigh.CancelState)
                            {
                                pState.CurrentState = PlayerStateEnum.unit_selected;
                            }
                            //Debug.Log("Player Selects A New Unit with id: " + unitId.EntityId.Id);
                            m_UISystem.FillUnitButtons(actions, unitComponentReferences.BaseDataSetComp, faction.Faction, playerFaction.Faction, unitId.EntityId.Id, playerEnergy);
                            m_UISystem.SetPortraitInfo(unitComponentReferences.BaseDataSetComp, faction.Faction, unitComponentReferences.AnimPortraitComp.PortraitClips, unitComponentReferences.TeamColorMeshesComp.color, true);

                            if(!EntityManager.HasComponent<Manalith.Component>(e))
                            {
                                m_UISystem.UpdatePortraitHealthText(playerFaction.Faction, faction.Faction, EntityManager.GetComponentData<Health.Component>(e));
                            }
                            else
                            {
                                m_UISystem.PopulateManlithInfoHexes((uint)unitId.EntityId.Id, playerFaction.Faction);
                            }

                            pState.SelectedActionId = -3;

                            pState.SelectedAction = new Action
                            {
                                Targets = new List<ActionTarget>(),
                                Effects = new List<ActionEffect>(),
                                Index = -3
                            };

                            pHigh.SelectedUnitFaction = faction.Faction;
                            pState.SelectedUnitCoordinate = unitCoord.CubeCoordinate;
                            pState.SelectedUnitId = unitId.EntityId.Id;
                        }
                    }
                }
            })
            .WithoutBurst()
            .Run();

            playerState = pState;
            playerHigh = pHigh;
        }

        public void ResetInputCoolDown(float coolDown)
        {
            Entities.ForEach((ref HighlightingDataComponent highlightingData) =>
            {
                highlightingData.InputCooldown = coolDown;
            })
            .WithoutBurst()
            .Run();
        }

        public void SetHoveredCoordinates(Vector3f cubeCoord, Vector3 pos)
        {
            Entities.ForEach((ref HighlightingDataComponent highlightingData) => 
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
            var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();

            if (playerState.CurrentState != state && playerState.CurrentState != PlayerStateEnum.ready)
            {
                playerState.CurrentState = state;

                if (state == PlayerStateEnum.waiting)
                {
                    playerState.SelectedUnitId = 0;
                    playerState.SelectedUnitCoordinate = new Vector3f(999, 999, 999);
                }
            }

            m_PlayerData.SetSingleton(playerState);
        }
    
        private bool AnyUnitClicked()
        {
            bool b = false;

            Entities.ForEach((ref MouseState mouseState) =>
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


