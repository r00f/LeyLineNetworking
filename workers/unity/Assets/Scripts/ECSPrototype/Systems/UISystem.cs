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
using UnityEngine.UI;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class UISystem : ComponentSystem
    {
        IDictionary<float, float> IDictTest;
        PlayerStateSystem m_PlayerStateSystem;
        SendActionRequestSystem m_SendActionRequestSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        EntityQuery m_PlayerData;
        EntityQuery m_AuthoritativePlayerData;
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        float ayy = 0f;

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
                ComponentType.ReadWrite<HighlightingDataComponent>(),
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
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            InitializeButtons();
        }

        public void InitializeButtons()
        {
            UIRef = Object.FindObjectOfType<UIReferences>();
            UIRef.EscapeMenu.ExitGameButton.onClick.AddListener(delegate { Application.Quit(); });
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
            var playerHighlightingDatas = m_AuthoritativePlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
            var gameState = gameStates[0];
            var authPlayerFaction = authPlayersFaction[0].Faction;
            var authPlayerState = authPlayersState[0];
            var playerEnergy = authPlayersEnergy[0];
            var playerHigh = playerHighlightingDatas[0];

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

                for (int i = 0; i < UIRef.TurnStepFlares.Count; i++)
                {
                    UIRef.TurnStepFlares[i].enabled = false;
                }
            }

            HandleKeyCodeInput(playerHigh, gameState);

            for(int i = 0; i < UIRef.SmallWheelColoredParts.Count; i++)
            {
                UIRef.SmallWheelColoredParts[i].color = settings.TurnStepColors[i];
            }

            for (int i = 0; i < UIRef.BigWheelColoredParts.Count; i++)
            {
                UIRef.BigWheelColoredParts[i].color = settings.TurnStepBgColors[i];
            }

            switch (gameState.CurrentState)
            {
                case GameStateEnum.planning:

                    foreach (UnitGroupUI g in UIRef.ExistingUnitGroups.Values)
                    {
                        UpdateUnitGroupBauble(g, authPlayersFaction[0]);
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
                if (UIRef.ManalithToolTipFab.isActiveAndEnabled)
                {
                    UIRef.ManalithToolTipFab.ActiveManalithID = 0;
                    UIRef.ManalithToolTipFab.gameObject.SetActive(false);
                }
                if(UIRef.IngameUIPanel.activeSelf)
                    UIRef.IngameUIPanel.SetActive(false);
            }
            else
            {
                UIRef.IngameUIPanel.SetActive(playerHigh.ShowIngameUI);

                if (Input.GetButtonDown("SwitchIngameUI"))
                {
                    playerHigh.ShowIngameUI = !playerHigh.ShowIngameUI;
                    playerHighlightingDatas[0] = playerHigh;
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

            Entities.With(m_UnitData).ForEach((Entity e, UnitHeadUIReferences unitHeadUIRef, ref Actions.Component actions, ref Health.Component health, ref IsVisible isVisible, ref MouseState mouseState, ref FactionComponent.Component faction) =>
            {
                uint unitId = (uint)EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
                var position = EntityManager.GetComponentObject<Transform>(e).position;
                var energy = EntityManager.GetComponentData<Energy.Component>(e);
                var lineRenderer = EntityManager.GetComponentObject<LineRendererComponent>(e);
                var animatedPortrait = EntityManager.GetComponentObject<AnimatedPortraitReference>(e).PortraitClip;
                var factionColor = faction.TeamColor;
                var stats = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);
                int actionCount = stats.Actions.Count + 2;
                int spawnActionCount = stats.SpawnActions.Count;

                //ONE TIME WHENEVER authPlayerState.UnitTargets.Count changes
                
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
                

                //}

                if (stats.SelectUnitButtonInstance)
                    UpdateSelectUnitButton(actions, stats.SelectUnitButtonInstance, energy, faction);

                //set topLeft healthBar values for this players hero
                if (stats.IsHero && faction.Faction == authPlayerFaction)
                {
                    SetHealthBarFillAmounts(unitHeadUIRef,UIRef.HeroHealthBar, health, faction.Faction);

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

                    //SEPERATE CODE THAT ONLY NEEDS TO BE DONE ON UNIT SELECTION (SET BUTTON INFO USW)

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

                    SetHealthBarFillAmounts(unitHeadUIRef, UIRef.PortraitHealthBar, health, faction.Faction);

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
                            unitHeadUIRef.HealthTextDelay -= Time.deltaTime;
                        }
                        else
                        {
                            unitHeadUIRef.UnitHeadUIInstance.FloatHealthAnimator.SetBool("Delayed", false);
                        }
                    }

                    if (health.CurrentHealth == 0)
                    {
                        if(unitHeadUIRef.UnitHeadUIInstance.FlagForDestruction == false)
                        {
                            CleanupUnitUI(unitHeadUIRef, stats, unitId, faction.Faction, authPlayerFaction);
                        }
                    }
                    else
                    {

                        GameObject healthBarGO = unitHeadUIRef.UnitHeadHealthBarInstance.gameObject;
                        //ADD ADVANCED HEALTHBAR VISIBILITY (ONLY DISPLAY IF DAMAGED/DAMAGE PREVIEW

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

                            if (health.CurrentHealth < health.MaxHealth || unitHeadUIRef.IncomingDamage > 0 || health.Armor > 0)
                            {
                                if(!unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.activeSelf)
                                    unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.SetActive(true);
                            }
                            else if(unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.activeSelf)
                                unitHeadUIRef.UnitHeadHealthBarInstance.HealthBarGo.SetActive(false);


                        }
                        else
                        {
                            if (healthBarGO.activeSelf)
                                healthBarGO.SetActive(false);
                        }

                        unitHeadUIRef.UnitHeadHealthBarInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0));
                        SetHealthBarFillAmounts(unitHeadUIRef, unitHeadUIRef.UnitHeadHealthBarInstance, health, faction.Faction);
                    }

                    if(unitHeadUIRef.UnitHeadUIInstance)
                        unitHeadUIRef.UnitHeadUIInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, unitHeadUIRef.HealthBarYOffset, 0));

                    if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (unitHeadUIRef.UnitHeadUIInstance.PlanningBufferTime > 0)
                        {
                            unitHeadUIRef.UnitHeadUIInstance.PlanningBufferTime -= Time.deltaTime;
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
                            if (actions.LockedAction.Index == -2)
                            {
                                display.ActionImage.sprite = stats.BasicMove.ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.BasicMove.ActionExecuteStep];
                            }
                            else if (actions.LockedAction.Index == -1)
                            {
                                display.ActionImage.sprite = stats.BasicAttack.ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.BasicAttack.ActionExecuteStep];
                            }
                            else if (actions.LockedAction.Index < stats.Actions.Count)
                            {
                                display.ActionImage.sprite = stats.Actions[actions.LockedAction.Index].ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.Actions[actions.LockedAction.Index].ActionExecuteStep];
                            }
                            else
                            {
                                int spawnactionindex = actions.LockedAction.Index - stats.Actions.Count;
                                display.ActionImage.sprite = stats.SpawnActions[spawnactionindex].ActionIcon;
                                display.TurnStepColorBG.color = settings.TurnStepColors[(int)stats.SpawnActions[spawnactionindex].ActionExecuteStep];
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

                //energyFill.fillAmount = Mathf.SmoothDamp(energyFill.fillAmount, currentEnergy / maxEnergy, ref ayy, 1f);
                UIRef.LeftCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.LeftCurrentEnergyFill.fillAmount, (float)playerEnergy.Energy / playerEnergy.MaxEnergy, Time.deltaTime);

                if (UIRef.LeftCurrentEnergyFill.fillAmount >= (float)playerEnergy.Energy / playerEnergy.MaxEnergy - .003f)
                {
                    //incomeFill.fillAmount = Mathf.SmoothStep(incomeFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, 1f);
                    UIRef.LeftEnergyIncomeFill.fillAmount = Mathf.Lerp(UIRef.LeftEnergyIncomeFill.fillAmount, (float)(playerEnergy.Energy + playerEnergy.Income) / playerEnergy.MaxEnergy, Time.deltaTime);
                }
            }
            //energyFill.fillAmount = Mathf.SmoothStep(energyFill.fillAmount, currentEnergy / maxEnergy, 1f);
            //IMPLEMENT CORRECT LERP CONTROL (Pass start / end values)
            UIRef.LeftCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.LeftCurrentEnergyFill.fillAmount, (float)playerEnergy.Energy / playerEnergy.MaxEnergy, Time.deltaTime);

            UIRef.CurrentEnergyText.text = playerEnergy.Energy.ToString();
            UIRef.MaxEnergyText.text = playerEnergy.MaxEnergy.ToString();

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

            float outSpeed = UIRef.ReadyOutSpeed * Time.deltaTime;
            float inSpeed = UIRef.ReadyInSpeed * Time.deltaTime;

            //all players
            Entities.With(m_PlayerData).ForEach((ref FactionComponent.Component faction, ref PlayerState.Component playerState) =>
            {
                if (faction.TeamColor == TeamColorEnum.blue)
                {
                    //remove scuffed hardcoded position checks

                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        if (UIRef.BlueReady.Rect.anchoredPosition.x > UIRef.BlueReady.StartPosition.x - UIRef.BlueReady.SlideOffset.x)
                        {
                            UIRef.BlueReady.Rect.Translate(new Vector3(0, -outSpeed, 0));
                        }
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (UIRef.BlueReady.Rect.anchoredPosition.x < UIRef.BlueReady.StartPosition.x)
                            UIRef.BlueReady.Rect.Translate(new Vector3(0, inSpeed, 0));
                    }
                }
                else if (faction.TeamColor == TeamColorEnum.red)
                {

                    //MOVE RECTTRANSFORM REDREADY IN Y / X AXIS                    
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                    {
                        if (UIRef.RedReady.Rect.anchoredPosition.y < UIRef.RedReady.StartPosition.y + UIRef.RedReady.SlideOffset.y)
                        {
                            UIRef.RedReady.Rect.Translate(new Vector3(0, outSpeed, 0));
                        }
                    }
                    else if (gameState.CurrentState == GameStateEnum.planning)
                    {
                        if (UIRef.RedReady.Rect.anchoredPosition.y > UIRef.RedReady.StartPosition.y)
                            UIRef.RedReady.Rect.Translate(new Vector3(0, -inSpeed, 0));
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

            //m_AuthoritativePlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
            playerHighlightingDatas.Dispose();
            #endregion
        }

        void HandleKeyCodeInput(HighlightingDataComponent playerHigh, GameState.Component gameState)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIRef.EscapeMenu.gameObject.SetActive(!UIRef.EscapeMenu.gameObject.activeSelf);
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                UIRef.Canvas.gameObject.SetActive(!UIRef.Canvas.gameObject.activeSelf);
            }


            if (playerHigh.InputCooldown <= 0 && gameState.CurrentState == GameStateEnum.planning)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[0].ActionIndex, UIRef.Actions[0].UnitId);
                    InitializeSelectedActionTooltip(UIRef.Actions[0]);
                    m_PlayerStateSystem.ResetInputCoolDown(0.3f);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[1].ActionIndex, UIRef.Actions[1].UnitId);
                    InitializeSelectedActionTooltip(UIRef.Actions[1]);
                    m_PlayerStateSystem.ResetInputCoolDown(0.3f);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[2].ActionIndex, UIRef.Actions[2].UnitId);
                    InitializeSelectedActionTooltip(UIRef.Actions[2]);
                    m_PlayerStateSystem.ResetInputCoolDown(0.3f);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    m_SendActionRequestSystem.SelectActionCommand(UIRef.Actions[3].ActionIndex, UIRef.Actions[3].UnitId);
                    m_PlayerStateSystem.ResetInputCoolDown(0.3f);
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
            inEnergyFill.fillAmount = Mathf.Lerp(inEnergyFill.fillAmount, inPercentage, Time.deltaTime);
        }

        public void SetHealthBarFillAmounts(UnitHeadUIReferences unitHeadUiRef, HealthBar healthBar, Health.Component health, uint unitFaction)
        {
            #region authPlayerData
            var authPlayersFaction = m_AuthoritativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            #endregion

            var playerFaction = authPlayersFaction[0].Faction;
            uint combinedHealth = health.CurrentHealth + health.Armor;
            uint combinedMaxHealth = health.MaxHealth + health.Armor;
            float healthPercentage = 1 - (float)health.Armor / combinedMaxHealth;
            float combinedPercentage = 1;
            
            if (combinedHealth < health.MaxHealth)
            {
                combinedPercentage = (float)combinedHealth / combinedMaxHealth;
            }

            if (unitFaction == playerFaction)
            {
                healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, combinedPercentage * healthPercentage, Time.deltaTime);
                healthBar.ArmorFill.fillAmount = Mathf.Lerp(healthBar.ArmorFill.fillAmount, combinedPercentage, Time.deltaTime);
            }
            else
            {
                healthBar.HealthFill.fillAmount = Mathf.Lerp(healthBar.HealthFill.fillAmount, (float)health.CurrentHealth / health.MaxHealth, Time.deltaTime);
                healthBar.ArmorFill.fillAmount = 0;
            }

            healthBar.DamageFill.fillAmount = Mathf.Lerp(healthBar.DamageFill.fillAmount, (float)unitHeadUiRef.IncomingDamage / combinedHealth, Time.deltaTime);
            healthBar.DamageRect.offsetMax = new Vector2((-healthBar.HealthBarRect.rect.width * (1 - combinedPercentage)) +3f, 0);
            authPlayersFaction.Dispose();
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
                else
                {
                    UIRef.SelectHeroButton.UnitId = unitId;
                    UIRef.SelectHeroButton.UnitButton.onClick.AddListener(delegate { SetSelectedUnitId(unitId); });
                }
            }
            else if (healthbar.UnitHeadUIInstance.ActionDisplay)
                Object.Destroy(healthbar.UnitHeadUIInstance.ActionDisplay.gameObject);
        }

        void CleanupUnitUI(UnitHeadUIReferences healthbar, Unit_BaseDataSet stats, long unitID, uint unitFaction, uint playerFaction)
        {
            //Delete headUI / UnitGroupUI on unit death (when health = 0)
            //INSTEAD OF DELETING DIRECTLY SET FlagForDestruction AND DESTROY FROM UNITCLEANUPSYSTEM AFTER
            healthbar.UnitHeadUIInstance.FlagForDestruction = true;
            Object.Destroy(healthbar.UnitHeadHealthBarInstance.gameObject);

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

