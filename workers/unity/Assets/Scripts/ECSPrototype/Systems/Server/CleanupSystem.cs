using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;
using LeyLineHybridECS;
using Unity.Collections;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
public class CleanupSystem : ComponentSystem
{
    CommandSystem m_CommandSystem;
    ResourceSystem m_ResourceSystem;
    ExecuteActionsSystem m_ExecuteSystem;
    TimerSystem m_TimerSystem;

    EntityQuery m_GameStateData;
    EntityQuery m_UnitData;
    EntityQuery m_UnitRemovedData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadWrite<Actions.Component>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<GameState.Component>()
        );

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
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
        m_ExecuteSystem = World.GetExistingSystem<ExecuteActionsSystem>();
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
    }

    protected override void OnUpdate()
    {
        Entities.With(m_GameStateData).ForEach((ref GameState.Component gameState, ref WorldIndex.Component WorldIndex) =>
        {
            if (gameState.CurrentState == GameStateEnum.cleanup)
            {
                m_TimerSystem.SubstractTurnDurations(WorldIndex.Value);
                ClearAllLockedActions(WorldIndex.Value);
                m_ResourceSystem.ResetArmor(WorldIndex.Value);
            }
        });
    }

    public void ClearAllLockedActions(uint worldIndex)
    {
        Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref Actions.Component actions) =>
        {
            if (unitWorldIndex.Value == worldIndex)
            {
                actions = ClearLockedActions(actions);
            }
        });
    }

    public Actions.Component ClearLockedActions(Actions.Component actions)
    {
        actions.LastSelected = actions.NullAction;
        actions.CurrentSelected = actions.NullAction;
        actions.LockedAction = actions.NullAction;
        actions.Executed = false;
        return actions;
    }

    public void DeleteDeadUnits(uint worldIndex)
    {
        Entities.With(m_UnitData).ForEach((ref SpatialEntityId entityId, ref WorldIndex.Component unitWorldIndex, ref Health.Component health) =>
        {
            if (unitWorldIndex.Value == worldIndex && health.CurrentHealth == 0)
            {
                var deleteEntityRequest = new WorldCommands.DeleteEntity.Request(entityId.EntityId);
                m_CommandSystem.SendCommand(deleteEntityRequest);
            }
        });
    }

    public bool CheckAllDeadUnitsDeleted(uint worldIndex)
    {
        bool allUnitsDeleted = true;

        Entities.With(m_UnitData).ForEach((ref SpatialEntityId entityId, ref WorldIndex.Component unitWorldIndex, ref Health.Component health) =>
        {
            if (unitWorldIndex.Value == worldIndex && health.CurrentHealth == 0)
            {
                allUnitsDeleted = false;
            }
        });

        if (m_UnitRemovedData.CalculateEntityCount() > 0)
            allUnitsDeleted = false;

        return allUnitsDeleted;
    }
}
