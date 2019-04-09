using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(ManalithSystem))]
public class ResourceSystem : ComponentSystem
{
    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<FactionComponent.Component> FactionData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
    }

    [Inject] private PlayerData m_PlayerData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Energy.Component> UnitEnergyData;
        public readonly ComponentDataArray<FactionComponent.Component> FactionData;
    }

    [Inject] private UnitData m_UnitData;

    struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameStates;
    }

    [Inject] private GameStateData m_GameStateData;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_GameStateData.Length; i++)
        {
            var gameStateWorldIndex = m_GameStateData.WorldIndexData[i].Value;
            var gameState = m_GameStateData.GameStates[i].CurrentState;

            for (int pi = 0; pi < m_PlayerData.Length; pi++)
            {
                var playerWorldIndex = m_PlayerData.WorldIndexData[pi].Value;
                var playerFaction = m_PlayerData.FactionData[pi].Faction;
                var playerEnergy = m_PlayerData.PlayerEnergyData[pi];
                var playerUpkeep = m_PlayerData.PlayerEnergyData[pi].Upkeep;

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    if(gameState == GameStateEnum.calculate_energy)
                    {
                        playerEnergy.Income = playerEnergy.BaseIncome;

                        //Income Calculation do this once at the beginning of each turn
                        for (int ui = 0; ui < m_UnitData.Length; ui++)
                        {
                            var unitFaction = m_UnitData.FactionData[ui].Faction;
                            var unitEnergy = m_UnitData.UnitEnergyData[ui];
                            //if the player and the unit share the same Faction
                            if (unitFaction == playerFaction)
                            {
                                if(unitEnergy.Harvesting)
                                    playerEnergy.Income += unitEnergy.EnergyIncome;

                                playerEnergy.Income -= unitEnergy.EnergyUpkeep;

                                if (playerEnergy.Energy + playerEnergy.Income <= playerEnergy.MaxEnergy)
                                {
                                    playerEnergy.Energy += playerEnergy.Income;
                                }
                                else
                                {
                                    playerEnergy.Energy = playerEnergy.MaxEnergy;
                                }
                            }
                        }

                        m_PlayerData.PlayerEnergyData[pi] = playerEnergy;
                    }
                }
            }
        }
    }
}
