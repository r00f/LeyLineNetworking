using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;
using LeyLineHybridECS;
using Unity.Jobs;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
public class CleanupSystem : JobComponentSystem
{
    CommandSystem m_CommandSystem;
    ResourceSystem m_ResourceSystem;
    ExecuteActionsSystem m_ExecuteSystem;
    TimerSystem m_TimerSystem;
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
ComponentType.ReadOnly<Health.Component>(),
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
                typeof(UnitLifeCycleSystem.UnitStateData)
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
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.ForEach((in GameState.Component gameState, in WorldIndexShared WorldIndex) =>
        {
            if (gameState.CurrentState == GameStateEnum.cleanup)
            {
                m_TimerSystem.SubstractTurnDurations(WorldIndex.Value);

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

    public void DeleteAllUnits(WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).WithAll<Health.Component>().ForEach((in SpatialEntityId entityId) =>
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
