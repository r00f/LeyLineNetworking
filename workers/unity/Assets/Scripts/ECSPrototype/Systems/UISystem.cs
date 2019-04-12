using UnityEngine;
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
            public readonly ComponentArray<Transform> TransformData;
            public readonly ComponentArray<BoxCollider> ColliderData;
            public readonly ComponentDataArray<Health.Component> HealthData;
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
        }

        [Inject] private AuthoritativePlayerData m_AuthoritativePlayerData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
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
                                UIRef.CurrentEnergyFill.color = Color.blue;
                                break;
                            case TeamColorEnum.red:
                                UIRef.CurrentEnergyFill.color = Color.red;
                                break;
                        }

                        UIRef.StartupPanel.SetActive(false);
                    }
                }
            }

            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var position = m_UnitData.TransformData[i].position;
                var health = m_UnitData.HealthData[i];
                var healthbar = m_UnitData.HealthbarData[i];
                var collider = m_UnitData.ColliderData[i];
                //var isVisible = m_Data.IsVisibleData[i];


                //if there is no healthbar, instantiate it into healthBarParent
                if(!healthbar.HealthBarInstance)
                {
                    health.CurrentHealth = health.MaxHealth;
                    healthbar.HealthBarInstance = Object.Instantiate(healthbar.HealthBarPrefab, position, Quaternion.identity, UIRef.HealthbarsPanel.transform);
                }
                else
                {
                    
                    if (gameState == GameStateEnum.planning || gameState == GameStateEnum.calculate_energy)
                    {
                        healthbar.HealthBarInstance.SetActive(true);
                    }
                    else
                    {
                        healthbar.HealthBarInstance.SetActive(false);
                    }
                    
                    healthbar.HealthBarInstance.transform.position = WorldToUISpace(UIRef.Canvas, position + new Vector3(0, collider.bounds.size.y + UIRef.HealthBarYOffset, 0));
                    healthbar.HealthBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount = Mathf.Lerp(healthbar.HealthBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>().fillAmount, (float)health.CurrentHealth / (float)health.MaxHealth, 0.1f);
                }
            }

            
            for (int i = 0; i < m_AuthoritativePlayerData.Length; i++)
            {
                float maxEnergy = m_AuthoritativePlayerData.PlayerEnergyData[i].MaxEnergy;
                float currentEnergy = m_AuthoritativePlayerData.PlayerEnergyData[i].Energy;
                float energyIncome = m_AuthoritativePlayerData.PlayerEnergyData[i].Income;
                Image energyFill = UIRef.CurrentEnergyFill;
                Image incomeEnergyFill = UIRef.EnergyIncomeFill;


                energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);

                if (energyFill.fillAmount >= currentEnergy / maxEnergy - .003f)
                    incomeEnergyFill.fillAmount = Mathf.Lerp(incomeEnergyFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);
            }
            

            for(int i = 0; i < m_PlayerData.Length; i++)
            {
                var faction = m_PlayerData.FactionData[i];
                var playerState = m_PlayerData.PlayerStateData[i].CurrentState;

                if (faction.TeamColor == TeamColorEnum.blue)
                {
                    if (playerState == PlayerStateEnum.ready && !UIRef.BlueToggle.isOn)
                    {
                        UIRef.BlueToggle.isOn = true;
                    }
                    else if(playerState != PlayerStateEnum.ready)
                    {
                        UIRef.BlueToggle.isOn = false;
                    }
                }
                else if(faction.TeamColor == TeamColorEnum.red)
                {
                    if (playerState == PlayerStateEnum.ready && !UIRef.RedToggle.isOn)
                    {
                        UIRef.RedToggle.isOn = true;
                    }
                    else if(playerState != PlayerStateEnum.ready)
                    {
                        UIRef.RedToggle.isOn = false;
                    }
                }
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

