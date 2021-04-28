using FMODUnity;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unit;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Cell;
using Improbable;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UISystem : ComponentSystem
    {
        //EventSystem m_EventSystem;
        HighlightingSystem m_HighlightingSystem;
        PlayerStateSystem m_PlayerStateSystem;
        SendActionRequestSystem m_SendActionRequestSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
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
                ComponentType.ReadOnly<ManalithUnit.Component>(),
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
            UIRef.MainMenuButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.EscapeMenu.gameObject); });
            UIRef.HelpButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.HelpPanel); });
            UIRef.SkilltreeButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.SkillTreePanel); });
            UIRef.EscapeMenu.ExitGameButton.onClick.AddListener(delegate { Application.Quit(); });

            UIRef.RevealVisionButton.onClick.AddListener(delegate { m_SendActionRequestSystem.RevealPlayerVision(); });


            UIRef.GOButtonScript.Button.onClick.AddListener(delegate { m_PlayerStateSystem.ResetCancelTimer(UIRef.CacelGraceTime); });
            UIRef.GOButtonScript.Button.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.waiting); });
            UIRef.GOButtonScript.Button.onClick.AddListener(delegate { m_HighlightingSystem.ResetHighlights(); });

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

        protected override void OnUpdate()
        {
            if (m_GameStateData.CalculateEntityCount() == 0 || m_AuthoritativePlayerData.CalculateEntityCount() == 0)
                return;

            #region GetData

            var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
            var authPlayersEnergy = m_AuthoritativePlayerData.ToComponentDataArray<PlayerEnergy.Component>(Allocator.TempJob);
            var authPlayersFaction = m_AuthoritativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            var authPlayersState = m_AuthoritativePlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            var playerHighlightingDatas = m_AuthoritativePlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

            var authPlayerCameras = m_AuthoritativePlayerData.ToComponentArray<Moba_Camera>();
            var authPlayerCam = authPlayerCameras[0];

            var gameState = gameStates[0];
            var authPlayerFaction = authPlayersFaction[0].Faction;
            var authPlayerState = authPlayersState[0];
            var playerEnergy = authPlayersEnergy[0];
            var playerHigh = playerHighlightingDatas[0];

            //GameObject unitInfoPanel = UIRef.InfoEnabledPanel;

            #endregion

            var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.InitializeMapEvent.Event>();

            var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

            if (initMapEvents.Count > 0)
            {
                m_SendActionRequestSystem.RevealPlayerVision();
                ClearUnitUIElements();

                switch (authPlayersFaction[0].TeamColor)
                {
                    case TeamColorEnum.blue:
                        UIRef.FriendlyIncomeColor = settings.FactionIncomeColors[0];
                        UIRef.FriendlyColor = settings.FactionColors[1];
                        UIRef.EnemyColor = settings.FactionColors[2];
                        break;
                    case TeamColorEnum.red:
                        UIRef.FriendlyIncomeColor = settings.FactionIncomeColors[1];
                        UIRef.FriendlyColor = settings.FactionColors[2];
                        UIRef.EnemyColor = settings.FactionColors[1];
                        break;
                }

                UIRef.HeroEnergyIncomeFill.color = UIRef.FriendlyIncomeColor;
                UIRef.TotalEnergyIncomeText.color = UIRef.FriendlyIncomeColor;

                UIRef.HeroPortraitPlayerColor.color = UIRef.FriendlyColor;

                UIRef.HeroCurrentEnergyFill.color = UIRef.FriendlyColor;
                UIRef.TopEnergyFill.color = UIRef.FriendlyColor;

                UIRef.FriendlyReadyDot.color = UIRef.FriendlyColor;
                UIRef.FriendlyReadySwoosh.color = UIRef.FriendlyColor;
                UIRef.FriendlyRope.color = UIRef.FriendlyColor;
                UIRef.EnergyConnectorPlayerColorFill.color = UIRef.FriendlyColor;

                UIRef.EnemyReadyDot.color = UIRef.EnemyColor;
                UIRef.EnemyReadySwoosh.color = UIRef.EnemyColor;
                UIRef.EnemyRope.color = UIRef.EnemyColor;

                UIRef.GOButtonScript.LightCircle.color = UIRef.FriendlyColor;
                UIRef.GOButtonScript.LightFlare.color = UIRef.FriendlyColor;
                UIRef.GOButtonScript.LightInner.color = UIRef.FriendlyColor;

                var fBurst = UIRef.FriendlyReadyBurstPS.main;
                fBurst.startColor = UIRef.FriendlyColor;
                var eBurst = UIRef.EnemyReadyBurstPS.main;
                eBurst.startColor = UIRef.EnemyColor;

                var main = UIRef.FriendlyReadySwooshParticle.LoopPS.main;
                main.startColor = UIRef.FriendlyColor;

                var main1 = UIRef.FriendlyRopeBarParticle.LoopPS.main;
                main1.startColor = UIRef.FriendlyColor;

                var main2 = UIRef.EnemyRopeBarParticle.LoopPS.main;
                main2.startColor = UIRef.EnemyColor;

                var main3 = UIRef.EnemyReadySwooshParticle.LoopPS.main;
                main3.startColor = UIRef.EnemyColor;

                if (!UIRef.MatchReadyPanel.activeSelf)
                {
                    UIRef.MatchReadyPanel.SetActive(true);
                }
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

            if (cleanUpStateEvents.Count > 0)
            {
                Entities.With(m_UnitData).ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Health.Component health, ref FactionComponent.Component faction, ref Energy.Component energy) =>
                {
                    //var energy = EntityManager.GetComponentData<Energy.Component>(e);
                    var stats = EntityManager.GetComponentObject<UnitDataSet>(e);

                    if (stats.SelectUnitButtonInstance)
                    {
                        UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);
                    }

                    if (unitHeadUIRef.UnitHeadHealthBarInstance)
                        ResetHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, health);
                });


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

                UIRef.FriendlyReadyDot.enabled = false;
                UIRef.EnemyReadyDot.enabled = false;
                UIRef.FriendlyReadySwoosh.fillAmount = 0;
                UIRef.EnemyReadySwoosh.fillAmount = 0;

                UIRef.RopeSlamOneTime = false;
                UIRef.RopeEndsLerpTime = 0;

                UIRef.FriendlyRope.fillAmount = 0;
                UIRef.EnemyRope.fillAmount = 0;

                UIRef.EnemyRope.color = UIRef.EnemyColor;
                UIRef.FriendlyRope.color = UIRef.FriendlyColor;

                UIRef.FriendlyReadyDot.color = UIRef.FriendlyColor;
                UIRef.EnemyReadyDot.color = UIRef.EnemyColor;
                UIRef.FriendlyReadySwoosh.color = UIRef.FriendlyColor;
                UIRef.EnemyReadySwoosh.color = UIRef.EnemyColor;
            }

            HandleKeyCodeInput(gameState.CurrentState);

            if (!UIRef.UIActive)
            {
                gameStates.Dispose();
                authPlayersEnergy.Dispose();
                authPlayersFaction.Dispose();
                authPlayersState.Dispose();
                playerHighlightingDatas.Dispose();
                return;
            }

            #region TurnStepEffects

            UIRef.ExecuteStepPanelAnimator.SetInteger("TurnStep", (int) gameState.CurrentState - 1);

            switch (gameState.CurrentState)
            {
                case GameStateEnum.planning:
                    foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                    {
                        UpdateUnitGroupBauble(g, authPlayersFaction[0], g.transform.GetSiblingIndex());
                    }
                    break;
                case GameStateEnum.interrupt:

                    if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.readyFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.interruptFired;
                    }

                    ClearSelectedActionToolTip();
                    break;
                case GameStateEnum.attack:

                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.attackFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.attackFired;
                    }
                    break;
                case GameStateEnum.move:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.moveFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.moveFired;
                    }
                    break;
                case GameStateEnum.skillshot:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.skillshotFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int) gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
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
                        else if (gameState.WinnerFaction == authPlayerFaction)
                        {
                            FireStepChangedEffects("Victory", Color.green, UIRef.ExecuteStepChangePath);
                        }
                        else
                        {
                            FireStepChangedEffects("Defeat", Color.red, UIRef.ExecuteStepChangePath);
                        }

                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.gameOverFired;
                    }
                    break;
            }
            #endregion

            #region GameOver
            if (gameState.CurrentState == GameStateEnum.game_over)
            {

                if (gameState.WinnerFaction == 0)
                {
                    if (!UIRef.GameOverPanel.activeSelf)
                    {
                        UIRef.DrawPanel.SetActive(true);
                        UIRef.GameOverPanel.SetActive(true);
                    }

                    UIRef.HeroHealthBar.HealthFill.fillAmount = Mathf.Lerp(UIRef.HeroHealthBar.HealthFill.fillAmount, 0, Time.DeltaTime);
                    UIRef.HeroHealthBar.ArmorFill.fillAmount = Mathf.Lerp(UIRef.HeroHealthBar.ArmorFill.fillAmount, 0, Time.DeltaTime);
                    UIRef.HeroHealthBar.DamageFill.fillAmount = Mathf.Lerp(UIRef.HeroHealthBar.DamageFill.fillAmount, 0, Time.DeltaTime);
                }
                else if (gameState.WinnerFaction == authPlayerFaction)
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
                    UIRef.HeroHealthBar.HealthFill.fillAmount = Mathf.Lerp(UIRef.HeroHealthBar.HealthFill.fillAmount, 0, Time.DeltaTime);
                    UIRef.HeroHealthBar.ArmorFill.fillAmount = Mathf.Lerp(UIRef.HeroHealthBar.ArmorFill.fillAmount, 0, Time.DeltaTime);
                    UIRef.HeroHealthBar.DamageFill.fillAmount = Mathf.Lerp(UIRef.HeroHealthBar.DamageFill.fillAmount, 0, Time.DeltaTime);
                }


            }

            #endregion

            #region Unitloops

            UnitLoop(authPlayerState, authPlayerFaction, gameState, playerEnergy, playerHigh);

            ManalithUnitLoop(authPlayerState, authPlayerFaction, gameState, playerEnergy, playerHigh);

            #endregion

            #region PlayerLoops

            HandleMenuSettings(authPlayerCam);

            UIRef.HeroCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.HeroCurrentEnergyFill.fillAmount, (float) playerEnergy.Energy / playerEnergy.MaxEnergy * UIRef.MaxFillAmount, Time.DeltaTime);

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                if (UIRef.GOButtonScript.Button.interactable)
                {
                    UIRef.GOButtonScript.Button.interactable = false;
                }
            }
            else
            {
                if (authPlayerState.CurrentState == PlayerStateEnum.ready || UIRef.GOButtonScript.RotatingBack)
                {
                    UIRef.GOButtonScript.Button.interactable = false;
                }
                else
                {
                    UIRef.GOButtonScript.Button.interactable = true;
                }

                if (UIRef.HeroCurrentEnergyFill.fillAmount >= (float) playerEnergy.Energy / playerEnergy.MaxEnergy * UIRef.MaxFillAmount - .003f)
                {
                    UIRef.HeroEnergyIncomeFill.fillAmount = Mathf.Lerp(UIRef.HeroEnergyIncomeFill.fillAmount, Mathf.Clamp(playerEnergy.Energy + playerEnergy.Income, 0, playerEnergy.MaxEnergy) / (float) playerEnergy.MaxEnergy * UIRef.MaxFillAmount, Time.DeltaTime);
                }
            }

            UIRef.CurrentEnergyText.text = playerEnergy.Energy.ToString();
            UIRef.MaxEnergyText.text = playerEnergy.MaxEnergy.ToString();

            UIRef.TotalEnergyIncomeText.text = "+" + playerEnergy.Income.ToString();

            if (authPlayerState.CurrentState != PlayerStateEnum.unit_selected && authPlayerState.CurrentState != PlayerStateEnum.waiting_for_target)
            {
                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.BottomLeftPortrait.UnitInfoPanel.activeSelf || UIRef.BottomLeftPortrait.ManalithInfoPanel.activeSelf)
                    {
                        UIRef.BottomLeftPortrait.UnitInfoPanel.SetActive(false);
                        UIRef.BottomLeftPortrait.ManalithInfoPanel.SetActive(false);
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
            /*else
            {
                if (!UIRef.PortraitHealthBar.gameObject.activeSelf)
                {
                    UIRef.PortraitHealthText.enabled = true;
                    UIRef.PortraitHealthBar.gameObject.SetActive(true);
                }
          
            }  */

            //all players
            Entities.With(m_PlayerData).ForEach((ref FactionComponent.Component faction, ref PlayerState.Component playerState) =>
            {
                if (authPlayerFaction == faction.Faction)
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        UIRef.GOButtonScript.SetLightsToPlayerColor(UIRef.FriendlyColor);
                        UIRef.GOButtonScript.PlayerReady = true;
                        //Slide out first if friendly

                        if (UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.planning || UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.enemyReadyFired)
                        {
                            FireStepChangedEffects("Waiting", UIRef.FriendlyColor, UIRef.ReadySoundEventPath);
                            UIRef.FriendlyReadyDot.enabled = true;
                            UIRef.FriendlyReadyBurstPS.time = 0;
                            UIRef.FriendlyReadyBurstPS.Play();
                            UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.readyFired;
                        }
                        UIRef.FriendlyReadySwoosh.fillAmount += UIRef.ReadyImpulseLerpSpeed * Time.DeltaTime;

                        if (UIRef.FriendlyReadySwoosh.fillAmount == 0 || UIRef.FriendlyReadySwoosh.fillAmount == 1)
                        {
                            UIRef.FriendlyReadyDot.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                            UIRef.FriendlyReadySwoosh.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                            UIRef.FriendlyReadySwooshParticle.LoopPS.time = 0;
                            UIRef.FriendlyReadySwooshParticle.LoopPS.Stop();
                        }
                        else
                        {
                            UIRef.FriendlyReadySwooshParticle.LoopPS.Play();
                        }

                        UIRef.FriendlyReadySwooshParticle.Rect.anchoredPosition = new Vector2(UIRef.FriendlyReadySwoosh.fillAmount * UIRef.FriendlyReadySwooshParticle.ParentRect.sizeDelta.x, UIRef.FriendlyReadySwooshParticle.Rect.anchoredPosition.y);
                        UIRef.SlideOutUIAnimator.SetBool("SlideOut", true);
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.GOButtonScript.PlayerReady = false;
                        UIRef.SlideOutUIAnimator.SetBool("SlideOut", false);
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
                            UIRef.EnemyReadyDot.enabled = true;
                            UIRef.EnemyReadyBurstPS.time = 0;
                            UIRef.EnemyReadyBurstPS.Play();
                            UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.enemyReadyFired;
                        }

                        UIRef.EnemyReadySwoosh.fillAmount += UIRef.ReadyImpulseLerpSpeed * Time.DeltaTime;

                        if (UIRef.EnemyReadySwoosh.fillAmount == 1 || UIRef.EnemyReadySwoosh.fillAmount == 0)
                        {
                            UIRef.EnemyReadySwooshParticle.LoopPS.Stop();
                        }
                        else
                        {
                            UIRef.EnemyReadySwooshParticle.Rect.anchoredPosition = new Vector2(1 - (UIRef.EnemyReadySwoosh.fillAmount * UIRef.EnemyReadySwooshParticle.ParentRect.sizeDelta.x), UIRef.EnemyReadySwooshParticle.Rect.anchoredPosition.y);
                            UIRef.EnemyReadySwooshParticle.LoopPS.Play();
                        }

                        if (UIRef.EnemyReadySwoosh.fillAmount >= 1)
                        {
                            UIRef.EnemyReadyDot.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                            UIRef.EnemyReadySwoosh.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                        }
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.OpponentReady = false;
                    }
                }
            });
            #endregion

            UIRef.GOButtonScript.PlayerInCancelState = playerHigh.CancelState;

            //Handle Ropes
            if (gameStates[0].CurrentRopeTime < gameStates[0].RopeTime)
            {
                UIRef.EnemyRopeBarParticle.Rect.anchoredPosition = new Vector2(1 - (UIRef.EnemyRope.fillAmount * UIRef.EnemyRopeBarParticle.ParentRect.sizeDelta.x), UIRef.EnemyRopeBarParticle.Rect.anchoredPosition.y);
                UIRef.FriendlyRopeBarParticle.Rect.anchoredPosition = new Vector2(UIRef.FriendlyRope.fillAmount * UIRef.FriendlyRopeBarParticle.ParentRect.sizeDelta.x, UIRef.FriendlyRopeBarParticle.Rect.anchoredPosition.y);

                UIRef.RopeTimeText.text = "0:" + ((int) gameState.CurrentRopeTime).ToString("D2");

                if (gameState.CurrentState == GameStateEnum.planning)
                {
                    UIRef.RopeTimeText.enabled = true;

                    if (!UIRef.RopeLoopEmitter.IsPlaying())
                        UIRef.RopeLoopEmitter.Play();

                    UIRef.RopeLoopEmitter.SetParameter("FadeInFastTikTok", 1 - gameStates[0].CurrentRopeTime / gameStates[0].RopeTime);

                    if (authPlayerState.CurrentState == PlayerStateEnum.ready)
                    {
                        UIRef.FriendlyRopeBarParticle.LoopPS.Play(false);
                        UIRef.RopeTimeText.color = UIRef.FriendlyColor;

                        if (!UIRef.OpponentReady)
                            UIRef.FriendlyRope.fillAmount = 1 - (gameStates[0].CurrentRopeTime / gameStates[0].RopeTime);
                    }
                    else
                    {
                        //GETTING ROPED
                        UIRef.EnemyRopeBarParticle.LoopPS.Play(false);
                        UIRef.RopeTimeText.color = UIRef.EnemyColor;
                        UIRef.FriendlyRope.fillAmount = 0;
                        UIRef.EnemyRope.fillAmount = 1 - (gameStates[0].CurrentRopeTime / gameStates[0].RopeTime);
                    }
                }
                else
                {
                    //Find Center and Lerp both ropes to Center
                    if (!UIRef.RopeSlamOneTime)
                    {
                        UIRef.EnemyRopeEndFillAmount = UIRef.EnemyRope.fillAmount;
                        UIRef.FriendlyRopeEndFillAmount = UIRef.FriendlyRope.fillAmount;
                        UIRef.RopeFillsEndDist = (1 - UIRef.FriendlyRopeEndFillAmount - UIRef.EnemyRopeEndFillAmount) / 2f;

                        UIRef.RopeTimeText.enabled = false;
                        UIRef.RopeSlamOneTime = true;
                    }

                    if (UIRef.RopeEndsLerpTime < 1)
                    {
                        if (UIRef.RopeSlamOneTime)
                        {
                            UIRef.RopeEndsLerpTime += Time.DeltaTime * UIRef.RopeEndLerpSpeed;
                            UIRef.FriendlyRope.fillAmount = Mathf.Lerp(UIRef.FriendlyRopeEndFillAmount, UIRef.FriendlyRopeEndFillAmount + UIRef.RopeFillsEndDist, UIRef.RopeEndsLerpTime);
                            UIRef.EnemyRope.fillAmount = Mathf.Lerp(UIRef.EnemyRopeEndFillAmount, UIRef.EnemyRopeEndFillAmount + UIRef.RopeFillsEndDist, UIRef.RopeEndsLerpTime);
                        }

                    }
                    else
                    {
                        if (UIRef.FriendlyRope.color.a == 1)
                        {
                            //BURST
                            UIRef.FriendlyRopeBarParticle.LoopPS.Stop();
                            UIRef.FriendlyRopeBarParticle.BurstPS.time = 0;
                            UIRef.FriendlyRopeBarParticle.BurstPS.Play();
                        }

                        if (UIRef.EnemyRope.color.a == 1)
                        {
                            //BURST
                            UIRef.EnemyRopeBarParticle.LoopPS.Stop();
                            UIRef.EnemyRopeBarParticle.BurstPS.time = 0;
                            UIRef.EnemyRopeBarParticle.BurstPS.Play();
                        }

                        UIRef.FriendlyRope.color -= new Color(0, 0, 0, UIRef.RopeEndFadeOutSpeed * Time.DeltaTime);
                        UIRef.EnemyRope.color -= new Color(0, 0, 0, UIRef.RopeEndFadeOutSpeed * Time.DeltaTime);
                    }
                }
            }
            else
            {
                UIRef.RopeLoopEmitter.Stop();
                UIRef.FriendlyRopeBarParticle.LoopPS.Stop();
                UIRef.EnemyRopeBarParticle.LoopPS.Stop();
                UIRef.RopeTimeText.enabled = false;
            }

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                UIRef.RopeLoopEmitter.Stop();
            }

            gameStates.Dispose();

            #region authPlayerData
            authPlayersEnergy.Dispose();
            authPlayersFaction.Dispose();
            authPlayersState.Dispose();
            //playerHighlightingDatas[0] = playerHigh;
            //m_AuthoritativePlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
            playerHighlightingDatas.Dispose();
            #endregion
        }

        public void UnitLoop(PlayerState.Component authPlayerState, uint authPlayerFaction, GameState.Component gameState, PlayerEnergy.Component playerEnergy, HighlightingDataComponent playerHigh)
        {
            Entities.With(m_UnitData).ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Health.Component health, ref IsVisible isVisible, ref MouseState mouseState, ref FactionComponent.Component faction) =>
            {
                uint unitId = (uint) EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
                var position = EntityManager.GetComponentObject<Transform>(e).position;
                var energy = EntityManager.GetComponentData<Energy.Component>(e);
                var lineRenderer = EntityManager.GetComponentObject<LineRendererComponent>(e);
                var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(e);
                var animatedPortraits = EntityManager.GetComponentObject<AnimatedPortraitReference>(e).PortraitClips;
                var factionColor = faction.TeamColor;
                var stats = EntityManager.GetComponentObject<UnitDataSet>(e);
                int actionCount = stats.Actions.Count;
                int spawnActionCount = stats.SpawnActions.Count;
                var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);

                if(EntityManager.HasComponent<AiUnit.Component>(e) && unitHeadUIRef.UnitHeadUIInstance)
                {
                    var AIUnitHeadUIRef = unitHeadUIRef.UnitHeadUIInstance as AIUnitHeadUI;
                    var AIUnit = EntityManager.GetComponentData<AiUnit.Component>(e);


                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (AIUnit.CurrentState == AiUnitStateEnum.aggroed)
                            AIUnitHeadUIRef.ExclamationMark.SetActive(true);
                        else
                            AIUnitHeadUIRef.ExclamationMark.SetActive(false);
                    }
                    else
                        AIUnitHeadUIRef.ExclamationMark.SetActive(false);
                }

                if (gameState.CurrentState == GameStateEnum.planning)
                {
                    var damagePreviewAmount = 0;
                    for (int i = 0; i < authPlayerState.UnitTargets.Count; i++)
                    {
                        CubeCoordinateList cubeCoordinateList = authPlayerState.UnitTargets.ElementAt(i).Value;
                        HashSet<Vector3f> coordHash = new HashSet<Vector3f>(cubeCoordinateList.CubeCoordinates);

                        if (coordHash.Contains(coord.CubeCoordinate))
                        {
                            damagePreviewAmount += cubeCoordinateList.DamageAmount;
                        }
                    }

                    unitHeadUIRef.IncomingDamage = (uint) damagePreviewAmount;
                }

                if (stats.SelectUnitButtonInstance)
                    UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);

                //set topLeft healthBar values for this players hero
                if (stats.IsHero && faction.Faction == authPlayerFaction)
                {
                    if (unitHeadUIRef.UnitHeadHealthBarInstance)
                        EqualizeHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, UIRef.HeroHealthBar, faction.Faction, authPlayerFaction);

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UpdateHeroBauble(lineRenderer, actions, UIRef.TopEnergyFill, UIRef.HeroBaubleEnergyText, energy, faction, (int) playerEnergy.BaseIncome);
                    }
                }

                if (authPlayerState.SelectedUnitId == unitId)
                {
                    if (!UIRef.BottomLeftPortrait.UnitInfoPanel.activeSelf)
                    {
                        UIRef.BottomLeftPortrait.ManalithInfoPanel.SetActive(false);
                        UIRef.BottomLeftPortrait.UnitInfoPanel.SetActive(true);
                    }
                    UpdateSAEnergyText(lineRenderer, actions, UIRef.SAEnergyText);

                    if (animatedPortraits.Count != 0 && UIRef.BottomLeftPortrait.AnimatedPortrait.AnimatorOverrideController.animationClips[0].GetHashCode() != animatedPortraits[(int) faction.Faction].GetHashCode())
                    {
                        UIRef.BottomLeftPortrait.AnimatedPortrait.AnimatorOverrideController["KingCroakPortrait"] = animatedPortraits[(int)faction.Faction];
                    }

                    UIRef.BottomLeftPortrait.PortraitNameText.text = stats.UnitName;
                    UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.enabled = true;
                    UIRef.BottomLeftPortrait.PortraitNameText.enabled = true;
                    UIRef.BottomLeftPortrait.AnimatedPortrait.GenericImage.enabled = true;
                    UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.color = unitEffects.PlayerColor;

                    string currentMaxHealth = health.CurrentHealth + "/" + health.MaxHealth;

                    if (health.Armor > 0 && faction.Faction == authPlayerFaction)
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

                    if (unitHeadUIRef.UnitHeadHealthBarInstance)
                        EqualizeHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, UIRef.BottomLeftPortrait.PortraitHealthBar, faction.Faction, authPlayerFaction);

                    if (faction.Faction == authPlayerFaction)
                    {
                        if (actions.CurrentSelected.Index == -3 && actions.LockedAction.Index == -3)
                        {
                            UIRef.SAInfoPanel.SetActive(false);
                        }
                        else
                        {
                            UIRef.SAInfoPanel.SetActive(true);
                        }

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
                        UIRef.SAInfoPanel.SetActive(false);

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

                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.SwapActionButton.gameObject.activeSelf)
                        UIRef.SwapActionButton.gameObject.SetActive(false);

                    UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.enabled = false;
                    UIRef.BottomLeftPortrait.PortraitNameText.enabled = false;
                    UIRef.BottomLeftPortrait.AnimatedPortrait.GenericImage.enabled = false;
                }

                if (!stats.UIInitialized)
                {
                    InitializeUnitUI(unitHeadUIRef, stats, unitId, faction.Faction, authPlayerFaction);
                    stats.UIInitialized = true;
                }
                else if(unitHeadUIRef.UnitHeadUIInstance)
                {
                    unitHeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.SetFloat("WaitTime", unitHeadUIRef.HealthTextDelay);

                    if (unitHeadUIRef.HealthTextDelay > 0)
                    {
                        unitHeadUIRef.HealthTextDelay -= Time.DeltaTime;
                    }
                    else
                    {
                        unitHeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.SetBool("Delayed", false);
                    }

                    if (unitEffects.CurrentHealth == 0)
                    {
                        if (unitHeadUIRef.UnitHeadUIInstance.FlagForDestruction == false)
                        {
                            CleanupUnitUI(isVisibleRef, unitHeadUIRef, stats, unitId, faction.Faction, authPlayerFaction);
                        }
                    }
                    else
                    {
                        GameObject healthBarGO = unitHeadUIRef.UnitHeadHealthBarInstance.gameObject;

                        if (isVisible.Value == 1)
                        {
                            if (!healthBarGO.activeSelf)
                                healthBarGO.SetActive(true);

                            if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(playerHigh.HoveredCoordinate))
                            {
                                unitHeadUIRef.UnitHeadHealthBarInstance.HoveredImage.enabled = true;
                            }
                            else if (unitHeadUIRef.UnitHeadHealthBarInstance.HoveredImage.enabled)
                                unitHeadUIRef.UnitHeadHealthBarInstance.HoveredImage.enabled = false;

                            if (unitEffects.CurrentHealth < health.MaxHealth || unitHeadUIRef.IncomingDamage > 0 || (health.Armor > 0 && faction.Faction == authPlayerFaction))
                            {
                                if (!unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.activeSelf)
                                    unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.SetActive(true);
                            }
                            else if (unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.activeSelf)
                            {
                                unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.SetActive(false);
                            }

                        }
                        else
                        {
                            if (healthBarGO.activeSelf)
                                healthBarGO.SetActive(false);
                        }
                    }

                    if (unitHeadUIRef.UnitHeadUIInstance && unitHeadUIRef.UnitHeadHealthBarInstance)
                    {
                        unitHeadUIRef.UnitHeadUIInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0)));
                        unitHeadUIRef.UnitHeadHealthBarInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0)));
                        SetHealthBarFillAmounts(gameState.CurrentState, unitEffects, unitHeadUIRef, unitHeadUIRef.UnitHeadHealthBarInstance, health, faction.Faction, authPlayerFaction);
                    }

                    if (gameState.CurrentState == GameStateEnum.planning || gameState.CurrentState == GameStateEnum.game_over)
                    {
                        if (unitHeadUIRef.UnitHeadUIInstance)
                        {
                            if (unitHeadUIRef.UnitHeadUIInstance.PlanningBufferTime > 0)
                            {
                                unitHeadUIRef.UnitHeadUIInstance.PlanningBufferTime -= Time.DeltaTime;
                            }
                            else if (unitHeadUIRef.UnitHeadUIInstance.ArmorPanel.activeSelf)
                            {
                                unitHeadUIRef.UnitHeadUIInstance.ArmorAnimator.SetTrigger("IdleTrigger");
                                unitHeadUIRef.UnitHeadUIInstance.ArmorPanel.SetActive(false);
                            }
                        }
                    }

                    if (unitHeadUIRef.UnitHeadUIInstance.ActionDisplay != null)
                    {
                        if (actions.LockedAction.Index != -3 && gameState.CurrentState == GameStateEnum.planning)
                        {
                            HeadUILockedActionDisplay display = unitHeadUIRef.UnitHeadUIInstance.ActionDisplay;
                            if (actions.LockedAction.Index < stats.Actions.Count)
                            {
                                display.ActionImage.sprite = stats.Actions[actions.LockedAction.Index].ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int) stats.Actions[actions.LockedAction.Index].ActionExecuteStep + 1];
                            }
                            else
                            {
                                int spawnactionindex = actions.LockedAction.Index - stats.Actions.Count;
                                display.ActionImage.sprite = stats.SpawnActions[spawnactionindex].ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int) stats.SpawnActions[spawnactionindex].ActionExecuteStep + 1];
                            }
                            display.gameObject.SetActive(true);
                        }
                        else
                        {
                            unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(false);
                        }
                    }

                }
            });


        }

        public void ManalithUnitLoop(PlayerState.Component authPlayerState, uint authPlayerFaction, GameState.Component gameState, PlayerEnergy.Component playerEnergy, HighlightingDataComponent playerHigh)
        {
            Entities.With(m_ManalithUnitData).ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref IsVisible isVisible, ref MouseState mouseState, ref FactionComponent.Component faction) =>
            {
                uint unitId = (uint) EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
                var position = EntityManager.GetComponentObject<Transform>(e).position;
                var energy = EntityManager.GetComponentData<Energy.Component>(e);
                var lineRenderer = EntityManager.GetComponentObject<LineRendererComponent>(e);
                var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(e);
                var animatedPortraits = EntityManager.GetComponentObject<AnimatedPortraitReference>(e).PortraitClips;
                var factionColor = faction.TeamColor;
                var stats = EntityManager.GetComponentObject<UnitDataSet>(e);
                int actionCount = stats.Actions.Count;
                int spawnActionCount = stats.SpawnActions.Count;
                //var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);
                var teamColorMeshes = EntityManager.GetComponentObject<TeamColorMeshes>(e);

                if (authPlayerState.SelectedUnitId == unitId)
                {
                    UpdateSAEnergyText(lineRenderer, actions, UIRef.SAEnergyText);

                    if (!UIRef.BottomLeftPortrait.ManalithInfoPanel.activeSelf)
                    {
                        UIRef.BottomLeftPortrait.ManalithInfoPanel.SetActive(true);
                        UIRef.BottomLeftPortrait.UnitInfoPanel.SetActive(false);
                    }

                    PopulateManlithInfoHexes(unitId, authPlayerFaction);

                    if (animatedPortraits.Count != 0 && UIRef.BottomLeftPortrait.AnimatedPortrait.AnimatorOverrideController.animationClips[0].GetHashCode() != animatedPortraits[(int)faction.Faction].GetHashCode())
                    {
                        UIRef.BottomLeftPortrait.AnimatedPortrait.AnimatorOverrideController["KingCroakPortrait"] = animatedPortraits[(int)faction.Faction];
                    }

                    UIRef.BottomLeftPortrait.PortraitNameText.text = stats.UnitName;
                    UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.enabled = true;
                    UIRef.BottomLeftPortrait.PortraitNameText.enabled = true;
                    UIRef.BottomLeftPortrait.AnimatedPortrait.GenericImage.enabled = true;
                    UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.color = teamColorMeshes.color;

                    if (faction.Faction == authPlayerFaction)
                    {
                        if (actions.CurrentSelected.Index == -3 && actions.LockedAction.Index == -3)
                        {
                            UIRef.SAInfoPanel.SetActive(false);
                        }
                        else
                        {
                            UIRef.SAInfoPanel.SetActive(true);
                        }

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
                        UIRef.SAInfoPanel.SetActive(false);

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

                if (authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.SwapActionButton.gameObject.activeSelf)
                        UIRef.SwapActionButton.gameObject.SetActive(false);

                    UIRef.BottomLeftPortrait.PortraitPlayerColorGlow.enabled = false;
                    UIRef.BottomLeftPortrait.PortraitNameText.enabled = false;
                    UIRef.BottomLeftPortrait.AnimatedPortrait.GenericImage.enabled = false;
                    //UIRef.AnimatedPortrait.PlayerColorImage.enabled = false;
                }

                if (!stats.UIInitialized)
                {
                    InitializeManalithUnitUI(unitHeadUIRef);
                    stats.UIInitialized = true;
                }
                else
                {
                    unitHeadUIRef.UnitHeadUIInstance.transform.localPosition = RoundVector3(WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0)));

                    if (unitHeadUIRef.UnitHeadUIInstance.ActionDisplay != null)
                    {
                        if (actions.LockedAction.Index != -3 && gameState.CurrentState == GameStateEnum.planning)
                        {
                            if (actions.LockedAction.Index < stats.Actions.Count)
                            {
                                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.ActionImage.sprite = stats.Actions[actions.LockedAction.Index].ActionIcon;
                                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.TurnStepColorBG.color = settings.TurnStepColors[(int) stats.Actions[actions.LockedAction.Index].ActionExecuteStep + 1];
                            }
                            else
                            {
                                int spawnactionindex = actions.LockedAction.Index - stats.Actions.Count;
                                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.ActionImage.sprite = stats.SpawnActions[spawnactionindex].ActionIcon;
                                unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.TurnStepColorBG.color = settings.TurnStepColors[(int) stats.SpawnActions[spawnactionindex].ActionExecuteStep + 1];
                            }
                            unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(true);
                        }
                        else
                        {
                            unitHeadUIRef.UnitHeadUIInstance.ActionDisplay.gameObject.SetActive(false);
                        }
                    }
                }
            });
        }

        protected void PopulateManlithInfoHexes(uint selectedUnitId, uint playerFaction)
        {
            Entities.With(m_ManalithData).ForEach((ref Manalith.Component manalith, ref FactionComponent.Component faction) =>
            {
                if (selectedUnitId == manalith.ManalithUnitId)
                {
                    if(faction.Faction == playerFaction)
                    {
                        UIRef.BottomLeftPortrait.ManalithEnergyGainText.text = manalith.CombinedEnergyGain.ToString();
                        UIRef.BottomLeftPortrait.ManalithEnergyGainText.enabled = true;
                    }
                    else
                    {
                        UIRef.BottomLeftPortrait.ManalithEnergyGainText.enabled = false;
                    }

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
                                UIRef.BottomLeftPortrait.InfoPanelHexes[i].EnergyRing.color = settings.FactionColors[(int)manalith.Manalithslots[i].OccupyingFaction];
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

            
            });
         

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
            UIRef.TurnStateText.color = effectColor;
            UIRef.BigMapTurnCounter.color = effectColor;
            UIRef.TurnStateText.text = char.ToUpper(stateName[0]) + stateName.Substring(1);
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
                UIRef.UIMainPanel.SetActive(!UIRef.UIMainPanel.activeSelf);
                InvertMenuPanelActive(UIRef.MapPanel.gameObject);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                InvertMenuPanelActive(UIRef.EscapeMenu.gameObject);
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                UIRef.UIActive = !UIRef.UIActive;
                UIRef.UISFXBus.setMute(!UIRef.UIActive);
                UIRef.Canvas.enabled = !UIRef.Canvas.enabled;
            }

            if (gameState == GameStateEnum.planning)
            {

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (UIRef.GOButtonScript.Button.interactable)
                        UIRef.GOButtonScript.Button.onClick.Invoke();
                }


                if (Input.GetKeyDown(KeyCode.Q) && UIRef.SwapActionButton.gameObject.activeSelf)
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
            var playerStates = m_AuthoritativePlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            var playerState = playerStates[0];
            playerState.SelectedUnitId = unitId;
            playerStates[0] = playerState;
            m_AuthoritativePlayerData.CopyFromComponentDataArray(playerStates);
            playerStates.Dispose();
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
                unitButton.EnergyFill.color = settings.FactionColors[(int) inFaction.TeamColor + 1];
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
                    Debug.Log("CombinedAmount greater than 0 in cleanupEvent");
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

        void UpdateSAEnergyText(LineRendererComponent lineRenderer, Actions.Component actions, Text inEnergyText)
        {
            //hacky fix to set current cost of move with linerenderer.count
            if (actions.CurrentSelected.Index == -2)
            {
                inEnergyText.text = (lineRenderer.lineRenderer.positionCount - 1).ToString();
            }
            else if (actions.CurrentSelected.Index != -3 && inEnergyText.text != actions.CurrentSelected.CombinedCost.ToString())
            {
                inEnergyText.text = actions.CurrentSelected.CombinedCost.ToString();
            }

            if (actions.LockedAction.Index != -3)
            {
                inEnergyText.text = actions.LockedAction.CombinedCost.ToString();
            }
        }

        void UpdateHeroBauble(LineRendererComponent lineRenderer, Actions.Component actions, Image inEnergyFill, Text inEnergyText, Energy.Component inEnergy, FactionComponent.Component inFaction, int baseManaGain)
        {
            int EnergyChangeAmount = baseManaGain;

            if (inEnergy.Harvesting)
            {
                EnergyChangeAmount += (int) inEnergy.EnergyIncome;
            }

            /*  if (actions.CurrentSelected.Index == -2)
              {
                  EnergyChangeAmount -= lineRenderer.lineRenderer.positionCount - 1;
              }
              else if (actions.CurrentSelected.Index != -3)
              {
                  EnergyChangeAmount -= (int)actions.CurrentSelected.CombinedCost;
              }*/

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
                    inEnergyFill.color = settings.FactionColors[(int) inFaction.TeamColor + 1];
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
                combinedHealth = health.CurrentHealth + health.Armor;
                healthPercentage = (float) health.CurrentHealth / health.MaxHealth;
                armorPercentage = (float) health.Armor / combinedHealth;
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
                        healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, healthBar.HealthFill.fillAmount + ((float) health.Armor / health.MaxHealth), Time.DeltaTime);
                    }
                    else
                    {
                        healthBar.NumberOfParts = (uint) combinedHealth / 20;
                        //EnableHealthBarParts(healthBar.Parts, );
                        //healthBar.Parts.pixelsPerUnitMultiplier = combinedHealth / 40f;
                        //healthBar.Parts.material.mainTextureScale = new Vector2(combinedHealth / 20f, 1f);
                        healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, 1 - armorPercentage, Time.DeltaTime);
                        healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, 1, Time.DeltaTime);
                    }

                    healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float) unitHeadUiRef.IncomingDamage / combinedHealth, Time.DeltaTime);
                    healthBar.DamageRect.offsetMax = new Vector2((-healthBar.HealthBarRect.rect.width * (1 - healthBar.ArmorFill.fillAmount)) + 3f, 0);
                }
                else
                {
                    healthBar.NumberOfParts = health.MaxHealth / 20;
                    //EnableHealthBarParts(healthBar.Parts, );
                    //healthBar.Parts.pixelsPerUnitMultiplier = (float)health.MaxHealth / 40f;
                    //healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);
                    healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, healthPercentage, Time.DeltaTime);
                    healthBar.ArmorFill.fillAmount = healthBar.HealthFill.fillAmount - 0.01f;

                    healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float) unitHeadUiRef.IncomingDamage / (float) health.CurrentHealth, Time.DeltaTime);
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
                        healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, healthBar.HealthFill.fillAmount + ((float) unitEffects.CurrentArmor / health.MaxHealth), Time.DeltaTime);
                    }
                    else
                    {
                        healthBar.NumberOfParts = (uint) combinedHealth / 20;
                        //EnableHealthBarParts(healthBar.Parts,);
                        // healthBar.Parts.pixelsPerUnitMultiplier = combinedHealth / 40f;
                        //healthBar.Parts.material.mainTextureScale = new Vector2(combinedHealth / 20f, 1f);
                        healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, 1 - healthBar.BgFill.fillAmount - armorPercentage, Time.DeltaTime);
                        healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, 1, Time.DeltaTime);
                    }
                }
                else if (unitFaction != playerFaction)
                {
                    healthBar.NumberOfParts = health.MaxHealth / 20;
                    //EnableHealthBarParts(healthBar.Parts, health.MaxHealth / 20);
                    //healthBar.Parts.pixelsPerUnitMultiplier = combinedHealth / 40f;
                    //healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);
                    healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, healthPercentage, Time.DeltaTime);
                    healthBar.ArmorFill.fillAmount = healthBar.HealthFill.fillAmount - 0.01f;
                }

                EnableHealthBarParts(healthBar.Parts, healthBar.NumberOfParts);
                healthBar.DamageFill.fillAmount = 0;
            }
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

            if (healthbar.UnitHeadUIInstance)
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

        public void InitializeUnitUI(UnitHeadUIReferences headUIRef, UnitDataSet stats, long unitId, uint unitFaction, uint playerFaction)
        {
            //Spawn UnitHeadUI / UnitGroup / SelectUnitButton

            headUIRef.UnitHeadUIInstance = Object.Instantiate(headUIRef.UnitHeadUIPrefab, headUIRef.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
            headUIRef.UnitHeadHealthBarInstance = headUIRef.UnitHeadUIInstance.HealthBar;
            headUIRef.UnitHeadHealthBarInstance.transform.SetParent(UIRef.HealthBarsPanel.transform, false);

            if (headUIRef.UnitHeadHealthBarInstance.PlayerColorImage && headUIRef.UnitHeadHealthBarInstance.HoveredImage)
            {
                headUIRef.UnitHeadHealthBarInstance.PlayerColorImage.color = settings.FactionColors[(int) unitFaction];
                headUIRef.UnitHeadHealthBarInstance.HoveredImage.color = settings.FactionColors[(int) unitFaction];
            }


            headUIRef.UnitHeadUIInstance.ArmorPanel.SetActive(false);
            //initialize GroupUI and hero select button
            if (unitFaction == playerFaction)
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
                        unitButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
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
                            unitButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
                            unitGroup.ExistingUnitIds.Add(unitId);
                            unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                        }
                    }
                }
                else
                {
                    UIRef.SelectHeroButton.UnitId = unitId;
                    UIRef.SelectHeroButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
                }
            }
            else if (headUIRef.UnitHeadUIInstance.ActionDisplay)
                Object.Destroy(headUIRef.UnitHeadUIInstance.ActionDisplay.gameObject);
        }

        public void InitializeManalithUnitUI(UnitHeadUIReferences headUIRef)
        {
            headUIRef.UnitHeadUIInstance = Object.Instantiate(headUIRef.UnitHeadUIPrefab, headUIRef.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
        }

        void CleanupUnitUI(IsVisibleReferences isVisibleRef, UnitHeadUIReferences unitHeadUIRef, UnitDataSet stats, long unitID, uint unitFaction, uint playerFaction)
        {
            unitHeadUIRef.UnitHeadUIInstance.FlagForDestruction = true;

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
                    /*
                    var cross = Object.Instantiate(isVisibleRef.BigMapTileInstance.DeathCrossPrefab, isVisibleRef.BigMapTileInstance.TileRect.position, Quaternion.identity, isVisibleRef.BigMapTileInstance.transform.parent);
                    cross.sizeDelta = isVisibleRef.BigMapTileInstance.DeathCrossSize;
                    Object.Destroy(cross.gameObject, 3f);
                    */
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
        }

        public void InitializeSelectedActionTooltip(ActionButton actionButton)
        {
            UIRef.SAEnergyText.text = actionButton.EnergyCost.ToString();
            UIRef.SAActionDescription.text = actionButton.ActionDescription;
            UIRef.SAActionName.text = actionButton.ActionName;
            UIRef.SAExecuteStepIcon.sprite = UIRef.ExecuteStepSprites[actionButton.ExecuteStepIndex];
            UIRef.SAExecuteStepIcon.color = settings.TurnStepBgColors[actionButton.ExecuteStepIndex];
            //UIRef.SAInfoPanel.SetActive(true);
        }

        public void InitializeSelectedActionTooltip(int index)
        {
            UIRef.SAEnergyText.text = UIRef.Actions[index].EnergyCost.ToString();
            UIRef.SAActionDescription.text = UIRef.Actions[index].ActionDescription;
            UIRef.SAActionName.text = UIRef.Actions[index].ActionName;
            UIRef.SAExecuteStepIcon.sprite = UIRef.ExecuteStepSprites[UIRef.Actions[index].ExecuteStepIndex];
            UIRef.SAExecuteStepIcon.color = settings.TurnStepBgColors[UIRef.Actions[index].ExecuteStepIndex];
            //UIRef.SAInfoPanel.SetActive(true);
        }

        public void ClearSelectedActionToolTip()
        {
            UIRef.SAInfoPanel.SetActive(false);
            UIRef.SAActionName.text = "";
            UIRef.SAActionDescription.text = "";
            UIRef.SAEnergyText.text = "0";
            UIRef.SACooldownText.text = "0";

        }
    }
}
