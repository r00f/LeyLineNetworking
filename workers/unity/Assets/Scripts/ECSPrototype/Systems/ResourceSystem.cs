using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Player;
using Unit;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
public class ResourceSystem : ComponentSystem
{
    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<FactionComponent.Component> FactionData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
    }

    [Inject] PlayerData m_PlayerData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIdData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Energy.Component> UnitEnergyData;
        public readonly ComponentDataArray<FactionComponent.Component> FactionData;
        public ComponentDataArray<Health.Component> HealthData;
    }

    [Inject] UnitData m_UnitData;

    struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameStates;
    }

    [Inject] GameStateData m_GameStateData;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_GameStateData.Length; i++)
        {
            var gameStateWorldIndex = m_GameStateData.WorldIndexData[i].Value;
            var gameState = m_GameStateData.GameStates[i].CurrentState;

            if(gameState == GameStateEnum.calculate_energy)
            {
                ResetIncomeAdded(gameStateWorldIndex);
            }
            else if(gameState == GameStateEnum.planning)
            {
                AddIncome(gameStateWorldIndex);
            }
        }
    }

    public void ResetIncomeAdded(uint worldIndex)
    {
        var workerSystem = World.GetExistingManager<WorkerSystem>();

        for (int pi = 0; pi < m_PlayerData.Length; pi++)
        {
            var playerWorldIndex = m_PlayerData.WorldIndexData[pi].Value;
            var playerEnergy = m_PlayerData.PlayerEnergyData[pi];

            if (playerWorldIndex == worldIndex && playerEnergy.IncomeAdded == true)
            {
                playerEnergy.IncomeAdded = false;
                m_PlayerData.PlayerEnergyData[pi] = playerEnergy;
                /*workerSystem.LogDispatcher.HandleLog(LogType.Warning, new LogEvent("ResetIncomeAdded.")
                .WithField("InComeAdded", m_PlayerData.PlayerEnergyData[pi].IncomeAdded));*/
            }
        }
    }

    public void AddIncome(uint worldIndex)
    {
        var workerSystem = World.GetExistingManager<WorkerSystem>();

        for (int pi = 0; pi < m_PlayerData.Length; pi++)
        {
            var playerWorldIndex = m_PlayerData.WorldIndexData[pi].Value;
            var playerFaction = m_PlayerData.FactionData[pi].Faction;
            var playerEnergy = m_PlayerData.PlayerEnergyData[pi];

            if (playerWorldIndex == worldIndex)
            {
                if (playerEnergy.IncomeAdded == false)
                {
                    playerEnergy.IncomeAdded = true;
                    playerEnergy.Income = playerEnergy.BaseIncome;

                    //Income Calculation do this once at the beginning of each turn
                    for (int ui = 0; ui < m_UnitData.Length; ui++)
                    {
                        var unitFaction = m_UnitData.FactionData[ui].Faction;
                        var unitEnergy = m_UnitData.UnitEnergyData[ui];

                        //if the player and the unit share the same Faction
                        if (unitFaction == playerFaction)
                        {
                            if (unitEnergy.Harvesting)
                                playerEnergy.Income += unitEnergy.EnergyIncome;

                            playerEnergy.Income -= unitEnergy.EnergyUpkeep;
                        }
                    }

                    if (playerEnergy.Energy + playerEnergy.Income <= playerEnergy.MaxEnergy)
                    {
                        playerEnergy.Energy += playerEnergy.Income;
                    }
                    else
                    {
                        playerEnergy.Energy = playerEnergy.MaxEnergy;
                    }

                    m_PlayerData.PlayerEnergyData[pi] = playerEnergy;

                    /*workerSystem.LogDispatcher.HandleLog(LogType.Warning, new LogEvent("AddIncome.")
                    .WithField("InComeAdded", m_PlayerData.PlayerEnergyData[pi].IncomeAdded));*/

                }
            }
        }
    }

    public void AddEnergy(uint playerFaction, uint energyAmount)
    {
        for (int i = 0; i < m_PlayerData.Length; i++)
        {
            var faction = m_PlayerData.FactionData[i].Faction;
            var energyComp = m_PlayerData.PlayerEnergyData[i];

            if(playerFaction == faction)
            {
                if (energyComp.Energy + energyAmount < energyComp.MaxEnergy)
                {
                    energyComp.Energy += energyAmount;
                }
                else
                {
                    energyComp.Energy = energyComp.MaxEnergy;
                }

                m_PlayerData.PlayerEnergyData[i] = energyComp;
            }
        }
    }

    public void SubstactEnergy(uint playerFaction, uint energyAmount)
    {
        for (int i = 0; i < m_PlayerData.Length; i++)
        {
            var faction = m_PlayerData.FactionData[i].Faction;
            var energyComp = m_PlayerData.PlayerEnergyData[i];

            if (playerFaction == faction)
            {
                //since uint cant be negative and turns into a huge number convert to int to check
                if ((int)energyComp.Energy - (int)energyAmount > 0)
                {
                    energyComp.Energy -= energyAmount;
                }
                else
                {
                    energyComp.Energy = 0;
                }

                m_PlayerData.PlayerEnergyData[i] = energyComp;
            }
        }
    }

    public int CheckPlayerEnergy(uint playerFaction, uint energyCost)
    {
        int leftOverEnergy = 0;

        for (int i = 0; i < m_PlayerData.Length; i++)
        {
            var faction = m_PlayerData.FactionData[i].Faction;
            var energyComp = m_PlayerData.PlayerEnergyData[i];

            if(playerFaction == faction)
            {
                leftOverEnergy = (int)energyComp.Energy - (int)energyCost;
            }
        }
        return leftOverEnergy;
    }

    public void Heal(long unitID, uint healAmount)
    {
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var id = m_UnitData.EntityIdData[i].EntityId.Id;
            var health = m_UnitData.HealthData[i];

            if (id == unitID)
            {
                if (health.CurrentHealth + healAmount < health.MaxHealth)
                {
                    health.CurrentHealth += healAmount;
                }
                else
                {
                    health.CurrentHealth = health.MaxHealth;
                }

                m_UnitData.HealthData[i] = health;
            }
        }
    }

    public void AddArmor(long unitID, uint armorAmount)
    {
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var id = m_UnitData.EntityIdData[i].EntityId.Id;
            var health = m_UnitData.HealthData[i];

            if (unitID == id)
            {
                health.Armor += armorAmount;
                m_UnitData.HealthData[i] = health;
            }
        }
    }

    public void RemoveArmor(long unitID, uint armorAmount)
    {
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var id = m_UnitData.EntityIdData[i].EntityId.Id;
            var health = m_UnitData.HealthData[i];

            if (unitID == id)
            {
                health.Armor -= armorAmount;
                m_UnitData.HealthData[i] = health;
            }
        }
    }

    public void ResetArmor(uint worldIndex)
    {
        UpdateInjectedComponentGroups();
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var id = m_UnitData.EntityIdData[i].EntityId;
            var health = m_UnitData.HealthData[i];
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;

            if(worldIndex == unitWorldIndex)
            {
                health.Armor = 0;
                m_UnitData.HealthData[i] = health;
            }
        }
    }

    public void DealDamage(long unitID, uint damageAmount)
    {
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var id = m_UnitData.EntityIdData[i].EntityId.Id;
            var health = m_UnitData.HealthData[i];

            if(unitID == id)
            {
                var combinedHealth = health.CurrentHealth + health.Armor;

                if((int)combinedHealth - (int)damageAmount > 0)
                {
                    combinedHealth -= damageAmount;
                    if (health.CurrentHealth > combinedHealth)
                        health.CurrentHealth = combinedHealth;
                    else
                        health.Armor = combinedHealth - health.CurrentHealth;
                }
                else
                {
                    health.CurrentHealth = 0;
                    Die(unitID);
                }
                m_UnitData.HealthData[i] = health;
            }
        }
    }

    public void Die(long unitID)
    {
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var id = m_UnitData.EntityIdData[i].EntityId;

            if (unitID.Equals(id))
            {
                //DEATH CODE

            }
        }
    }
}
