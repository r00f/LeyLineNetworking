﻿using FMODUnity;
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

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
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

        public UIReferences UIRef{get; set;}

        Settings settings;

        protected override void OnCreate()
        {
            base.OnCreate();
            //m_EventSystem = Object.FindObjectOfType<EventSystem>();

            m_PlayerStateSystem = World.GetExistingSystem<PlayerStateSystem>();
            m_SendActionRequestSystem = World.GetExistingSystem<SendActionRequestSystem>();
            settings = Resources.Load<Settings>("Settings");

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<Unit_BaseDataSet>(),
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

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>()
                );

            m_AuthoritativePlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<HighlightingDataComponent>(),
                ComponentType.ReadWrite<PlayerState.Component>()
                );


            m_PlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<PlayerState.Component>()
                );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            UIRef = Object.FindObjectOfType<UIReferences>();
            InitializeButtons();
        }

        public void InitializeButtons()
        {
            UIRef.MainMenuButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.EscapeMenu.gameObject); });
            UIRef.HelpButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.HelpPanel); });
            UIRef.SkilltreeButton.Button.onClick.AddListener(delegate { InvertMenuPanelActive(UIRef.SkillTreePanel); });
            UIRef.EscapeMenu.ExitGameButton.onClick.AddListener(delegate { Application.Quit(); });


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

        ActionButton FillButtonFields(ActionButton inButton, Unit_BaseDataSet inBaseData, long inUnitId, int inIndex, bool isSpawnAction, bool hasBasicAction)
        {
            if(isSpawnAction)
            {
                inButton.ExecuteStepIndex = (int)inBaseData.SpawnActions[inIndex].ActionExecuteStep;
                inButton.EnergyCost = inBaseData.SpawnActions[inIndex].Targets[0].energyCost;
                //need to construct Desc with stats DMG RANGE AOE (to enabler progression damage scaling)
                inButton.ActionDescription = inBaseData.SpawnActions[inIndex].Description;
                inButton.ActionName = inBaseData.SpawnActions[inIndex].ActionName;
                inButton.Icon.sprite = inBaseData.SpawnActions[inIndex].ActionIcon;
                inButton.ActionIndex = inBaseData.Actions.Count + inIndex;
            }
            else
            {
                inButton.ExecuteStepIndex = (int)inBaseData.Actions[inIndex].ActionExecuteStep;
                inButton.EnergyCost = inBaseData.Actions[inIndex].Targets[0].energyCost;
                inButton.ActionDescription = inBaseData.Actions[inIndex].Description;
                inButton.ActionName = inBaseData.Actions[inIndex].ActionName;
                inButton.Icon.sprite = inBaseData.Actions[inIndex].ActionIcon;
                inButton.ActionIndex = inIndex;
            }
            inButton.UnitId = (int)inUnitId;
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
            var gameState = gameStates[0];
            var authPlayerFaction = authPlayersFaction[0].Faction;
            var authPlayerState = authPlayersState[0];
            var playerEnergy = authPlayersEnergy[0];
            var playerHigh = playerHighlightingDatas[0];

            GameObject unitInfoPanel = UIRef.InfoEnabledPanel;

            #endregion

            var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

            if (cleanUpStateEvents.Count > 0)
            {
                Entities.With(m_UnitData).ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Health.Component health, ref FactionComponent.Component faction) =>
                {
                    var energy = EntityManager.GetComponentData<Energy.Component>(e);
                    var stats = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);

                    if (stats.SelectUnitButtonInstance)
                    {
                        UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);
                    }

                    ResetHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, health);
                });


                if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.planning)
                {
                    FireStepChangedEffects("Turn " + gameState.TurnCounter, settings.TurnStepColors[0], UIRef.PlanningSlideInPath);
                    UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.planning;
                }

                foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                {
                    g.CleanupReset = true;
                }

                UIRef.FriendlyReadySlider.BurstPlayed = false;
                UIRef.EnemyReadySlider.BurstPlayed = false;

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

            #region Initialize
            if (gameState.CurrentState != GameStateEnum.waiting_for_players)
            {
                //RETARDED IF REQUIRES STARTUPPANEL TO BE ACTIVE TO WORK
                if (UIRef.StartupPanel.activeSelf)
                {
                    if (!UIRef.MatchReadyPanel.activeSelf)
                    {
                        UIRef.MatchReadyPanel.SetActive(true);
                    }
                    if (UIRef.StartUpWaitTime > 0)
                    {
                        UIRef.StartUpWaitTime -= Time.DeltaTime;
                    }
                    else
                    {
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

                        UIRef.FriendlyReadySlider.PlayerColorImage.color = UIRef.FriendlyColor;
                        UIRef.FriendlyReadyDot.color = UIRef.FriendlyColor;
                        UIRef.FriendlyReadySwoosh.color = UIRef.FriendlyColor;
                        UIRef.FriendlyRope.color = UIRef.FriendlyColor;
                        UIRef.EnergyConnectorPlayerColorFill.color = UIRef.FriendlyColor;

                        UIRef.EnemyReadySlider.PlayerColorImage.color = UIRef.EnemyColor;
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

                        UIRef.StartupPanel.SetActive(false);
                    }
                }
            }
            #endregion

            HandleKeyCodeInput(gameState.CurrentState);

            #region TurnStepEffects

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                for (int i = 0; i < UIRef.TurnStepFlares.Count; i++)
                {
                    UIRef.TurnStepFlares[i].enabled = false;
                }
            }

            //RESETTING ALL THE COLORS ALL THE TIME RETARDATION
        
            for (int i = 0; i < UIRef.SmallWheelColoredParts.Count; i++)
            {
                UIRef.SmallWheelColoredParts[i].color = settings.TurnStepColors[i + 1];
            }

            for (int i = 0; i < UIRef.BigWheelColoredParts.Count; i++)
            {
                UIRef.BigWheelColoredParts[i].color = settings.TurnStepBgColors[i];
            }

            UIRef.TurnWheelAnimator.SetInteger("TurnStep", (int)gameState.CurrentState - 1);

            switch (gameState.CurrentState)
            {
                case GameStateEnum.planning:
                    foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                    {
                        UpdateUnitGroupBauble(g, authPlayersFaction[0], g.transform.GetSiblingIndex());
                    }
                    break;
                case GameStateEnum.interrupt:
                    
                    if(UIRef.CurrentEffectsFiredState == UIReferences.UIEffectsFired.readyFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.interruptFired;
                    }

                    ClearSelectedActionToolTip();
                    break;
                case GameStateEnum.attack:

                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.attackFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.attackFired;
                    }
                    break;
                case GameStateEnum.move:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.moveFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.moveFired;
                    }
                    break;
                case GameStateEnum.skillshot:
                    if (UIRef.CurrentEffectsFiredState != UIReferences.UIEffectsFired.skillshotFired)
                    {
                        //AND SET UIEFFECTSFIREDSTATE TO INTERRUPTFIRED
                        FireStepChangedEffects(gameState.CurrentState.ToString(), settings.TurnStepColors[(int)gameState.CurrentState - 1], UIRef.ExecuteStepChangePath);
                        UIRef.CurrentEffectsFiredState = UIReferences.UIEffectsFired.skillshotFired;
                    }
                    break;
            }
            #endregion

            #region SwitchIngameUI

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                if (UIRef.ManalithToolTipFab.isActiveAndEnabled)
                {
                    UIRef.ManalithToolTipFab.ActiveManalithID = 0;
                    UIRef.ManalithToolTipFab.gameObject.SetActive(false);
                }
                //if(UIRef.IngameUIPanel.activeSelf)
                    //UIRef.IngameUIPanel.SetActive(false);
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
            
            #region Unitloop

            Entities.With(m_UnitData).ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Health.Component health, ref IsVisible isVisible, ref MouseState mouseState, ref FactionComponent.Component faction) =>
            {
                uint unitId = (uint)EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
                var position = EntityManager.GetComponentObject<Transform>(e).position;
                var energy = EntityManager.GetComponentData<Energy.Component>(e);
                var lineRenderer = EntityManager.GetComponentObject<LineRendererComponent>(e);
                var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(e);
                var animatedPortrait = EntityManager.GetComponentObject<AnimatedPortraitReference>(e).PortraitClip;
                var factionColor = faction.TeamColor;
                var stats = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);
                int actionCount = stats.Actions.Count;
                int spawnActionCount = stats.SpawnActions.Count;
                var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);

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

                    unitHeadUIRef.IncomingDamage = (uint)damagePreviewAmount;
                }

                if (stats.SelectUnitButtonInstance)
                    UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);

                //set topLeft healthBar values for this players hero
                if (stats.IsHero && faction.Faction == authPlayerFaction)
                {
                    if (unitHeadUIRef.UnitHeadHealthBarInstance)
                        EqualizeHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, UIRef.HeroHealthBar, faction.Faction, authPlayerFaction);

                    if(gameState.CurrentState == GameStateEnum.planning)
                    {
                        UpdateHeroBauble(lineRenderer, actions, UIRef.TopEnergyFill, UIRef.HeroBaubleEnergyText, energy, faction, (int)playerEnergy.BaseIncome);
                    }
                }
                
                if (authPlayerState.SelectedUnitId == unitId)
                {
                    UpdateSAEnergyText(lineRenderer, actions, UIRef.SAEnergyText);

                    if (UIRef.AnimatedPortrait.AnimatorOverrideController.animationClips[0].GetHashCode() != animatedPortrait.GetHashCode())
                    {
                        UIRef.AnimatedPortrait.AnimatorOverrideController["KingCroakPortrait"] = animatedPortrait;
                    }
                    else
                    {
                        UIRef.PortraitNameText.text = stats.UnitName;
                        UIRef.PortraitPlayerColorGlow.enabled = true;
                        UIRef.PortraitNameText.enabled = true;
                        UIRef.AnimatedPortrait.GenericImage.enabled = true;
                        UIRef.AnimatedPortrait.PlayerColorImage.enabled = true;
                    }


                    string currentMaxHealth = health.CurrentHealth + "/" + health.MaxHealth;

                    if (health.Armor > 0 && faction.Faction == authPlayerFaction)
                    {

                        UIRef.PortraitArmorText.enabled = true;
                        UIRef.PortraitHealthText.text = currentMaxHealth;
                        UIRef.PortraitArmorText.text = health.Armor.ToString();

                    }
                    else
                    {
                        UIRef.PortraitArmorText.enabled = false;
                        UIRef.PortraitHealthText.text = currentMaxHealth;
                    }

                    if(unitHeadUIRef.UnitHeadHealthBarInstance)
                        EqualizeHealthBarFillAmounts(unitHeadUIRef.UnitHeadHealthBarInstance, UIRef.PortraitHealthBar, faction.Faction, authPlayerFaction);

                    if (faction.Faction == authPlayerFaction)
                    {
                        //if (UIRef.AnimatedPortrait.OverrideAnimSet)
                        //{
                            UIRef.PortraitPlayerColor.color = UIRef.FriendlyColor;
                            UIRef.PortraitPlayerColorGlow.color = UIRef.FriendlyColor;
                        //}

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
                            if(UIRef.SpawnButtonGroup.activeSelf)
                            {
                                UIRef.ActionButtonGroup.SetActive(true);
                                UIRef.SpawnButtonGroup.SetActive(false);
                            }

                            if (UIRef.SwapActionButton.gameObject.activeSelf)
                            {
                                if(UIRef.SwapActionButton.ButtonInverted)
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
                        

                        for (int i = 0; i < UIRef.TurnStepFlares.Count; i++)
                        {
                            if(actions.CurrentSelected.Index != -3)
                            {
                                if (i == (int)actions.CurrentSelected.ActionExecuteStep)
                                    UIRef.TurnStepFlares[i].enabled = true;
                                else
                                    UIRef.TurnStepFlares[i].enabled = false;
                            }
                            else if (actions.LockedAction.Index != -3)
                            {
                                if (i == (int)actions.LockedAction.ActionExecuteStep)
                                    UIRef.TurnStepFlares[i].enabled = true;
                                else
                                    UIRef.TurnStepFlares[i].enabled = false;
                            }
                            else
                                UIRef.TurnStepFlares[i].enabled = false;
                        }
                    }
                    else
                    {
                        UIRef.PortraitPlayerColor.color = UIRef.EnemyColor;
                        UIRef.PortraitPlayerColorGlow.color = UIRef.EnemyColor;

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

                    UIRef.PortraitPlayerColorGlow.enabled = false;
                    UIRef.PortraitNameText.enabled = false;
                    UIRef.AnimatedPortrait.GenericImage.enabled = false;
                    UIRef.AnimatedPortrait.PlayerColorImage.enabled = false;
                }

                if (!stats.UIInitialized)
                {
                    InitializeUnitUI(unitHeadUIRef, stats, unitId, faction.Faction, authPlayerFaction);
                    stats.UIInitialized = true;
                }
                else
                {
                    if (unitHeadUIRef.UnitHeadUIInstance)
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
                    }

                    if (unitEffects.CurrentHealth == 0)
                    {
                        if(unitHeadUIRef.UnitHeadUIInstance.FlagForDestruction == false)
                        {
                            CleanupUnitUI(isVisibleRef, unitHeadUIRef, stats, unitId, faction.Faction, authPlayerFaction);
                        }
                    }
                    else
                    {
                        GameObject healthBarGO = unitHeadUIRef.UnitHeadHealthBarInstance.gameObject;

                        if (isVisible.Value == 1)
                        {
                            if(!healthBarGO.activeSelf)
                                healthBarGO.SetActive(true);

                            if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(playerHigh.HoveredCoordinate))
                            {
                                unitHeadUIRef.UnitHeadHealthBarInstance.HoveredImage.enabled = true;
                            }
                            else if (unitHeadUIRef.UnitHeadHealthBarInstance.HoveredImage.enabled)
                                unitHeadUIRef.UnitHeadHealthBarInstance.HoveredImage.enabled = false;

                            if (unitEffects.CurrentHealth < health.MaxHealth || unitHeadUIRef.IncomingDamage > 0 || (health.Armor > 0 && faction.Faction == authPlayerFaction))
                            {
                                if(!unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.activeSelf)
                                    unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.SetActive(true);
                            }
                            else if(unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.activeSelf)
                            {
                                Debug.Log(unitHeadUIRef.IncomingDamage);
                                unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.SetActive(false);
                            }

                        }
                        else
                        {
                            if (healthBarGO.activeSelf)
                                healthBarGO.SetActive(false);
                        }

                        unitHeadUIRef.UnitHeadHealthBarInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0));
                    }

                    if(unitHeadUIRef.UnitHeadUIInstance)
                        unitHeadUIRef.UnitHeadUIInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0));

                    if (gameState.CurrentState == GameStateEnum.planning || gameState.CurrentState == GameStateEnum.game_over)
                    {
                        SetHealthBarFillAmounts(unitHeadUIRef, unitHeadUIRef.UnitHeadHealthBarInstance, health, faction.Faction, authPlayerFaction);

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
                    
                    if (unitHeadUIRef.UnitHeadUIInstance.ActionDisplay != null)
                    {
                        if (actions.LockedAction.Index != -3 && gameState.CurrentState == GameStateEnum.planning)
                        {
                            HeadUILockedActionDisplay display = unitHeadUIRef.UnitHeadUIInstance.ActionDisplay;
                            if (actions.LockedAction.Index < stats.Actions.Count)
                            {
                                display.ActionImage.sprite = stats.Actions[actions.LockedAction.Index].ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.Actions[actions.LockedAction.Index].ActionExecuteStep + 1];
                            }
                            else
                            {
                                int spawnactionindex = actions.LockedAction.Index - stats.Actions.Count;
                                display.ActionImage.sprite = stats.SpawnActions[spawnactionindex].ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.SpawnActions[spawnactionindex].ActionExecuteStep + 1];
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

            #endregion

            #region PlayerLoops

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

                UIRef.HeroCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.HeroCurrentEnergyFill.fillAmount, (float)playerEnergy.Energy / playerEnergy.MaxEnergy, Time.DeltaTime);

                if (UIRef.HeroCurrentEnergyFill.fillAmount >= (float)playerEnergy.Energy / playerEnergy.MaxEnergy - .003f)
                {
                    UIRef.HeroEnergyIncomeFill.fillAmount = Mathf.Lerp(UIRef.HeroEnergyIncomeFill.fillAmount, (float)(playerEnergy.Energy + playerEnergy.Income) / playerEnergy.MaxEnergy, Time.DeltaTime);
                }
            }

            //IMPLEMENT CORRECT LERP CONTROL (Pass start / end values)
            UIRef.HeroCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.HeroCurrentEnergyFill.fillAmount, (float)playerEnergy.Energy / playerEnergy.MaxEnergy, Time.DeltaTime);

            UIRef.CurrentMaxEnergyText.text = playerEnergy.Energy + " / " + playerEnergy.MaxEnergy;
            UIRef.TotalEnergyIncomeText.text = "+" + playerEnergy.Income.ToString();

            if (authPlayerState.CurrentState != PlayerStateEnum.unit_selected && authPlayerState.CurrentState != PlayerStateEnum.waiting_for_target)
            {
                if(authPlayerState.SelectedUnitId == 0)
                {
                    if (UIRef.PortraitHealthBar.gameObject.activeSelf)
                    {
                        UIRef.PortraitHealthText.enabled = false;
                        UIRef.PortraitArmorText.enabled = false;
                        UIRef.PortraitRegenText.enabled = false;
                        UIRef.PortraitHealthBar.gameObject.SetActive(false);
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
            else
            {
                if (!UIRef.PortraitHealthBar.gameObject.activeSelf)
                {
                    UIRef.PortraitHealthText.enabled = true;
                    UIRef.PortraitHealthBar.gameObject.SetActive(true);
                }
            }

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
                        if (UIRef.FriendlyReadySlider.LerpTime < 1)
                        {
                            UIRef.FriendlyReadySlider.LerpTime += UIRef.ReadyOutSpeed * Time.DeltaTime;
                            if (!UIRef.FriendlyReadySlider.BurstPlayed)
                            {
                                UIRef.FriendlyReadySlider.BurstPS.time = 0;
                                UIRef.FriendlyReadySlider.BurstPS.Play();
                                UIRef.FriendlyReadySlider.BurstPlayed = true;
                            }
                            UIRef.FriendlyReadySlider.Rect.anchoredPosition = new Vector3(UIRef.FriendlyReadySlider.StartPosition.x, Mathf.Lerp(UIRef.FriendlyReadySlider.StartPosition.y, UIRef.FriendlyReadySlider.StartPosition.y + UIRef.FriendlyReadySlider.SlideOffset.y, UIRef.FriendlyReadySlider.LerpTime), 0);
                        }
                        else
                        {
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
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.GOButtonScript.PlayerReady = false;
                        UIRef.SlideOutUIAnimator.SetBool("SlideOut", false);

                        if (UIRef.FriendlyReadySlider.LerpTime > 0)
                        {
                            UIRef.FriendlyReadySlider.LerpTime -= UIRef.ReadyInSpeed * Time.DeltaTime;
                            UIRef.FriendlyReadySlider.Rect.anchoredPosition = new Vector3(UIRef.FriendlyReadySlider.StartPosition.x, Mathf.Lerp(UIRef.FriendlyReadySlider.StartPosition.y, UIRef.FriendlyReadySlider.StartPosition.y + UIRef.FriendlyReadySlider.SlideOffset.y, UIRef.FriendlyReadySlider.LerpTime), 0);
                        }
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
                            if (!UIRef.EnemyReadySlider.BurstPlayed)
                            {
                                UIRef.EnemyReadySlider.BurstPS.time = 0;
                                UIRef.EnemyReadySlider.BurstPS.Play();
                                UIRef.EnemyReadySlider.BurstPlayed = true;
                            }
                            UIRef.EnemyReadyDot.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);
                            UIRef.EnemyReadySwoosh.color -= new Color(0, 0, 0, UIRef.ReadySwooshFadeOutSpeed * Time.DeltaTime);

                            if (UIRef.EnemyReadySlider.LerpTime < 1)
                            {
                                UIRef.EnemyReadySlider.LerpTime += UIRef.ReadyOutSpeed * Time.DeltaTime;
                                UIRef.EnemyReadySlider.Rect.anchoredPosition = new Vector3(Mathf.Lerp(UIRef.EnemyReadySlider.StartPosition.x, UIRef.EnemyReadySlider.StartPosition.x + UIRef.EnemyReadySlider.SlideOffset.x, UIRef.EnemyReadySlider.LerpTime), UIRef.EnemyReadySlider.StartPosition.y, 0);
                            }
                        }
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UIRef.OpponentReady = false;
                        if (UIRef.EnemyReadySlider.LerpTime > 0)
                        {
                            UIRef.EnemyReadySlider.LerpTime -= UIRef.ReadyInSpeed * Time.DeltaTime;
                            UIRef.EnemyReadySlider.Rect.anchoredPosition = new Vector3(Mathf.Lerp(UIRef.EnemyReadySlider.StartPosition.x, UIRef.EnemyReadySlider.StartPosition.x + UIRef.EnemyReadySlider.SlideOffset.x, UIRef.EnemyReadySlider.LerpTime), UIRef.EnemyReadySlider.StartPosition.y, 0);
                        }
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

                UIRef.RopeTimeText.text = "0:" + ((int)gameState.CurrentRopeTime).ToString("D2");

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

                        if(!UIRef.OpponentReady)
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
                        if(UIRef.RopeSlamOneTime)
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

            if(gameState.CurrentState != GameStateEnum.planning)
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

        void FireStepChangedEffects(string stateName, Color effectColor, string soundEffectPath)
        {
            //SET TEXT COLOR / STRING
            RuntimeManager.PlayOneShot(soundEffectPath);
            UIRef.TurnStateText.color = effectColor;
            UIRef.TurnStateText.text = char.ToUpper(stateName[0]) + stateName.Substring(1);
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

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                InvertMenuPanelActive(UIRef.EscapeMenu.gameObject);
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                UIRef.Canvas.gameObject.SetActive(!UIRef.Canvas.gameObject.activeSelf);
            }

            if(gameState == GameStateEnum.planning)
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
            //Convert the local point to world point
            return parentCanvas.transform.TransformPoint(movePos);
        }

        void UpdateSelectUnitButton(Actions.Component actions, SelectUnitButton unitButton, Energy.Component inEnergy, FactionComponent.Component inFaction)
        {
            unitButton.EnergyAmountChange = 0;
            if (inEnergy.Harvesting)
            {
                unitButton.EnergyAmountChange += (int)inEnergy.EnergyIncome;
            }
            if(actions.LockedAction.Index != -3)
            {
                unitButton.EnergyAmountChange -= (int)actions.LockedAction.CombinedCost;

            }

            if(unitButton.EnergyAmountChange > 0)
            {
                unitButton.EnergyFill.enabled = true;
                unitButton.EnergyFill.color = UIRef.FriendlyIncomeColor;
            }
            else if(unitButton.EnergyAmountChange < 0)
            {

                unitButton.EnergyFill.enabled = true;
                unitButton.EnergyFill.color = settings.FactionColors[(int)inFaction.TeamColor+1];
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

            foreach(SelectUnitButton selectButton in unitGroupUI.SelectUnitButtons)
            {
                if(selectButton.EnergyAmountChange > 0)
                {
                    percentageGainFill += 1f;
                }
                else if(selectButton.EnergyAmountChange < 0)
                {
                    percentageToFill += 1f;
                    percentageGainFill += 1f;
                }
                combinedAmount += selectButton.EnergyAmountChange;
            }

            unitGroupUI.CombinedEnergyCost = combinedAmount;

            //Combined cost change
            if(unitGroupUI.CombinedEnergyCost != unitGroupUI.LastCombinedEnergyCost)
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
                EnergyChangeAmount += (int)inEnergy.EnergyIncome;
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
                EnergyChangeAmount -= (int)actions.LockedAction.CombinedCost;
            }

            if(EnergyChangeAmount != 0)
            {
                if (inEnergyFill.fillAmount < 1)
                    inEnergyFill.fillAmount = Mathf.Lerp(inEnergyFill.fillAmount, 1, Time.DeltaTime);

                if(EnergyChangeAmount > 0)
                {
                    inEnergyText.text = "+" + EnergyChangeAmount.ToString();
                    inEnergyFill.color = UIRef.FriendlyIncomeColor;
                }
                else
                {
                    inEnergyText.text = EnergyChangeAmount.ToString();
                    inEnergyFill.color = settings.FactionColors[(int)inFaction.TeamColor+1];
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

            if (toHealthBar.Parts)
            {
                toHealthBar.Parts.material.mainTextureScale = fromHealthBar.Parts.material.mainTextureScale;
                toHealthBar.Parts.material.mainTexture = fromHealthBar.Parts.material.mainTexture;
                toHealthBar.Parts.SetMaterialDirty();
            }
        }

        public void ResetHealthBarFillAmounts(HealthBar healthBar, Health.Component health)
        {
            float healthPercentage = (float)health.CurrentHealth / health.MaxHealth;
            healthBar.ArmorFill.fillAmount = 0;
            healthBar.BgFill.fillAmount = 0;
            healthBar.DamageFill.fillAmount = 0;
            healthBar.HealthFill.fillAmount = healthPercentage;
        }

        public void SetHealthBarFillAmounts(UnitHeadUIReferences unitHeadUiRef, HealthBar healthBar, Health.Component health, uint unitFaction, uint playerFaction)
        {
            float combinedHealth = health.CurrentHealth + health.Armor;
            float healthPercentage = (float)health.CurrentHealth / health.MaxHealth;
            float armorPercentage = (float)health.Armor / combinedHealth;
            healthBar.BgFill.fillAmount = 0;

            if (health.MaxHealth + health.Armor >= 100)
            {
                healthBar.Parts.material.SetTexture("_MainTex", healthBar.HealthSectionsBig);
                healthBar.Parts.SetMaterialDirty();
            }
            else
            {
                healthBar.Parts.material.SetTexture("_MainTex", healthBar.HealthSectionsSmall);
                healthBar.Parts.SetMaterialDirty();
            }

            if (unitFaction == playerFaction)
            {
                if (combinedHealth < health.MaxHealth)
                {
                    //DONT SCALE
                    healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);
                    
                    healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, healthPercentage, Time.DeltaTime);
                    healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, healthBar.HealthFill.fillAmount + ((float)health.Armor / health.MaxHealth), Time.DeltaTime);
                }
                else
                {
                    healthBar.Parts.material.mainTextureScale = new Vector2(combinedHealth / 20f, 1f);

                    healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, 1 - armorPercentage, Time.DeltaTime);
                    healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, 1, Time.DeltaTime);
                }

                healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float)unitHeadUiRef.IncomingDamage / combinedHealth, Time.DeltaTime);
                healthBar.DamageRect.offsetMax = new Vector2((-healthBar.HealthBarRect.rect.width * (1 - healthBar.ArmorFill.fillAmount)) + 3f, 0);
            }
            else
            {
                healthBar.Parts.material.mainTextureScale = new Vector2((float)health.MaxHealth / 20f, 1f);

                healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, healthPercentage, Time.DeltaTime);
                healthBar.ArmorFill.fillAmount = 0;

                healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float)unitHeadUiRef.IncomingDamage / (float)health.CurrentHealth, Time.DeltaTime);
                healthBar.DamageRect.offsetMax = new Vector2((-healthBar.HealthBarRect.rect.width * (1 - healthBar.HealthFill.fillAmount)) + 3f, 0);
            }
        }

        public void SetArmorDisplay(Entity e, uint inArmorAmount, float displayTime, bool shatter = false)
        {
            var healthbar = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);

            if(healthbar.UnitHeadUIInstance)
            {
                Text armorText = healthbar.UnitHeadUIInstance.ArmorText;
                Animator anim = healthbar.UnitHeadUIInstance.ArmorAnimator;
                GameObject armorPanel = healthbar.UnitHeadUIInstance.ArmorPanel;
                armorText.text = inArmorAmount.ToString();
                armorPanel.SetActive(true);

                if (shatter)
                    anim.SetTrigger("Shatter");

                healthbar.UnitHeadUIInstance.PlanningBufferTime = displayTime;
            }
        }

        public void TriggerUnitDeathUI(Entity e)
        {
            var headUI = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);

            if(headUI.UnitHeadUIInstance)
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

            if(healthbar.UnitHeadUIInstance)
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

        public void InitializeUnitUI(UnitHeadUIReferences healthbar, Unit_BaseDataSet stats, long unitId, uint unitFaction, uint playerFaction)
        {
            //Spawn UnitHeadUI / UnitGroup / SelectUnitButton

            healthbar.UnitHeadUIInstance = Object.Instantiate(healthbar.UnitHeadUIPrefab, healthbar.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
            healthbar.UnitHeadHealthBarInstance = Object.Instantiate(healthbar.UnitHeadHealthBarPrefab, healthbar.transform.position, Quaternion.identity, UIRef.HealthBarsPanel.transform);

            if (healthbar.UnitHeadHealthBarInstance.PlayerColorImage && healthbar.UnitHeadHealthBarInstance.HoveredImage)
            {
                healthbar.UnitHeadHealthBarInstance.PlayerColorImage.color = settings.FactionColors[(int)unitFaction];
                healthbar.UnitHeadHealthBarInstance.HoveredImage.color = settings.FactionColors[(int)unitFaction];
            }


            healthbar.UnitHeadUIInstance.ArmorPanel.SetActive(false);
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
                        unitGroup.EnergyFill.color = settings.FactionColors[(int)playerFaction];
                        unitGroup.EnergyGainFill.color = UIRef.FriendlyIncomeColor;
                        SelectUnitButton unitButton = Object.Instantiate(UIRef.UnitButtonPrefab, unitGroup.UnitsPanel.transform);
                        unitButton.UnitId = unitId;
                        unitButton.EnergyFill.color = settings.FactionColors[(int)playerFaction];

                        if(stats.UnitSprite)
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
                            unitButton.EnergyFill.color = settings.FactionColors[(int)playerFaction];
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
            else if (healthbar.UnitHeadUIInstance.ActionDisplay)
                Object.Destroy(healthbar.UnitHeadUIInstance.ActionDisplay.gameObject);
        }

        void CleanupUnitUI(IsVisibleReferences isVisibleRef, UnitHeadUIReferences healthbar, Unit_BaseDataSet stats, long unitID, uint unitFaction, uint playerFaction)
        {

            if (isVisibleRef.MiniMapTileInstance)
            {
                if (isVisibleRef.MiniMapTileInstance.DeathCrossPrefab && isVisibleRef.MiniMapTileInstance.isActiveAndEnabled)
                {
                    var cross = Object.Instantiate(isVisibleRef.MiniMapTileInstance.DeathCrossPrefab, isVisibleRef.MiniMapTileInstance.TileRect.position, Quaternion.identity, isVisibleRef.MiniMapTileInstance.transform.parent);
                    Object.Destroy(cross, 3f);
                }

                Object.Destroy(isVisibleRef.MiniMapTileInstance.gameObject, 0.5f);
            }

            //Delete headUI / UnitGroupUI on unit death (when health = 0)
            //INSTEAD OF DELETING DIRECTLY SET FlagForDestruction AND DESTROY FROM UNITCLEANUPSYSTEM AFTER
            healthbar.UnitHeadUIInstance.FlagForDestruction = true;
            healthbar.UnitHeadHealthBarInstance.gameObject.SetActive(false);

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

