using FMODUnity;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Cell;
using Improbable;
using Unity.Jobs;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UISystem : JobComponentSystem
    {
        //EventSystem m_EventSystem;
        HighlightingSystem m_HighlightingSystem;
        PlayerStateSystem m_PlayerStateSystem;
        SendActionRequestSystem m_SendActionRequestSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        CommandSystem m_CommandSystem;
        EntityQuery m_PlayerData;
        EntityQuery m_AuthoritativePlayerData;
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        EntityQuery m_ManalithUnitData;
        EntityQuery m_ManalithData;
        DollyCameraComponent dollyCam;

        public UIReferences UIRef { get; set; }

        Settings settings;

        protected override void OnCreate()
        {
            base.OnCreate();
            //m_EventSystem = Object.FindObjectOfType<EventSystem>();

            m_PlayerStateSystem = World.GetExistingSystem<PlayerStateSystem>();
            m_SendActionRequestSystem = World.GetExistingSystem<SendActionRequestSystem>();
            settings = Resources.Load<Settings>("Settings");

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<Energy.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<UnitDataSet>(),
                ComponentType.ReadOnly<MouseState>(),
                ComponentType.ReadOnly<Health.Component>(),
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
                ComponentType.ReadOnly<GameState.Component>()
                );

            m_AuthoritativePlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<HighlightingDataComponent>(),
                ComponentType.ReadWrite<Moba_Camera>(),
                ComponentType.ReadWrite<PlayerState.Component>()
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
            dollyCam = Object.FindObjectOfType<DollyCameraComponent>();
            m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CommandSystem = World.GetExistingSystem<CommandSystem>();
            UIRef = Object.FindObjectOfType<UIReferences>();
            UIRef.MasterBus = RuntimeManager.GetBus(UIRef.MasterBusString);
            UIRef.IngameSFXBus = RuntimeManager.GetBus(UIRef.InGameSFXBusString);
            UIRef.EnvironmentBus = RuntimeManager.GetBus(UIRef.EnvironmentString);
            UIRef.SFXBus = RuntimeManager.GetBus(UIRef.SFXBusString);
            UIRef.MusicBus = RuntimeManager.GetBus(UIRef.MusicBusString);
            UIRef.UINonMapSFXBus = RuntimeManager.GetBus(UIRef.UINonMapSFXBusString);

            InitializeButtons();
            AddSettingsListeners();
        }

        public void InitializeButtons()
        {
            UIRef.MainMenuButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.EscapeMenu.gameObject); SetEscapePanelMenuActive(0); });
            UIRef.HelpButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.HelpPanel); });
            UIRef.HelpButton.Button.onClick.AddListener(delegate { SetHelpPanelMenuActive(0); });
            UIRef.SkilltreeButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.SkillTreePanel); });
            UIRef.EscapeMenu.ExitGameButton.onClick.AddListener(delegate { Application.Quit(); });

            UIRef.CancelActionButton.onClick.AddListener(delegate { CancelLockedAction(); });
            UIRef.RevealVisionButton.onClick.AddListener(delegate { m_SendActionRequestSystem.RevealPlayerVision(); });

            UIRef.TurnStatePnl.GOButtonScript.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetCancelTimer(UIRef.TurnStatePnl.CacelGraceTime);});
            UIRef.TurnStatePnl.GOButtonScript.Button.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.waiting); });
            UIRef.TurnStatePnl.GOButtonScript.Button.onClick.AddListener(delegate { m_HighlightingSystem.ResetHighlightsNoIn(); });

            UIRef.EscapeMenu.ConcedeButton.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.conceded); });

            for (int pi = 0; pi < UIRef.HelpPanelComponent.TabButtons.Count; pi++)
            {
                //Debug.Log("Add Esc button listener with index: " + pi);
                MainMenuTabButton ab = UIRef.HelpPanelComponent.TabButtons[pi];
                ab.index = pi;
                ab.Button.onClick.AddListener(delegate { SetHelpPanelMenuActive(ab.index); });
            }

            for (int pi = 0; pi < UIRef.EscapeMenu.PanelButtons.Count; pi++)
            {
                //Debug.Log("Add Esc button listener with index: " + pi);
                MainMenuTabButton ab = UIRef.EscapeMenu.PanelButtons[pi];
                ab.index = pi;
                ab.Button.onClick.AddListener(delegate { SetEscapePanelMenuActive(ab.index); });
            }

            for (int bi = 0; bi < UIRef.Actions.Count; bi++)
            {
                ActionButton ab = UIRef.Actions[bi];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
                ab.Button.onClick.AddListener(delegate { InitializeSelectedActionTooltip(ab); });
                ab.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetInputCoolDown(0.3f); });
            }

            for (int si = 0; si < UIRef.SpawnActions.Count; si++)
            {
                ActionButton ab = UIRef.SpawnActions[si];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
                ab.Button.onClick.AddListener(delegate { InitializeSelectedActionTooltip(ab); });
                ab.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetInputCoolDown(0.3f); });
            }
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
                if(v.HoveredHandler.Hovered)
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

        ActionButton FillButtonFields(ActionButton inButton, UnitDataSet inBaseData, long inUnitId, int inIndex, bool isSpawnAction, bool hasBasicAction)
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
            inButton.TurnStepBauble.color = settings.TurnStepColors[inButton.ExecuteStepIndex + 1];

            return inButton;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_GameStateData.CalculateEntityCount() == 0 || m_AuthoritativePlayerData.CalculateEntityCount() == 0)
                return inputDeps;

            #region GetData
            var authPlayerEntity = m_AuthoritativePlayerData.GetSingletonEntity();
            var authPlayerCam = EntityManager.GetComponentObject<Moba_Camera>(authPlayerEntity);

            var gameState = m_GameStateData.GetSingleton<GameState.Component>();
            var authPlayerFaction = m_AuthoritativePlayerData.GetSingleton<FactionComponent.Component>();
            var authPlayerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
            var playerEnergy = m_AuthoritativePlayerData.GetSingleton<PlayerEnergy.Component>();
            var playerHigh = m_AuthoritativePlayerData.GetSingleton<HighlightingDataComponent>();
            #endregion

            var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.InitializeMapEvent.Event>();
            var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();
            var energyChangeEvents = m_ComponentUpdateSystem.GetEventsReceived<PlayerEnergy.EnergyChangeEvent.Event>();

            /*
            var armorChangeEvents = m_ComponentUpdateSystem.GetEventsReceived<Health.ArmorChangeEvent.Event>();

            for (int i = 0; i < armorChangeEvents.Count; i++)
            {
                var armorChangeUnitId = armorChangeEvents[i].EntityId.Id;

                Entities.ForEach((ref SpatialEntityId id, ref Health.Component health, ref FactionComponent.Component faction) =>
                {
                    if(authPlayerState.SelectedUnitId == id.EntityId.Id && armorChangeUnitId == id.EntityId.Id)
                        UpdatePortraitHealthText(authPlayerFaction.Faction, faction.Faction, health);
                })
                .WithoutBurst()
                .Run();
            }
            */

            if (initMapEvents.Count > 0)
            {
                ClearUnitUIElements();

                UIRef.FriendlyIncomeColor = settings.FactionIncomeColors[(int)authPlayerFaction.Faction];
                UIRef.FriendlyColor = settings.FactionColors[(int)authPlayerFaction.Faction];

                if(authPlayerFaction.Faction == 1)
                    UIRef.EnemyColor = settings.FactionColors[2];
                else
                    UIRef.EnemyColor = settings.FactionColors[1];

                UIRef.HeroEnergyIncomeFill.color = UIRef.FriendlyIncomeColor;
                UIRef.TotalEnergyIncomeText.color = UIRef.FriendlyIncomeColor;

                UIRef.HeroPortraitPlayerColor.color = UIRef.FriendlyColor;

                UIRef.HeroCurrentEnergyFill.color = UIRef.FriendlyColor;
                UIRef.TopEnergyFill.color = UIRef.FriendlyColor;

                UIRef.TurnStatePnl.FriendlyReadyDot.color = UIRef.FriendlyColor;
                UIRef.TurnStatePnl.FriendlyRope.color = UIRef.FriendlyColor;
                UIRef.EnergyConnectorPlayerColorFill.color = UIRef.FriendlyColor;

                UIRef.TurnStatePnl.EnemyReadyDot.color = UIRef.EnemyColor;
                UIRef.TurnStatePnl.EnemyRope.color = UIRef.EnemyColor;

                UIRef.TurnStatePnl.GOButtonScript.LightCircle.color = UIRef.FriendlyColor;
                UIRef.TurnStatePnl.GOButtonScript.LightFlare.color = UIRef.FriendlyColor;
                UIRef.TurnStatePnl.GOButtonScript.LightInner.color = UIRef.FriendlyColor;

                var fBurst = UIRef.FriendlyReadyBurstPS.main;
                fBurst.startColor = UIRef.FriendlyColor;
                var eBurst = UIRef.EnemyReadyBurstPS.main;
                eBurst.startColor = UIRef.EnemyColor;

                var main1 = UIRef.FriendlyRopeBarParticle.LoopPS.main;
                main1.startColor = UIRef.FriendlyColor;

                var main2 = UIRef.EnemyRopeBarParticle.LoopPS.main;
                main2.startColor = UIRef.EnemyColor;

                if (!UIRef.MatchReadyPanel.activeSelf)
                {
                    UIRef.MatchReadyPanel.SetActive(true);
                }
            }

            if (energyChangeEvents.Count > 0)
            {
                //Only convert energy numbers to string when energy change event is fired
                UIRef.CurrentEnergyText.text = playerEnergy.Energy.ToString();
                UIRef.MaxEnergyText.text = playerEnergy.MaxEnergy.ToString();
            }

            if (cleanUpStateEvents.Count > 0)
            {
                Entities.ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Health.Component health, ref Energy.Component energy, in FactionComponent.Component faction) =>
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
                })
                .WithoutBurst()
                .Run();

                if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.planning)
                {
                    if (UIRef.EscapeMenu.TurnOverrideInputField.text == "")
                        FireStepChangedEffects("Turn " + gameState.TurnCounter, settings.TurnStepColors[0], UIRef.PlanningSlideInPath);
                    else
                        FireStepChangedEffects("Turn " + UIRef.EscapeMenu.TurnOverrideInputField.text, settings.TurnStepColors[0], UIRef.PlanningSlideInPath);

                    UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.planning;
                }

                foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                {
                    g.CleanupReset = true;
                }

                UIRef.GameOverPanel.SetActive(false);

                UIRef.TurnStatePnl.FriendlyReadyDot.enabled = false;
                UIRef.TurnStatePnl.EnemyReadyDot.enabled = false;

                UIRef.RopeSlamOneTime = false;
                UIRef.RopeEndsLerpTime = 0;

                UIRef.TurnStatePnl.FriendlyRope.fillAmount = 0;
                UIRef.TurnStatePnl.EnemyRope.fillAmount = 0;

                UIRef.TurnStatePnl.EnemyRope.color = UIRef.EnemyColor;
                UIRef.TurnStatePnl.FriendlyRope.color = UIRef.FriendlyColor;

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

            if (dollyCam.RevealVisionTrigger)
            {
                m_SendActionRequestSystem.RevealPlayerVision();
                dollyCam.RevealVisionTrigger = false;
            }
            
            HandleTutorialVideoPlayers();

            UnitLoop(authPlayerState, authPlayerFaction.Faction, gameState, playerEnergy, playerHigh);

            ManalithUnitLoop(authPlayerState, authPlayerFaction.Faction, gameState, playerEnergy, playerHigh);

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetBool("Planning", false);
                UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetInteger("TurnStep", (int)gameState.CurrentState - 2);

                if (UIRef.TurnStatePnl.GOButtonScript.Button.interactable)
                {
                    UIRef.TurnStatePnl.GOButtonScript.Button.interactable = false;
                }

                UIRef.TurnStatePnl.RopeLoopEmitter.Stop();
            }
            else
            {
                UpdateActionHoverTooltip();
                UIRef.TurnStatePnl.ExecuteStepPanelAnimator.SetBool("Planning", true);

                var hoveredTurnstepIndex = -1;
                for(int i = 0; i < UIRef.TurnStatePnl.TurnStepHoveredHandlers.Count; i++)
                {
                    if(UIRef.TurnStatePnl.TurnStepHoveredHandlers[i].Hovered)
                    {
                        hoveredTurnstepIndex = i;
                        UIRef.TurnStatePnl.TurnStepHoveredHandlers[i].HoverPanel.SetActive(true);
                    }
                    else
                        UIRef.TurnStatePnl.TurnStepHoveredHandlers[i].HoverPanel.SetActive(false);
                }

                if(hoveredTurnstepIndex != -1)
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

                if (UIRef.HeroCurrentEnergyFill.fillAmount >= (float) playerEnergy.Energy / playerEnergy.MaxEnergy * UIRef.MaxFillAmount - .003f)
                {
                    UIRef.HeroEnergyIncomeFill.fillAmount = Mathf.Lerp(UIRef.HeroEnergyIncomeFill.fillAmount, Mathf.Clamp(playerEnergy.Energy + playerEnergy.Income, 0, playerEnergy.MaxEnergy) / (float) playerEnergy.MaxEnergy * UIRef.MaxFillAmount, Time.DeltaTime);
                    //FillBarToDesiredValue(UIRef.HeroEnergyIncomeFill, Mathf.Clamp(playerEnergy.Energy + playerEnergy.Income, 0, playerEnergy.MaxEnergy) / (float) playerEnergy.MaxEnergy * UIRef.MaxFillAmount, UIRef.EnergyLerpSpeed);
                }
            }

            switch (gameState.CurrentState)
            {
                case GameStateEnum.planning:
                    foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                    {
                        UpdateUnitGroupBauble(g, authPlayerFaction, g.transform.GetSiblingIndex());
                    }
                    break;
                case GameStateEnum.interrupt:
                    if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.readyFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 2], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.interruptFired;
                    }
                    break;
                case GameStateEnum.attack:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.attackFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 2], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.attackFired;
                    }
                    break;
                case GameStateEnum.move:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.moveFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 2], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.moveFired;
                    }
                    break;
                case GameStateEnum.skillshot:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.skillshotFired)
                    {
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 2], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.skillshotFired;
                    }
                    break;
                case GameStateEnum.game_over:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.gameOverFired)
                    {
                        if (gameState.WinnerFaction == 0)
                        {
                            //TODO: ADD GAMEOVER SOUND EFFECTS
                            FireStepChangedEffects("Draw", settings.FactionColors[0], UIRef.ExecuteStepChangePath);
                        }
                        else if (gameState.WinnerFaction == authPlayerFaction.Faction)
                        {
                            FireStepChangedEffects("Victory", Color.green, UIRef.ExecuteStepChangePath);
                        }
                        else
                        {
                            FireStepChangedEffects("Defeat", Color.red, UIRef.ExecuteStepChangePath);
                        }
                        HandleGameOver(gameState, authPlayerFaction);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.gameOverFired;
                    }
                    break;
            }

            #region PlayerLoops

            HandleMenuSettings(authPlayerCam);

            UIRef.HeroCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.HeroCurrentEnergyFill.fillAmount, (float) playerEnergy.Energy / playerEnergy.MaxEnergy * UIRef.MaxFillAmount, Time.DeltaTime);

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
                            FireStepChangedEffects("Waiting", UIRef.FriendlyColor, UIRef.ReadySoundEventPath);
                            UIRef.TurnStatePnl.FriendlyReadyDot.enabled = true;
                            UIRef.FriendlyReadyBurstPS.time = 0;
                            UIRef.FriendlyReadyBurstPS.Play();
                            UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.readyFired;
                        }


                        UIRef.TurnStatePnl.FriendlyReadyDot.color -= new Color(0, 0, 0, UIRef.TurnStatePnl.ReadySwooshFadeOutSpeed * Time.DeltaTime);

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
                            FireStepChangedEffects("Enemy Waiting", UIRef.EnemyColor, UIRef.OpponentReadySoundEventPath);
                            UIRef.TurnStatePnl.EnemyReadyDot.enabled = true;
                            UIRef.EnemyReadyBurstPS.time = 0;
                            UIRef.EnemyReadyBurstPS.Play();
                            UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.enemyReadyFired;
                        }

                        //UIRef.EnemyReadySwoosh.fillAmount += UIRef.ReadyImpulseLerpSpeed * Time.DeltaTime;

                        //if (UIRef.EnemyReadySwoosh.fillAmount == 1 || UIRef.EnemyReadySwoosh.fillAmount == 0)
                        //{
                            //UIRef.EnemyReadySwooshParticle.LoopPS.Stop();
                        //}
                        //else
                        //{
                            //UIRef.EnemyReadySwooshParticle.Rect.anchoredPosition = new Vector2(1 - (UIRef.EnemyReadySwoosh.fillAmount * UIRef.EnemyReadySwooshParticle.ParentRect.sizeDelta.x), UIRef.EnemyReadySwooshParticle.Rect.anchoredPosition.y);
                            //UIRef.EnemyReadySwooshParticle.LoopPS.Play();
                        //}

                       // if (UIRef.EnemyReadySwoosh.fillAmount >= 1)
                        //{
                            UIRef.TurnStatePnl.EnemyReadyDot.color -= new Color(0, 0, 0, UIRef.TurnStatePnl.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                            //UIRef.EnemyReadySwoosh.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                        //}
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.OpponentReady = false;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            #endregion

            UIRef.TurnStatePnl.GOButtonScript.PlayerInCancelState = playerHigh.CancelState;

            //Handle Ropes
            if (gameState.CurrentRopeTime < gameState.RopeTime)
            {
                UIRef.EnemyRopeBarParticle.Rect.anchoredPosition = new Vector2(1 - (UIRef.TurnStatePnl.EnemyRope.fillAmount * UIRef.EnemyRopeBarParticle.ParentRect.sizeDelta.x), UIRef.EnemyRopeBarParticle.Rect.anchoredPosition.y);
                UIRef.FriendlyRopeBarParticle.Rect.anchoredPosition = new Vector2(UIRef.TurnStatePnl.FriendlyRope.fillAmount * UIRef.FriendlyRopeBarParticle.ParentRect.sizeDelta.x, UIRef.FriendlyRopeBarParticle.Rect.anchoredPosition.y);

                UIRef.TurnStatePnl.RopeTimeText.text = "0:" + ((int)gameState.CurrentRopeTime).ToString("D2");

                if (gameState.CurrentState == GameStateEnum.planning)
                {
                    UIRef.TurnStatePnl.RopeTimeText.enabled = true;

                    if (!UIRef.TurnStatePnl.RopeLoopEmitter.IsPlaying())
                        UIRef.TurnStatePnl.RopeLoopEmitter.Play();

                    UIRef.TurnStatePnl.RopeLoopEmitter.SetParameter("FadeInFastTikTok", 1 - gameState.CurrentRopeTime / gameState.RopeTime);

                    if (authPlayerState.CurrentState == PlayerStateEnum.ready)
                    {
                        UIRef.FriendlyRopeBarParticle.LoopPS.Play(false);
                        UIRef.TurnStatePnl.RopeTimeText.color = UIRef.FriendlyColor;

                        if (!UIRef.OpponentReady)
                            UIRef.TurnStatePnl.FriendlyRope.fillAmount = 1 - (gameState.CurrentRopeTime / gameState.RopeTime);
                    }
                    else
                    {
                        //GETTING ROPED
                        UIRef.EnemyRopeBarParticle.LoopPS.Play(false);
                        UIRef.TurnStatePnl.RopeTimeText.color = UIRef.EnemyColor;
                        UIRef.TurnStatePnl.FriendlyRope.fillAmount = 0;
                        UIRef.TurnStatePnl.EnemyRope.fillAmount = 1 - (gameState.CurrentRopeTime / gameState.RopeTime);
                    }
                }
                else
                {
                    //Find Center and Lerp both ropes to Center
                    if (!UIRef.RopeSlamOneTime)
                    {
                        UIRef.EnemyRopeEndFillAmount = UIRef.TurnStatePnl.EnemyRope.fillAmount;
                        UIRef.FriendlyRopeEndFillAmount = UIRef.TurnStatePnl.FriendlyRope.fillAmount;
                        UIRef.TurnStatePnl.RopeFillsEndDist = (1 - UIRef.FriendlyRopeEndFillAmount - UIRef.EnemyRopeEndFillAmount) / 2f;

                        UIRef.TurnStatePnl.RopeTimeText.enabled = false;
                        UIRef.RopeSlamOneTime = true;
                    }

                    if (UIRef.RopeEndsLerpTime < 1)
                    {
                        if (UIRef.RopeSlamOneTime)
                        {
                            UIRef.RopeEndsLerpTime += Time.DeltaTime * UIRef.TurnStatePnl.RopeEndLerpSpeed;
                            UIRef.TurnStatePnl.FriendlyRope.fillAmount = Mathf.Lerp(UIRef.FriendlyRopeEndFillAmount, UIRef.FriendlyRopeEndFillAmount + UIRef.TurnStatePnl.RopeFillsEndDist, UIRef.RopeEndsLerpTime);
                            UIRef.TurnStatePnl.EnemyRope.fillAmount = Mathf.Lerp(UIRef.EnemyRopeEndFillAmount, UIRef.EnemyRopeEndFillAmount + UIRef.TurnStatePnl.RopeFillsEndDist, UIRef.RopeEndsLerpTime);
                        }

                    }
                    else
                    {
                        if (UIRef.TurnStatePnl.FriendlyRope.color.a == 1)
                        {
                            UIRef.FriendlyRopeBarParticle.LoopPS.Stop();
                            UIRef.FriendlyRopeBarParticle.BurstPS.time = 0;
                            UIRef.FriendlyRopeBarParticle.BurstPS.Play();
                        }

                        if (UIRef.TurnStatePnl.EnemyRope.color.a == 1)
                        {
                            UIRef.EnemyRopeBarParticle.LoopPS.Stop();
                            UIRef.EnemyRopeBarParticle.BurstPS.time = 0;
                            UIRef.EnemyRopeBarParticle.BurstPS.Play();
                        }

                        UIRef.TurnStatePnl.FriendlyRope.color -= new Color(0, 0, 0, UIRef.TurnStatePnl.RopeEndFadeOutSpeed * Time.DeltaTime);
                        UIRef.TurnStatePnl.EnemyRope.color -= new Color(0, 0, 0, UIRef.TurnStatePnl.RopeEndFadeOutSpeed * Time.DeltaTime);
                    }
                }
            }
            else
            {
                UIRef.TurnStatePnl.RopeLoopEmitter.Stop();
                UIRef.FriendlyRopeBarParticle.LoopPS.Stop();
                UIRef.EnemyRopeBarParticle.LoopPS.Stop();
                UIRef.TurnStatePnl.RopeTimeText.enabled = false;
            }

            m_AuthoritativePlayerData.SetSingleton(playerHigh);

            HandleKeyCodeInput(gameState.CurrentState);

            return inputDeps;
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
            var anyButtonHovered = false;
            foreach(ActionButton b in UIRef.Actions)
            {
                if(b.Hovered)
                {
                    UIRef.SAToolTip.Rect.anchoredPosition = new Vector2(b.ButtonRect.anchoredPosition.x, UIRef.SAToolTip.Rect.anchoredPosition.y);
                    UIRef.SAToolTip.gameObject.SetActive(true);
                    InitializeSelectedActionTooltip(b);
                    anyButtonHovered = true;
                }
            }
            foreach (ActionButton b in UIRef.SpawnActions)
            {
                if (b.Hovered)
                {
                    UIRef.SAToolTip.Rect.anchoredPosition = new Vector2(b.ButtonRect.anchoredPosition.x, UIRef.SAToolTip.Rect.anchoredPosition.y);
                    UIRef.SAToolTip.gameObject.SetActive(true);
                    InitializeSelectedActionTooltip(b);
                    anyButtonHovered = true;
                }
            }

            if(!anyButtonHovered)
                UIRef.SAToolTip.gameObject.SetActive(false);
        }

        public void UnitLoop(PlayerState.Component authPlayerState, uint authPlayerFaction, GameState.Component gameState, PlayerEnergy.Component playerEnergy, HighlightingDataComponent playerHigh)
        {
            Entities.ForEach((Entity e, UnitComponentReferences unitCompRef, ref Energy.Component energy, ref Actions.Component actions, ref Health.Component health, ref IsVisible isVisible, ref MouseState mouseState, in FactionComponent.Component faction) =>
            {
                uint unitId = (uint)EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);

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
                    InitializeUnitUI(playerEnergy, actions, unitCompRef, unitCompRef.BaseDataSetComp, unitId, faction.Faction, authPlayerFaction);
                    unitCompRef.BaseDataSetComp.UIInitialized = true;
                }
                else
                {
                    if (unitCompRef.UnitEffectsComp.CurrentHealth == 0)
                    {
                        if (unitCompRef.HeadUIRef.UnitHeadUIInstance.FlagForDestruction == false)
                        {
                            CleanupUnitUI(unitCompRef.IsVisibleRefComp, unitCompRef.HeadUIRef, unitCompRef.BaseDataSetComp, unitId, faction.Faction, authPlayerFaction);
                        }
                    }
                    else
                    {
                        if(unitCompRef.HeadUIRef.UnitHeadUIInstance)
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, unitCompRef.transform.position + new Vector3(0, unitCompRef.HeadUIRef.HealthBarYOffset, 0)));
                        if(unitCompRef.HeadUIRef.UnitHeadHealthBarInstance)
                            unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, unitCompRef.transform.position + new Vector3(0, unitCompRef.HeadUIRef.HealthBarYOffset, 0)));
                        SetHealthBarFillAmounts(gameState.CurrentState, unitCompRef.UnitEffectsComp, unitCompRef.HeadUIRef, unitCompRef.HeadUIRef.UnitHeadHealthBarInstance, health, faction.Faction, authPlayerFaction);
                        HandleEnergyGainOverHeadDisplay(isVisible, actions, energy, authPlayerState, unitId, faction.Faction, authPlayerFaction, unitCompRef, coord.CubeCoordinate, playerHigh, gameState);
                        HandleUnitVisibility(isVisible, unitCompRef, health, faction.Faction, authPlayerFaction);

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

                        if (unitCompRef.HeadUIRef.UnitHeadUIInstance.ActionDisplay != null)
                        {
                            if ((actions.CurrentSelected.Index != -3 || actions.LockedAction.Index != -3) && gameState.CurrentState == GameStateEnum.planning)
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
            Entities.ForEach((Entity e, UnitComponentReferences unitCompRef, ref Energy.Component energy, ref Actions.Component actions, ref IsVisible isVisible, ref MouseState mouseState, ref Manalith.Component m, in FactionComponent.Component faction) =>
            {
                uint unitId = (uint) EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);

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

                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.SwapActionButton.gameObject.activeSelf)
                        UIRef.SwapActionButton.gameObject.SetActive(false);

                    SetPortraitInfo(unitCompRef.BaseDataSetComp, faction.Faction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.TeamColorMeshesComp.color, false);
                }

                if (!unitCompRef.BaseDataSetComp.UIInitialized)
                {
                    InitializeManalithUnitUI(unitCompRef.HeadUIRef);
                    unitCompRef.BaseDataSetComp.UIInitialized = true;
                }
                else
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

                    if (faction.Faction == authPlayerFaction && (actions.CurrentSelected.Index != -3 || actions.LockedAction.Index != -3) && gameState.CurrentState == GameStateEnum.planning)
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

        public void HandleUnitVisibility(IsVisible isVisible, UnitComponentReferences unitCompRef, Health.Component health, uint unitFaction, uint playerFaction)
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
            else
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
                        if (!unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.enabled)
                        {
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.text = "+" + energy.EnergyIncome.ToString();
                            unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.enabled = true;
                        }
                    }
                    else
                    {
                        unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.enabled = false;
                    }
                }
                else
                {
                    unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.enabled = false;
                }
            }
            else
            {
                unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.enabled = false;
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

                        UIRef.SpawnActions[si] = FillButtonFields(UIRef.SpawnActions[si], stats, unitId, si, true, true);
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
                            if (stats.Actions[bi].Targets[0].energyCost > playerEnergy.Energy + actions.LockedAction.CombinedCost)
                            {
                                UIRef.Actions[bi].Button.interactable = false;
                            }
                            else
                            {
                                UIRef.Actions[bi].Button.interactable = true;
                            }
                            UIRef.Actions[bi] = FillButtonFields(UIRef.Actions[bi], stats, unitId, bi, false, false);
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

                            if (manalith.Manalithslots[i].CorrespondingCell.IsTaken)
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

            //set position of overhead manalith objects??
            //UpdateLeyLineTooltipPosition(clientData.IngameIconRef.RectTrans.anchoredPosition);

        }

        public Vector3 RoundVector3(Vector3 inVector)
        {
            return new Vector3((int) inVector.x, (int) inVector.y, (int) inVector.z);
        }

        void ClearUnitUIElements()
        {

            for (var i = 0; i < UIRef.MinimapComponent.MiniMapUnitTilesPanel.transform.childCount; i++)
            {
                Object.Destroy(UIRef.MinimapComponent.MiniMapUnitTilesPanel.transform.GetChild(i).gameObject);
            }

            for (var i = 0; i < UIRef.BigMapComponent.MiniMapUnitTilesPanel.transform.childCount; i++)
            {
                Object.Destroy(UIRef.BigMapComponent.MiniMapUnitTilesPanel.transform.GetChild(i).gameObject);
            }

            for (var i = 0; i < UIRef.HealthBarsPanel.transform.childCount; i++)
            {
                Object.Destroy(UIRef.HealthBarsPanel.transform.GetChild(i).gameObject);
            }
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

        void FireStepChangedEffects(string stateName, Color effectColor, string soundEffectPath)
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
        }

        void InvertMenuPanelActive(GameObject menuPanel)
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }

        void HandleKeyCodeInput(GameStateEnum gameState)
        {
            if (UIRef.DollyPathCameraActive)
                return;

            if (Input.GetButtonDown("SwitchIngameUI"))
            {
                UIRef.IngameUIPanel.SetActive(!UIRef.IngameUIPanel.activeSelf);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                UIRef.UIActive = !UIRef.UIActive;
                UIRef.IngameSFXBus.setMute(!UIRef.UIActive);
                UIRef.UINonMapSFXBus.setMute(!UIRef.UIActive);
                UIRef.EnvironmentBus.setMute(!UIRef.UIActive);
                //UIRef.UIMainPanel.SetActive(!UIRef.UIMainPanel.activeSelf);
                InvertMenuPanelActive(UIRef.MapPanel.gameObject);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                //InvertMenuPanelActive(UIRef.EscapeMenu.gameObject);
                UIRef.MainMenuButton.Button.onClick.Invoke();
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

                if (Input.GetKeyDown(KeyCode.Tab) && UIRef.SwapActionButton.gameObject.activeSelf)
                {
                    UIRef.SwapActionButton.Button.onClick.Invoke();
                }

                if (!UIRef.EscapeMenu.gameObject.activeSelf)
                {
                    if (UIRef.ActionButtonGroup.activeSelf)
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1))
                        {
                            UIRef.Actions[0].Button.Select();
                            UIRef.Actions[0].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha2))
                        {
                            UIRef.Actions[1].Button.Select();
                            UIRef.Actions[1].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha3))
                        {
                            UIRef.Actions[2].Button.Select();
                            UIRef.Actions[2].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha4))
                        {
                            UIRef.Actions[3].Button.Select();
                            UIRef.Actions[3].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha5))
                        {
                            UIRef.Actions[4].Button.Select();
                            UIRef.Actions[4].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha6))
                        {
                            UIRef.Actions[5].Button.Select();
                            UIRef.Actions[5].Button.onClick.Invoke();
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1))
                        {
                            UIRef.SpawnActions[0].Button.Select();
                            UIRef.SpawnActions[0].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha2))
                        {
                            UIRef.SpawnActions[1].Button.Select();
                            UIRef.SpawnActions[1].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha3))
                        {
                            UIRef.SpawnActions[2].Button.Select();
                            UIRef.SpawnActions[2].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha4))
                        {
                            UIRef.SpawnActions[3].Button.Select();
                            UIRef.SpawnActions[3].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha5))
                        {
                            UIRef.SpawnActions[4].Button.Select();
                            UIRef.SpawnActions[4].Button.onClick.Invoke();
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha6))
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

        public void ResetEnergyBaubles()
        {
            //UIRef.SAEnergyFill.fillAmount = Mathf.Lerp(UIRef.SAEnergyFill.fillAmount, 0, Time.DeltaTime);
            UIRef.TopEnergyFill.fillAmount = Mathf.Lerp(UIRef.TopEnergyFill.fillAmount, 0, Time.DeltaTime);

            UIRef.HeroBaubleEnergyText.text = 0.ToString();
            //UIRef.SAEnergyText.text = 0.ToString();
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

        public void TriggerUnitDeathUI(Entity e)
        {
            var headUI = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);

            if (headUI.UnitHeadUIInstance)
            {
                headUI.UnitHeadUIInstance.DeathBlowImage.SetActive(true);
            }

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

        public void InitializeUnitUI(PlayerEnergy.Component playerEnergy, Actions.Component actions,  UnitComponentReferences unitCompRef, UnitDataSet stats, long unitId, uint unitFaction, uint playerFaction)
        {
            unitCompRef.HeadUIRef.UnitHeadUIInstance = Object.Instantiate(unitCompRef.HeadUIRef.UnitHeadUIPrefab, unitCompRef.HeadUIRef.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
            unitCompRef.HeadUIRef.UnitHeadHealthBarInstance = unitCompRef.HeadUIRef.UnitHeadUIInstance.HealthBar;
            unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.transform.SetParent(UIRef.HealthBarsPanel.transform, false);

            if (unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.PlayerColorImage && unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.HoveredImage)
            {
                unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.PlayerColorImage.color = settings.FactionColors[(int) unitFaction];
                unitCompRef.HeadUIRef.UnitHeadHealthBarInstance.HoveredImage.color = settings.FactionColors[(int) unitFaction];
            }

            if(unitFaction != 0)
                unitCompRef.HeadUIRef.UnitHeadUIInstance.EnergyGainText.color = settings.FactionIncomeColors[(int) unitFaction];

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
                        unitButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); FillUnitButtons(actions, stats, unitFaction, playerFaction, unitId, playerEnergy); SetPortraitInfo(stats, unitFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.TeamColorMeshesComp.color, true); });


                        

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
                            unitButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); FillUnitButtons(actions, stats, unitFaction, playerFaction, unitId, playerEnergy); SetPortraitInfo(stats, unitFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.TeamColorMeshesComp.color, true); });
                            unitGroup.ExistingUnitIds.Add(unitId);
                            unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                        }
                    }
                }
                else
                {
                    UIRef.SelectHeroButton.UnitId = unitId;
                    UIRef.SelectHeroButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); FillUnitButtons(actions, stats, unitFaction, playerFaction, unitId, playerEnergy); SetPortraitInfo(stats, unitFaction, unitCompRef.AnimPortraitComp.PortraitClips, unitCompRef.TeamColorMeshesComp.color, true); });
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
                    isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.transform.parent = UIRef.MinimapComponent.MiniMapEffectsPanel.transform;
                    isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.gameObject.SetActive(true);

                    if (isVisibleRef.MiniMapTileInstance.EmitSoundEffect && isVisibleRef.MiniMapTileInstance.isActiveAndEnabled)
                        isVisibleRef.MiniMapTileInstance.DeathBlowMapEffect.FMODEmitter.Play();
                }

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

            if(unitHeadUIRef.UnitHeadUIInstance)
                Object.Destroy(unitHeadUIRef.UnitHeadUIInstance.gameObject, unitHeadUIRef.UnitHeadUIInstance.DestroyWaitTime);

            if (unitHeadUIRef.UnitHeadHealthBarInstance)
                Object.Destroy(unitHeadUIRef.UnitHeadHealthBarInstance.gameObject);

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

        public void InitializeSelectedActionTooltip(ActionButton actionButton)
        {
            UIRef.SAToolTip.EnergyText.text = actionButton.EnergyCost.ToString();
            UIRef.SAToolTip.ActionDescription.text = actionButton.ActionDescription;
            UIRef.SAToolTip.ActionName.text = actionButton.ActionName;
            UIRef.SAToolTip.ExecuteStepIcon.sprite = UIRef.ExecuteStepSprites[actionButton.ExecuteStepIndex];
            UIRef.SAToolTip.ExecuteStepIcon.color = settings.TurnStepBgColors[actionButton.ExecuteStepIndex];
            //UIRef.SAInfoPanel.SetActive(true);
        }

        public void CancelLockedAction()
        {
            var playerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
            var playerFaction = m_AuthoritativePlayerData.GetSingleton<FactionComponent.Component>();

            Entities.ForEach((Entity e, ref SpatialEntityId unitId,  ref Actions.Component actions, in FactionComponent.Component faction) =>
            {
                if (unitId.EntityId.Id == playerState.SelectedUnitId && faction.Faction == playerFaction.Faction)
                {
                    if (actions.LockedAction.Index != -3 || actions.CurrentSelected.Index != -3)
                    {
                        m_SendActionRequestSystem.SelectActionCommand(-3, unitId.EntityId.Id);
                        //Call methods so line/target gets disabled instantly
                        m_HighlightingSystem.ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id);
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
                        m_HighlightingSystem.ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id);
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }
    }
}
