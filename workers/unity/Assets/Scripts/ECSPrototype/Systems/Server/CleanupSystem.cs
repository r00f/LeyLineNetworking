using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(ExecuteActionsSystem))]
public class CleanupSystem : ComponentSystem
{
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Health.Component> HealthData;
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
                DeleteDeadUnits(worldIndex);
                m_TimerSystem.SubstractTurnDurations(worldIndex);
                m_ExecuteSystem.ClearAllLockedActions(worldIndex);
                m_ResourceSystem.ResetArmor(worldIndex);
            }
        }
    }

    public void DeleteDeadUnits(uint worldIndex)
    {
        for (var i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var requestSender = m_UnitData.DeleteEntitySenders[i];
            var currentHealth = m_UnitData.HealthData[i].CurrentHealth;
            var entityId = m_UnitData.EntityIds[i].EntityId;

            
            if(unitWorldIndex == worldIndex && currentHealth == 0)
            {
                Debug.Log("Delete Unit with id: " + entityId.Id);

                requestSender.RequestsToSend.Add(new WorldCommands.DeleteEntity.Request
                (
                    entityId
                ));
                m_UnitData.DeleteEntitySenders[i] = requestSender;
            }
            
        }
    }
}
