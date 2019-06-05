using Unity.Entities;
using Unit;
using Improbable.Gdk.Core;
using Generic;
using Cell;
using LeyLineHybridECS;
using UnityEngine;
using System.Collections.Generic;
using Improbable;

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
                                    SetUnitSpawn(actions.LockedAction.Effects[0].SpawnUnitNested.UnitName, faction, actions.LockedAction.Targets[0].TargetId);
                                }
                                break;
                            case GameStateEnum.defending:
                                //add generic defense action effect type
                                if (actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.gain_armor)
                                {
                                    m_TimerSystem.AddTimedEffect(actions.LockedAction.Targets[0].TargetId, actions.LockedAction.Effects[0]);
                                }
                                break;
                            case GameStateEnum.attacking:
                                for (int j = 0; j < actions.LockedAction.Effects.Count; j++)
                                {
                                    if (actions.LockedAction.Effects[j].EffectType == EffectTypeEnum.deal_damage)
                                    {
                                        switch (actions.LockedAction.Effects[j].ApplyToTarget)
                                        {
                                            case ApplyToTargetsEnum.primary:
                                                
                                                //Attack(actions.LockedAction.Effects[0].DealDamageNested.DamageAmount, unitId, actions.LockedAction.Targets[0].TargetId);
                                                m_ResourceSystem.DealDamage(actions.LockedAction.Targets[0].TargetId, actions.LockedAction.Effects[j].DealDamageNested.DamageAmount);
                                                break;
                                            case ApplyToTargetsEnum.secondary:
                                                
                                                if (actions.LockedAction.Targets[0].Mods.Count != 0)
                                                {
                                                    Debug.Log("Ayaya");
                                                    foreach (long id in AreaToUnitIDConversion(actions.LockedAction.Targets[0].Mods[0].Coordinates, actions.LockedAction.Effects[j].ApplyToRestrictions, unitId,faction.Faction))
                                                    {
                                                        Debug.Log(id);
                                                        m_ResourceSystem.DealDamage(id, actions.LockedAction.Effects[j].DealDamageNested.DamageAmount);
                                                    }
                                                }
                                                break;
                                            case ApplyToTargetsEnum.both:
                                                m_ResourceSystem.DealDamage(actions.LockedAction.Targets[0].TargetId, actions.LockedAction.Effects[j].DealDamageNested.DamageAmount);
                                                if (actions.LockedAction.Targets[0].Mods.Count != 0)
                                                {
                                                    Debug.Log("Ayaya");
                                                    foreach (long id in AreaToUnitIDConversion(actions.LockedAction.Targets[0].Mods[0].Coordinates, actions.LockedAction.Effects[j].ApplyToRestrictions, unitId, faction.Faction))
                                                    {
                                                        m_ResourceSystem.DealDamage(id, actions.LockedAction.Effects[j].DealDamageNested.DamageAmount);
                                                    }
                                                }
                                                break;
                                        }

                                    }
                                }
                                break;
                            case GameStateEnum.moving:
                                if (actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                                {

                                }
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
                //Debug.Log("ClearAllLockedActions");
                //clear locked action
                actions.LastSelected = actions.NullAction;
                actions.CurrentSelected = actions.NullAction;
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

    public List<long> AreaToUnitIDConversion(List<Vector3f> inCoords, ApplyToRestrictionsEnum restricitons, long usingID, uint usingFaction)
    {
        Debug.Log("Restriction: " + restricitons + "ID: " + usingID + "Faction: " + usingFaction);
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
