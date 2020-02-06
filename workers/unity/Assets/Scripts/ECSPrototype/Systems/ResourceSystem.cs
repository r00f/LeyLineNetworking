using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Player;
using Unit;
using Unity.Collections;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
public class ResourceSystem : ComponentSystem
{
    EntityQuery m_PlayerData;
    EntityQuery m_UnitData;
    EntityQuery m_GameStateData;
    private EntityQuery projectorGroup;

    Settings settings;
    CleanupSystem m_CleanupSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        //var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName).GetComponent<Unit_BaseDataSet>();
        settings = Resources.Load<Settings>("Settings");

        projectorGroup = Worlds.GameLogicWorld.CreateEntityQuery(

               ComponentType.ReadWrite<Transform>(),
               ComponentType.ReadWrite<Projector>()
        );

        m_PlayerData = Worlds.GameLogicWorld.CreateEntityQuery(
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<PlayerEnergy.Component>()

            );
        m_UnitData = Worlds.GameLogicWorld.CreateEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<Health.Component>(),
            ComponentType.ReadWrite<Actions.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Energy.Component>()
        );

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_CleanupSystem = World.GetExistingSystem<CleanupSystem>();
    }

    protected override void OnUpdate()
    {
        Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) => 
        {
            /*
            if(gameState.CurrentState == GameStateEnum.calculate_energy)
            {
                ResetIncomeAdded(gameStateWorldIndex.Value);
            }
            else*/
            if (gameState.CurrentState == GameStateEnum.planning)
            {
                CalculateIncome(gameStateWorldIndex.Value);
            }
            else if (gameState.CurrentState == GameStateEnum.interrupt)
            {
                AddIncome(gameStateWorldIndex.Value);
            }
        });
    }

    /*
    public void ResetIncomeAdded(uint worldIndex)
    {
        //var workerSystem = World.GetExistingSystem<WorkerSystem>();
        Entities.With(m_PlayerData).ForEach((ref WorldIndex.Component playerWorldIndex, ref PlayerEnergy.Component playerEnergy) =>
        {
            if (playerWorldIndex.Value == worldIndex && playerEnergy.IncomeAdded == true)
            {
                playerEnergy.IncomeAdded = false;
                workerSystem.LogDispatcher.HandleLog(LogType.Warning, new LogEvent("ResetIncomeAdded.")
                .WithField("InComeAdded", m_PlayerData.PlayerEnergyData[pi].IncomeAdded));
            }
        });
    }
   */

    public void CalculateIncome(uint worldIndex)
    {
        var workerSystem = World.GetExistingSystem<WorkerSystem>();

        #region unitDataVars
        //var unitsWorldIndex = m_UnitData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        //var unitsAction = m_UnitData.ToComponentDataArray<Actions.Component>(Allocator.TempJob);
        //var unitsId = m_UnitData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
        var unitsFaction = m_UnitData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        var unitsEnergy = m_UnitData.ToComponentDataArray<Energy.Component>(Allocator.TempJob);
        #endregion


        Entities.With(m_PlayerData).ForEach((ref WorldIndex.Component playerWorldIndex, ref FactionComponent.Component playerFaction, ref PlayerEnergy.Component playerEnergy) =>
        {
            if (playerWorldIndex.Value == worldIndex)
            {
                if (playerEnergy.IncomeAdded == true)
                {
                    playerEnergy.Income = playerEnergy.BaseIncome;

                    //Income Calculation do this once at the beginning of each turn
                    for (int ui = 0; ui < unitsEnergy.Length; ui++)
                    {
                        var unitFaction = unitsFaction[ui].Faction;
                        var unitEnergy = unitsEnergy[ui];

                        //if the player and the unit share the same Faction
                        if (unitFaction == playerFaction.Faction)
                        {
                            if (unitEnergy.Harvesting)
                                playerEnergy.Income += unitEnergy.EnergyIncome;

                            playerEnergy.Income -= unitEnergy.EnergyUpkeep;
                        }
                    }

                    playerEnergy.IncomeAdded = false;
                    /*
                    if (playerEnergy.Energy + playerEnergy.Income <= playerEnergy.MaxEnergy)
                    {
                        playerEnergy.Energy += playerEnergy.Income;
                    }
                    else
                    {
                        playerEnergy.Energy = playerEnergy.MaxEnergy;
                    }
                    */
                    //playersEnergy[pi] = playerEnergy;

                    /*workerSystem.LogDispatcher.HandleLog(LogType.Warning, new LogEvent("AddIncome.")
                    .WithField("InComeAdded", m_PlayerData.PlayerEnergyData[pi].IncomeAdded));*/

                }
            }
        });
        unitsFaction.Dispose();
        unitsEnergy.Dispose();
    }

    void AddIncome(uint worldIndex)
    {
        Entities.With(m_PlayerData).ForEach((ref WorldIndex.Component playerWorldIndex, ref PlayerEnergy.Component energyComp, ref FactionComponent.Component faction) =>
        {
            if (playerWorldIndex.Value == worldIndex)
            {
                if (energyComp.IncomeAdded == false)
                {
                    if (energyComp.Energy + energyComp.Income < energyComp.MaxEnergy)
                    {
                        energyComp.Energy += energyComp.Income;
                    }
                    else
                    {
                        energyComp.Energy = energyComp.MaxEnergy;
                    }

                    energyComp.IncomeAdded = true;
                }
            }
        });
    }

    public void AddEnergy(uint playerFaction, uint energyAmount)
    {
        Entities.With(m_PlayerData).ForEach((ref PlayerEnergy.Component energyComp, ref FactionComponent.Component faction) =>
        {
            if (playerFaction == faction.Faction && energyComp.IncomeAdded == false)
            {
                if (energyComp.Energy + energyAmount < energyComp.MaxEnergy)
                {
                    energyComp.Energy += energyAmount;
                }
                else
                {
                    energyComp.Energy = energyComp.MaxEnergy;
                }

                energyComp.IncomeAdded = true;
            }
        });
    }

    public void SubstactEnergy(uint playerFaction, uint energyAmount)
    {
        Entities.With(m_PlayerData).ForEach((ref PlayerEnergy.Component energyComp, ref FactionComponent.Component faction) =>
        {
            if (playerFaction == faction.Faction)
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
            }
        });
    }

    public int CheckPlayerEnergy(uint playerFaction, uint energyCost = 0)
    {
        int leftOverEnergy = 0;
        Entities.With(m_PlayerData).ForEach((ref PlayerEnergy.Component energyComp, ref FactionComponent.Component faction) =>
        {
            if (playerFaction == faction.Faction)
            {
                leftOverEnergy = (int)energyComp.Energy - (int)energyCost;
            }
        });
        return leftOverEnergy;
    }

    public void Heal(long unitID, uint healAmount)
    {
        Entities.With(m_UnitData).ForEach((ref Health.Component health, ref SpatialEntityId id) =>
        {
            if (id.EntityId.Id == unitID)
            {
                if (health.CurrentHealth + healAmount < health.MaxHealth)
                {
                    health.CurrentHealth += healAmount;
                }
                else
                {
                    health.CurrentHealth = health.MaxHealth;
                }

            }
        });
    }

    public void AddArmor(long unitID, uint armorAmount)
    {
        Entities.With(m_UnitData).ForEach((ref Health.Component health, ref SpatialEntityId id) =>
        {
            if (unitID == id.EntityId.Id)
            {
                health.Armor += armorAmount;

            }
        });
    }

    public void RemoveArmor(long unitID, uint armorAmount)
    {
        Entities.With(m_UnitData).ForEach((ref Health.Component health, ref SpatialEntityId id) =>
        {
            if (unitID == id.EntityId.Id)
            {
                health.Armor -= armorAmount;
            }
        });
    }

    public void ResetArmor(uint worldIndex)
    {
        Entities.With(m_UnitData).ForEach((ref Health.Component health, ref SpatialEntityId id, ref WorldIndex.Component unitWorldIndex) =>
        {

            if (worldIndex == unitWorldIndex.Value)
            {
                health.Armor = 0;
            }
        });
    }

    public void DealDamage(long unitID, uint damageAmount, ExecuteStepEnum executeStep)
    {
        Entities.With(m_UnitData).ForEach((ref Health.Component health, ref SpatialEntityId id) =>
        {
            if (unitID == id.EntityId.Id)
            {
                var combinedHealth = health.CurrentHealth + health.Armor;

                if ((int)combinedHealth - (int)damageAmount > 0)
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
                    Die(unitID, executeStep);
                }
            }
        });
    }

    public void Die(long unitID, ExecuteStepEnum executeStep)
    {
        Entities.With(m_UnitData).ForEach((ref Actions.Component actions, ref SpatialEntityId id) =>
        {
            //clear dying unit actions if its lockedAction is in a different state then the killing action;
            if (unitID == id.EntityId.Id && executeStep != actions.LockedAction.ActionExecuteStep)
            {
                actions = m_CleanupSystem.ClearLockedActions(actions);
            }
        });
    }
}
