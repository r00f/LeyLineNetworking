using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;
using LeyLineHybridECS;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
public class CleanupSystem : ComponentSystem
{
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Health.Component> HealthData;
        public ComponentDataArray<Actions.Component> ActionData;
        public ComponentDataArray<WorldCommands.DeleteEntity.CommandSender> DeleteEntitySenders;
    }

    [Inject] UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameStates;
    }

    [Inject] GameStateData m_GameStateData;

    [Inject] ExecuteActionsSystem m_ExecuteSystem;

    [Inject] TimerSystem m_TimerSystem;

    [Inject] ResourceSystem m_ResourceSystem;

    protected override void OnUpdate()
    {
        for (var i = 0; i < m_GameStateData.Length; i++)
        {
            var worldIndex = m_GameStateData.WorldIndexData[i].Value;
            var gameState = m_GameStateData.GameStates[i].CurrentState;

            if (gameState == GameStateEnum.cleanup)
            {
                m_TimerSystem.SubstractTurnDurations(worldIndex);
                ClearAllLockedActions(worldIndex);
                m_ResourceSystem.ResetArmor(worldIndex);
            }
        }
    }


    public void ClearAllLockedActions(uint worldIndex)
    {
        UpdateInjectedComponentGroups();
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var actions = m_UnitData.ActionData[i];

            if (unitWorldIndex == worldIndex)
            {
                m_UnitData.ActionData[i] = ClearLockedActions(actions);
            }
        }
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
        UpdateInjectedComponentGroups();
        for (var i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var requestSender = m_UnitData.DeleteEntitySenders[i];
            var currentHealth = m_UnitData.HealthData[i].CurrentHealth;
            var entityId = m_UnitData.EntityIds[i].EntityId;

            if(unitWorldIndex == worldIndex && currentHealth == 0)
            {
                requestSender.RequestsToSend.Add(new WorldCommands.DeleteEntity.Request
                (
                    entityId
                ));
                m_UnitData.DeleteEntitySenders[i] = requestSender;
            }
            
        }
    }

}
