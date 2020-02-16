using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unit;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UISystem : ComponentSystem
    {
        PlayerStateSystem m_PlayerStateSystem;
        SendActionRequestSystem m_SendActionRequestSystem;
        EntityQuery m_PlayerData;
        EntityQuery m_AuthoritativePlayerData;
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;

        public UIReferences UIRef{get; set;}


            Settings settings;

        protected override void OnCreate()
        {
            base.OnCreateManager();
            m_PlayerStateSystem = World.GetExistingSystem<PlayerStateSystem>();
            m_SendActionRequestSystem = World.GetExistingSystem<SendActionRequestSystem>();
            settings = Resources.Load<Settings>("Settings");

            m_UnitData = Worlds.ClientWorld.CreateEntityQuery(
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
                ComponentType.ReadWrite<UnitHeadUIReferences>()
            );

            m_GameStateData = Worlds.ClientWorld.CreateEntityQuery(
                ComponentType.ReadOnly<GameState.Component>()
                );

            m_AuthoritativePlayerData = Worlds.ClientWorld.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerState.ComponentAuthority>(),
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadWrite<PlayerState.Component>()
                );

            m_AuthoritativePlayerData.SetFilter(PlayerState.ComponentAuthority.Authoritative);

            m_PlayerData = Worlds.ClientWorld.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<PlayerState.Component>()
                );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            InitializeButtons();
        }

        public void InitializeButtons()
        {
            UIRef = Object.FindObjectOfType<UIReferences>();
            UIRef.ReadyButton.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.ready); });

            for (int bi = 0; bi < UIRef.Actions.Count; bi++)
            {
                ActionButton ab = UIRef.Actions[bi];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
                ab.Button.onClick.AddListener(delegate { InitializeSelectedActionTooltip(ab); });
            }

            for (int si = 0; si < UIRef.SpawnActions.Count; si++)
            {
                ActionButton ab = UIRef.SpawnActions[si];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
                ab.Button.onClick.AddListener(delegate { InitializeSelectedActionTooltip(ab); });
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
            else if(hasBasicAction)
            {
                if(inIndex == 0)
                {
                    inButton.EnergyCost = 0;
                    inButton.ExecuteStepIndex = (int)inBaseData.BasicMove.ActionExecuteStep;
                    inButton.ActionDescription = inBaseData.BasicMove.Description;
                    inButton.ActionName = inBaseData.BasicMove.ActionName;
                    inButton.Icon.sprite = inBaseData.BasicMove.ActionIcon;
                    inButton.ActionIndex = -2;
                }
                else if(inIndex == 1)
                {
                    inButton.ExecuteStepIndex = (int)inBaseData.BasicAttack.ActionExecuteStep;
                    inButton.EnergyCost = inBaseData.BasicAttack.Targets[0].energyCost;
                    inButton.ActionDescription = inBaseData.BasicAttack.Description;
                    inButton.ActionName = inBaseData.BasicAttack.ActionName;
                    inButton.Icon.sprite = inBaseData.BasicAttack.ActionIcon;
                    inButton.ActionIndex = -1;
                }
                else
                {
                    inButton.ExecuteStepIndex = (int)inBaseData.Actions[inIndex - 2].ActionExecuteStep;
                    inButton.EnergyCost = inBaseData.Actions[inIndex - 2].Targets[0].energyCost;
                    inButton.ActionDescription = inBaseData.Actions[inIndex - 2].Description;
                    inButton.ActionName = inBaseData.Actions[inIndex - 2].ActionName;
                    inButton.Icon.sprite = inBaseData.Actions[inIndex - 2].ActionIcon;
                    inButton.ActionIndex = inIndex - 2;
                }
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
            inButton.TurnStepBauble.color = settings.TurnStepColors[inButton.ExecuteStepIndex];

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
            var gameState = gameStates[0];
            var authPlayerFaction = authPlayersFaction[0].Faction;
            var authPlayerState = authPlayersState[0];
            var playerEnergy = authPlayersEnergy[0];
            
            GameObject unitInfoPanel = UIRef.InfoEnabledPanel;

            #endregion

            #region Initialize
            if (gameState.CurrentState != GameStateEnum.waiting_for_players)
            {
                if (UIRef.StartupPanel.activeSelf)
                {
                    if (!UIRef.MatchReadyPanel.activeSelf)
                    {
                        UIRef.MatchReadyPanel.SetActive(true);
                    }
                    if (UIRef.StartUpWaitTime > 0)
                    {
                        UIRef.StartUpWaitTime -= Time.deltaTime;
                    }
                    else
                    {
                        switch (authPlayersFaction[0].TeamColor)
                        {
                            case TeamColorEnum.blue:
                                UIRef.HeroPortraitPlayerColor.color = settings.FactionColors[1];
                                UIRef.LeftCurrentEnergyFill.color = settings.FactionColors[1];
                                UIRef.TopEnergyFill.color = settings.FactionColors[1];
                                UIRef.SAEnergyFill.color = settings.FactionColors[1];

                                break;
                            case TeamColorEnum.red:
                                UIRef.HeroPortraitPlayerColor.color = settings.FactionColors[2];
                                UIRef.LeftCurrentEnergyFill.color = settings.FactionColors[2];
                                UIRef.TopEnergyFill.color = settings.FactionColors[2];
                                UIRef.SAEnergyFill.color = settings.FactionColors[2];
                                break;
                        }

                        UIRef.StartupPanel.SetActive(false);
                    }
                }
            }
            #endregion

            #region TurnStepWheel
            //TURN THE WHEEL
            //Debug.Log(UIRef.TurnWheelBig.eulerAngles.z);
            float degreesPerFrame = UIRef.WheelRotationSpeed * Time.deltaTime;
            float rOffset = degreesPerFrame / 2f;

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                ResetEnergyBaubles();
            }

            switch (gameState.CurrentState)
            {
                case GameStateEnum.planning:
                    foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                    {
                        UpdateUnitGroupBauble(g, authPlayersFaction[0]);
                    }

                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[0].ActionIndex, UIRef.Actions[0].UnitId);
                        InitializeSelectedActionTooltip(UIRef.Actions[0]);
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[1].ActionIndex, UIRef.Actions[1].UnitId);
                        InitializeSelectedActionTooltip(UIRef.Actions[1]);
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha3))
                    {
                        m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[2].ActionIndex, UIRef.Actions[2].UnitId);
                        InitializeSelectedActionTooltip(UIRef.Actions[2]);
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha4))
                    {
                        m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[3].ActionIndex, UIRef.Actions[3].UnitId);

                    }

                    if (UIRef.TurnWheelBig.eulerAngles.z < 360 - rOffset && UIRef.TurnWheelBig.eulerAngles.z > 180)
                    {
                        UIRef.TurnWheelBig.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                        UIRef.TurnWheelSmol.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                    }
                    else if (UIRef.TurnWheelBig.eulerAngles.z > rOffset && UIRef.TurnWheelBig.eulerAngles.z < 180)
                    {
                        UIRef.TurnWheelBig.Rotate(new Vector3(0f, 0f, -degreesPerFrame), Space.Self);
                        UIRef.TurnWheelSmol.Rotate(new Vector3(0f, 0f, -degreesPerFrame), Space.Self);
                    }
                    break;
                case GameStateEnum.interrupt:
                    ClearSelectedActionToolTip();
                    if (UIRef.TurnWheelBig.eulerAngles.z < 78.75f - rOffset || UIRef.TurnWheelBig.eulerAngles.z > 78.75f + rOffset)
                    {
                        UIRef.TurnWheelBig.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                        UIRef.TurnWheelSmol.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                    }
                    break;
                case GameStateEnum.attack:
                    if (UIRef.TurnWheelBig.eulerAngles.z < 146.25f - rOffset || UIRef.TurnWheelBig.eulerAngles.z > 146.25f + rOffset)
                    {
                        UIRef.TurnWheelBig.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                        UIRef.TurnWheelSmol.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                    }
                    break;
                case GameStateEnum.move:
                    if (UIRef.TurnWheelBig.eulerAngles.z < 213.75f - rOffset || UIRef.TurnWheelBig.eulerAngles.z > 213.75f + rOffset)
                    {
                        UIRef.TurnWheelBig.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                        UIRef.TurnWheelSmol.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                    }
                    break;
                case GameStateEnum.skillshot:
                    if (UIRef.TurnWheelBig.eulerAngles.z < 280.25f - rOffset || UIRef.TurnWheelBig.eulerAngles.z > 280.25f + rOffset)
                    {
                        UIRef.TurnWheelBig.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                        UIRef.TurnWheelSmol.Rotate(new Vector3(0f, 0f, degreesPerFrame), Space.Self);
                    }
                    break;
            }
            #endregion

            #region SwitchIngameUI

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                if(UIRef.IngameUIPanel.activeSelf)
                    UIRef.IngameUIPanel.SetActive(false);

                if(gameState.CurrentState == GameStateEnum.cleanup)
                    UIRef.IngameUIPanel.SetActive(true);
            }
            else
            {
                if(Input.GetButtonDown("SwitchIngameUI"))
                {
                    UIRef.IngameUIPanel.SetActive(!UIRef.IngameUIPanel.activeSelf);
                }
            }

            #endregion

            #region GameOver
            if (gameState.CurrentState == GameStateEnum.game_over)
            {
                if (!UIRef.GameOverPanel.activeSelf)
                {
                    if(gameState.WinnerFaction == 0)
                    {
                        UIRef.DrawPanel.SetActive(true);
                    }
                    else if(gameState.WinnerFaction == authPlayerFaction)
                    {
                        UIRef.VictoryPanel.SetActive(true);
                    }
                    else
                    {
                        UIRef.DefeatPanel.SetActive(true);
                    }

                    UIRef.GameOverPanel.SetActive(true);
                }

            }
            #endregion

            #region Unitloop

            Entities.With(m_UnitData).ForEach((Entity e, UnitHeadUIReferences healthbar, ref Actions.Component actions, ref Health.Component health, ref IsVisible isVisible, ref MouseState mouseState, ref FactionComponent.Component faction) =>
            {
                uint unitId = (uint)EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var position = EntityManager.GetComponentObject<Transform>(e).position;
                var energy = EntityManager.GetComponentData<Energy.Component>(e);
                var lineRenderer = EntityManager.GetComponentObject<LineRendererComponent>(e);
                var animatedPortrait = EntityManager.GetComponentObject<AnimatedPortraitReference>(e).PortraitClip;
                var factionColor = faction.TeamColor;
                var stats = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);
                int actionCount = stats.Actions.Count + 2;
                int spawnActionCount = stats.SpawnActions.Count;

                if (gameState.CurrentState == GameStateEnum.planning && healthbar.UnitHeadUIInstance)
                {
                    if(healthbar.UnitHeadUIInstance.ArmorPanelDisplayTime != 0)
                        healthbar.UnitHeadUIInstance.ArmorPanelDisplayTime = 0;
                }

                //WHEN TO UPDATE UNIT BUTTON
                if (stats.SelectUnitButtonInstance)
                    UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);

                //set topLeft healthBar values for this players hero
                if (stats.IsHero && faction.Faction == authPlayerFaction)
                {
                    SetHealthBarFillAmounts(UIRef.TopHealthFill, UIRef.TopArmorFill, health, faction.Faction);

                    if(gameState.CurrentState == GameStateEnum.planning)
                    {
                        UpdateHeroBauble(lineRenderer, actions, UIRef.TopEnergyFill, UIRef.TopEnergyText, energy, faction);
                    }

                }

                if (authPlayerState.SelectedUnitId == unitId)
                {
                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        UpdateCircleBauble(lineRenderer, actions, UIRef.SAEnergyFill, UIRef.SAEnergyText);
                    }

                    if (UIRef.AnimatedPortrait.portraitAnimationClip.name != animatedPortrait.name)
                        UIRef.AnimatedPortrait.portraitAnimationClip = animatedPortrait;

                    string currentMaxHealth = health.CurrentHealth + "/" + health.MaxHealth;

                    if (health.Armor > 0 && faction.Faction == authPlayerFaction)
                    {
                        UIRef.HealthText.text = currentMaxHealth + " +" + health.Armor;
                    }
                    else
                    {
                        UIRef.HealthText.text = currentMaxHealth;
                    }

                    SetHealthBarFillAmounts(UIRef.HealthFill, UIRef.ArmorFill, health, faction.Faction);

                    if (factionColor == TeamColorEnum.blue)
                    {
                        if (UIRef.PortraitPlayerColor.color != settings.FactionColors[1])
                            UIRef.PortraitPlayerColor.color = settings.FactionColors[1];
                    }
                    else if (factionColor == TeamColorEnum.red)
                    {
                        if (UIRef.PortraitPlayerColor.color != settings.FactionColors[2])
                            UIRef.PortraitPlayerColor.color = settings.FactionColors[2];
                    }

                    if (faction.Faction == authPlayerFaction)
                    {
                        if (stats.SpawnActions.Count == 0)
                        {
                            UIRef.SpawnActionsToggle.isOn = false;
                            UIRef.ActionsToggle.isOn = true;
                            UIRef.SpawnToggleGO.SetActive(false);
                        }
                        else
                        {
                            UIRef.SpawnToggleGO.SetActive(true);
                        }

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

                                if (stats.BasicMove != null && stats.BasicAttack != null)
                                {
                                    if (bi == 0)
                                    {
                                        if (playerEnergy.Energy == 0 && actions.LockedAction.Index == -3)// && lockedAction.cost <= stats.BasicMove.cost
                                        {
                                            UIRef.Actions[bi].Button.interactable = false;
                                        }
                                        else
                                        {
                                            UIRef.Actions[bi].Button.interactable = true;
                                        }
                                    }
                                    else if (bi == 1)
                                    {
                                        if (stats.BasicAttack.Targets[0].energyCost > playerEnergy.Energy + actions.LockedAction.CombinedCost)
                                        {
                                            UIRef.Actions[bi].Button.interactable = false;
                                        }
                                        else
                                        {
                                            UIRef.Actions[bi].Button.interactable = true;
                                        }
                                    }
                                    //all other actions
                                    else
                                    {
                                        if (stats.Actions[bi - 2].Targets[0].energyCost > playerEnergy.Energy + actions.LockedAction.CombinedCost)
                                        {
                                            UIRef.Actions[bi].Button.interactable = false;
                                        }
                                        else
                                        {
                                            UIRef.Actions[bi].Button.interactable = true;
                                        }
                                    }

                                    UIRef.Actions[bi] = FillButtonFields(UIRef.Actions[bi], stats, unitId, bi, false, true);
                                }
                                else
                                {
                                    if (bi < actionCount - 2)
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
                            }
                            else
                            {
                                UIRef.Actions[bi].Visuals.SetActive(false);
                            }
                        }
                    }
                    else
                    {
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

                if (authPlayerState.SelectedUnitId != 0)
                {
                    if (!unitInfoPanel.activeSelf)
                        unitInfoPanel.SetActive(true);
                }

                else
                {
                    if (unitInfoPanel.activeSelf)
                        unitInfoPanel.SetActive(false);
                }


                if (!stats.UIInitialized)
                {
                    InitializeUnitUI(healthbar, stats, unitId, faction.Faction, authPlayerFaction);
                    stats.UIInitialized = true;
                }
                else
                {
                    //does not get called when units get destroyed because a player disconnects
                    //
                    if (health.CurrentHealth == 0)
                    {
                        if(healthbar.UnitHeadUIInstance)
                            CleanupUnitUI(healthbar, stats, unitId, faction.Faction, authPlayerFaction);
                    }
                    else
                    {
                        GameObject healthBarGO = healthbar.UnitHeadHealthBarInstance.gameObject;

                        if (!healthBarGO.activeSelf && isVisible.Value == 1)
                        {
                            healthBarGO.SetActive(true);
                        }
                        else if (isVisible.Value == 0)
                        {
                            healthBarGO.SetActive(false);
                        }

                        if(healthbar.UnitHeadUIInstance.ArmorPanelDisplayTime > 0)
                        {
                            healthbar.UnitHeadUIInstance.ArmorPanelDisplayTime -= Time.deltaTime;
                        }
                        else if(healthbar.UnitHeadUIInstance.ArmorPanel.activeSelf)
                        {
                            healthbar.UnitHeadUIInstance.ArmorPanel.SetActive(false);
                        }

                        healthbar.UnitHeadUIInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, healthbar.HealthBarYOffset, 0));
                        healthbar.UnitHeadHealthBarInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, healthbar.HealthBarYOffset, 0));

                        SetHealthBarFillAmounts(healthbar.UnitHeadHealthBarInstance.HealthFill, healthbar.UnitHeadHealthBarInstance.ArmorFill, health, faction.Faction);
                    }
                }
            });
            #endregion

            #region PlayerLoops
            //this clients player
            Entities.With(m_AuthoritativePlayerData).ForEach((ref PlayerEnergy.Component authPlayerEnergy) =>
            {
                float maxEnergy = authPlayerEnergy.MaxEnergy;
                float currentEnergy = authPlayerEnergy.Energy;
                float energyIncome = authPlayerEnergy.Income;
                Image energyFill = UIRef.LeftCurrentEnergyFill;
                Image incomeFill = UIRef.LeftEnergyIncomeFill;
                Text currentEnergyText = UIRef.CurrentEnergyText;
                Text maxEnergyText = UIRef.MaxEnergyText;

                if (gameState.CurrentState != GameStateEnum.planning)
                {
                    if (UIRef.ReadyButton.interactable)
                    {
                        UIRef.ReadyButton.interactable = false;
                    }
                }
                else
                {
                    if (!UIRef.ReadyButton.interactable)
                    {
                        UIRef.ReadyButton.interactable = true;
                    }

                    energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);

                    if (energyFill.fillAmount >= currentEnergy / maxEnergy - .003f)
                    {
                        incomeFill.fillAmount = Mathf.Lerp(incomeFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);
                    }
                }

                energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);

                currentEnergyText.text = currentEnergy.ToString();
                maxEnergyText.text = maxEnergy.ToString();

                if (authPlayerState.CurrentState != PlayerStateEnum.unit_selected && authPlayerState.CurrentState != PlayerStateEnum.waiting_for_target)
                {
                    for (int bi = 0; bi < UIRef.Actions.Count; bi++)
                    {
                        UIRef.Actions[bi].Visuals.SetActive(false);
                    }

                    for (int si = 0; si < UIRef.SpawnActions.Count; si++)
                    {
                        UIRef.SpawnActions[si].Visuals.SetActive(false);
                    }
                }
            });

            float outSpeed = UIRef.ReadyOutSpeed * Time.deltaTime;
            float inSpeed = UIRef.ReadyInSpeed * Time.deltaTime;

            //all players
            Entities.With(m_PlayerData).ForEach((ref FactionComponent.Component faction, ref PlayerState.Component playerState) =>
            {
                if (faction.TeamColor == TeamColorEnum.blue)
                {
                    //MOVE RECTTRANSFORM BLUEREADY IN Y / X AXIS

                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        if (UIRef.BlueReady.localPosition.x > -88f)
                            UIRef.BlueReady.Translate(new Vector3(0, -outSpeed, 0));
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        //UIRef.BlueReady.localPosition = new Vector2(-69f, UIRef.BlueReady.localPosition.y);
                        if(UIRef.BlueReady.localPosition.x < -69f)
                            UIRef.BlueReady.Translate(new Vector3(0, inSpeed, 0));
                    }

                }
                else if (faction.TeamColor == TeamColorEnum.red)
                {

                    //MOVE RECTTRANSFORM REDREADY IN Y / X AXIS                    
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        if (UIRef.RedReady.localPosition.y < 86f)
                            UIRef.RedReady.Translate(new Vector3(0, outSpeed, 0));
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if(UIRef.RedReady.localPosition.y > 69f)
                            UIRef.RedReady.Translate(new Vector3(0, -inSpeed, 0));
                    }

                }
            });
            #endregion

            if (gameStates[0].CurrentPlanningTime <= gameStates[0].RopeDisplayTime)
            {
                UIRef.RopeBar.enabled = true;
                UIRef.RopeBar.fillAmount = gameStates[0].CurrentPlanningTime / gameStates[0].RopeDisplayTime;
            }
            else
            {
                UIRef.RopeBar.enabled = false;
            }

            gameStates.Dispose();

            #region authPlayerData
            authPlayersEnergy.Dispose();
            authPlayersFaction.Dispose();
            authPlayersState.Dispose();
            #endregion
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
                unitButton.EnergyFill.color = settings.UIEnergyIncomeColor;
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

        void UpdateUnitGroupBauble(UnitGroupUI unitGroupUI, FactionComponent.Component faction)
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

            percentageToFill /= unitGroupUI.SelectUnitButtons.Count;
            percentageGainFill /= unitGroupUI.SelectUnitButtons.Count;

            
            if (combinedAmount > 0)
            {
                unitGroupUI.EnergyChangeText.text = "+" + combinedAmount;
        //        unitGroupUI.EnergyFill.color = settings.UIEnergyIncomeColor;
            }
            else
            {
                unitGroupUI.EnergyChangeText.text = combinedAmount.ToString();
             //   unitGroupUI.EnergyFill.color = settings.FactionColors[(int)faction.TeamColor + 1];
            }
            
            LerpEnergyFillAmount(unitGroupUI.EnergyFill, percentageToFill);
            LerpEnergyFillAmount(unitGroupUI.EnergyGainFill, percentageGainFill);
        }

        void UpdateCircleBauble(LineRendererComponent lineRenderer, Actions.Component actions, Image inEnergyFill, Text inEnergyText)
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
                if (inEnergyFill.fillAmount < 1)
                    LerpEnergyFillAmount(inEnergyFill, 1);

                inEnergyText.text = actions.LockedAction.CombinedCost.ToString();

            }
            else
            {
                if (inEnergyFill.fillAmount > 0)
                    LerpEnergyFillAmount(inEnergyFill, 0);
            }
        }

        void UpdateHeroBauble(LineRendererComponent lineRenderer, Actions.Component actions, Image inEnergyFill, Text inEnergyText, Energy.Component inEnergy, FactionComponent.Component inFaction)
        {
            int EnergyChangeAmount = 0;
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
                    LerpEnergyFillAmount(inEnergyFill, 1);
                if(EnergyChangeAmount > 0)
                {
                    inEnergyText.text = "+" + EnergyChangeAmount.ToString();
                    inEnergyFill.color = settings.UIEnergyIncomeColor;
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
                    LerpEnergyFillAmount(inEnergyFill, 0);
            }
    }

        public void ResetEnergyBaubles()
        {
            LerpEnergyFillAmount(UIRef.SAEnergyFill, 0);
            LerpEnergyFillAmount(UIRef.TopEnergyFill, 0);

            UIRef.TopEnergyText.text = 0.ToString();
            UIRef.SAEnergyText.text = 0.ToString();
        }

        public void LerpEnergyFillAmount(Image inEnergyFill, float inPercentage)
        {
            inEnergyFill.fillAmount = Mathf.Lerp(inEnergyFill.fillAmount, inPercentage, 0.05f);
        }

        public void SetHealthBarFillAmounts(Image inHealthFill, Image inArmorFill, Health.Component health, uint unitFaction)
        {
            #region authPlayerData
            var authPlayersFaction = m_AuthoritativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            #endregion

            var playerFaction = authPlayersFaction[0].Faction;

            if(unitFaction == playerFaction)
            {
                uint combinedHealth = health.CurrentHealth + health.Armor;
                uint combinedMaxHealth = health.MaxHealth + health.Armor;
                float healthPercentage = 1 - (float)health.Armor / combinedMaxHealth;
                float combinedPercentage = 1;

                if (combinedHealth < health.MaxHealth)
                {
                    combinedPercentage = (float)combinedHealth / combinedMaxHealth;
                }

                inHealthFill.fillAmount = Mathf.Lerp(inHealthFill.fillAmount, combinedPercentage * healthPercentage, 0.1f);
                inArmorFill.fillAmount = Mathf.Lerp(inArmorFill.fillAmount, combinedPercentage, 0.1f);
            }
            else
            {
                inHealthFill.fillAmount = Mathf.Lerp(inHealthFill.fillAmount, (float)health.CurrentHealth / health.MaxHealth, 0.1f);
                inArmorFill.fillAmount = 0;
            }
            authPlayersFaction.Dispose();
        }

        public void SetArmorDisplay(Entity e, uint inArmorAmount, float displayTime, bool shatter = false)
        {
            var healthbar = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);
            Text armorText = healthbar.UnitHeadUIInstance.ArmorText;
            Animator anim = healthbar.UnitHeadUIInstance.ArmorAnimator;
            GameObject armorPanel = healthbar.UnitHeadUIInstance.ArmorPanel;
            armorText.text = inArmorAmount.ToString();
            armorPanel.SetActive(true);

            if(shatter)
                anim.SetTrigger("Shatter");

            healthbar.UnitHeadUIInstance.ArmorPanelDisplayTime = displayTime;

        }

        public void SetHealthFloatText(Entity e, bool positive, uint inHealthAmount, Color color)
        {
            var healthbar = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);
            Text healthText = healthbar.UnitHeadUIInstance.FloatHealthText;
            Animator anim = healthbar.UnitHeadUIInstance.FloatHealthAnimator;

            if(healthText)
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
                anim.Play("HealthText", 0, 0);

            }

            /*
            if (isHeal)
            {
                healthColor.r = Color.green.r;
                healthColor.g = Color.green.g;
                healthColor.b = Color.green.b;
            }
            else
            {
                healthColor.r = Color.red.r;
                healthColor.g = Color.red.g;
                healthColor.b = Color.red.b;
            }
            */
            //Debug.Log("PlayFloatingHealthAnim");

        }

        public void InitializeUnitUI(UnitHeadUIReferences healthbar, Unit_BaseDataSet stats, long unitId, uint unitFaction, uint playerFaction)
        {
            //Spawn UnitHeadUI / UnitGroup / SelectUnitButton

            healthbar.UnitHeadUIInstance = Object.Instantiate(healthbar.UnitHeadUIPrefab, healthbar.transform.position, Quaternion.identity, UIRef.ActionEffectUIPanel.transform);
            healthbar.UnitHeadHealthBarInstance = Object.Instantiate(healthbar.UnitHeadHealthBarPrefab, healthbar.transform.position, Quaternion.identity, UIRef.HealthBarsPanel.transform);

            //if there is no group of this unitType, create one

            if (!stats.IsHero)
            {
                if(unitFaction == playerFaction)
                {
                    if (!UIRef.ExistingUnitGroups.ContainsKey(stats.UnitTypeId))
                    {
                        //spawn a group into groups parent and add it to the ExistingUnitGroups Dict
                        UnitGroupUI unitGroup = Object.Instantiate(UIRef.UnitGroupPrefab, UIRef.UnitGroupsParent.transform);
                        unitGroup.UnitTypeImage.sprite = stats.UnitTypeSprite;
                        //if faction is even set to factionColors 1 if odd to factioncolors2
                        unitGroup.EnergyFill.color = settings.FactionColors[(int)playerFaction];
                        unitGroup.EnergyGainFill.color = settings.UIEnergyIncomeColor;
                        SelectUnitButton unitButton = Object.Instantiate(UIRef.UnitButtonPrefab, unitGroup.UnitsPanel.transform);
                        unitButton.UnitId = unitId;
                        unitButton.EnergyFill.color = settings.FactionColors[(int)playerFaction];
                        unitButton.UnitIcon.sprite = stats.UnitTypeSprite;
                        if (!unitGroup.SelectUnitButtons.Contains(unitButton))
                            unitGroup.SelectUnitButtons.Add(unitButton);
                        stats.SelectUnitButtonInstance = unitButton;
                        //problematic if unit has a locked action
                        //unitButton.UnitButton.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(-2, unitId); });
                        unitButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
                        unitGroup.ExistingUnitIds.Add(unitId);
                        unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                        UIRef.ExistingUnitGroups.Add(stats.UnitTypeId, unitGroup);
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
                            unitButton.UnitIcon.sprite = stats.UnitTypeSprite;
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
            }
            else
            {
                UIRef.SelectHeroButton.UnitId = unitId;
                UIRef.SelectHeroButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
            }
        }

        void CleanupUnitUI(UnitHeadUIReferences healthbar, Unit_BaseDataSet stats, long unitID, uint unitFaction, uint playerFaction)
        {
            //Delete headUI / UnitGroupUI on unit death (when health = 0)
            Object.Destroy(healthbar.UnitHeadUIInstance.gameObject);
            Object.Destroy(healthbar.UnitHeadHealthBarInstance.gameObject);

            if (!stats.IsHero && unitFaction == playerFaction)
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
            UIRef.SAExecuteStepBackGround.color = settings.TurnStepBgColors[actionButton.ExecuteStepIndex];
        }

        public void InitializeSelectedActionTooltip(int index)
        {
            UIRef.SAExecuteStepBackGround.enabled = true;
            UIRef.SAExecuteStepIcon.enabled = true;
            UIRef.SAEnergyText.text = UIRef.Actions[index].EnergyCost.ToString();
            UIRef.SAActionDescription.text = UIRef.Actions[index].ActionDescription;
            UIRef.SAActionName.text = UIRef.Actions[index].ActionName;
            UIRef.SAExecuteStepIcon.sprite = UIRef.ExecuteStepSprites[UIRef.Actions[index].ExecuteStepIndex];
            UIRef.SAExecuteStepBackGround.color = settings.TurnStepBgColors[UIRef.Actions[index].ExecuteStepIndex];
        }

        void ClearSelectedActionToolTip()
        {
            UIRef.SAActionName.text = "";
            UIRef.SAActionDescription.text = "";
            UIRef.SAExecuteStepBackGround.enabled = false;
            UIRef.SAExecuteStepIcon.enabled = false;
            UIRef.SAEnergyText.text = "0";
        }
    }
}

