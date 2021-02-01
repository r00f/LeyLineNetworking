using Unity.Entities;
using UnityEngine;
using Unit;
using Improbable.Gdk.Core;
using Generic;
using Cell;
using LeyLineHybridECS;
using System.Collections.Generic;
using Unity.Collections;
using Improbable.Gdk.PlayerLifecycle;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
public class ExecuteActionsSystem : ComponentSystem
{
    PathFindingSystem m_PathFindingSystem;
    HandleCellGridRequestsSystem m_HandleCellGridSystem;
    ResourceSystem m_ResourceSystem;
    TimerSystem m_TimerSystem;
    SpawnUnitsSystem m_SpawnSystem;

    EntityQuery m_GameStateData;
    EntityQuery m_CellData;
    EntityQuery m_UnitData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadWrite<CubeCoordinate.Component>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadWrite<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_HandleCellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
        m_SpawnSystem = World.GetExistingSystem<SpawnUnitsSystem>();

        m_UnitData = GetEntityQuery(
        EntityArchetypes.UnitArchetype.GetComponentTypes()
        );
    }

    protected override void OnUpdate()
    {
        if (m_GameStateData.CalculateEntityCount() == 0)
            return;

        var gameStateData = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
        var gameStateWorldIndexData = m_GameStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var gameState = gameStateData[0];
        var gamestateWorldIndex = gameStateWorldIndexData[0];

        GetUnitActions(gameState, gamestateWorldIndex);

        if (!gameState.AttackDamageDealt && gameState.DamageDict.Count != 0)
        { 
            m_ResourceSystem.DealDamage(gameState.DamageDict, (ExecuteStepEnum)(int)gameState.CurrentState - 1);
            gameState.DamageDict.Clear();
            gameState.AttackDamageDealt = true;
        }

        #region Dispose
        gameStateData[0] = gameState;
        m_GameStateData.CopyFromComponentDataArray(gameStateData);
        gameStateWorldIndexData.Dispose();
        gameStateData.Dispose();
        #endregion
    }

    public void GetUnitActions(GameState.Component gameState, WorldIndex.Component gamestateWorldIndex)
    {
        Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref Actions.Component actions, ref SpatialEntityId unitId, ref FactionComponent.Component faction, ref OwningWorker.Component owningWOrker) =>
        {
            var uWi = unitWorldIndex.Value;
            var a = actions;
            var f = faction;
            var id = unitId.EntityId.Id;

            if (a.LockedAction.Effects.Count != 0 && gameState.CurrentState != GameStateEnum.planning)
            {
                switch (gameState.CurrentState)
                {
                    case GameStateEnum.interrupt:
                        if ((int)a.LockedAction.ActionExecuteStep == 0 && !a.Executed)
                        {
                            ExecuteAction(gameState, gamestateWorldIndex.Value, a.LockedAction, f, id, owningWOrker);
                            a.Executed = true;
                        }
                        break;
                    case GameStateEnum.attack:
                        if ((int)a.LockedAction.ActionExecuteStep == 1 && !a.Executed)
                        {
                            ExecuteAction(gameState, gamestateWorldIndex.Value, a.LockedAction, f, id, owningWOrker);
                            a.Executed = true;
                        }
                        break;
                    case GameStateEnum.move:
                        if ((int)a.LockedAction.ActionExecuteStep == 2 && !a.Executed)
                        {
                            ExecuteAction(gameState, gamestateWorldIndex.Value, a.LockedAction, f, id, owningWOrker);
                            a.Executed = true;
                        }
                        break;
                    case GameStateEnum.skillshot:
                        if ((int)a.LockedAction.ActionExecuteStep == 3 && !a.Executed)
                        {
                            ExecuteAction(gameState, gamestateWorldIndex.Value, a.LockedAction, f, id, owningWOrker);
                            a.Executed = true;
                        }
                        break;
                }
            }
            actions = a;
        });
    }

    public void AddDamageToDict(GameState.Component gameState, long unitId, uint damageAmount)
    {
        if (gameState.DamageDict.ContainsKey(unitId))
        {
            gameState.DamageDict[unitId] += damageAmount;
        }
        else
        {
            gameState.DamageDict.Add(unitId, damageAmount);
        }

    }

    public void ExecuteAction(GameState.Component gameState, uint worldIndex, Action action, FactionComponent.Component faction, long unitId, OwningWorker.Component owningWorker)
    {
        for (int j = 0; j < action.Effects.Count; j++)
        {
            switch (action.Effects[j].EffectType)
            {
                case EffectTypeEnum.deal_damage:
                    switch (action.Effects[j].ApplyToTarget)
                    {
                        case ApplyToTargetsEnum.primary:

                            AddDamageToDict(gameState, action.Targets[0].TargetId, action.Effects[j].DealDamageNested.DamageAmount);
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
                                    AddDamageToDict(gameState, id, action.Effects[j].DealDamageNested.DamageAmount);
                                }
                            }
                            break;
                        case ApplyToTargetsEnum.both:
                            AddDamageToDict(gameState, action.Targets[0].TargetId, action.Effects[j].DealDamageNested.DamageAmount);
                            if (action.Targets[0].Mods.Count != 0)
                            {
                                List<Vector3f> coords = new List<Vector3f>();
                                foreach (CoordinatePositionPair p in action.Targets[0].Mods[0].CoordinatePositionPairs)
                                {
                                    coords.Add(p.CubeCoordinate);
                                }

                                foreach (long id in AreaToUnitIDConversion(coords, action.Effects[j].ApplyToRestrictions, unitId, faction.Faction))
                                {
                                    AddDamageToDict(gameState, id, action.Effects[j].DealDamageNested.DamageAmount);
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
                    m_SpawnSystem.SpawnUnit(worldIndex, action.Effects[j].SpawnUnitNested.UnitName, faction.Faction, action.Targets[0].TargetCoordinate, owningWorker.WorkerId);
                    break;
                case EffectTypeEnum.move_along_path:
                    break;
            }
        }
    }

    public List<long> AreaToUnitIDConversion(List<Vector3f> inCoords, ApplyToRestrictionsEnum restricitons, long usingID, uint usingFaction)
    {
        HashSet<Vector3f> Coords = new HashSet<Vector3f>(inCoords);
        List<long> unitIds = new List<long>();

        Entities.With(m_UnitData).ForEach((Entity e, ref CubeCoordinate.Component unitCoord, ref SpatialEntityId unitId) =>
        {
            if (Coords.Contains(unitCoord.CubeCoordinate))
            {
                if (m_PathFindingSystem.ValidateUnitTarget(e, (UnitRequisitesEnum)(int)restricitons, usingID, usingFaction))
                {
                    unitIds.Add(unitId.EntityId.Id);
                }
            }
        });

        return unitIds;
    }

}
