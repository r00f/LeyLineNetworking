using Unity.Entities;
using Unit;
using Improbable.Gdk.Core;
using Generic;
using Cell;
using LeyLineHybridECS;
using UnityEngine;
using System.Collections.Generic;
using Improbable;
using System.Linq;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
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

    [Inject] UnitData m_UnitData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public ComponentDataArray<UnitToSpawn.Component> UnitToSpawnData;
    }

    [Inject] CellData m_CellData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameStates;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
    }

    [Inject] GameStateData m_GameStateData;

    [Inject] HandleCellGridRequestsSystem m_HandleCellGridSystem;

    [Inject] ResourceSystem m_ResourceSystem;

    [Inject] TimerSystem m_TimerSystem;

    [Inject] SpawnUnitsSystem m_SpawnSystem;


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
                        switch (gameState)
                        {
                            case GameStateEnum.interrupt:
                                if((int)actions.LockedAction.ActionExecuteStep == 0 && !actions.Executed)
                                {
                                    ExecuteAction(gamestateWorldIndex, actions.LockedAction, faction, unitId);
                                    actions.Executed = true;
                                    m_UnitData.ActionData[i] = actions;
                                }
                                break;
                            case GameStateEnum.attack:
                                if ((int)actions.LockedAction.ActionExecuteStep == 1 && !actions.Executed)
                                {
                                    ExecuteAction(gamestateWorldIndex, actions.LockedAction, faction, unitId);
                                    actions.Executed = true;
                                    m_UnitData.ActionData[i] = actions;
                                }
                                break;
                            case GameStateEnum.move:
                                if ((int)actions.LockedAction.ActionExecuteStep == 2 && !actions.Executed)
                                {
                                    ExecuteAction(gamestateWorldIndex, actions.LockedAction, faction, unitId);
                                    //actions.LockedAction = actions.NullAction;
                                    //m_UnitData.ActionData[i] = actions;
                                }
                                break;
                            case GameStateEnum.skillshot:
                                if ((int)actions.LockedAction.ActionExecuteStep == 3 && !actions.Executed)
                                {
                                    ExecuteAction(gamestateWorldIndex, actions.LockedAction, faction, unitId);
                                    actions.Executed = true;
                                    m_UnitData.ActionData[i] = actions;
                                }
                                break;
                            case GameStateEnum.cleanup:
                                if ((int)actions.LockedAction.ActionExecuteStep == 4 && !actions.Executed)
                                {
                                    ExecuteAction(gamestateWorldIndex, actions.LockedAction, faction, unitId);
                                    actions.Executed = true;
                                    m_UnitData.ActionData[i] = actions;
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    public void ExecuteAction(uint worldIndex, Action action, FactionComponent.Component faction, long unitId)
    {
        for (int j = 0; j < action.Effects.Count; j++)
        {
            switch (action.Effects[j].EffectType)
            {
                case EffectTypeEnum.deal_damage:
                    switch (action.Effects[j].ApplyToTarget)
                    {
                        case ApplyToTargetsEnum.primary:

                            //Attack(actions.LockedAction.Effects[0].DealDamageNested.DamageAmount, unitId, actions.LockedAction.Targets[0].TargetId);
                            m_ResourceSystem.DealDamage(action.Targets[0].TargetId, action.Effects[j].DealDamageNested.DamageAmount, action.ActionExecuteStep);
                            break;
                        case ApplyToTargetsEnum.secondary:

                            if (action.Targets[0].Mods.Count != 0)
                            {
                                List<Vector3f> coords = new List<Vector3f>();
                                foreach(CoordinatePositionPair p in action.Targets[0].Mods[0].CoordinatePositionPairs)
                                {
                                    coords.Add(p.CubeCoordinate);
                                }

                                foreach (long id in AreaToUnitIDConversion(coords, action.Effects[j].ApplyToRestrictions, unitId, faction.Faction))
                                {
                                    m_ResourceSystem.DealDamage(id, action.Effects[j].DealDamageNested.DamageAmount, action.ActionExecuteStep);
                                }
                            }
                            break;
                        case ApplyToTargetsEnum.both:
                            m_ResourceSystem.DealDamage(action.Targets[0].TargetId, action.Effects[j].DealDamageNested.DamageAmount, action.ActionExecuteStep);
                            if (action.Targets[0].Mods.Count != 0)
                            {
                                List<Vector3f> coords = new List<Vector3f>();
                                foreach (CoordinatePositionPair p in action.Targets[0].Mods[0].CoordinatePositionPairs)
                                {
                                    coords.Add(p.CubeCoordinate);
                                }

                                foreach (long id in AreaToUnitIDConversion(coords, action.Effects[j].ApplyToRestrictions, unitId, faction.Faction))
                                {
                                    m_ResourceSystem.DealDamage(id, action.Effects[j].DealDamageNested.DamageAmount, action.ActionExecuteStep);
                                }
                            }
                            break;
                    }
                    break;
                case EffectTypeEnum.gain_armor:
                    m_TimerSystem.AddTimedEffect(action.Targets[0].TargetId, action.Effects[0]);
                    break;
                case EffectTypeEnum.spawn_unit:
                    //SetUnitSpawn(action.Effects[j].SpawnUnitNested.UnitName, faction, action.Targets[0].TargetCoordinate);
                    m_SpawnSystem.SpawnUnit(worldIndex, action.Effects[j].SpawnUnitNested.UnitName, faction.Faction, action.Targets[0].TargetCoordinate);
                    break;
                case EffectTypeEnum.move_along_path:
                    break;
            }
        }
    }
    /*
    public void SetUnitSpawn(string unitName, FactionComponent.Component unitFaction, Vector3f cubeCoord)
    {
        for (int i = 0; i < m_CellData.Length; i++)
        {
            var coord = m_CellData.CoordinateData[i].CubeCoordinate;
            var unitToSpawn = m_CellData.UnitToSpawnData[i];

            if (coord == cubeCoord)
            {
                //Debug.Log("SetUnitSpawn at coordinate: " + cubeCoord);
                unitToSpawn.Faction = unitFaction.Faction;
                unitToSpawn.TeamColor = unitFaction.TeamColor;
                unitToSpawn.UnitName = unitName;
                m_CellData.UnitToSpawnData[i] = unitToSpawn;
            }
        }
    }
    */

    public List<long> AreaToUnitIDConversion(List<Vector3f> inCoords, ApplyToRestrictionsEnum restricitons, long usingID, uint usingFaction)
    {
        HashSet<Vector3f> Coords = new HashSet<Vector3f>(inCoords);
        List<long> unitIds = new List<long>();
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var unitCoord = m_UnitData.CoordinateData[i].CubeCoordinate;
            var unitID = m_UnitData.EntityIds[i].EntityId.Id;
            if (Coords.Contains(unitCoord))
            {
                if(m_HandleCellGridSystem.ValidateUnitTarget(unitID, usingID, usingFaction, (UnitRequisitesEnum)(int)restricitons))
                {
                    unitIds.Add(unitID);
                }
            }
        }
        return unitIds;
    }

}
