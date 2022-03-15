using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;
using LeyLineHybridECS;
using Unity.Jobs;
using Player;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
public class CleanupSystem : JobComponentSystem
{
    CommandSystem m_CommandSystem;
    ResourceSystem m_ResourceSystem;
    ExecuteActionsSystem m_ExecuteSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;

    //EntityQuery m_GameStateData;
    //EntityQuery m_UnitData;
    //EntityQuery m_UnitToRemoveData;
    //EntityQuery m_HealthUnitData;
    EntityQuery m_UnitRemovedData;

    protected override void OnCreate()
    {
        base.OnCreate();

        /*
m_HealthUnitData = GetEntityQuery(
ComponentType.ReadOnly<SpatialEntityId>(),
ComponentType.ReadOnly<CubeCoordinate.Component>(),
ComponentType.ReadWrite<Actions.Component>()
);


var unitsToRemoveDesc = new EntityQueryDesc
{
    None = new ComponentType[]
    {
        ComponentType.ReadOnly<Manalith.Component>()
    },
    All = new ComponentType[]
    {
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadWrite<Actions.Component>()
    }
};

m_UnitToRemoveData = GetEntityQuery(unitsToRemoveDesc);


m_UnitData = GetEntityQuery(
ComponentType.ReadOnly<SpatialEntityId>(),
ComponentType.ReadOnly<CubeCoordinate.Component>(),
ComponentType.ReadWrite<Actions.Component>()
);

m_GameStateData = GetEntityQuery(
ComponentType.ReadOnly<GameState.Component>()
);
*/

        var unitRemovedDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
        {
                ComponentType.ReadOnly<CubeCoordinate.Component>()
        },
            All = new ComponentType[]
        {
                typeof(UnitLifeCycleSystemServer.UnitStateData)
        }
        };

        m_UnitRemovedData = GetEntityQuery(unitRemovedDesc);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
        m_ExecuteSystem = World.GetExistingSystem<ExecuteActionsSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.ForEach((in GameState.Component gameState, in WorldIndexShared WorldIndex) =>
        {
            if (gameState.CurrentState == GameStateEnum.cleanup)
            {
                m_ResourceSystem.ResetArmor(WorldIndex);
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }

    public void ClearAllLockedActions(WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref Actions.Component actions) =>
        {
            actions = ClearLockedActions(actions);
        })
        .WithoutBurst()
        .Run();
    }

    public Actions.Component ClearLockedActions(Actions.Component actions)
    {
        actions.LastSelected = actions.NullAction;
        actions.CurrentSelected = actions.NullAction;
        actions.LockedAction = actions.NullAction;
        actions.Executed = false;
        return actions;
    }

    public void DeleteMap(WorldIndexShared worldIndex)
    {
        //Delete units first to prevent moveUpdate from throwing index out of range
        Entities.WithAll<Actions.Component>().WithSharedComponentFilter(worldIndex).ForEach((in SpatialEntityId entityId) =>
        {
            var deleteEntityRequest = new WorldCommands.DeleteEntity.Request(entityId.EntityId);
            m_CommandSystem.SendCommand(deleteEntityRequest);
        })
        .WithoutBurst()
        .Run();

        Entities.WithNone<GameState.Component, PlayerState.Component>().WithSharedComponentFilter(worldIndex).ForEach((in SpatialEntityId entityId) =>
        {
            var deleteEntityRequest = new WorldCommands.DeleteEntity.Request(entityId.EntityId);
            m_CommandSystem.SendCommand(deleteEntityRequest);
        })
        .WithoutBurst() 
        .Run();
    }

    public void DeleteDeadUnits(WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).WithAll<IsDeadTag>().ForEach((in SpatialEntityId entityId) =>
        {
            var deleteEntityRequest = new WorldCommands.DeleteEntity.Request(entityId.EntityId);
            m_CommandSystem.SendCommand(deleteEntityRequest);
        })
        .WithoutBurst()
        .Run();
    }

    public bool CheckAllDeadUnitsDeleted(WorldIndexShared worldIndex)
    {
        bool allUnitsDeleted = true;

        Entities.WithSharedComponentFilter(worldIndex).WithAll<IsDeadTag>().ForEach((in SpatialEntityId entityId) =>
        {
            allUnitsDeleted = false;
        })
        .WithoutBurst()
        .Run();

        if (m_UnitRemovedData.CalculateEntityCount() > 0)
            allUnitsDeleted = false;

        return allUnitsDeleted;
    }
}
