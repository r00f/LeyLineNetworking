using FMODUnity;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Cell;
using Improbable;
using Unity.Jobs;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Restricted;
using Improbable.Gdk.Core.Commands;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UISystem : JobComponentSystem
    {
        //EventSystem m_EventSystem;
        ILogDispatcher logger;
        HighlightingSystem m_HighlightingSystem;
        PlayerStateSystem m_PlayerStateSystem;
        SendActionRequestSystem m_SendActionRequestSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        CommandSystem m_CommandSystem;
        WorkerSystem m_workerSystem;
        EntityQuery m_PlayerData;
        EntityQuery m_AuthoritativePlayerData;
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        EntityQuery m_ManalithUnitData;
        EntityQuery m_ManalithData;
        EntityQuery m_WorkerConnectorData;


        public UIReferences UIRef { get; set; }

        Settings settings;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<Energy.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<UnitDataSet>(),
                ComponentType.ReadOnly<MouseState>(),
                ComponentType.ReadOnly<IsVisible>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadOnly<AnimatedPortraitReference>(),
                ComponentType.ReadWrite<UnitComponentReferences>(),
                ComponentType.ReadWrite<UnitHeadUIReferences>(),
                ComponentType.ReadWrite<UnitEffects>()
            );

            m_ManalithUnitData = GetEntityQuery(
                ComponentType.ReadOnly<Manalith.Component>(),
                ComponentType.ReadOnly<Energy.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<UnitDataSet>(),
                ComponentType.ReadOnly<MouseState>(),
                ComponentType.ReadOnly<IsVisible>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadOnly<AnimatedPortraitReference>(),
                ComponentType.ReadWrite<UnitComponentReferences>(),
                ComponentType.ReadWrite<UnitHeadUIReferences>(),
                ComponentType.ReadWrite<UnitEffects>()

            );

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );

            m_AuthoritativePlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<HighlightingDataComponent>(),
                ComponentType.ReadWrite<Moba_Camera>(),
                ComponentType.ReadWrite<PlayerState.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );


            m_PlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<PlayerState.Component>()
                );
            m_ManalithData = GetEntityQuery(
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Manalith.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
                );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
            m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
            m_workerSystem = World.GetExistingSystem<WorkerSystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CommandSystem = World.GetExistingSystem<CommandSystem>();
            m_PlayerStateSystem = World.GetExistingSystem<PlayerStateSystem>();
            m_SendActionRequestSystem = World.GetExistingSystem<SendActionRequestSystem>();
            settings = Resources.Load<Settings>("Settings");

            UIRef = Object.FindObjectOfType<UIReferences>();
            UIRef.MasterBus = RuntimeManager.GetBus(UIRef.MasterBusString);
            UIRef.IngameSFXBus = RuntimeManager.GetBus(UIRef.InGameSFXBusString);
            UIRef.EnvironmentBus = RuntimeManager.GetBus(UIRef.EnvironmentString);
            UIRef.SFXBus = RuntimeManager.GetBus(UIRef.SFXBusString);
            UIRef.MusicBus = RuntimeManager.GetBus(UIRef.MusicBusString);
            UIRef.UINonMapSFXBus = RuntimeManager.GetBus(UIRef.UINonMapSFXBusString);
            AddSettingsListeners();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            /*
            var deleteEntityResponses = m_CommandSystem.GetResponses<PlayerEnergy.PlayerDeletion.ReceivedResponse>();

            for (int i = 0; i < deleteEntityResponses.Count; i++)
            {
                Worlds.DefaultWorld.DestroyAndResetAllEntities();
                SceneManager.LoadScene("MainMenu");
            }
            */

            if (m_GameStateData.CalculateEntityCount() != 1 || m_AuthoritativePlayerData.CalculateEntityCount() == 0)
                return inputDeps;

            #region GetData
            var authPlayerEntity = m_AuthoritativePlayerData.GetSingletonEntity();
            var authPlayerCam = EntityManager.GetComponentObject<Moba_Camera>(authPlayerEntity);
            var authPlayerFaction = m_AuthoritativePlayerData.GetSingleton<FactionComponent.Component>();
            var authPlayerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
            var authPlayerEnergy = m_AuthoritativePlayerData.GetSingleton<PlayerEnergy.Component>();
            var authPlayerHigh = m_AuthoritativePlayerData.GetSingleton<HighlightingDataComponent>();

            var gameStateId = m_GameStateData.GetSingleton<SpatialEntityId>();
            var gameState = m_GameStateData.GetSingleton<GameState.Component>();
            var gameStatePosition = m_GameStateData.GetSingleton<Position.Component>();

            #endregion

            var mapInitializedEvent = m_ComponentUpdateSystem.GetEventsReceived<ClientWorkerIds.MapInitializedEvent.Event>();
            var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();
            var energyChangeEvents = m_ComponentUpdateSystem.GetEventsReceived<PlayerEnergy.EnergyChangeEvent.Event>();




            for (int i = 0; i < mapInitializedEvent.Count; i++)
            {
                if (mapInitializedEvent[i].EntityId.Id == gameStateId.EntityId.Id)
                {
                    Debug.Log("InitMapClientEvent");
                    //ClearUnitUIElements();
                    InitializeButtons();

                    UIRef.MinimapComponent.MapCenter += new Vector3((float) gameStatePosition.Coords.X, (float) gameStatePosition.Coords.Y, (float) gameStatePosition.Coords.Z);
                    UIRef.BigMapComponent.MapCenter += new Vector3((float) gameStatePosition.Coords.X, (float) gameStatePosition.Coords.Y, (float) gameStatePosition.Coords.Z);

                    UIRef.FriendlyIncomeColor = settings.FactionIncomeColors[(int) authPlayerFaction.Faction];
                    UIRef.FriendlyColor = settings.FactionColors[(int) authPlayerFaction.Faction];

                    if (authPlayerFaction.Faction == 1)
                        UIRef.EnemyColor = settings.FactionColors[2];
                    else
                        UIRef.EnemyColor = settings.FactionColors[1];

                    UIRef.HeroEnergyBar.HeroEnergyIncomeFill.color = UIRef.FriendlyIncomeColor;
                    UIRef.TotalEnergyIncomeText.color = UIRef.FriendlyIncomeColor;

                    UIRef.HeroPortraitPlayerColor.color = UIRef.FriendlyColor;

                    UIRef.HeroEnergyBar.HeroCurrentEnergyFill.color = UIRef.FriendlyColor;
                    UIRef.TopEnergyFill.color = UIRef.FriendlyColor;

                    UIRef.TurnStatePnl.FriendlyReadyDot.color = UIRef.FriendlyColor;

                    UIRef.EnergyConnectorPlayerColorFill.color = UIRef.FriendlyColor;

                    UIRef.TurnStatePnl.EnemyReadyDot.color = UIRef.EnemyColor;

                    foreach (Rope r in UIRef.Ropes)
                    {
                        r.FriendlyRope.color = UIRef.FriendlyColor;
                        r.EnemyRope.color = UIRef.EnemyColor;

                        var fBurst = r.FriendlyReadyBurstPS.main;
                        fBurst.startColor = UIRef.FriendlyColor;
                        var eBurst = r.EnemyReadyBurstPS.main;
                        eBurst.startColor = UIRef.EnemyColor;

                        var main1 = r.FriendlyRopeBarParticle.LoopPS.main;
                        main1.startColor = UIRef.FriendlyColor;

                        var main2 = r.EnemyRopeBarParticle.LoopPS.main;
                        main2.startColor = UIRef.EnemyColor;
                    }

                    UIRef.TurnStatePnl.GOButtonScript.LightCircle.color = UIRef.FriendlyColor;
                    UIRef.TurnStatePnl.GOButtonScript.LightFlare.color = UIRef.FriendlyColor;
                    UIRef.TurnStatePnl.GOButtonScript.LightInner.color = UIRef.FriendlyColor;


                    if (!UIRef.MatchReadyPanel.activeSelf)
                    {
                        UIRef.MatchReadyPanel.SetActive(true);
                    }
                }
            }

            if (energyChangeEvents.Count > 0)
            {
                //Only convert energy numbers to string when energy change event is fired
                UIRef.TurnDisplay.EnergyGained = authPlayerEnergy.LastGained;
                UIRef.CurrentEnergyText.text = authPlayerEnergy.Energy.ToString();
                UIRef.MaxEnergyText.text = authPlayerEnergy.MaxEnergy.ToString();
            }

            if (cleanUpStateEvents.Count > 0)
            {
                Entities.ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Energy.Component energy, in FactionComponent.Component faction, in Health.Component health) =>
                {
                    //var energy = EntityManager.GetComponentData<Energy.Component>(e);
                    var stats = EntityManager.GetComponentObject<UnitDataSet>(e);

                    if (stats.SelectUnitButtonInstance)
                    {
                        UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);
                    }

                    if (unitHeadUIRef.UnitHeadHealthBarInstance)
                        ResetHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, health);
                })
                .WithoutBurst()
                .Run();

                Entities.ForEach((UnitHeadUIReferences unitHeadUIRef, ref Manalith.Component m, in FactionComponent.Component faction) =>
                {
                    unitHeadUIRef.UnitHeadUIInstance.EnergyGainText.color = settings.FactionIncomeColors[(int) faction.Faction];
                    unitHeadUIRef.UnitHeadUIInstance.EnergyGainImage.color = settings.FactionIncomeColors[(int) faction.Faction];
                })
                .WithoutBurst()
                .Run();

                foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                {
                    g.CleanupReset = true;
                }

                UIRef.ActionPanel.SetActive(true);

                UIRef.GameOverPanel.SetActive(false);

                UIRef.TurnStatePnl.FriendlyReadyDot.enabled = false;
                UIRef.TurnStatePnl.EnemyReadyDot.enabled = false;



                foreach (Rope r in UIRef.Ropes)
                {
                    r.RopeSlamOneTime = false;
                    r.RopeEndsLerpTime = 0;
                    r.FriendlyRope.fillAmount = 0;
                    r.EnemyRope.fillAmount = 0;
                    r.EnemyRope.color = UIRef.EnemyColor;
                    r.FriendlyRope.color = UIRef.FriendlyColor;
                }

                UIRef.TurnStatePnl.FriendlyReadyDot.color = UIRef.FriendlyColor;
                UIRef.TurnStatePnl.EnemyReadyDot.color = UIRef.EnemyColor;

                //UIRef.TotalEnergyIncomeText.text = "+" + playerEnergy.Income.ToString();
            }

            if (gameState.CurrentState != GameStateEnum.waiting_for_players)
            {
                if (UIRef.StartUpWaitTime > 0)
                {
                    UIRef.StartUpWaitTime -= Time.DeltaTime;
                }
                else
                {
                    UIRef.StartupPanel.SetActive(false);
                }
            }

            if (UIRef.dollyCam && UIRef.dollyCam.RevealVisionTrigger)
            {
                m_SendActionRequestSystem.RevealPlayerVision();
                UIRef.dollyCam.RevealVisionTrigger = false;
            }

            HandleTutorialVideoPlayers();

            UnitLoop(authPlayerState, authPlayerFaction.Faction, gameState, authPlayerEnergy, authPlayerHigh, authPlayerCam);

            ManalithUnitLoop(authPlayerState, authPlayerFaction.Faction, gameState, authPlayerEnergy, authPlayerHigh);

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetBool("Planning", false);
                UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetInteger("TurnStep", (int) gameState.CurrentState - 2);

                if (UIRef.TurnStatePnl.GOButtonScript.Button.interactable)
                {
                    UIRef.TurnStatePnl.GOButtonScript.Button.interactable = false;
                }

                UIRef.RopeLoopEmitter.Stop();
            }
            else
            {
                UpdateActionHoverTooltip();
                UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetBool("Planning", true);

                var hoveredTurnstepIndex = -1;
                for (int i = 0; i < UIRef.TurnStatePnl.TurnStepHoveredHandlers.Count; i++)
                {
                    if (UIRef.TurnStatePnl.TurnStepHoveredHandlers[i].Hovered)
                    {
                        hoveredTurnstepIndex = i;
                        UIRef.TurnStatePnl.TurnStepHoveredHandlers[i].HoverPanel.SetActive(true);
                    }
                    else
                        UIRef.TurnStatePnl.TurnStepHoveredHandlers[i].HoverPanel.SetActive(false);
                }

                if (hoveredTurnstepIndex != -1)
                    UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetInteger("TurnStep", hoveredTurnstepIndex + 1);
                else
                    UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetInteger("TurnStep", 0);


                if (authPlayerState.CurrentState == PlayerStateEnum.ready || UIRef.TurnStatePnl.GOButtonScript.RotatingBack)
                {
                    UIRef.TurnStatePnl.GOButtonScript.Button.interactable = false;
                }
                else
                {
                    UIRef.TurnStatePnl.GOButtonScript.Button.interactable = true;
                }

                if (UIRef.HeroEnergyBar.HeroCurrentEnergyFill.fillAmount >= (float) authPlayerEnergy.Energy / authPlayerEnergy.MaxEnergy * UIRef.HeroEnergyBar.MaxFillAmount - .003f)
                {
                    UIRef.HeroEnergyBar.HeroEnergyIncomeFill.fillAmount = Mathf.Lerp(UIRef.HeroEnergyBar.HeroEnergyIncomeFill.fillAmount, Mathf.Clamp(authPlayerEnergy.Energy + authPlayerEnergy.Income, 0, authPlayerEnergy.MaxEnergy) / (float) authPlayerEnergy.MaxEnergy * UIRef.HeroEnergyBar.MaxFillAmount, Time.DeltaTime);
                    //FillBarToDesiredValue(UIRef.HeroEnergyIncomeFill, Mathf.Clamp(playerEnergy.Energy + playerEnergy.Income, 0, playerEnergy.MaxEnergy) / (float) playerEnergy.MaxEnergy * UIRef.MaxFillAmount, UIRef.EnergyLerpSpeed);
                }
            }

            UIRef.TurnDisplay.StepIsActive = gameState.TurnStateIsActive;

            switch (gameState.CurrentState)
            {
                case GameStateEnum.planning:
                    /*
                    logger.HandleLog(LogType.Warning,
                    new LogEvent("Lerp down Planning Text")
                    .WithField("CurrentStepId", UIRef.TurnDisplay.CurrentStepID)
                    .WithField("timer", UIRef.TurnDisplay.timer)
                    );
                    */
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.planning && UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.readyFired && UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.enemyReadyFired)
                    {
                        if (UIRef.EscapeMenu.TurnOverrideInputField.text == "")
                            FireStepChangedEffects("Turn " + gameState.TurnCounter, settings.TurnStepColors[0], UIRef.PlanningSlideInPath, 0);
                        else
                            FireStepChangedEffects("Turn " + UIRef.EscapeMenu.TurnOverrideInputField.text, settings.TurnStepColors[0], UIRef.PlanningSlideInPath, 0);

                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.planning;
                    }
                    foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                    {
                        UpdateUnitGroupBauble(g, authPlayerFaction, g.transform.GetSiblingIndex());
                    }
                    break;
                case GameStateEnum.interrupt:
                    if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.readyFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 2], UIRef.ExecuteStepChangePath, (uint) gameState.CurrentState - 2);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.interruptFired;
                    }
                    break;
                case GameStateEnum.attack:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.attackFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 2], UIRef.ExecuteStepChangePath, (uint) gameState.CurrentState - 2);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.attackFired;
                    }
                    break;
                case GameStateEnum.move:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.moveFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 2], UIRef.ExecuteStepChangePath, (uint) gameState.CurrentState - 2);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.moveFired;
                    }
                    break;
                case GameStateEnum.skillshot:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.skillshotFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 2], UIRef.ExecuteStepChangePath, (uint) gameState.CurrentState - 2);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.skillshotFired;
                    }
                    break;
                case GameStateEnum.game_over:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.gameOverFired)
                    {
                        if (gameState.WinnerFaction == 0)
                        {
                            //TODO: ADD GAMEOVER SOUND EFFECTS
                            FireStepChangedEffects("Draw", settings.FactionColors[0], UIRef.ExecuteStepChangePath, 5);
                        }
                        else if (gameState.WinnerFaction == authPlayerFaction.Faction)
                        {
                            FireStepChangedEffects("Victory", Color.green, UIRef.ExecuteStepChangePath, 5);
                        }
                        else
                        {
                            FireStepChangedEffects("Defeat", Color.red, UIRef.ExecuteStepChangePath, 5);
                        }
                        HandleGameOver(gameState, authPlayerFaction);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.gameOverFired;
                    }
                    break;
            }




            #region PlayerLoops

            HandleMenuSettings(authPlayerCam);

            UIRef.HeroEnergyBar.HeroCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.HeroEnergyBar.HeroCurrentEnergyFill.fillAmount, (float) authPlayerEnergy.Energy / authPlayerEnergy.MaxEnergy * UIRef.HeroEnergyBar.MaxFillAmount, Time.DeltaTime);

            if (authPlayerState.CurrentState != PlayerStateEnum.unit_selected && authPlayerState.CurrentState != PlayerStateEnum.waiting_for_target)
            {
                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.BottomLeftPortrait.UnitInfoPanel.activeSelf || UIRef.BottomLeftPortrait.ManalithInfoPanel.activeSelf)
                    {
                        UIRef.BottomLeftPortrait.UnitInfoPanel.SetActive(false);
                        UIRef.BottomLeftPortrait.ManalithInfoPanel.SetActive(false);
                        UIRef.UnitInfoPanel.UnitStatsPanel.SetActive(false);
                        UIRef.UnitInfoPanel.ManalithStatsPanel.SetActive(false);
                    }
                }

                UIRef.SwapActionButton.gameObject.SetActive(false);

                for (int bi = 0; bi < UIRef.Actions.Count; bi++)
                {
                    UIRef.Actions[bi].Visuals.SetActive(false);
                }

                for (int si = 0; si < UIRef.SpawnActions.Count; si++)
                {
                    UIRef.SpawnActions[si].Visuals.SetActive(false);
                }
            }

            //all players
            Entities.ForEach((ref PlayerState.Component playerState, in FactionComponent.Component faction) =>
            {
                if (authPlayerFaction.Faction == faction.Faction)
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        UIRef.TurnStatePnl.GOButtonScript.SetLightsToPlayerColor(UIRef.FriendlyColor);
                        UIRef.TurnStatePnl.GOButtonScript.PlayerReady = true;
                        //Slide out first if friendly

                        if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.planning || UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.enemyReadyFired)
                        {
                            FireStepChangedEffects("Waiting", UIRef.FriendlyColor, UIRef.ReadySoundEventPath, 5);
                            UIRef.TurnStatePnl.FriendlyReadyDot.enabled = true;

                            foreach (Rope r in UIRef.Ropes)
                            {
                                r.FriendlyReadyBurstPS.time = 0;
                                r.FriendlyReadyBurstPS.Play();
                            }

                            UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.readyFired;
                        }

                        UIRef.TurnStatePnl.FriendlyReadyDot.color -= new Color(0, 0, 0, UIRef.TurnStatePnl.RopeComponent.ReadySwooshFadeOutSpeed * Time.DeltaTime);

                        //UIRef.SlideOutUIAnimator.SetBool("SlideOut", true);
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.TurnStatePnl.GOButtonScript.PlayerReady = false;
                        //UIRef.SlideOutUIAnimator.SetBool("SlideOut", false);
                    }
                }
                else
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        UIRef.OpponentReady = true;
                        if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.planning)
                        {
                            FireStepChangedEffects("Enemy Waiting", UIRef.EnemyColor, UIRef.OpponentReadySoundEventPath, 5);
                            UIRef.TurnStatePnl.EnemyReadyDot.enabled = true;

                            foreach (Rope r in UIRef.Ropes)
                            {
                                r.EnemyReadyBurstPS.time = 0;
                                r.EnemyReadyBurstPS.Play();
                            }

                            UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.enemyReadyFired;
                        }
                        UIRef.TurnStatePnl.EnemyReadyDot.color -= new Color(0, 0, 0, UIRef.TurnStatePnl.RopeComponent.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.OpponentReady = false;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            UIRef.TurnStatePnl.GOButtonScript.PlayerInCancelState = authPlayerHigh.CancelState;

            #endregion
            if (gameState.CurrentRopeTime < gameState.RopeTime)
            {
                if (gameState.CurrentState == GameStateEnum.planning)
                {
                    if (!UIRef.RopeLoopEmitter.IsPlaying())
                        UIRef.RopeLoopEmitter.Play();

                    UIRef.RopeLoopEmitter.SetParameter("FadeInFastTikTok", 1 - gameState.CurrentRopeTime / gameState.RopeTime);
                }
            }
            else
                UIRef.RopeLoopEmitter.Stop();

            foreach (Rope r in UIRef.Ropes)
            {
                //Handle Ropes
                if (gameState.CurrentRopeTime < gameState.RopeTime)
                {
                    r.EnemyRopeBarParticle.Rect.anchoredPosition = new Vector2(1 - (r.EnemyRope.fillAmount * r.EnemyRopeBarParticle.ParentRect.sizeDelta.x - r.EnemyRopeBarParticle.ParentRect.sizeDelta.x), r.EnemyRopeBarParticle.Rect.anchoredPosition.y);
                    r.FriendlyRopeBarParticle.Rect.anchoredPosition = new Vector2(r.FriendlyRope.fillAmount * r.FriendlyRopeBarParticle.ParentRect.sizeDelta.x - r.FriendlyRopeBarParticle.ParentRect.sizeDelta.x, r.FriendlyRopeBarParticle.Rect.anchoredPosition.y);
                    r.RopeTimeText.text = "0:" + ((int) gameState.CurrentRopeTime).ToString("D2");

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        r.RopeTimeText.enabled = true;

                        if (authPlayerState.CurrentState == PlayerStateEnum.ready)
                        {
                            if (!UIRef.OpponentReady)
                            {
                                r.RopeTimeText.color = UIRef.FriendlyColor;
                                r.FriendlyRopeBarParticle.LoopPS.Play(false);
                                r.FriendlyRope.fillAmount = 1 - (gameState.CurrentRopeTime / gameState.RopeTime);
                            }
                        }
                        else
                        {
                            //GETTING ROPED
                            r.RopeTimeText.color = UIRef.EnemyColor;
                            r.EnemyRopeBarParticle.LoopPS.Play(false);
                            r.FriendlyRope.fillAmount = 0;
                            r.EnemyRope.fillAmount = 1 - (gameState.CurrentRopeTime / gameState.RopeTime);
                        }
                    }
                    else
                    {
                        //Find Center and Lerp both ropes to Center
                        if (!r.RopeSlamOneTime)
                        {
                            r.EnemyRopeEndFillAmount = UIRef.TurnStatePnl.RopeComponent.EnemyRope.fillAmount;
                            r.FriendlyRopeEndFillAmount = UIRef.TurnStatePnl.RopeComponent.FriendlyRope.fillAmount;
                            r.RopeTimeText.enabled = false;
                            r.RopeFillsEndDist = (1 - r.FriendlyRopeEndFillAmount - r.EnemyRopeEndFillAmount) / 2f;
                            r.RopeSlamOneTime = true;
                        }

                        if (r.RopeEndsLerpTime < 1)
                        {
                            if (r.RopeSlamOneTime)
                            {
                                r.RopeEndsLerpTime += Time.DeltaTime * UIRef.TurnStatePnl.RopeComponent.RopeEndLerpSpeed;
                                r.FriendlyRope.fillAmount = Mathf.Lerp(r.FriendlyRopeEndFillAmount, r.FriendlyRopeEndFillAmount + r.RopeFillsEndDist, r.RopeEndsLerpTime);
                                r.EnemyRope.fillAmount = Mathf.Lerp(r.EnemyRopeEndFillAmount, r.EnemyRopeEndFillAmount + r.RopeFillsEndDist, r.RopeEndsLerpTime);
                            }
                        }
                        else
                        {
                            if (r.FriendlyRope.color.a == 1)
                            {
                                r.EnemyRopeBarParticle.LoopPS.Stop();
                                r.EnemyRopeBarParticle.BurstPS.time = 0;
                                r.EnemyRopeBarParticle.BurstPS.Play();
                            }

                            if (r.EnemyRope.color.a == 1)
                            {
                                r.FriendlyRopeBarParticle.LoopPS.Stop();
                                r.FriendlyRopeBarParticle.BurstPS.time = 0;
                                r.FriendlyRopeBarParticle.BurstPS.Play();
                            }

                            r.FriendlyRope.color -= new Color(0, 0, 0, r.RopeEndFadeOutSpeed * Time.DeltaTime);
                            r.EnemyRope.color -= new Color(0, 0, 0, r.RopeEndFadeOutSpeed * Time.DeltaTime);
                        }
                    }
                }
                else
                {
                    r.FriendlyRopeBarParticle.LoopPS.Stop();
                    r.EnemyRopeBarParticle.LoopPS.Stop();
                    r.RopeTimeText.enabled = false;
                }
            }

            m_AuthoritativePlayerData.SetSingleton(authPlayerHigh);

            HandleKeyCodeInput(gameState.CurrentState);

            return inputDeps;
        }

        public void SwapActiveMenuPanel(GameObject menuPanelToActivate)
        {
            //if menuPanelToActivate is the active Panel, invert its active state
            if (UIRef.ActiveMenuPanel == menuPanelToActivate)
            {
                UIRef.ActiveMenuPanel.SetActive(!UIRef.ActiveMenuPanel.activeSelf);
            }
            //if menuPanelToActivate is not the active Panel, deactivate active menu and swap it with menuPanelToActivate
            else
            {
                if (UIRef.ActiveMenuPanel)
                    UIRef.ActiveMenuPanel.SetActive(false);

                UIRef.ActiveMenuPanel = menuPanelToActivate;
                UIRef.ActiveMenuPanel.SetActive(true);
            }
        }

        void DeactivateActiveMenuActivateMainMenu()
        {
            if (UIRef.ActiveMenuPanel && UIRef.ActiveMenuPanel.activeSelf)
            {
                UIRef.ActiveMenuPanel.SetActive(false);
                UIRef.ActiveMenuPanel = null;
            }
            else
            {
                UIRef.ActiveMenuPanel = UIRef.EscapeMenu.gameObject;
                UIRef.ActiveMenuPanel.SetActive(true);
            }
        }

        void DisposeWorldAndSwitchToMainMenu()
        {
            var waitTime = 0f;

#if UNITY_EDITOR
            //all players
            waitTime = .2f;
            Entities.ForEach((in PlayerState.Component playerState, in FactionComponent.Component faction, in SpatialEntityId id) =>
            {
                var playerDeleteReq = new PlayerEnergy.PlayerDeletion.Request
                (
                 id.EntityId,
                 new PlayerDeletionRequest()
                );

                m_CommandSystem.SendCommand(playerDeleteReq);
            })
            .WithoutBurst()
            .Run();
#else
            var playerDeleteReq = new PlayerEnergy.PlayerDeletion.Request
             (
                 m_AuthoritativePlayerData.GetSingleton<SpatialEntityId>().EntityId,
                 new PlayerDeletionRequest()
             );

            m_CommandSystem.SendCommand(playerDeleteReq);
#endif

            //Open loading panel
            UIRef.LoadSceneAfterSeconds(waitTime);
        }

        public void InitializeButtons()
        {

            UIRef.MainMenuButton.Button.onClick.AddListener(delegate { SwapActiveMenuPanel(UIRef.EscapeMenu.gameObject); SetEscapePanelMenuActive(0); });
            UIRef.HelpButton.Button.onClick.AddListener(delegate { SwapActiveMenuPanel(UIRef.HelpPanel); SetHelpPanelMenuActive(0); });
            UIRef.SkilltreeButton.Button.onClick.AddListener(delegate { SwapActiveMenuPanel(UIRef.SkillTreePanel); });
            UIRef.EscapeMenu.ExitGameButton.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.conceded); DisposeWorldAndSwitchToMainMenu(); });
            UIRef.GameOverPanelButton.onClick.AddListener(delegate { DisposeWorldAndSwitchToMainMenu(); });
            UIRef.CancelActionButton.onClick.AddListener(delegate { CancelLockedAction(); });
            UIRef.RevealVisionButton.onClick.AddListener(delegate { m_SendActionRequestSystem.RevealPlayerVision(); });
            UIRef.TurnStatePnl.GOButtonScript.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetCancelTimer(UIRef.TurnStatePnl.CacelGraceTime);});
            UIRef.TurnStatePnl.GOButtonScript.Button.onClick.AddListener(delegate { InvertActionsPanelActive(); });
            UIRef.TurnStatePnl.GOButtonScript.Button.onClick.AddListener(delegate { m_HighlightingSystem.ResetHighlightsNoIn(); });
            UIRef.EscapeMenu.ConcedeButton.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.conceded); });

            for (int pi = 0; pi < UIRef.HelpPanelComponent.TabButtons.Count; pi++)
            {
                MainMenuTabButton ab = UIRef.HelpPanelComponent.TabButtons[pi];
                ab.index = pi;
                ab.Button.onClick.AddListener(delegate { SetHelpPanelMenuActive(ab.index); });
            }

            for (int pi = 0; pi < UIRef.EscapeMenu.PanelButtons.Count; pi++)
            {
                MainMenuTabButton ab = UIRef.EscapeMenu.PanelButtons[pi];
                ab.index = pi;
                ab.Button.onClick.AddListener(delegate { SetEscapePanelMenuActive(ab.index); });
            }

            for (int bi = 0; bi < UIRef.Actions.Count; bi++)
            {
                ActionButton ab = UIRef.Actions[bi];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
                ab.Button.onClick.AddListener(delegate { InitializeSelectedActionTooltip(ab, UIRef.SAToolTip); });
                ab.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetInputCoolDown(0.3f); });
            }

            for (int si = 0; si < UIRef.SpawnActions.Count; si++)
            {
                ActionButton ab = UIRef.SpawnActions[si];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
                ab.Button.onClick.AddListener(delegate { InitializeSelectedActionTooltip(ab, UIRef.SAToolTip); });
                ab.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetInputCoolDown(0.3f); });
            }
        }

        public void InvertActionsPanelActive()
        {
            UIRef.ActionPanel.SetActive(!UIRef.ActionPanel.activeSelf);
        }

        public void SetHelpPanelMenuActive(int index)
        {
            for (int i = 0; i < UIRef.HelpPanelComponent.HelpSections.Count; i++)
            {
                if (i == index)
                {
                    if (UIRef.HelpPanelComponent.ActiveHelpSection != UIRef.HelpPanelComponent.HelpSections[i])
                    {
                        UIRef.HelpPanelComponent.ActiveHelpSection = UIRef.HelpPanelComponent.HelpSections[i];
                        UIRef.HelpPanelComponent.HelpSections[i].gameObject.SetActive(true);
                        UIRef.HelpPanelComponent.HelpSections[i].ScrollRect.ScrollToTop();

                        foreach (VideoPlayerHandler v in UIRef.HelpPanelComponent.HelpSections[i].VideoPlayerHandlers)
                        {
                            v.VideoPlayer.time = 0;
                            v.VideoPlayer.Play();
                        }
                    }
                }
                else
                {
                    UIRef.HelpPanelComponent.HelpSections[i].gameObject.SetActive(false);
                }
            }
        }

        public void SetEscapePanelMenuActive(int index)
        {
            for (int i = 0; i < UIRef.EscapeMenu.Panels.Count; i++)
            {
                if(i == index)
                {
                    UIRef.EscapeMenu.Panels[i].SetActive(true);
                }
                else
                {
                    UIRef.EscapeMenu.Panels[i].SetActive(false);
                }
            }
        }

        public void HandleTutorialVideoPlayers()
        {
            if (!UIRef.HelpPanelComponent.ActiveHelpSection)
                return;

            foreach(VideoPlayerHandler v in UIRef.HelpPanelComponent.ActiveHelpSection.VideoPlayerHandlers)
            {
                if(v.HoveredHandler.Hovered && UIRef.HelpPanelComponent.gameObject.activeSelf)
                {
                    if (!v.VideoPlayer.isPlaying)
                        v.VideoPlayer.Play();
                }
                else
                {
                    if (v.VideoPlayer.isPlaying && v.VideoPlayer.frame > 0)
                    {
                        v.VideoPlayer.Pause();
                    }
                }
            }
        }

        public void AddSettingsListeners()
        {
            //VOLUME SLIDERS
            UIRef.EscapeMenu.MasterVolumeSlider.onValueChanged.AddListener(delegate { OnVolumeSliderChanged(UIRef.MasterBus, UIRef.EscapeMenu.MasterVolumeSlider.value); });
            UIRef.EscapeMenu.SFXVolumeSlider.onValueChanged.AddListener(delegate { OnVolumeSliderChanged(UIRef.SFXBus, UIRef.EscapeMenu.SFXVolumeSlider.value); });
            UIRef.EscapeMenu.MusicVolumeSlider.onValueChanged.AddListener(delegate { OnVolumeSliderChanged(UIRef.MusicBus, UIRef.EscapeMenu.MusicVolumeSlider.value); });

        }

        void OnVolumeSliderChanged(FMOD.Studio.Bus bus, float volume)
        {
            bus.setVolume(volume);
        }

        ActionButton FillButtonFields(ActionButton inButton, UnitDataSet inBaseData, long inUnitId, int inIndex, bool isSpawnAction)
        {
            if (isSpawnAction)
            {
                inButton.ExecuteStepIndex = (int) inBaseData.SpawnActions[inIndex].ActionExecuteStep;
                inButton.EnergyCost = inBaseData.SpawnActions[inIndex].Targets[0].energyCost;
                //need to construct Desc with stats DMG RANGE AOE (to enabler progression damage scaling)
                inButton.ActionDescription = inBaseData.SpawnActions[inIndex].Description;
                inButton.ActionName = inBaseData.SpawnActions[inIndex].ActionName;
                inButton.Icon.sprite = inBaseData.SpawnActions[inIndex].ActionIcon;
                inButton.ActionIndex = inBaseData.Actions.Count + inIndex;
            }
            else
            {
                inButton.ExecuteStepIndex = (int) inBaseData.Actions[inIndex].ActionExecuteStep;
                inButton.EnergyCost = inBaseData.Actions[inIndex].Targets[0].energyCost;
                inButton.ActionDescription = inBaseData.Actions[inIndex].Description;
                inButton.ActionName = inBaseData.Actions[inIndex].ActionName;
                inButton.Icon.sprite = inBaseData.Actions[inIndex].ActionIcon;
                inButton.ActionIndex = inIndex;
            }
            inButton.UnitId = (int) inUnitId;
            inButton.SelectedGlow.color = settings.TurnStepColors[inButton.ExecuteStepIndex + 1];
            inButton.StaticGlow.color = settings.TurnStepColors[inButton.ExecuteStepIndex + 1];

            return inButton;
        }

        public void HandleGameOver(GameState.Component gameState, FactionComponent.Component authPlayerFaction)
        {
            if (gameState.WinnerFaction == 0)
            {
                if (!UIRef.GameOverPanel.activeSelf)
                {
                    UIRef.DrawPanel.SetActive(true);
                    UIRef.GameOverPanel.SetActive(true);
                }
            }
            else if (gameState.WinnerFaction == authPlayerFaction.Faction)
            {
                if (!UIRef.GameOverPanel.activeSelf)
                {
                    UIRef.VictoryPanel.SetActive(true);
                    UIRef.GameOverPanel.SetActive(true);
                }
            }
            else
            {
                if (!UIRef.GameOverPanel.activeSelf)
                {
                    UIRef.DefeatPanel.SetActive(true);
                    UIRef.GameOverPanel.SetActive(true);
                }
            }

            UIRef.MainMenuButton.gameObject.SetActive(false);
            //Disable all but menu canvas
            DisableAllCanvases(3);

        }

        public void DisableAllCanvases(int skipDisableIndex)
        {
            for(int i = 0; i < UIRef.Canvases.Count; i++)
            {
                if (i != skipDisableIndex)
                    UIRef.Canvases[i].enabled = false;
            }
        }

        public void UpdateActionHoverTooltip()
        {
            bool SAanyButtonHovered = false;
            bool INanyButtonHovered = false;

            foreach (ActionButton b in UIRef.Actions)
            {
                if (b.Hovered)
                {
                    UIRef.SAToolTip.Rect.anchoredPosition = new Vector2(b.ButtonRect.anchoredPosition.x, UIRef.SAToolTip.Rect.anchoredPosition.y);
                    InitializeSelectedActionTooltip(b, UIRef.SAToolTip);
                    SAanyButtonHovered = true;
                }
            }
            foreach (ActionButton b in UIRef.SpawnActions)
            {
                if (b.Hovered)
                {
                    UIRef.SAToolTip.Rect.anchoredPosition = new Vector2(b.ButtonRect.anchoredPosition.x, UIRef.SAToolTip.Rect.anchoredPosition.y);
                    InitializeSelectedActionTooltip(b, UIRef.SAToolTip);
                    SAanyButtonHovered = true;
                }
            }
            foreach (ActionButton b in UIRef.UnitInspection.HoverOnlyButtons)
            {
                if (b.Hovered)
                {
                    UIRef.UnitInspection.ToolTip.Rect.anchoredPosition = new Vector2(b.ButtonRect.anchoredPosition.x - (UIRef.UnitInspection.ToolTip.Rect.rect.width / 2 + b.ButtonRect.rect.width), UIRef.UnitInspection.ToolTip.Rect.anchoredPosition.y);
                    InitializeSelectedActionTooltip(b, UIRef.UnitInspection.ToolTip);
                    INanyButtonHovered = true;
                }
            }

            UIRef.SAToolTip.gameObject.SetActive(SAanyButtonHovered);
            UIRef.UnitInspection.ToolTip.gameObject.SetActive(INanyButtonHovered);
        }

        public void UnitLoop(PlayerState.Component authPlayerState, uint authPlayerFaction, GameState.Component gameState, PlayerEnergy.Component playerEnergy, HighlightingDataComponent playerHigh, Moba_Camera playerCam)
        {
            Entities.ForEach((Entity e, UnitComponentReferences unitCompRef, ref Energy.Component energy, ref IsVisible isVisible, ref MouseState mouseState, in ClientActionRequest.Component clientActionRequest, in Actions.Component actions, in Health.Component health) =>
            {
                uint unitId = (uint)EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var worldIndex = EntityManager.GetComponentData<WorldIndex.Component>(e);
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
                var faction = EntityManager.GetComponentData<FactionComponent.Component>(e);

                if(mouseState.RightClickEvent == 1)
                {
                    if (authPlayerState.CurrentState != PlayerStateEnum.waiting_for_target && isVisible.Value==1 && (faction.Faction != authPlayerFaction || actions.LockedAction.Index == -3))
                    {
                        FillInspectWindowInformation(actions, unitCompRef.BaseDataSetComp);
                        SwapActiveMenuPanel(UIRef.UnitInspection.gameObject);
                        SetInspectionPortraitInfo(faction.Faction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor);
                        foreach (ActionButton b in UIRef.UnitInspection.HoverOnlyButtons)
                        {
                            b.Hovered = false;
                        }
                    }
                }

                if (EntityManager.HasComponent<AiUnit.Component>(e) && unitCompRef.HeadUIRef.UnitHeadUIInstance)
                {
                    var AIUnitHeadUIRef = unitCompRef.HeadUIRef.UnitHeadUIInstance as AIUnitHeadUI;
                    var AIUnit = EntityManager.GetComponentData<AiUnit.Component>(e);

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (AIUnit.CurrentState == AiUnitStateEnum.aggroed && AIUnit.AggroedUnitFaction == authPlayerFaction)
                            AIUnitHeadUIRef.ExclamationMark.SetActive(true);
                        else
                            AIUnitHeadUIRef.ExclamationMark.SetActive(false);
                    }
                    else
                        AIUnitHeadUIRef.ExclamationMark.SetActive(false);
                }

                if (gameState.CurrentState == GameStateEnum.planning)
                {
                    uint damagePreviewAmount = 0;
                    uint armorPreviewAmount = 0;

                    foreach(KeyValuePair<long, CubeCoordinateList> kv in authPlayerState.UnitTargets)
                    {
                        if(kv.Value.CubeCoordinates.ContainsKey(coord.CubeCoordinate))
                        {
                            if (kv.Value.CubeCoordinates[coord.CubeCoordinate])
                            {
                                armorPreviewAmount += kv.Value.ArmorAmount;
                                damagePreviewAmount += kv.Value.DamageAmount;
                            }
                        }
                    }
                    unitCompRef.HeadUIRef.IncomingDamage = damagePreviewAmount;
                    unitCompRef.HeadUIRef.IncomingArmor = armorPreviewAmount;
                }

                if (unitCompRef.BaseDataSetComp.SelectUnitButtonInstance)
                    UpdateSelectUnitButton(actions, unitCompRef.BaseDataSetComp.SelectUnitButtonInstance, energy, faction);

                //set topLeft healthBar values for this players hero
                if (unitCompRef.BaseDataSetComp.IsHero && faction.Faction == authPlayerFaction)
                {
                    if (unitCompRef.HeadUIRef.UnitHeadHealthBarInstance)
                        EqualizeHealthBarFillAmounts(unitCompRef.HeadUIRef.UnitHeadHealthBarInstance, UIRef.HeroHealthBar, faction.Faction, authPlayerFaction);

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UpdateHeroBauble(actions, UIRef.TopEnergyFill, UIRef.HeroBaubleEnergyText, energy, faction, (int) playerEnergy.BaseIncome);
                    }
                }

                if (authPlayerState.SelectedUnitId == unitId)
                {
                    if (!UIRef.BottomLeftPortrait.UnitInfoPanel.activeSelf)
                    {
                        UIRef.BottomLeftPortrait.ManalithInfoPanel.SetActive(false);
                        UIRef.UnitInfoPanel.ManalithStatsPanel.SetActive(false);
                        UIRef.BottomLeftPortrait.UnitInfoPanel.SetActive(true);
                        UIRef.UnitInfoPanel.UnitStatsPanel.SetActive(true);
                    }

                    if (unitCompRef.HeadUIRef.UnitHeadHealthBarInstance)
                        EqualizeHealthBarFillAmounts(unitCompRef.HeadUIRef.UnitHeadHealthBarInstance, UIRef.BottomLeftPortrait.PortraitHealthBar, faction.Faction, authPlayerFaction);
                }

                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.SwapActionButton.gameObject.activeSelf)
                        UIRef.SwapActionButton.gameObject.SetActive(false);

                    SetPortraitInfo(unitCompRef.BaseDataSetComp, faction.Faction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor, false);
                }

                if (!unitCompRef.BaseDataSetComp.UIInitialized)
                {
                    InitializeUnitUI(playerEnergy, actions, unitCompRef, unitCompRef.BaseDataSetComp, unitId, faction.Faction, authPlayerFaction, worldIndex.Value, playerCam);
                    unitCompRef.BaseDataSetComp.UIInitialized = true;
                }
                else
                {
                    if (unitCompRef.UnitEffectsComp.CurrentHealth == 0)
                    {

                    }
                    else
                    {
                        if(unitCompRef.HeadUIRef.UnitHeadUIInstance)
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, unitCompRef.transform.position + new Vector3(0, unitCompRef.HeadUIRef.HealthBarYOffset, 0)));
                        if(unitCompRef.HeadUIRef.UnitHeadHealthBarInstance)
                        {
                            unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, unitCompRef.transform.position + new Vector3(0, unitCompRef.HeadUIRef.HealthBarYOffset, 0)));
                            SetHealthBarFillAmounts(gameState.CurrentState, unitCompRef.UnitEffectsComp, unitCompRef.HeadUIRef, unitCompRef.HeadUIRef.UnitHeadHealthBarInstance, health, faction.Faction, authPlayerFaction);
                        }

                        HandleEnergyGainOverHeadDisplay(isVisible, actions, energy, authPlayerState, unitId, faction.Faction, authPlayerFaction, unitCompRef, coord.CubeCoordinate, playerHigh, gameState);
                        HandleUnitVisibility(isVisible, unitCompRef, health);

                        if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.readyFired)
                        {
                            if (faction.Faction == authPlayerFaction && energy.Harvesting)
                            {
                                SetHealthFloatText(e, true, energy.EnergyIncome, settings.FactionIncomeColors[(int) faction.Faction]);
                            }
                        }

                        if (UIRef.UIActive && unitCompRef.HeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.gameObject.activeInHierarchy)
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.SetFloat("WaitTime", unitCompRef.HeadUIRef.HealthTextDelay);

                        if (unitCompRef.HeadUIRef.HealthTextDelay > 0)
                        {
                            unitCompRef.HeadUIRef.HealthTextDelay -= Time.DeltaTime;
                        }
                        else if (UIRef.UIActive && unitCompRef.HeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.gameObject.activeInHierarchy)
                        {
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.SetBool("Delayed", false);
                        }

                        if (gameState.CurrentState == GameStateEnum.planning || gameState.CurrentState == GameStateEnum.game_over)
                        {
                            if (unitCompRef.HeadUIRef.UnitHeadUIInstance.PlanningBufferTime > 0)
                            {
                                unitCompRef.HeadUIRef.UnitHeadUIInstance.PlanningBufferTime -= Time.DeltaTime;
                            }
                            else if (unitCompRef.HeadUIRef.UnitHeadUIInstance.ArmorPanel.activeSelf)
                            {
                                if (UIRef.UIActive)
                                    unitCompRef.HeadUIRef.UnitHeadUIInstance.ArmorAnimator.SetTrigger("IdleTrigger");

                                unitCompRef.HeadUIRef.UnitHeadUIInstance.ArmorPanel.SetActive(false);
                            }
                        }

                        UpdateUnitMapTilePosition(UIRef.MinimapComponent, unitCompRef.transform.position, ref unitCompRef.IsVisibleRefComp, true);
                        UpdateUnitMapTilePosition(UIRef.BigMapComponent, unitCompRef.transform.position, ref unitCompRef.IsVisibleRefComp, false);

                        if (unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay != null)
                        {
                            if ((clientActionRequest.ActionId != -3 || Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) != Vector3.zero) && gameState.CurrentState == GameStateEnum.planning)
                            {
                                unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(true);
                            }
                            else
                            {
                                unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        public void ManalithUnitLoop(PlayerState.Component authPlayerState, uint authPlayerFaction, GameState.Component gameState, PlayerEnergy.Component playerEnergy, HighlightingDataComponent playerHigh)
        {
            Entities.ForEach((Entity e, UnitComponentReferences unitCompRef, ref Energy.Component energy, ref Actions.Component actions, ref IsVisible isVisible, ref MouseState mouseState, ref Manalith.Component m, in ClientActionRequest.Component clientActionRequest) =>
            {
                uint unitId = (uint) EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
                var faction = EntityManager.GetComponentData<FactionComponent.Component>(e);

                if (authPlayerState.SelectedUnitId == unitId)
                {
                    if (!UIRef.BottomLeftPortrait.ManalithInfoPanel.activeSelf)
                    {
                        UIRef.BottomLeftPortrait.ManalithInfoPanel.SetActive(true);
                        UIRef.UnitInfoPanel.ManalithStatsPanel.SetActive(true);
                        UIRef.BottomLeftPortrait.UnitInfoPanel.SetActive(false);
                        UIRef.UnitInfoPanel.UnitStatsPanel.SetActive(false);
                    }
                }

                if (mouseState.RightClickEvent == 1)
                {
                    if (authPlayerState.CurrentState != PlayerStateEnum.waiting_for_target && (faction.Faction != authPlayerFaction || actions.LockedAction.Index == -3))
                    {
                        FillInspectWindowInformation(actions, unitCompRef.BaseDataSetComp);
                        SwapActiveMenuPanel(UIRef.UnitInspection.gameObject);
                        SetInspectionPortraitInfo(faction.Faction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor);
                        foreach (ActionButton b in UIRef.UnitInspection.HoverOnlyButtons)
                        {
                            b.Hovered = false;
                        }
                    }
                }


                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.SwapActionButton.gameObject.activeSelf)
                        UIRef.SwapActionButton.gameObject.SetActive(false);

                    SetPortraitInfo(unitCompRef.BaseDataSetComp, faction.Faction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor, false);
                }

                if (!unitCompRef.BaseDataSetComp.UIInitialized)
                {
                    InitializeManalithUnitUI(unitCompRef.HeadUIRef);
                    unitCompRef.BaseDataSetComp.UIInitialized = true;
                }
                else if (unitCompRef.HeadUIRef.UnitHeadUIInstance)
                {
                    HandleEnergyGainOverHeadDisplay(isVisible, actions, energy, authPlayerState, unitId, faction.Faction, authPlayerFaction, unitCompRef, coord.CubeCoordinate, playerHigh, gameState);

                    if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.readyFired)
                    {
                        if (faction.Faction == authPlayerFaction && energy.Harvesting)
                        {
                            SetHealthFloatText(e, true, energy.EnergyIncome, settings.FactionIncomeColors[(int) faction.Faction]);
                        }
                    }

                    unitCompRef.HeadUIRef.UnitHeadUIInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, unitCompRef.transform.position + new Vector3(0, unitCompRef.HeadUIRef.HealthBarYOffset, 0)));

                    if (faction.Faction == authPlayerFaction && (clientActionRequest.ActionId != -3 || Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) != Vector3.zero) && gameState.CurrentState == GameStateEnum.planning)
                    {
                        unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(true);
                    }
                    else if (unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay)
                    {
                        unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(false);
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        public void HandleUnitVisibility(IsVisible isVisible, UnitComponentReferences unitCompRef, Health.Component health)
        {
            if (isVisible.Value == 1)
            {
                foreach (GameObject g in unitCompRef.HeadUIRef.UnitHeadUIInstance.EnableIfVisibleGameObjects)
                {
                    if (!g.activeSelf)
                        g.SetActive(true);
                }

                if (unitCompRef.UnitEffectsComp.CurrentHealth < health.MaxHealth || unitCompRef.HeadUIRef.IncomingDamage > 0 || unitCompRef.HeadUIRef.IncomingArmor > 0)
                {
                    if (!unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.gameObject.activeSelf)
                        unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.gameObject.SetActive(true);
                }
                else if (unitCompRef.HeadUIRef.UnitHeadHealthBarInstance && unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.gameObject.activeSelf)
                {
                    unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.gameObject.SetActive(false);
                }
            }
            else if(unitCompRef.HeadUIRef.UnitHeadHealthBarInstance)
            {
                if (unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.gameObject.activeSelf)
                    unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.gameObject.SetActive(false);

                foreach (GameObject g in unitCompRef.HeadUIRef.UnitHeadUIInstance.EnableIfVisibleGameObjects)
                {
                    if (g.activeSelf)
                        g.SetActive(false);
                }
            }
        }

        public void HandleEnergyGainOverHeadDisplay(IsVisible isVisible, Actions.Component actions, Energy.Component energy, PlayerState.Component authPlayerState, uint unitId, uint unitFaction, uint playerFaction, UnitComponentReferences unitCompRef, Vector3f unitCoord, HighlightingDataComponent playerHigh, GameState.Component gameState)
        {
            if (isVisible.Value == 1)
            {
                if (energy.Harvesting && !unitCompRef.AnimatorComp.IsMoving)
                {
                    if ((authPlayerState.SelectedUnitId == unitId || Vector3fext.ToUnityVector(unitCoord) == Vector3fext.ToUnityVector(playerHigh.HoveredCoordinate)) && actions.LockedAction.Index == -3 && actions.CurrentSelected.Index == -3 && gameState.CurrentState == GameStateEnum.planning && unitFaction == playerFaction)
                    {
                        if (!unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.gameObject.activeSelf)
                        {
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.text = "+" + energy.EnergyIncome.ToString();
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.gameObject.SetActive(false);
                }
            }
            else
            {
                unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.gameObject.SetActive(false);
            }
        }

        public void InitializeUnitOverHeadActionDisplay(int actionIndex, UnitDataSet stats, UnitHeadUIReferences unitHeadUIRef)
        {
            if (actionIndex < stats.Actions.Count)
            {
                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.ActionImage.sprite = stats.Actions[actionIndex].ActionIcon;
                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.Actions[actionIndex].ActionExecuteStep + 1];
            }
            else
            {
                int spawnactionindex = actionIndex - stats.Actions.Count;
                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.ActionImage.sprite = stats.SpawnActions[spawnactionindex].ActionIcon;
                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.TurnStepColorBG.color = settings.TurnStepColors[(int) stats.SpawnActions[spawnactionindex].ActionExecuteStep + 1];
            }
        }

        public void DeactivateActionDisplay(Entity e, float buffertime)
        {
            if (buffertime > 0)
            {
                buffertime -= Time.DeltaTime;
                DeactivateActionDisplay(e, buffertime);
            }
            else
            {
                var unitHeadUIRef = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);
                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(false);
            }
        }

        public void FillUnitButtons(Actions.Component actions, UnitDataSet stats, uint faction, uint authPlayerFaction, long unitId, PlayerEnergy.Component playerEnergy)
        {
            if (faction == authPlayerFaction)
            {
                var spawnActionCount = stats.SpawnActions.Count;
                var actionCount = stats.Actions.Count;

                if (stats.SpawnActions.Count == 0)
                {
                    if (UIRef.SpawnButtonGroup.activeSelf)
                    {
                        UIRef.ActionButtonGroup.SetActive(true);
                        UIRef.SpawnButtonGroup.SetActive(false);
                    }

                    if (UIRef.SwapActionButton.gameObject.activeSelf)
                    {
                        if (UIRef.SwapActionButton.ButtonInverted)
                            UIRef.SwapActionButton.InvertButton();
                        UIRef.SwapActionButton.gameObject.SetActive(false);
                    }
                }
                else
                {
                    UIRef.SwapActionButton.gameObject.SetActive(true);
                }

#region FILL UI BUTTON INFO
                for (int si = 0; si < UIRef.SpawnActions.Count; si++)
                {
                    if (si < spawnActionCount)
                    {
                        if (stats.SpawnActions[si].Targets[0].energyCost > playerEnergy.Energy)
                        {
                            UIRef.SpawnActions[si].Button.interactable = false;
                        }
                        else
                        {
                            UIRef.SpawnActions[si].Button.interactable = true;
                        }
                        UIRef.SpawnActions[si].Visuals.SetActive(true);

                        UIRef.SpawnActions[si] = FillButtonFields(UIRef.SpawnActions[si], stats, unitId, si, true);
                    }
                    else
                    {
                        UIRef.SpawnActions[si].Visuals.SetActive(false);
                    }
                }

                for (int bi = 0; bi < UIRef.Actions.Count; bi++)
                {
                    if (bi < actionCount)
                    {
                        UIRef.Actions[bi].Visuals.SetActive(true);

                        if (bi < actionCount)
                        {
                            if (stats.Actions[bi].Targets[0].energyCost > playerEnergy.Energy + actions.LockedAction.CombinedCost || playerEnergy.Energy == 0)
                            {
                                UIRef.Actions[bi].Button.interactable = false;
                            }
                            else
                            {
                                UIRef.Actions[bi].Button.interactable = true;
                            }
                            UIRef.Actions[bi] = FillButtonFields(UIRef.Actions[bi], stats, unitId, bi, false);
                        }
                        else
                        {
                            UIRef.Actions[bi].Visuals.SetActive(false);
                        }
                    }
                    else
                    {
                        UIRef.Actions[bi].Visuals.SetActive(false);
                    }
                }
#endregion
            }
            else
            {
                //UIRef.SAInfoPanel.SetActive(false);

                for (int bi = 0; bi < UIRef.Actions.Count; bi++)
                {
                    UIRef.Actions[bi].Visuals.SetActive(false);
                }

                for (int si = 0; si < UIRef.SpawnActions.Count; si++)
                {
                    UIRef.SpawnActions[si].Visuals.SetActive(false);
                }
            }
        }

        public void SetPortraitInfo(UnitDataSet stats, uint faction, List<AnimationClip> animatedPortraits, Color teamColor, bool active)
        {
            if(active)
            {
                if (animatedPortraits.Count != 0 && UIRef.BottomLeftPortrait.AnimatedPortrait.AnimatorOverrideController.animationClips[0].GetHashCode() != animatedPortraits[(int) faction].GetHashCode())
                {
                    UIRef.BottomLeftPortrait.AnimatedPortrait.AnimatorOverrideController["KingCroakPortrait"] = animatedPortraits[(int) faction];
                }

                UIRef.BottomLeftPortrait.PortraitNameText.text = stats.UnitName;
                UIRef.BottomLeftPortrait.UnitDescription.text = stats.UnitDescription;
                UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.enabled = true;
                UIRef.BottomLeftPortrait.PortraitNameText.enabled = true;
                UIRef.BottomLeftPortrait.AnimatedPortrait.GenericImage.enabled = true;
                UIRef.BottomLeftPortrait.UnitDescription.enabled = true;
                UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.color = teamColor;
            }
            else
            {
                UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.enabled = false;
                UIRef.BottomLeftPortrait.PortraitNameText.enabled = false;
                UIRef.BottomLeftPortrait.AnimatedPortrait.GenericImage.enabled = false;
                UIRef.BottomLeftPortrait.UnitDescription.enabled = false;
            }
        }

        public void UpdatePortraitHealthText(uint playerFaction, uint unitFaction, Health.Component health)
        {
            string currentMaxHealth = health.CurrentHealth + "/" + health.MaxHealth;

            if (health.Armor > 0 && unitFaction == playerFaction)
            {
                UIRef.BottomLeftPortrait.PortraitArmorText.enabled = true;
                UIRef.BottomLeftPortrait.PortraitHealthText.text = currentMaxHealth;
                UIRef.BottomLeftPortrait.PortraitArmorText.text = health.Armor.ToString();
            }
            else
            {
                UIRef.BottomLeftPortrait.PortraitArmorText.enabled = false;
                UIRef.BottomLeftPortrait.PortraitHealthText.text = currentMaxHealth;
            }
        }

        public void PopulateManlithInfoHexes(uint selectedUnitId, uint playerFaction)
        {
            Entities.ForEach((ref SpatialEntityId id, ref Manalith.Component manalith, in FactionComponent.Component faction) =>
            {
                if (selectedUnitId == id.EntityId.Id)
                {
                    UIRef.BottomLeftPortrait.ManalithEnergyGainText.color = settings.FactionIncomeColors[(int) faction.Faction];

                    if (faction.Faction == playerFaction)
                    {
                        UIRef.BottomLeftPortrait.ManalithEnergyGainText.text = "+" + manalith.CombinedEnergyGain;
                        //UIRef.BottomLeftPortrait.ManalithEnergyGainText.enabled = true;
                        UIRef.UnitInfoPanel.IncomeValue.text = manalith.CombinedEnergyGain.ToString();
                    }
                    else
                    {
                        UIRef.UnitInfoPanel.IncomeValue.text = "0";
                        UIRef.BottomLeftPortrait.ManalithEnergyGainText.text = "+0";
                        //UIRef.BottomLeftPortrait.ManalithEnergyGainText.enabled = false;
                    }

                    //UIRef.UnitInfoPanel.ConnectionsValue.text = manalith.
                    //UIRef.UnitInfoPanel.BaseGainValue.text = manalith.BaseIncome.ToString();

                    /*
                    if (faction.Faction != 0)
                    {
                        //UIRef.BottomLeftPortrait.ManalithEnergyGainText.color = settings.FactionColors[(int)faction.Faction];
                    }
                    else
                    {
                        UIRef.BottomLeftPortrait.ManalithEnergyGainText.color = settings.UINeutralColor;
                    }
                    */

                    for (int i = 0; i < UIRef.BottomLeftPortrait.InfoPanelHexes.Count; i++)
                    {
                        if (i < manalith.Manalithslots.Count)
                        {
                            UIRef.BottomLeftPortrait.InfoPanelHexes[i].Hex.enabled = true;

                            if (manalith.Manalithslots[i].IsTaken)
                            {
                                UIRef.BottomLeftPortrait.InfoPanelHexes[i].EnergyRing.gameObject.SetActive(true);
                                UIRef.BottomLeftPortrait.InfoPanelHexes[i].EnergyRing.color = settings.FactionIncomeColors[(int) manalith.Manalithslots[i].OccupyingFaction];
                            }
                            else
                            {
                                UIRef.BottomLeftPortrait.InfoPanelHexes[i].EnergyRing.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            UIRef.BottomLeftPortrait.InfoPanelHexes[i].EnergyRing.gameObject.SetActive(false);
                            UIRef.BottomLeftPortrait.InfoPanelHexes[i].Hex.enabled = false;
                        }
                    }
                }
            
            })
            .WithoutBurst()
            .Run();
        }

        void UpdateUnitMapTilePosition(MinimapScript map, Vector3 position, ref IsVisibleReferences isVisibleRef, bool isMiniMap)
        {
            Vector3 pos = position - map.MapCenter;
            Vector2 invertedPos = new Vector2(pos.x * map.MapSize, pos.z * map.MapSize);

            if (isVisibleRef.MiniMapTileInstance && isMiniMap)
                isVisibleRef.MiniMapTileInstance.TileRect.anchoredPosition = invertedPos;

            if (isVisibleRef.BigMapTileInstance && !isMiniMap)
                isVisibleRef.BigMapTileInstance.TileRect.anchoredPosition = invertedPos;
        }

        public Vector3 RoundVector3(Vector3 inVector)
        {
            return new Vector3((int) inVector.x, (int) inVector.y, (int) inVector.z);
        }

        void HandleMenuSettings(Moba_Camera playerCam)
        {
            playerCam.settings.movement.edgeHoverMovement = UIRef.EscapeMenu.EdgeHoverToggle.isOn;
            //if(UIRef.EscapeMenu.CamSpeedInputField.onValueChanged)
            playerCam.settings.movement.cameraMovementRate = UIRef.EscapeMenu.CamSpeedSlider.value;
            playerCam.settings.rotation.cameraRotationRate.y = UIRef.EscapeMenu.CamRotationSlider.value;
            playerCam.settings.zoom.zoomChangePerScroll = UIRef.EscapeMenu.CamZoomDistSlider.value;
            playerCam.settings.zoom.zoomLerpRate = UIRef.EscapeMenu.CamZoomSpeedSlider.value;

        }

        void FireStepChangedEffects(string stateName, Color effectColor, string soundEffectPath, uint turnStepID)
        {
            RuntimeManager.PlayOneShot(soundEffectPath);
            UIRef.TurnStatePnl.TurnStateText.color = effectColor;
            UIRef.BigMapTurnCounter.color = effectColor;
            UIRef.TurnStatePnl.TurnStateText.text = char.ToUpper(stateName[0]) + stateName.Substring(1);
            UIRef.BigMapTurnCounter.text = char.ToUpper(stateName[0]) + stateName.Substring(1);
            //FIRE PARTICLES
            foreach (ParticleSystem p in UIRef.CowExhaleParticleSystems)
            {
                ParticleSystem.MainModule main = p.main;
                main.startColor = effectColor;
                p.time = 0;
                p.Play();
            }
            UIRef.TurnDisplay.StateName = stateName;
            UIRef.TurnDisplay.ColorizeText = effectColor;
            UIRef.TurnDisplay.CurrentStepID = turnStepID;
            UIRef.TurnDisplay.StateName = char.ToUpper(stateName[0]) + stateName.Substring(1);
        }

        void HandleKeyCodeInput(GameStateEnum gameState)
        {
            if (UIRef.DollyPathCameraActive)
                return;

            if (Input.GetKeyDown(KeyCode.M))
            {
                UIRef.UIActive = !UIRef.UIActive;
                UIRef.IngameSFXBus.setMute(!UIRef.UIActive);
                UIRef.UINonMapSFXBus.setMute(!UIRef.UIActive);
                UIRef.EnvironmentBus.setMute(!UIRef.UIActive);
                UIRef.MapPanel.gameObject.SetActive(!UIRef.MapPanel.gameObject.activeSelf);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeactivateActiveMenuActivateMainMenu();
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                UIRef.UIActive = !UIRef.UIActive;
                UIRef.UISFXBus.setMute(!UIRef.UIActive);
                SetUserInterfaceEnabled(UIRef.UIActive);
            }

            if (gameState == GameStateEnum.planning)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (UIRef.TurnStatePnl.GOButtonScript.Button.interactable)
                        UIRef.TurnStatePnl.GOButtonScript.Button.onClick.Invoke();
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    //UIRef.IngameUIPanel.SetActive(!UIRef.IngameUIPanel.activeSelf);
                    if(UIRef.SwapActionButton.gameObject.activeSelf)
                        UIRef.SwapActionButton.Button.onClick.Invoke();
                }

                if (!UIRef.EscapeMenu.gameObject.activeSelf)
                {
                    if (UIRef.ActionButtonGroup.activeSelf)
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1) && UIRef.Actions[0].Visuals.activeSelf)
                        {
                            UIRef.Actions[0].Button.Select();
                            UIRef.Actions[0].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha2) && UIRef.Actions[1].Visuals.activeSelf)
                        {
                            UIRef.Actions[1].Button.Select();
                            UIRef.Actions[1].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha3) && UIRef.Actions[2].Visuals.activeSelf)
                        {
                            UIRef.Actions[2].Button.Select();
                            UIRef.Actions[2].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha4) && UIRef.Actions[3].Visuals.activeSelf)
                        {
                            UIRef.Actions[3].Button.Select();
                            UIRef.Actions[3].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha5) && UIRef.Actions[4].Visuals.activeSelf)
                        {
                            UIRef.Actions[4].Button.Select();
                            UIRef.Actions[4].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha6) && UIRef.Actions[5].Visuals.activeSelf)
                        {
                            UIRef.Actions[5].Button.Select();
                            UIRef.Actions[5].Button.onClick.Invoke();
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1) && UIRef.Actions[0].Visuals.activeSelf)
                        {
                            UIRef.SpawnActions[0].Button.Select();
                            UIRef.SpawnActions[0].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha2) && UIRef.Actions[1].Visuals.activeSelf)
                        {
                            UIRef.SpawnActions[1].Button.Select();
                            UIRef.SpawnActions[1].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha3) && UIRef.Actions[2].Visuals.activeSelf)
                        {
                            UIRef.SpawnActions[2].Button.Select();
                            UIRef.SpawnActions[2].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha4) && UIRef.Actions[3].Visuals.activeSelf)
                        {
                            UIRef.SpawnActions[3].Button.Select();
                            UIRef.SpawnActions[3].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha5) && UIRef.Actions[4].Visuals.activeSelf)
                        {
                            UIRef.SpawnActions[4].Button.Select();
                            UIRef.SpawnActions[4].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha6) && UIRef.Actions[5].Visuals.activeSelf)
                        {
                            UIRef.SpawnActions[5].Button.Select();
                            UIRef.SpawnActions[5].Button.onClick.Invoke();
                        }
                    }
                }
            }
        }

        void SetSelectedUnitId(long unitId)
        {
            var playerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
            playerState.SelectedUnitId = unitId;
            m_AuthoritativePlayerData.SetSingleton(playerState);
        }

        public Vector3 WorldToUISpace(Canvas parentCanvas, Vector3 worldPos)
        {
            //Convert the world for screen point so that it can be used with ScreenPointToLocalPointInRectangle function
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            //Convert the screenpoint to ui rectangle local point
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out Vector2 movePos);

            return movePos;
        }

        void UpdateSelectUnitButton(Actions.Component actions, SelectUnitButton unitButton, Energy.Component inEnergy, FactionComponent.Component inFaction)
        {
            unitButton.EnergyAmountChange = 0;
            if (inEnergy.Harvesting)
            {
                unitButton.EnergyAmountChange += (int) inEnergy.EnergyIncome;
            }
            if (actions.LockedAction.Index != -3)
            {
                unitButton.EnergyAmountChange -= (int) actions.LockedAction.CombinedCost;
            }

            if (unitButton.EnergyAmountChange > 0)
            {
                unitButton.EnergyFill.enabled = true;
                unitButton.EnergyFill.color = UIRef.FriendlyIncomeColor;
            }
            else if (unitButton.EnergyAmountChange < 0)
            {
                unitButton.EnergyFill.enabled = true;
                unitButton.EnergyFill.color = settings.FactionColors[(int)inFaction.Faction];
            }
            else
            {
                unitButton.EnergyFill.enabled = false;
            }
        }

        void UpdateUnitGroupBauble(UnitGroupUI unitGroupUI, FactionComponent.Component faction, int index)
        {
            //int totalCost = 0;
            int combinedAmount = 0;
            float percentageToFill = 0;
            float percentageGainFill = 0;

            foreach (SelectUnitButton selectButton in unitGroupUI.SelectUnitButtons)
            {
                if (selectButton.EnergyAmountChange > 0)
                {
                    percentageGainFill += 1f;
                }
                else if (selectButton.EnergyAmountChange < 0)
                {
                    percentageToFill += 1f;
                    percentageGainFill += 1f;
                }
                combinedAmount += selectButton.EnergyAmountChange;
            }

            unitGroupUI.CombinedEnergyCost = combinedAmount;

            //Combined cost change
            if (unitGroupUI.CombinedEnergyCost != unitGroupUI.LastCombinedEnergyCost)
            {
                percentageToFill /= unitGroupUI.SelectUnitButtons.Count;
                percentageGainFill /= unitGroupUI.SelectUnitButtons.Count;
                UIRef.EnergyConnectorNegativeFill.fillAmount = 0;
                UIRef.EnergyConnectorPlayerColorFill.fillAmount = 0;
                unitGroupUI.PositiveLerpTime = 0;
                unitGroupUI.NegativeLerpTime = 0;
                unitGroupUI.FillEvent = true;
                unitGroupUI.LastCombinedEnergyCost = unitGroupUI.CombinedEnergyCost;
            }

            if (unitGroupUI.CleanupReset)
            {
                //Instantly reset all the Values from cleanup
                if (combinedAmount > 0)
                {
                    //Debug.Log("CombinedAmount greater than 0 in cleanupEvent");
                    unitGroupUI.EnergyChangeText.text = "+" + combinedAmount;
                    unitGroupUI.EnergyFill.fillAmount = 0;
                    unitGroupUI.EnergyGainFill.fillAmount = 1;
                }
                else
                {
                    unitGroupUI.EnergyChangeText.text = combinedAmount.ToString();
                    unitGroupUI.EnergyFill.fillAmount = 0;
                    unitGroupUI.EnergyGainFill.fillAmount = 0;
                }

                unitGroupUI.CleanupReset = false;
            }

            else if (unitGroupUI.FillEvent)
            {
                //GAIN
                if (combinedAmount > 0)
                {

                    unitGroupUI.EnergyChangeText.text = "+" + combinedAmount;
                    unitGroupUI.PositiveLerpTime += UIRef.EnergyConnectorNegativeSpeed * Time.DeltaTime;
                    unitGroupUI.EnergyFill.fillAmount -= UIRef.EnergyConnectorNegativeSpeed * Time.DeltaTime;
                    unitGroupUI.EnergyGainFill.fillAmount = Mathf.Lerp(0, percentageGainFill, unitGroupUI.PositiveLerpTime);

                    if (unitGroupUI.PositiveLerpTime >= 1)
                        unitGroupUI.FillEvent = false;
                }
                //COST
                else
                {
                    unitGroupUI.EnergyChangeText.text = combinedAmount.ToString();
                    //RETURN ENERGY UP
                    if (unitGroupUI.EnergyFill.fillAmount > percentageToFill)
                    {
                        unitGroupUI.NegativeLerpTime += UIRef.EnergyConnectorPositiveSpeed * Time.DeltaTime;

                        unitGroupUI.EnergyGainFill.fillAmount -= UIRef.EnergyConnectorNegativeSpeed * Time.DeltaTime;
                        UIRef.EnergyConnectorNegativeFill.fillAmount = Mathf.Lerp(UIRef.ECFillAmounts.x + UIRef.ECFillAmounts.y * index, 0, unitGroupUI.NegativeLerpTime);
                        UIRef.EnergyConnectorPlayerColorFill.fillAmount = Mathf.Lerp(UIRef.ECFillAmounts.x + UIRef.ECFillAmounts.y * index, 0, unitGroupUI.PositiveLerpTime);
                        unitGroupUI.EnergyFill.fillAmount = Mathf.Lerp(1, percentageToFill, unitGroupUI.PositiveLerpTime);

                        if (unitGroupUI.NegativeLerpTime >= 1)
                        {
                            unitGroupUI.PositiveLerpTime += UIRef.EnergyConnectorNegativeSpeed * Time.DeltaTime;
                        }
                    }
                    else if (unitGroupUI.EnergyFill.fillAmount < percentageToFill)
                    {
                        //COST ENERGY DOWN
                        unitGroupUI.PositiveLerpTime += UIRef.EnergyConnectorPositiveSpeed * Time.DeltaTime;
                        unitGroupUI.EnergyGainFill.fillAmount -= UIRef.EnergyConnectorNegativeSpeed * Time.DeltaTime;
                        UIRef.EnergyConnectorPlayerColorFill.fillAmount = Mathf.Lerp(0, UIRef.ECFillAmounts.x + UIRef.ECFillAmounts.y * index, unitGroupUI.PositiveLerpTime);

                        if (UIRef.EnergyConnectorPlayerColorFill.fillAmount >= UIRef.ECFillAmounts.x + UIRef.ECFillAmounts.y * index - 0.001f)
                        {
                            unitGroupUI.NegativeLerpTime += UIRef.EnergyConnectorNegativeSpeed * Time.DeltaTime;
                            UIRef.EnergyConnectorNegativeFill.fillAmount = Mathf.Lerp(0, UIRef.ECFillAmounts.x + UIRef.ECFillAmounts.y * index, unitGroupUI.NegativeLerpTime);
                            unitGroupUI.EnergyFill.fillAmount = Mathf.Lerp(0, percentageToFill, unitGroupUI.NegativeLerpTime);
                        }
                    }
                    else
                    {
                        unitGroupUI.FillEvent = false;
                    }
                }

            }
        }

        public void SetUserInterfaceEnabled(bool enabled)
        {
            for(int i = 0; i < UIRef.Canvases.Count - 1; i++)
            {
                UIRef.Canvases[i].enabled = enabled;
            }
        }

        void UpdateHeroBauble(Actions.Component actions, Image inEnergyFill, Text inEnergyText, Energy.Component inEnergy, FactionComponent.Component inFaction, int baseManaGain)
        {
            int EnergyChangeAmount = baseManaGain;

            if (inEnergy.Harvesting)
            {
                EnergyChangeAmount += (int) inEnergy.EnergyIncome;
            }

            if (actions.LockedAction.Index != -3)
            {
                EnergyChangeAmount -= (int) actions.LockedAction.CombinedCost;
            }

            if (EnergyChangeAmount != 0)
            {
                if (inEnergyFill.fillAmount < 1)
                    inEnergyFill.fillAmount = Mathf.Lerp(inEnergyFill.fillAmount, 1, Time.DeltaTime);

                if (EnergyChangeAmount > 0)
                {
                    inEnergyText.text = "+" + EnergyChangeAmount.ToString();
                    inEnergyFill.color = UIRef.FriendlyIncomeColor;
                }
                else
                {
                    inEnergyText.text = EnergyChangeAmount.ToString();
                    inEnergyFill.color = settings.FactionColors[(int)inFaction.Faction];
                }
            }
            else
            {
                inEnergyText.text = "0";
                if (inEnergyFill.fillAmount > 0)
                    inEnergyFill.fillAmount = Mathf.Lerp(inEnergyFill.fillAmount, 0, Time.DeltaTime);
            }
        }

        public void EqualizeHealthBarFillAmounts(HealthBar fromHealthBar, HealthBar toHealthBar, uint unitFaction, uint playerFaction)
        {
            toHealthBar.HealthFill.fillAmount = fromHealthBar.HealthFill.fillAmount;
            toHealthBar.ArmorFill.fillAmount = fromHealthBar.ArmorFill.fillAmount;
            toHealthBar.DamageFill.fillAmount = fromHealthBar.DamageFill.fillAmount;
            toHealthBar.BgFill.fillAmount = fromHealthBar.BgFill.fillAmount;

            if (unitFaction == playerFaction)
            {
                toHealthBar.DamageRect.offsetMax = new Vector2((-toHealthBar.HealthBarRect.rect.width * (1 - fromHealthBar.ArmorFill.fillAmount)) + 3f, 0);
            }
            else
            {
                toHealthBar.DamageRect.offsetMax = new Vector2((-toHealthBar.HealthBarRect.rect.width * (1 - toHealthBar.HealthFill.fillAmount)) + 3f, 0);
            }

            toHealthBar.NumberOfParts = fromHealthBar.NumberOfParts;
            EnableHealthBarParts(toHealthBar.Parts, fromHealthBar.NumberOfParts);
        }

        public void ResetHealthBarFillAmounts(HealthBar healthBar, Health.Component health)
        {
            float healthPercentage = (float) health.CurrentHealth / health.MaxHealth;

            healthBar.BgFill.fillAmount = 0;
            healthBar.DamageFill.fillAmount = 0;
            healthBar.HealthFill.fillAmount = healthPercentage;
            healthBar.ArmorFill.fillAmount = healthPercentage - .01f;
        }

        public void SetHealthBarFillAmounts(GameStateEnum gameState, UnitEffects unitEffects, UnitHeadUIReferences unitHeadUiRef, HealthBar healthBar, Health.Component health, uint unitFaction, uint playerFaction)
        {
            float combinedHealth = 0;
            float healthPercentage = 0;
            float armorPercentage = 0;

            if (gameState == GameStateEnum.planning)
            {
                combinedHealth = health.CurrentHealth + unitHeadUiRef.IncomingArmor;
                healthPercentage = (float) health.CurrentHealth / health.MaxHealth;
                armorPercentage = (float) unitHeadUiRef.IncomingArmor / combinedHealth;
                healthBar.BgFill.fillAmount = 0;

                if (combinedHealth < health.MaxHealth)
                {
                    healthBar.HealthFill.fillAmount = healthPercentage;
                }

                if (unitFaction == playerFaction)
                {
                    if (combinedHealth < health.MaxHealth)
                    {
                        healthBar.NumberOfParts = health.MaxHealth / 20;
                        //EnableHealthBarParts(healthBar.Parts,);
                        //healthBar.Parts.pixelsPerUnitMultiplier = (float) health.MaxHealth / 40f;
                        //healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);
                        FillBarToDesiredValue(healthBar.ArmorFill, healthBar.HealthFill.fillAmount + ((float) unitHeadUiRef.IncomingArmor / health.MaxHealth));
                        //healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, healthBar.HealthFill.fillAmount + ((float) health.Armor / health.MaxHealth), Time.DeltaTime);
                    }
                    else
                    {
                        healthBar.NumberOfParts = (uint) combinedHealth / 20;
                        //EnableHealthBarParts(healthBar.Parts, );
                        //healthBar.Parts.pixelsPerUnitMultiplier = combinedHealth / 40f;
                        //healthBar.Parts.material.mainTextureScale = new Vector2(combinedHealth / 20f, 1f);
                        FillBarToDesiredValue(healthBar.HealthFill, 1 - armorPercentage);
                        FillBarToDesiredValue(healthBar.ArmorFill, 1);
                        //healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, 1 - armorPercentage, Time.DeltaTime);
                        //healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, 1, Time.DeltaTime);
                    }
                    FillBarToDesiredValue(healthBar.DamageFill, (float) unitHeadUiRef.IncomingDamage / combinedHealth);
                    //healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float) unitHeadUiRef.IncomingDamage / combinedHealth, Time.DeltaTime);
                    healthBar.DamageRect.offsetMax = new Vector2((-healthBar.HealthBarRect.rect.width * (1 - healthBar.ArmorFill.fillAmount)) + 3f, 0);
                }
                else
                {
                    healthBar.NumberOfParts = health.MaxHealth / 20;
                    //EnableHealthBarParts(healthBar.Parts, );
                    //healthBar.Parts.pixelsPerUnitMultiplier = (float)health.MaxHealth / 40f;
                    //healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);

                    FillBarToDesiredValue(healthBar.HealthFill, healthPercentage);
                    //healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, healthPercentage, Time.DeltaTime);
                    healthBar.ArmorFill.fillAmount = healthBar.HealthFill.fillAmount - 0.01f;


                    FillBarToDesiredValue(healthBar.DamageFill, (float) unitHeadUiRef.IncomingDamage / (float) health.CurrentHealth);
                    //healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float) unitHeadUiRef.IncomingDamage / (float) health.CurrentHealth, Time.DeltaTime);
                    healthBar.DamageRect.offsetMax = new Vector2((-healthBar.HealthBarRect.rect.width * (1 - healthBar.HealthFill.fillAmount)) + 3f, 0);
                }
                EnableHealthBarParts(healthBar.Parts, healthBar.NumberOfParts);
            }
            else
            {
                combinedHealth = unitEffects.CurrentHealth + unitEffects.CurrentArmor;
                healthPercentage = (float) unitEffects.CurrentHealth / health.MaxHealth;
                armorPercentage = (float) unitEffects.CurrentArmor / combinedHealth;

                //Debug.Log("CombinedHealth: " + combinedHealth + ", armorPercentage: " + armorPercentage);

                if (unitEffects.CurrentArmor > 0)
                {
                    if (combinedHealth < health.MaxHealth)
                    {
                        healthBar.NumberOfParts = health.MaxHealth / 20;
                        //EnableHealthBarParts(healthBar.Parts, health.MaxHealth / 20);
                        //healthBar.Parts.pixelsPerUnitMultiplier = (float) health.MaxHealth / 40f;
                        //healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);
                        FillBarToDesiredValue(healthBar.ArmorFill, healthBar.HealthFill.fillAmount + ((float) unitEffects.CurrentArmor / health.MaxHealth));
                        //healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, healthBar.HealthFill.fillAmount + ((float) unitEffects.CurrentArmor / health.MaxHealth), Time.DeltaTime);
                    }
                    else
                    {
                        healthBar.NumberOfParts = (uint) combinedHealth / 20;
                        //EnableHealthBarParts(healthBar.Parts,);
                        // healthBar.Parts.pixelsPerUnitMultiplier = combinedHealth / 40f;
                        //healthBar.Parts.material.mainTextureScale = new Vector2(combinedHealth / 20f, 1f);
                        FillBarToDesiredValue(healthBar.HealthFill, 1 - healthBar.BgFill.fillAmount - armorPercentage);
                        FillBarToDesiredValue(healthBar.ArmorFill, 1);
                        //healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, 1 - healthBar.BgFill.fillAmount - armorPercentage, Time.DeltaTime);
                        //healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, 1, Time.DeltaTime);
                    }
                }
                else if (unitFaction != playerFaction)
                {
                    healthBar.NumberOfParts = health.MaxHealth / 20;
                    //EnableHealthBarParts(healthBar.Parts, health.MaxHealth / 20);
                    //healthBar.Parts.pixelsPerUnitMultiplier = combinedHealth / 40f;
                    //healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);
                    FillBarToDesiredValue(healthBar.HealthFill, healthPercentage);
                    //healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, healthPercentage, Time.DeltaTime);
                    healthBar.ArmorFill.fillAmount = healthBar.HealthFill.fillAmount - 0.01f;
                }

                EnableHealthBarParts(healthBar.Parts, healthBar.NumberOfParts);
                healthBar.DamageFill.fillAmount = 0;
            }
        }

        public void FillBarToDesiredValue(Image fill, float targetValue, float speed = 1f)
        {
            //increment fillamount if it is smaller than targetValue
            if (fill.fillAmount < targetValue - Time.DeltaTime * speed)
                fill.fillAmount += Time.DeltaTime * speed;
            //else decrement it
            else if (fill.fillAmount > targetValue + Time.DeltaTime * speed)
                fill.fillAmount -= Time.DeltaTime * speed;
            else
                fill.fillAmount = targetValue;
        }

        public void EnableHealthBarParts(List<GameObject> HealthBarParts, uint numberToEnable)
        {
            for (int i = 0; i < HealthBarParts.Count; i++)
            {
                if (i < numberToEnable)
                {
                    HealthBarParts[i].SetActive(true);
                }
                else
                {
                    HealthBarParts[i].SetActive(false);
                }
            }
        }

        public void SetArmorDisplay(Entity e, uint inArmorAmount, float displayTime, bool shatter = false)
        {
            var healthbar = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);

            if (healthbar.UnitHeadUIInstance)
            {
                Text armorText = healthbar.UnitHeadUIInstance.ArmorText;
                Animator anim = healthbar.UnitHeadUIInstance.ArmorAnimator;
                GameObject armorPanel = healthbar.UnitHeadUIInstance.ArmorPanel;
                healthbar.UnitHeadUIInstance.PlanningBufferTime = 0;
                armorText.text = inArmorAmount.ToString();
                armorPanel.SetActive(true);

                if (shatter)
                {
                    anim.SetTrigger("Shatter");
                    healthbar.UnitHeadUIInstance.PlanningBufferTime = displayTime;
                }
            }
        }

        public void TriggerUnitDeathUI(Entity e, uint unitFaction, uint authPlayerFaction, long unitID, bool displayDeathSkull)
        {
            var unitCompRef = EntityManager.GetComponentObject<UnitComponentReferences>(e);
            var headUI = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);

            CleanupUnitUI(unitCompRef.IsVisibleRefComp, unitCompRef.HeadUIRef, unitCompRef.BaseDataSetComp, unitID, unitFaction, authPlayerFaction);

            if (headUI.UnitHeadUIInstance)
            {
                if(displayDeathSkull)
                    headUI.UnitHeadUIInstance.DeathBlowImage.SetActive(true);
                Object.Destroy(headUI.UnitHeadUIInstance.gameObject, headUI.UnitHeadUIInstance.DestroyWaitTime);
            }

            if (headUI.UnitHeadHealthBarInstance)
                Object.Destroy(headUI.UnitHeadHealthBarInstance.gameObject);
        }

        public void SetHealthFloatText(Entity e, bool positive, uint inHealthAmount, Color color, float waitTime = 0f)
        {
            var healthbar = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);
            Text healthText = healthbar.UnitHeadUIInstance.FloatHealthText;
            Animator anim = healthbar.UnitHeadUIInstance.FloatHealthAnimator;

            if (healthText)
            {
                if (positive)
                {
                    healthText.text = "+" + inHealthAmount.ToString();
                }
                else
                {
                    healthText.text = "-" + inHealthAmount.ToString();
                }

                healthText.color = color;
            }

            if (healthbar.UnitHeadUIInstance && UIRef.UIActive)
            {
                if (waitTime != 0f)
                {
                    //Debug.Log("SetHealthFloatTextDelayed");
                    healthbar.HealthTextDelay = waitTime;
                    healthbar.UnitHeadUIInstance.FloatHealthAnimator.SetFloat("WaitTime", waitTime);
                    healthbar.UnitHeadUIInstance.FloatHealthAnimator.SetBool("Delayed", true);
                }
                else
                {
                    //Debug.Log("SetHealthFloatTextDirectly");
                    healthbar.UnitHeadUIInstance.FloatHealthAnimator.SetBool("Delayed", false);
                    anim.Play("HealthText", 0, 0);
                }
            }
        }

        public void InitializeUnitUI(PlayerEnergy.Component playerEnergy, Actions.Component actions, UnitComponentReferences unitCompRef, UnitDataSet stats, long unitId, uint unitFaction, uint playerFaction, uint worldIndex, Moba_Camera playerCam)
        {
            unitCompRef.HeadUIRef.UnitHeadUIInstance = Object.Instantiate(unitCompRef.HeadUIRef.UnitHeadUIPrefab, unitCompRef.HeadUIRef.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
            unitCompRef.HeadUIRef.UnitHeadHealthBarInstance = unitCompRef.HeadUIRef.UnitHeadUIInstance.HealthBar;
            unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.transform.SetParent(UIRef.HealthBarsPanel.transform, false);

            if (unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.PlayerColorImage && unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.HoveredImage)
            {
                unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.PlayerColorImage.color = settings.FactionColors[(int) unitFaction];
                unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.HoveredImage.color = settings.FactionColors[(int) unitFaction];
            }

            if (unitFaction != 0)
            {
                unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.color = settings.FactionIncomeColors[(int) unitFaction];
                unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainImage.color = settings.FactionIncomeColors[(int) unitFaction];
            }
            unitCompRef.HeadUIRef.UnitHeadUIInstance.ArmorPanel.SetActive(false);
            //initialize GroupUI and hero select button
            if (unitFaction == playerFaction && playerFaction != 0)
            {
                if (!stats.IsHero)
                {
                    if (!UIRef.ExistingUnitGroups.ContainsKey(stats.UnitTypeId))
                    {
                        UnitGroupUI unitGroup;
                        //spawn a group into groups parent and add it to the ExistingUnitGroups Dict

                        //if peasant
                        if (stats.EnergyIncome > 0)
                        {
                            //Set Dict position = child position
                            unitGroup = Object.Instantiate(UIRef.UnitGroupPeasantPrefab, UIRef.UnitGroupsParent.transform);
                            unitGroup.transform.SetAsFirstSibling();
                        }
                        else
                        {
                            unitGroup = Object.Instantiate(UIRef.UnitGroupPrefab, UIRef.UnitGroupsParent.transform);
                        }

                        unitGroup.UnitTypeImage.sprite = stats.UnitGroupSprite;
                        //if faction is even set to factionColors 1 if odd to factioncolors2
                        unitGroup.EnergyFill.color = settings.FactionColors[(int) playerFaction];
                        unitGroup.EnergyGainFill.color = UIRef.FriendlyIncomeColor;
                        SelectUnitButton unitButton = Object.Instantiate(UIRef.UnitButtonPrefab, unitGroup.UnitsPanel.transform);
                        unitButton.UnitId = unitId;
                        unitButton.EnergyFill.color = settings.FactionColors[(int) playerFaction];

                        if (stats.UnitSprite)
                            unitButton.UnitIcon.sprite = stats.UnitSprite;
                        else
                            unitButton.UnitIcon.sprite = stats.UnitGroupSprite;


                        if (!unitGroup.SelectUnitButtons.Contains(unitButton))
                            unitGroup.SelectUnitButtons.Add(unitButton);

                        stats.SelectUnitButtonInstance = unitButton;
                        //problematic if unit has a locked action
                        //unitButton.UnitButton.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(-2, unitId); });
                        unitButton.UnitButton.onClick.AddListener(delegate {
                            SetSelectedUnitId(unitId);
                            FillUnitButtons(actions, stats, unitFaction, playerFaction, unitId, playerEnergy);
                            SetPortraitInfo(stats, unitFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor, true); playerCam.settings.cameraLocked = true;
                        });
                        unitButton.rightClick.AddListener(delegate
                        {
                            FillInspectWindowInformation(actions, unitCompRef.BaseDataSetComp);
                            SwapActiveMenuPanel(UIRef.UnitInspection.gameObject);
                            SetInspectionPortraitInfo(playerFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor);
                            foreach (ActionButton b in UIRef.UnitInspection.HoverOnlyButtons)
                            {
                                b.Hovered = false;
                            }
                        });
                        unitGroup.ExistingUnitIds.Add(unitId);
                        unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                        UIRef.ExistingUnitGroups.Add(stats.UnitTypeId, unitGroup);
                        //IF PEASANT SET AS FIRST SIBLING
                    }
                    else
                    {
                        //if a group of this type already exists, instanciate a button inside it for this unit
                        UnitGroupUI unitGroup = UIRef.ExistingUnitGroups[stats.UnitTypeId];

                        if (!unitGroup.ExistingUnitIds.Contains(unitId))
                        {
                            SelectUnitButton unitButton = Object.Instantiate(UIRef.UnitButtonPrefab, unitGroup.UnitsPanel.transform);
                            unitButton.EnergyFill.color = settings.FactionColors[(int) playerFaction];
                            unitButton.UnitId = unitId;

                            if (stats.UnitSprite)
                                unitButton.UnitIcon.sprite = stats.UnitSprite;
                            else
                                unitButton.UnitIcon.sprite = stats.UnitGroupSprite;

                            if (!unitGroup.SelectUnitButtons.Contains(unitButton))
                                unitGroup.SelectUnitButtons.Add(unitButton);
                            stats.SelectUnitButtonInstance = unitButton;
                            //problematic if unit has a locked action
                            //unitButton.UnitButton.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(-2, unitId); });
                            unitButton.UnitButton.onClick.AddListener(delegate {
                                SetSelectedUnitId(unitId);
                                FillUnitButtons(actions, stats, unitFaction, playerFaction, unitId, playerEnergy);
                                SetPortraitInfo(stats, unitFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor, true); playerCam.settings.cameraLocked = true;
                            });
                            unitButton.rightClick.AddListener(delegate
                            {
                                FillInspectWindowInformation(actions, unitCompRef.BaseDataSetComp);
                                SwapActiveMenuPanel(UIRef.UnitInspection.gameObject);
                                SetInspectionPortraitInfo(playerFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor);
                                foreach (ActionButton b in UIRef.UnitInspection.HoverOnlyButtons)
                                {
                                    b.Hovered = false;
                                }
                            });
                            unitGroup.ExistingUnitIds.Add(unitId);
                            unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                        }
                    }
                }
                else
                {
                    UIRef.SelectHeroButton.UnitId = unitId;
                    UIRef.SelectHeroButton.UnitButton.onClick.AddListener(delegate {
                        SetSelectedUnitId(unitId);
                        FillUnitButtons(actions, stats, unitFaction, playerFaction, unitId, playerEnergy);
                        SetPortraitInfo(stats, unitFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.TeamColorMeshesComp.color, true); playerCam.settings.cameraLocked = true;
                    });
                    UIRef.SelectHeroButton.rightClick.AddListener(delegate
                    {
                        FillInspectWindowInformation(actions, unitCompRef.BaseDataSetComp);
                        SwapActiveMenuPanel(UIRef.UnitInspection.gameObject);
                        SetInspectionPortraitInfo(playerFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.UnitEffectsComp.PlayerColor);
                        foreach (ActionButton b in UIRef.UnitInspection.HoverOnlyButtons)
                        {
                            b.Hovered = false;
                        }
                    });
                }
            }
            else if (unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay)
                Object.Destroy(unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject);
        }

        public void InitializeManalithUnitUI(UnitHeadUIReferences headUIRef)
        {
            headUIRef.UnitHeadUIInstance = Object.Instantiate(headUIRef.UnitHeadUIPrefab, headUIRef.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
        }

        void CleanupUnitUI(IsVisibleReferences isVisibleRef, UnitHeadUIReferences unitHeadUIRef, UnitDataSet stats, long unitID, uint unitFaction, uint playerFaction)
        {
            if (isVisibleRef.MiniMapTileInstance)
            {
                if (isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect && isVisibleRef.MiniMapTileInstance.isActiveAndEnabled)
                {
                    isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.Rect.sizeDelta = isVisibleRef.MiniMapTileInstance.DeathCrossSize;
                    isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.transform.SetParent(UIRef.MinimapComponent.MiniMapEffectsPanel.transform, false);
                    isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.gameObject.SetActive(true);

                    if (isVisibleRef.MiniMapTileInstance.EmitSoundEffect && isVisibleRef.MiniMapTileInstance.isActiveAndEnabled)
                        isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.FMODEmitter.Play();
                }
                /*
                logger.HandleLog(LogType.Warning,
                new LogEvent("Destroy MapTile")
                .WithField("Unit Name", isVisibleRef.transform.name));
                */
                Object.Destroy(isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.gameObject, 2f);
                Object.Destroy(isVisibleRef.MiniMapTileInstance.gameObject, 0.5f);
            }

            if (isVisibleRef.BigMapTileInstance)
            {
                if (isVisibleRef.BigMapTileInstance.DeathBlowMapEffect && isVisibleRef.BigMapTileInstance.isActiveAndEnabled)
                {
                    isVisibleRef.BigMapTileInstance.DeathBlowMapEffect.Rect.sizeDelta = isVisibleRef.BigMapTileInstance.DeathCrossSize;
                    isVisibleRef.BigMapTileInstance.DeathBlowMapEffect.transform.parent = UIRef.BigMapComponent.MiniMapEffectsPanel.transform;
                    isVisibleRef.BigMapTileInstance.DeathBlowMapEffect.gameObject.SetActive(true);

                    if (isVisibleRef.BigMapTileInstance.EmitSoundEffect && isVisibleRef.BigMapTileInstance.isActiveAndEnabled)
                        isVisibleRef.BigMapTileInstance.DeathBlowMapEffect.FMODEmitter.Play();
                }

                Object.Destroy(isVisibleRef.BigMapTileInstance.DeathBlowMapEffect.gameObject, 2f);
                Object.Destroy(isVisibleRef.BigMapTileInstance.gameObject, 0.5f);
            }

            if (!stats.IsHero && unitFaction == playerFaction && UIRef.ExistingUnitGroups.ContainsKey(stats.UnitTypeId))
            {
                //remove unitID from unitGRPUI / delete selectUnitButton
                UnitGroupUI unitGroup = UIRef.ExistingUnitGroups[stats.UnitTypeId];
                if (unitGroup.SelectUnitButtons.Contains(stats.SelectUnitButtonInstance))
                    unitGroup.SelectUnitButtons.Remove(stats.SelectUnitButtonInstance);
                unitGroup.ExistingUnitIds.Remove(unitID);
                unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                Object.Destroy(stats.SelectUnitButtonInstance.gameObject);

                if (unitGroup.ExistingUnitIds.Count == 0)
                {
                    UIRef.ExistingUnitGroups.Remove(stats.UnitTypeId);
                    Object.Destroy(unitGroup.gameObject);
                }
            }

            unitHeadUIRef.UnitHeadUIInstance.FlagForDestruction = true;
        }

        public void InitializeSelectedActionTooltip(ActionButton actionButton, SelectedActionToolTip inTooltip)
        {
            inTooltip.EnergyText.text = actionButton.EnergyCost.ToString();
            inTooltip.ActionDescription.text = actionButton.ActionDescription;
            inTooltip.ActionName.text = actionButton.ActionName;
            inTooltip.ExecuteStepIcon.sprite = UIRef.ExecuteStepSprites[actionButton.ExecuteStepIndex];
            inTooltip.ExecuteStepIcon.color = settings.TurnStepBgColors[actionButton.ExecuteStepIndex];
        }
        public void CancelLockedAction()
        {
            var playerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
            var playerFaction = m_AuthoritativePlayerData.GetSingleton<FactionComponent.Component>();
            var playerHigh = m_AuthoritativePlayerData.GetSingleton<HighlightingDataComponent>();

            Entities.ForEach((Entity e, ref SpatialEntityId unitId,  ref Actions.Component actions, in FactionComponent.Component faction) =>
            {
                if (unitId.EntityId.Id == playerState.SelectedUnitId && faction.Faction == playerFaction.Faction)
                {
                    if (actions.LockedAction.Index != -3 || actions.CurrentSelected.Index != -3)
                    {
                        m_SendActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                        //Call methods so line/target gets disabled instantly
                        m_HighlightingSystem.ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id, playerHigh);
                    }
                }
            })
            .WithoutBurst()
            .Run();


            Entities.ForEach((Entity e, ref SpatialEntityId unitId, ref Actions.Component actions, ref Manalith.Component m, in FactionComponent.Component faction) =>
            {
                if (unitId.EntityId.Id == playerState.SelectedUnitId && faction.Faction == playerFaction.Faction)
                {
                    if (actions.LockedAction.Index != -3 || actions.CurrentSelected.Index != -3)
                    {
                        m_SendActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                        //Call methods so line/target gets disabled instantly
                        m_HighlightingSystem.ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id, playerHigh);
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        public void FillInspectWindowInformation(Actions.Component actions, UnitDataSet stats)
        {
            int actionCount = stats.Actions.Count;
            UIRef.UnitInspection.UnitDescription.text = stats.UnitDescription;
            UIRef.UnitInspection.UnitName.text = stats.UnitName;

            for (int i = 0; i < 6; i++)
            {
                if (i < actionCount)
                {
                    UIRef.UnitInspection.HoverOnlyButtons[i] = FillButtonFields(UIRef.UnitInspection.HoverOnlyButtons[i], stats, 0, i, false);
                    UIRef.UnitInspection.HoverOnlyButtons[i].Visuals.gameObject.SetActive(true);

                }
                else
                {
                    UIRef.UnitInspection.HoverOnlyButtons[i].Visuals.gameObject.SetActive(false);
                }
            }
        }

        public void SetInspectionPortraitInfo(uint faction, List<AnimationClip> animatedPortraits, Color teamColor)
        {
            if (animatedPortraits.Count != 0 && UIRef.UnitInspection.Portrait.AnimatorOverrideController.animationClips[0].GetHashCode() != animatedPortraits[(int) faction].GetHashCode())
            {
                UIRef.UnitInspection.Portrait.AnimatorOverrideController["KingCroakPortrait"] = animatedPortraits[(int) faction];
                UIRef.UnitInspection.PortraitGlow.color = teamColor;
            }
        }
    }
}
