using Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Player;
using Unit;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateAfter(typeof(PlayerStateSystem))]
    public class UISystem : ComponentSystem
    {
        struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<SpatialEntityId> EntityIdData;
            public readonly ComponentArray<Transform> TransformData;
            public readonly ComponentArray<Unit_BaseDataSet> BaseDataSets;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Health.Component> HealthData;
            public readonly ComponentDataArray<IsVisible> IsVisibleData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<Actions.Component> Actions;
            public readonly ComponentArray<AnimatedPortraitReference> PortraitData;
            public ComponentArray<UnitComponentReferences> ComponentReferences;
            public ComponentArray<Healthbar> HealthbarData;
        }

        [Inject] UnitData m_UnitData;

        struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<GameState.Component> GameStates;
        }

        [Inject] GameStateData m_GameStateData;

        public struct AuthoritativePlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public ComponentDataArray<PlayerState.Component> PlayerStateData;
        }

        [Inject] AuthoritativePlayerData m_AuthoritativePlayerData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
        }

        [Inject] PlayerData m_PlayerData;

        [Inject] PlayerStateSystem m_PlayerStateSystem;

        [Inject] SendActionRequestSystem m_SendActionRequestSystem;

        UIReferences UIRef;

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

        protected override void OnUpdate()
        {
            if (m_GameStateData.GameStates.Length == 0 || m_AuthoritativePlayerData.Length == 0)
                return;

            var gameState = m_GameStateData.GameStates[0].CurrentState;

            if (gameState != GameStateEnum.waiting_for_players)
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
                        switch (m_AuthoritativePlayerData.FactionData[0].TeamColor)
                        {
                            case TeamColorEnum.blue:
                                //UIRef.HeroPortraitTeamColour.color = Color.blue;
                                UIRef.CurrentEnergyFill.color = Color.blue;
                                break;
                            case TeamColorEnum.red:
                                //UIRef.HeroPortraitTeamColour.color = Color.red;
                                UIRef.CurrentEnergyFill.color = Color.red;
                                break;
                        }

                        UIRef.StartupPanel.SetActive(false);
                    }
                }
            }

            var authPlayerState = m_AuthoritativePlayerData.PlayerStateData[0];
            var playerEnergy = m_AuthoritativePlayerData.PlayerEnergyData[0];

            for (int i = 0; i < m_UnitData.Length; i++)
            {
                GameObject unitInfoPanel = UIRef.InfoEnabledPanel;
                var authPlayerFaction = m_AuthoritativePlayerData.FactionData[0].Faction;
                uint unitId = (uint)m_UnitData.EntityIdData[i].EntityId.Id;
                var action = m_UnitData.Actions[i];
                var position = m_UnitData.TransformData[i].position;
                var health = m_UnitData.HealthData[i];
                var healthbar = m_UnitData.HealthbarData[i];
                var isVisible = m_UnitData.IsVisibleData[i];
                var animatedPortrait = m_UnitData.PortraitData[i].PortraitClip;
                var mouseState = m_UnitData.MouseStateData[i].CurrentState;
                var faction = m_UnitData.FactionData[i];
                var factionColor = m_UnitData.FactionData[i].TeamColor;
                var stats = m_UnitData.BaseDataSets[i];
                int actionCount = stats.Actions.Count + 2;
                int spawnActionCount = stats.SpawnActions.Count;

                if(authPlayerState.SelectedUnitId == unitId)
                {
                    if (UIRef.AnimatedPortrait.portraitAnimationClip.name != animatedPortrait.name)
                        UIRef.AnimatedPortrait.portraitAnimationClip = animatedPortrait;

                    string currentMaxHealth = health.CurrentHealth + "/" + health.MaxHealth;

                    if(health.Armor > 0)
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
                        if (UIRef.PortraitPlayerColor.color != Color.blue)
                            UIRef.PortraitPlayerColor.color = Color.blue;
                    }
                    else if (factionColor == TeamColorEnum.red)
                    {
                        if (UIRef.PortraitPlayerColor.color != Color.red)
                            UIRef.PortraitPlayerColor.color = Color.red;
                    }

                    if (faction.Faction == authPlayerFaction)
                    {
                        if(stats.SpawnActions.Count == 0)
                        {
                            UIRef.SpawnToggle.SetActive(false);
                        }
                        else
                        {
                            UIRef.SpawnToggle.SetActive(true);
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
                                UIRef.SpawnActions[si].ActionName = stats.SpawnActions[si].ActionName;
                                UIRef.SpawnActions[si].Icon.sprite = stats.SpawnActions[si].ActionIcon;
                                UIRef.SpawnActions[si].ActionIndex = stats.Actions.Count + si;
                                UIRef.SpawnActions[si].UnitId = (int)unitId;
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
                                UIRef.Actions[bi].UnitId = (int)unitId;
                                //basic move
                                //disable if not enough energy
                                if (bi == 0)
                                {
                                    if(playerEnergy.Energy == 0 && action.LockedAction.Index == -3)// && lockedAction.cost <= stats.BasicMove.cost
                                    {
                                        UIRef.Actions[bi].Button.interactable = false;
                                    }
                                    else
                                    {
                                        UIRef.Actions[bi].Button.interactable = true;
                                    }
                                    UIRef.Actions[bi].ActionName = stats.BasicMove.ActionName;
                                    UIRef.Actions[bi].Icon.sprite = stats.BasicMove.ActionIcon;
                                    UIRef.Actions[bi].ActionIndex = -2;
                                }
                                //basic attack
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
                                    UIRef.Actions[bi].ActionName = stats.BasicAttack.ActionName;
                                    UIRef.Actions[bi].Icon.sprite = stats.BasicAttack.ActionIcon;
                                    UIRef.Actions[bi].ActionIndex = -1;
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
                                    UIRef.Actions[bi].ActionName = stats.Actions[bi - 2].ActionName;
                                    UIRef.Actions[bi].Icon.sprite = stats.Actions[bi - 2].ActionIcon;
                                    UIRef.Actions[bi].ActionIndex = bi - 2;
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

                //if there is no healthbar, instantiate it into healthBarParent
                if(!healthbar.UnitHeadUIInstance)
                {
                    healthbar.UnitHeadUIInstance = Object.Instantiate(healthbar.UnitHeadUIPrefab, position, Quaternion.identity, UIRef.HealthbarsPanel.transform);                }
                else
                {

                    //enable healthTextGO when health is changing

                    GameObject healthBarGO = healthbar.UnitHeadUIInstance.transform.GetChild(0).gameObject;

                    if (gameState == GameStateEnum.planning && !healthBarGO.activeSelf && isVisible.Value == 1)
                    {
                        healthBarGO.SetActive(true);
                    }
                    else if(gameState != GameStateEnum.planning || isVisible.Value == 0)
                    {
                        healthBarGO.SetActive(false);
                    }

                    healthbar.UnitHeadUIInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, UIRef.HealthBarYOffset, 0));
                    SetHealthBarFillAmounts(healthBarGO.transform.GetChild(0).GetChild(1).GetComponent<Image>(), healthBarGO.transform.GetChild(0).GetChild(0).GetComponent<Image>(), health, faction.Faction);
                }
            }

            //this clients player
            for (int i = 0; i < m_AuthoritativePlayerData.Length; i++)
            {
                float maxEnergy = m_AuthoritativePlayerData.PlayerEnergyData[i].MaxEnergy;
                float currentEnergy = m_AuthoritativePlayerData.PlayerEnergyData[i].Energy;
                float energyIncome = m_AuthoritativePlayerData.PlayerEnergyData[i].Income;
                Image energyFill = UIRef.CurrentEnergyFill;
                Image incomeEnergyFill = UIRef.EnergyIncomeFill;
                Text energyText = UIRef.HeroEnergyText;

                if(gameState != GameStateEnum.planning)
                {
                    if(UIRef.ReadyButton.interactable)
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

                if (energyFill.fillAmount >= currentEnergy / maxEnergy - .003f)
                    incomeEnergyFill.fillAmount = Mathf.Lerp(incomeEnergyFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);

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
            }

            //all players
            for(int i = 0; i < m_PlayerData.Length; i++)
            {
                var faction = m_PlayerData.FactionData[i];
                var playerState = m_PlayerData.PlayerStateData[i].CurrentState;

                if (faction.TeamColor == TeamColorEnum.blue)
                {
                    if (playerState == PlayerStateEnum.ready && !UIRef.BlueReady.activeSelf)
                    {
                        UIRef.BlueReady.SetActive(true);
                    }
                    else if(playerState != PlayerStateEnum.ready)
                    {
                        UIRef.BlueReady.SetActive(false);
                    }
                }
                else if(faction.TeamColor == TeamColorEnum.red)
                {
                    if (playerState == PlayerStateEnum.ready && !UIRef.RedReady.activeSelf)
                    {
                        UIRef.RedReady.SetActive(true);
                    }
                    else if(playerState != PlayerStateEnum.ready)
                    {
                        UIRef.RedReady.SetActive(false);
                    }
                }
            }

            if(m_GameStateData.GameStates[0].CurrentPlanningTime <= m_GameStateData.GameStates[0].RopeDisplayTime)
            {
                UIRef.RopeBar.enabled = true;
                UIRef.RopeBar.fillAmount = m_GameStateData.GameStates[0].CurrentPlanningTime / m_GameStateData.GameStates[0].RopeDisplayTime;
            }
            else
            {
                UIRef.RopeBar.enabled = false;
            }
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
            var playerFaction = m_PlayerData.FactionData[0].Faction;

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
        }

        public void SetHealthFloatText(long inUnitId, uint inHealthAmount, bool isHeal = false)
        {
            UpdateInjectedComponentGroups();
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var unitId = m_UnitData.EntityIdData[i].EntityId.Id;
                var healthbar = m_UnitData.HealthbarData[i];

                if(inUnitId == unitId)
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
                    Debug.Log("PlayFloatingHealthAnim");
                    anim.Play("HealthText", 0, 0);
                }
            }
        }
    }
}

