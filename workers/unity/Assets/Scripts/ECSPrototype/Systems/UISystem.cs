﻿using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UI;
using Unit;
using Player;
using Generic;
using Improbable.Gdk.Core;

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
            //public readonly ComponentArray<BoxCollider> ColliderData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Health.Component> HealthData;
            public readonly ComponentDataArray<IsVisible> IsVisibleData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentArray<AnimatedPortraitReference> PortraitData;
            public ComponentArray<Healthbar> HealthbarData;
        }

        [Inject] private UnitData m_UnitData;

        struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<GameState.Component> GameStates;
        }

        [Inject] private GameStateData m_GameStateData;

        public struct AuthoritativePlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public ComponentDataArray<PlayerState.Component> PlayerStateData;
        }

        [Inject] private AuthoritativePlayerData m_AuthoritativePlayerData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public readonly ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
        }

        [Inject] private PlayerData m_PlayerData;

        [Inject] private PlayerStateSystem m_PlayerStateSystem;

        UIReferences UIRef;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            UIRef = Object.FindObjectOfType<UIReferences>();
            UIRef.ReadyButton.onClick.AddListener(delegate { m_PlayerStateSystem.SetPlayerState(PlayerStateEnum.ready); });
        }

        protected override void OnUpdate()
        {
            if (m_GameStateData.GameStates.Length == 0)
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
                                UIRef.HeroPortraitTeamColour.color = Color.blue;
                                UIRef.CurrentEnergyFill.color = Color.blue;
                                break;
                            case TeamColorEnum.red:
                                UIRef.HeroPortraitTeamColour.color = Color.red;
                                UIRef.CurrentEnergyFill.color = Color.red;
                                break;
                        }

                        UIRef.StartupPanel.SetActive(false);
                    }
                }
            }

            for (int i = 0; i < m_UnitData.Length; i++)
            {
                GameObject unitInfoPanel = UIRef.InfoEnabledPanel;
                var playerState = m_AuthoritativePlayerData.PlayerStateData[0];
                uint unitId = (uint)m_UnitData.EntityIdData[i].EntityId.Id;
                var position = m_UnitData.TransformData[i].position;
                float currentHealth = m_UnitData.HealthData[i].CurrentHealth;
                float maxHealth = m_UnitData.HealthData[i].MaxHealth;
                var healthbar = m_UnitData.HealthbarData[i];
                var isVisible = m_UnitData.IsVisibleData[i];
                var animatedPortrait = m_UnitData.PortraitData[i].PortraitClip;
                var mouseState = m_UnitData.MouseStateData[i].CurrentState;
                var faction = m_UnitData.FactionData[i];
                var factionColor = m_UnitData.FactionData[i].TeamColor;
                var stats = m_UnitData.BaseDataSets[i];
                //var portraitPlayerColor = ;

                if(mouseState == MouseState.State.Clicked)
                {
                    if(playerState.SelectedUnitId != unitId)
                    {
                        playerState.SelectedUnitId = unitId;
                        m_AuthoritativePlayerData.PlayerStateData[0] = playerState;
                    }
                }

                if (playerState.SelectedUnitId != 0)
                {
                    if (!unitInfoPanel.activeSelf)
                        unitInfoPanel.SetActive(true);
                }
                else
                {
                    if (unitInfoPanel.activeSelf)
                        unitInfoPanel.SetActive(false);
                }

                if (unitId == playerState.SelectedUnitId)
                {
                    if(stats.IsHero)
                    {
                        if (!UIRef.HeroEnergyPanel.activeSelf)
                            UIRef.HeroEnergyPanel.SetActive(true);

                        

                        //get player with faction

                        for(int pi = 0; pi < m_PlayerData.Length; pi++)
                        {
                            var playerEnergy = m_PlayerData.PlayerEnergyData[pi];
                            var playerFaction = m_PlayerData.FactionData[pi];

                            if(faction.Faction == playerFaction.Faction)
                            {
                                switch(playerFaction.TeamColor)
                                {
                                    case TeamColorEnum.blue:
                                        UIRef.HeroCurrentEnergyFill.color = Color.blue;
                                        break;
                                    case TeamColorEnum.red:
                                        UIRef.HeroCurrentEnergyFill.color = Color.red;
                                        break;
                                }

                                UIRef.HeroCurrentEnergyFill.fillAmount = Mathf.Lerp(UIRef.HeroCurrentEnergyFill.fillAmount, (float)playerEnergy.Energy / playerEnergy.MaxEnergy, .1f);

                                if (UIRef.HeroCurrentEnergyFill.fillAmount >= (float)playerEnergy.Energy / playerEnergy.MaxEnergy - .003f)
                                    UIRef.HeroEnergyIncomeFill.fillAmount = Mathf.Lerp(UIRef.HeroEnergyIncomeFill.fillAmount, (float)(playerEnergy.Energy + playerEnergy.Income) / playerEnergy.MaxEnergy, .1f);

                                UIRef.HeroEnergyText.text = playerEnergy.Energy + " / " + playerEnergy.MaxEnergy;

                            }

                        }


                    }
                    else
                    {
                        if (!UIRef.HeroEnergyPanel.activeSelf)
                            UIRef.HeroEnergyPanel.SetActive(false);
                    }

                    if (UIRef.AnimatedPortrait.portraitAnimationClip.name != animatedPortrait.name)
                        UIRef.AnimatedPortrait.portraitAnimationClip = animatedPortrait;

                    UIRef.HealthText.text = currentHealth + " / " + maxHealth;
                    UIRef.HealthFill.fillAmount = Mathf.Lerp(UIRef.HealthFill.fillAmount, currentHealth / maxHealth, 0.1f);

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
                }


                //if there is no healthbar, instantiate it into healthBarParent
                if(!healthbar.HealthBarInstance)
                {
                    currentHealth = maxHealth;
                    healthbar.HealthBarInstance = Object.Instantiate(healthbar.HealthBarPrefab, position, Quaternion.identity, UIRef.HealthbarsPanel.transform);
                }
                else
                {
                    if (gameState == GameStateEnum.planning && !healthbar.HealthBarInstance.activeSelf && isVisible.Value == 1)
                    {
                        healthbar.HealthBarInstance.SetActive(true);
                    }
                    else if(gameState != GameStateEnum.planning || isVisible.Value == 0)
                    {
                        healthbar.HealthBarInstance.SetActive(false);
                    }
                    
                    healthbar.HealthBarInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, UIRef.HealthBarYOffset, 0));
                    healthbar.HealthBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount = Mathf.Lerp(healthbar.HealthBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount, currentHealth / maxHealth, 0.1f);
                }
            }

            //this clients player
            for (int i = 0; i < m_AuthoritativePlayerData.Length; i++)
            {
                var playerState = m_AuthoritativePlayerData.PlayerStateData[i];
                float maxEnergy = m_AuthoritativePlayerData.PlayerEnergyData[i].MaxEnergy;
                float currentEnergy = m_AuthoritativePlayerData.PlayerEnergyData[i].Energy;
                float energyIncome = m_AuthoritativePlayerData.PlayerEnergyData[i].Income;
                Image energyFill = UIRef.CurrentEnergyFill;
                Image incomeEnergyFill = UIRef.EnergyIncomeFill;

                energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);

                if (energyFill.fillAmount >= currentEnergy / maxEnergy - .003f)
                    incomeEnergyFill.fillAmount = Mathf.Lerp(incomeEnergyFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);
            }

            //all players
            for(int i = 0; i < m_PlayerData.Length; i++)
            {
                var faction = m_PlayerData.FactionData[i];
                var playerState = m_PlayerData.PlayerStateData[i].CurrentState;

                if (faction.TeamColor == TeamColorEnum.blue)
                {
                    if (playerState == PlayerStateEnum.ready && !UIRef.BlueReady.enabled)
                    {
                        UIRef.BlueReady.enabled = true;
                    }
                    else if(playerState != PlayerStateEnum.ready)
                    {
                        UIRef.BlueReady.enabled = false;
                    }
                }
                else if(faction.TeamColor == TeamColorEnum.red)
                {
                    if (playerState == PlayerStateEnum.ready && !UIRef.RedReady.enabled)
                    {
                        UIRef.RedReady.enabled = true;
                    }
                    else if(playerState != PlayerStateEnum.ready)
                    {
                        UIRef.RedReady.enabled = false;
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

    }

}
