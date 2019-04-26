﻿using Unity.Entities;
using Unit;
using Improbable.Gdk.Core;
using Generic;
using Cells;
using LeyLineHybridECS;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
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
                    if(!actions.LockedAction.Equals(actions.NullAction) && gameState != GameStateEnum.planning)
                    {
                        switch (gameState)
                        {
                            case GameStateEnum.spawning:
                                if(actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.spawn_unit)
                                {
                                    SetUnitSpawn(actions.LockedAction.Effects[0].SpawnUnitNested.UnitName, faction, actions.LockedAction.Targets[0].CellTargetNested.TargetId);
                                    actions.LockedAction = actions.NullAction;
                                    m_UnitData.ActionData[i] = actions;
                                }
                                break;
                            case GameStateEnum.attacking:




                                break;
                            case GameStateEnum.moving:




                                break;
                        }
                    }
                }
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
}
