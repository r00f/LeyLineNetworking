using Unity.Entities;
using Unit;
using Improbable.Gdk.Core;
using Generic;
using Cells;
using LeyLineHybridECS;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem)), UpdateBefore(typeof(PlayerStateSystem)), UpdateBefore(typeof(GameStateSystem)), UpdateBefore(typeof(SpawnUnitsSystem))]
public class ExecuteActionsSystem : ComponentSystem
{
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Health.Component> HealthData;
        public readonly ComponentDataArray<FactionComponent.Component> FactionData;
        public ComponentDataArray<Actions.Component> ActionData;
    }

    [Inject] private UnitData m_UnitData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public ComponentDataArray<UnitToSpawn.Component> UnitToSpawnData;
    }

    [Inject] private CellData m_CellData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameStates;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
    }

    [Inject] private GameStateData m_GameStateData;

    [Inject] private ResourceSystem m_ResourceSystem;


    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var actions = m_UnitData.ActionData[i];
            var unitId = m_UnitData.EntityIds[i].EntityId.Id;
            var faction = m_UnitData.FactionData[i];

            for(int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gamestateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;
                var gameState = m_GameStateData.GameStates[gi].CurrentState;

                if(unitWorldIndex == gamestateWorldIndex)
                {
                    if(actions.LockedAction.Effects.Count != 0 && gameState != GameStateEnum.planning)
                    {
                        //Debug.Log("lockedAction != nullaction");

                        switch (gameState)
                        {
                            case GameStateEnum.spawning:
                                if(actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.spawn_unit)
                                {
                                    SetUnitSpawn(actions.LockedAction.Effects[0].SpawnUnitNested.UnitName, faction, actions.LockedAction.Targets[0].CellTargetNested.TargetId);
                                }
                                break;
                            case GameStateEnum.attacking:
                                if (actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.deal_damage)
                                {
                                    Attack(actions.LockedAction.Effects[0].DealDamageNested.DamageAmount, unitId, actions.LockedAction.Targets[0].UnitTargetNested.TargetId);
                                }
                                break;
                            case GameStateEnum.moving:
                                if (actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                                {

                                }
                                break;
                            case GameStateEnum.calculate_energy:

                                break;
                        }
                    }
                }
            }
        }
    }

    public void ClearAllLockedActions(uint worldIndex)
    {
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var actions = m_UnitData.ActionData[i];

            if(unitWorldIndex == worldIndex)
            {
                //clear locked action
                actions.LockedAction = actions.NullAction;
                m_UnitData.ActionData[i] = actions;
            }
        }
    }

        public void SetUnitSpawn(string unitName, FactionComponent.Component unitFaction, long cellId)
    {
        for(int i= 0; i < m_CellData.Length; i++)
        {
            var id = m_CellData.EntityIds[i].EntityId.Id;
            var unitToSpawn = m_CellData.UnitToSpawnData[i];

            if(cellId == id)
            {
                unitToSpawn.Faction = unitFaction.Faction;
                unitToSpawn.TeamColor = unitFaction.TeamColor;
                unitToSpawn.UnitName = unitName;

                m_CellData.UnitToSpawnData[i] = unitToSpawn;
            }
        }
    }

    public void Attack(uint damage, long attackingUnitId, long targetUnitId)
    {
        Debug.Log("Execute Attack with damage to unit: " + damage + ", " + targetUnitId);
        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var actions = m_UnitData.ActionData[i];
            var unitId = m_UnitData.EntityIds[i].EntityId.Id;
            var faction = m_UnitData.FactionData[i];

            if (unitId == attackingUnitId)
            {
                //set animation triggers / spawn partilce effects / sound fx usw.
            }
            else if (unitId == targetUnitId)
            {
                //trigger getHit animation / sound fx

            }
        }

        m_ResourceSystem.DealDamage(targetUnitId, damage);
    }
}
