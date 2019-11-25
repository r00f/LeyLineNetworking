using Generic;
using Improbable.Gdk.Core;
using Player;
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

        UIReferences UIRef;

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
                ComponentType.ReadWrite<Healthbar>()
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
            }

            for (int si = 0; si < UIRef.SpawnActions.Count; si++)
            {
                ActionButton ab = UIRef.SpawnActions[si];
                ab.Button.onClick.AddListener(delegate { m_SendActionRequestSystem.SelectActionCommand(ab.ActionIndex, ab.UnitId); });
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

            return inButton;
        }

        protected override void OnUpdate()
        {
            if (m_GameStateData.CalculateEntityCount() == 0 || m_AuthoritativePlayerData.CalculateEntityCount() == 0)
                return;

            var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

            #region authPlayerData
            var authPlayersEnergy = m_AuthoritativePlayerData.ToComponentDataArray<PlayerEnergy.Component>(Allocator.TempJob);
            var authPlayersFaction = m_AuthoritativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            var authPlayersState = m_AuthoritativePlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
            #endregion

            var gameState = gameStates[0];

            if (gameState.CurrentState == GameStateEnum.waiting_for_players)
            {
            }
            else
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
                                //UIRef.HeroPortraitTeamColour.color = Color.blue;

                                UIRef.CurrentEnergyFill.color = settings.FactionColors[1];
                                UIRef.TopCurrentEnergyFill.color = settings.FactionColors[1];

                                break;
                            case TeamColorEnum.red:
                                //UIRef.HeroPortraitTeamColour.color = Color.red;
                                UIRef.CurrentEnergyFill.color = settings.FactionColors[2];
                                UIRef.TopCurrentEnergyFill.color = settings.FactionColors[1];

                                break;
                        }

                        UIRef.BlueColor.color = settings.FactionColors[1];
                        UIRef.RedColor.color = settings.FactionColors[2];

                        UIRef.StartupPanel.SetActive(false);
                    }
                }

                if ((int)gameState.CurrentState <= UIRef.TurnStateToggles.Count && !UIRef.TurnStateToggles[(int)gameState.CurrentState - 1].isOn)
                    UIRef.TurnStateToggles[(int)gameState.CurrentState - 1].isOn = true;
            }

            var authPlayerFaction = authPlayersFaction[0].Faction;
            var authPlayerState = authPlayersState[0];
            var playerEnergy = authPlayersEnergy[0];
            
            GameObject unitInfoPanel = UIRef.InfoEnabledPanel;
            

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
            #region Unitloop

            Entities.With(m_UnitData).ForEach((Entity e, Healthbar healthbar, ref Actions.Component action, ref Health.Component health, ref IsVisible isVisible, ref MouseState mouseState, ref FactionComponent.Component faction) =>
            {
                uint unitId = (uint)EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var position = EntityManager.GetComponentObject<Transform>(e).position;
                var animatedPortrait = EntityManager.GetComponentObject<AnimatedPortraitReference>(e).PortraitClip;
                var factionColor = faction.TeamColor;
                var stats = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);
                int actionCount = stats.Actions.Count + 2;
                int spawnActionCount = stats.SpawnActions.Count;

                //set topLeft healthBar values for this players hero
                if(stats.IsHero && faction.Faction == authPlayerFaction)
                {
                    SetHealthBarFillAmounts(UIRef.TopHealthFill, UIRef.TopArmorFill, health, faction.Faction);
                }

                if (authPlayerState.SelectedUnitId == unitId)
                {
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
                                        if (playerEnergy.Energy == 0 && action.LockedAction.Index == -3)// && lockedAction.cost <= stats.BasicMove.cost
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
                                        if (stats.BasicAttack.Targets[0].energyCost > playerEnergy.Energy + action.LockedAction.CombinedCost)
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
                                        if (stats.Actions[bi - 2].Targets[0].energyCost > playerEnergy.Energy + action.LockedAction.CombinedCost)
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
                                        if (stats.Actions[bi].Targets[0].energyCost > playerEnergy.Energy + action.LockedAction.CombinedCost)
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
                    InitializeUnitUI(healthbar, stats, unitId);
                    stats.UIInitialized = true;
                }
                else
                {
                    //does not get called when units get destroyed because a player disconnects
                    //
                    if (health.CurrentHealth == 0)
                    {
                        if(healthbar.UnitHeadUIInstance)
                            CleanupUnitUI(healthbar, stats, unitId);
                    }
                    else
                    {
                        GameObject healthBarGO = healthbar.UnitHeadUIInstance.transform.GetChild(0).gameObject;

                        if (gameState.CurrentState == GameStateEnum.planning && !healthBarGO.activeSelf && isVisible.Value == 1)
                        {
                            healthBarGO.SetActive(true);
                        }
                        else if (gameState.CurrentState != GameStateEnum.planning || isVisible.Value == 0)
                        {
                            healthBarGO.SetActive(false);
                        }

                        healthbar.UnitHeadUIInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, UIRef.HealthBarYOffset, 0));
                        SetHealthBarFillAmounts(healthBarGO.transform.GetChild(0).GetChild(1).GetComponent<Image>(), healthBarGO.transform.GetChild(0).GetChild(0).GetComponent<Image>(), health, faction.Faction);
                    }
                }
            });
            #endregion
            //this clients player
            Entities.With(m_AuthoritativePlayerData).ForEach((ref PlayerEnergy.Component authPlayerEnergy) =>
            {
                float maxEnergy = authPlayerEnergy.MaxEnergy;
                float currentEnergy = authPlayerEnergy.Energy;
                float energyIncome = authPlayerEnergy.Income;
                Image energyFill = UIRef.CurrentEnergyFill;
                Image incomeEnergyFill = UIRef.EnergyIncomeFill;
                Image topEnergyFill = UIRef.TopCurrentEnergyFill;
                Image topIncomeEnergyFill = UIRef.TopEnergyIncomeFill;
                Text energyText = UIRef.HeroEnergyText;

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

                }

                energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);
                topEnergyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);

                if (energyFill.fillAmount >= currentEnergy / maxEnergy - .003f)
                {
                    incomeEnergyFill.fillAmount = Mathf.Lerp(incomeEnergyFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);
                    topIncomeEnergyFill.fillAmount = Mathf.Lerp(incomeEnergyFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);
                }


                energyText.text = currentEnergy + " / " + maxEnergy;


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

            //all players
            Entities.With(m_PlayerData).ForEach((ref FactionComponent.Component faction, ref PlayerState.Component playerState) =>
            {


                if (faction.TeamColor == TeamColorEnum.blue)
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready && !UIRef.BlueReady.activeSelf)
                    {
                        UIRef.BlueReady.SetActive(true);
                    }
                    else if (playerState.CurrentState != PlayerStateEnum.ready)
                    {
                        UIRef.BlueReady.SetActive(false);
                    }
                }
                else if (faction.TeamColor == TeamColorEnum.red)
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready && !UIRef.RedReady.activeSelf)
                    {
                        UIRef.RedReady.SetActive(true);
                    }
                    else if (playerState.CurrentState != PlayerStateEnum.ready)
                    {
                        UIRef.RedReady.SetActive(false);
                    }
                }
            });

            if(gameStates[0].CurrentPlanningTime <= gameStates[0].RopeDisplayTime)
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

        public void SetHealthFloatText(long inUnitId, uint inHealthAmount, bool isHeal = false)
        {
            Entities.With(m_UnitData).ForEach((Healthbar healthbar, ref SpatialEntityId spatialId) =>
            {
                var unitId = spatialId.EntityId.Id;

                if (inUnitId == unitId)
                {
                    GameObject healthTextGO = healthbar.UnitHeadUIInstance.transform.GetChild(1).gameObject;
                    Text healthText = healthTextGO.GetComponent<Text>();
                    Animator anim = healthTextGO.GetComponent<Animator>();
                    healthText.text = inHealthAmount.ToString();
                    Color healthColor = healthText.color;

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
                    //Debug.Log("PlayFloatingHealthAnim");
                    anim.Play("HealthText", 0, 0);
                }
            });
        }

        public void InitializeUnitUI(Healthbar healthbar, Unit_BaseDataSet stats, long unitId)
        {
            //Spawn UnitHeadUI / UnitGroup / SelectUnitButton

            healthbar.UnitHeadUIInstance = Object.Instantiate(healthbar.UnitHeadUIPrefab, healthbar.transform.position, Quaternion.identity, UIRef.HealthbarsPanel.transform);
            //if there is no group of this unitType, create one

            if (!stats.IsHero)
            {
                if (!UIRef.ExistingUnitGroups.ContainsKey(stats.UnitTypeId))
                {
                    //spawn a group into groups parent and add it to the ExistingUnitGroups Dict
                    UnitGroupUI unitGroup = Object.Instantiate(UIRef.UnitGroupPrefab, UIRef.UnitGroupsParent.transform);
                    unitGroup.UnitTypeImage.sprite = stats.UnitTypeSprite;
                    SelectUnitButton unitButton = Object.Instantiate(UIRef.UnitButtonPrefab, unitGroup.UnitsPanel.transform);
                    unitButton.UnitId = unitId;
                    unitButton.UnitButton.image.sprite = stats.UnitTypeSprite;
                    stats.SelectUnitButtonInstance = unitButton.gameObject;
                    unitButton.UnitButton.onClick.AddListener(delegate {SetSelectedUnitId(unitId); });
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
                        unitButton.UnitId = unitId;
                        unitButton.UnitButton.image.sprite = stats.UnitTypeSprite;
                        stats.SelectUnitButtonInstance = unitButton.gameObject;
                        unitButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
                        unitGroup.ExistingUnitIds.Add(unitId);
                        unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
                    }
                }
            }
        }

        void CleanupUnitUI(Healthbar healthbar, Unit_BaseDataSet stats, long unitID)
        {
            //Delete headUI / UnitGroupUI on unit death (when health = 0)
            Object.Destroy(healthbar.UnitHeadUIInstance);

            //remove unitID from unitGRPUI / delete selectUnitButton
            UnitGroupUI unitGroup = UIRef.ExistingUnitGroups[stats.UnitTypeId];
            unitGroup.ExistingUnitIds.Remove(unitID);
            unitGroup.UnitCountText.text = "" + unitGroup.ExistingUnitIds.Count;
            Object.Destroy(stats.SelectUnitButtonInstance);

            if (unitGroup.ExistingUnitIds.Count == 0)
            {
                UIRef.ExistingUnitGroups.Remove(stats.UnitTypeId);
                Object.Destroy(unitGroup.gameObject);
            }
        }
    }
}

