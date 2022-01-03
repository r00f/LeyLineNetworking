using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Player;
using Unit;
using Unity.Jobs;
using Cell;
using System.Collections.Generic;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
public class ResourceSystem : JobComponentSystem
{
    EntityQuery m_PlayerData;
    EntityQuery m_UnitData;
    EntityQuery m_GameStateData;
    EntityQuery m_ManalithData;
    private EntityQuery projectorGroup;

    Settings settings;
    CleanupSystem m_CleanupSystem;
    ComponentUpdateSystem componentUpdateSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        //var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName).GetComponent<Unit_BaseDataSet>();
        settings = Resources.Load<Settings>("Settings");
        componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

        projectorGroup = Worlds.GameLogicWorld.CreateEntityQuery(

               ComponentType.ReadWrite<Transform>(),
               ComponentType.ReadWrite<Projector>()
        );

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_CleanupSystem = World.GetExistingSystem<CleanupSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.ForEach((in WorldIndexShared gameStateWorldIndex, in GameState.Component gameState) => 
        {
            if (gameState.CurrentState == GameStateEnum.interrupt)
            {
                AddIncome(gameStateWorldIndex);
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }

    public void CalculateIncome(WorldIndexShared worldIndex)
    {
        var workerSystem = World.GetExistingSystem<WorkerSystem>();

        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref PlayerEnergy.Component playerEnergy, in FactionComponent.Component playerFaction) =>
        {
            if (playerEnergy.IncomeAdded == true)
            {
                playerEnergy.Income = AddIncomeFromManaliths(worldIndex, playerEnergy.BaseIncome, playerFaction.Faction);
                playerEnergy.IncomeAdded = false;
            }
        })
        .WithoutBurst()
        .Run();
    }

    uint AddIncomeFromManaliths(WorldIndexShared worldIndex, uint income, uint playerFaction)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((in FactionComponent.Component manalithFaction, in Manalith.Component manalith) =>
        {
            if (manalithFaction.Faction == playerFaction)
            {
                income += manalith.CombinedEnergyGain;
            }
        })
        .WithoutBurst()
        .Run();

        return income;
    }

    void AddIncome(WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref PlayerEnergy.Component energyComp, in FactionComponent.Component faction, in SpatialEntityId playerId) =>
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

                componentUpdateSystem.SendEvent(
                new PlayerEnergy.EnergyChangeEvent.Event(),
                playerId.EntityId);

                energyComp.IncomeAdded = true;
            }
        })
        .WithoutBurst()
        .Run();
    }

    public void AddEnergy(WorldIndexShared worldIndex, uint playerFaction, uint energyAmount)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref PlayerEnergy.Component energyComp, in FactionComponent.Component faction, in SpatialEntityId playerId) =>
        {
            if (playerFaction == faction.Faction)
            {
                if (energyComp.Energy + energyAmount < energyComp.MaxEnergy)
                {
                    energyComp.Energy += energyAmount;
                }
                else
                {
                    energyComp.Energy = energyComp.MaxEnergy;
                }

                componentUpdateSystem.SendEvent(
                new PlayerEnergy.EnergyChangeEvent.Event(),
                playerId.EntityId);
            }
        })
        .WithoutBurst()
        .Run();
    }

    public void SubstactEnergy(WorldIndexShared worldIndex, uint playerFaction, uint energyAmount)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref PlayerEnergy.Component energyComp, in FactionComponent.Component faction, in SpatialEntityId playerId) =>
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

                componentUpdateSystem.SendEvent(
                new PlayerEnergy.EnergyChangeEvent.Event(),
                playerId.EntityId);
            }
        })
        .WithoutBurst()
        .Run();
    }

    public int CheckPlayerEnergy(WorldIndexShared worldIndex, uint playerFaction, uint energyCost = 0)
    {
        int leftOverEnergy = 0;
        Entities.WithSharedComponentFilter(worldIndex).ForEach((in PlayerEnergy.Component energyComp, in FactionComponent.Component faction) =>
        {
            if (playerFaction == faction.Faction)
            {
                leftOverEnergy = (int)energyComp.Energy - (int)energyCost;
            }
        })
        .WithoutBurst()
        .Run();
        return leftOverEnergy;
    }

    public void ResetArmor(WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref Health.Component health, in SpatialEntityId id) =>
        {
            if(health.Armor > 0)
                health.Armor = 0;
            /*
            componentUpdateSystem.SendEvent(
            new Health.ArmorChangeEvent.Event(),
            id.EntityId);
            */
        })
        .WithoutBurst()
        .Run();
    }

    /*
    public void Heal(long unitID, uint healAmount)
    {
        Entities.ForEach((ref Health.Component health, in SpatialEntityId id) =>
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
        })
        .WithoutBurst()
        .Run();
    }
    */
    /*
    public void AddArmor(long unitID, uint armorAmount)
    {
        Entities.ForEach((ref Health.Component health, in SpatialEntityId id) =>
        {
            if (unitID == id.EntityId.Id)
            {
                health.Armor += armorAmount;

                componentUpdateSystem.SendEvent(
                new Health.ArmorChangeEvent.Event(),
                id.EntityId);
            }
        })
        .WithoutBurst()
        .Run();
    }
    */
    /*
    public void RemoveArmor(long unitID, uint armorAmount)
    {
        Entities.ForEach((ref Health.Component health, ref SpatialEntityId id) =>
        {
            if (unitID == id.EntityId.Id)
            {
                health.Armor -= armorAmount;

                componentUpdateSystem.SendEvent(
                new Health.ArmorChangeEvent.Event(),
                id.EntityId);
            }
        })
        .WithoutBurst()
        .Run();
    }
    */
    /*
    public void DealDamage(Dictionary<long, uint> damageDict, ExecuteStepEnum executeStep)
    {
        Entities.ForEach((ref Health.Component health, in SpatialEntityId id) =>
        {
            if (damageDict.ContainsKey(id.EntityId.Id))
            {
                var combinedHealth = health.CurrentHealth + health.Armor;

                if ((int)combinedHealth - (int)damageDict[id.EntityId.Id] > 0)
                {
                    combinedHealth -= damageDict[id.EntityId.Id];
                    if (health.CurrentHealth > combinedHealth)
                        health.CurrentHealth = combinedHealth;
                    else
                        health.Armor = combinedHealth - health.CurrentHealth;
                }
                else
                {
                    health.CurrentHealth = 0;
                    Die(id.EntityId.Id, executeStep);
                }
            }
        })
        .WithoutBurst()
        .Run();
    }
    */
    /*
    public void Die(long unitID, ExecuteStepEnum killingExecuteStep)
    {
        Entities.ForEach((ref Actions.Component actions, in SpatialEntityId id) =>
        {
            //clear dying unit actions if its lockedAction is in an later step then the killing action;
            if (unitID == id.EntityId.Id && actions.LockedAction.ActionExecuteStep > killingExecuteStep)
            {
                actions = m_CleanupSystem.ClearLockedActions(actions);
            }
        })
        .WithoutBurst()
        .Run();
    }
    */
}
