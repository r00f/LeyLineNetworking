using UnityEngine;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;
using Generic;
using Cell;
using Unit;
using Unity.Collections;
using Player;
using Unity.Jobs;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class HandleCellGridRequestsSystem : JobComponentSystem
{
    PathFindingSystem m_PathFindingSystem;
    CommandSystem m_CommandSystem;
    TimerSystem m_TimerSystem;
    ResourceSystem m_ResourceSystem;

    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;

    protected override void OnCreate()
    {
        base.OnCreate();
        /*
        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>()
        );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );
            */
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        #region select action

        var selectActionRequests = m_CommandSystem.GetRequests<Actions.SelectActionCommand.ReceivedRequest>();

        for (int i = 0; i < selectActionRequests.Count; i++)
        {
            var selectActionRequest = selectActionRequests[i];

            Entities.ForEach((Entity e, ref Actions.Component unitActions, ref CellsToMark.Component unitCellsToMark, in FactionComponent.Component unitFaction,  in WorldIndexShared unitWorldIndex, in SpatialEntityId unitEntityId, in CubeCoordinate.Component unitCoord) =>
            {
                //if this unit has sent a selectActionCommand
                if (unitEntityId.EntityId.Id == selectActionRequest.EntityId.Id)
                {
                    unitCellsToMark.SetClientRange = false;

                    m_ResourceSystem.AddEnergy(unitWorldIndex, unitFaction.Faction, unitActions.LockedAction.CombinedCost);

                    unitActions.LockedAction = unitActions.NullAction;

                    int index = selectActionRequest.Payload.ActionId;
                    Action actionToSelect = unitActions.NullAction;

                    if (m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction) > 0)
                    {
                        if (index >= 0)
                        {
                            var a = unitActions.ActionsList[index];
                            a.CombinedCost = CalculateCombinedCost(unitActions.ActionsList[index].Targets[0]);
                            unitActions.ActionsList[index] = a;

                            if (m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction, unitActions.ActionsList[index].CombinedCost) >= 0)
                            {
                                actionToSelect = unitActions.ActionsList[index];
                            }
                        }
                    }

                    unitActions.CurrentSelected = actionToSelect;

                    if (unitActions.CurrentSelected.Targets.Count != 0)
                    {
                        bool self = false;
                        if (unitActions.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                        {
                            if (unitActions.CurrentSelected.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                            {
                                var pos = EntityManager.GetComponentData<Position.Component>(e);
                                //Set target instantly
                                self = true;
                                unitActions.LockedAction = SetLockedAction(unitWorldIndex, unitEntityId.EntityId.Id, unitActions.CurrentSelected, unitCoord.CubeCoordinate, unitCoord.CubeCoordinate, unitFaction.Faction, unitCellsToMark, pos);
                                unitActions.CurrentSelected = unitActions.NullAction;
                            }
                        }
                        if ((!unitActions.CurrentSelected.Equals(unitActions.LastSelected) || unitCellsToMark.CellsInRange.Count == 0) && !self)
                        {
                            unitCellsToMark.CellsInRange = m_PathFindingSystem.GetRadius(unitCoord.CubeCoordinate, (uint)unitActions.CurrentSelected.Targets[0].Targettingrange, unitWorldIndex);
                            unitCellsToMark.CachedPaths.Clear();

                            switch (unitActions.CurrentSelected.Targets[0].Higlighter)
                            {
                                case UseHighlighterEnum.pathing:
                                    uint range = (uint)unitActions.CurrentSelected.Targets[0].Targettingrange;
                                    if (unitActions.CurrentSelected.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                                    {
                                        if (m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction, 0) < (uint)unitActions.CurrentSelected.Targets[0].Targettingrange)
                                        {
                                            range = (uint)m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction, 0);
                                        }
                                    }
                                    unitCellsToMark.CachedPaths = m_PathFindingSystem.GetAllPathsInRadius(range, unitCellsToMark.CellsInRange, unitCellsToMark.CellsInRange[0].Cell);
                                    break;
                                case UseHighlighterEnum.no_pathing:
                                    break;
                            }
                        }
                    }

                    unitActions.LastSelected = unitActions.CurrentSelected;
                    unitCellsToMark.SetClientRange = true;
                    unitCellsToMark.CachedPaths = unitCellsToMark.CachedPaths;
                }
            })
            .WithoutBurst()
            .Run();
        }

        #endregion

        #region set target

        //VALIDATE BEHAVIOUR NEEDS TO BE MOVED TO PATHFINDING SYSTEM SO WE CAN USE IT ON CLIENT ASWELL
        var setTargetRequests = m_CommandSystem.GetRequests<Actions.SetTargetCommand.ReceivedRequest>();

        for (int i = 0; i < setTargetRequests.Count; i++)
        {
            var setTargetRequest = setTargetRequests[i];

            Entities.ForEach((ref Actions.Component requestingUnitActions, in CellsToMark.Component requestingUnitCellsToMark, in CubeCoordinate.Component requestingUnitCoord, in FactionComponent.Component requestingUnitFaction, in SpatialEntityId requestingUnitId, in WorldIndexShared worldIndex) =>
            {
                if (requestingUnitId.EntityId.Id == setTargetRequest.EntityId.Id)
                {
                    var targetCoord = setTargetRequest.Payload.TargetCoordinate;

                    if (requestingUnitActions.CurrentSelected.Index != -3 && requestingUnitActions.LockedAction.Index == -3)
                    {
                        switch (requestingUnitActions.CurrentSelected.Targets[0].TargetType)
                        {
                            case TargetTypeEnum.cell:
                                requestingUnitActions = ValidateCellTarget(worldIndex, requestingUnitActions, targetCoord, requestingUnitCoord.CubeCoordinate, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction, requestingUnitCellsToMark);
                                break;
                            case TargetTypeEnum.unit:
                                bool valid = m_PathFindingSystem.ValidateTarget(worldIndex, requestingUnitCoord.CubeCoordinate, targetCoord, requestingUnitActions.CurrentSelected, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction, requestingUnitCellsToMark.CachedPaths);

                                if (valid)
                                {
                                    requestingUnitActions.LockedAction = SetLockedAction(worldIndex, requestingUnitId.EntityId.Id, requestingUnitActions.CurrentSelected, requestingUnitCoord.CubeCoordinate, targetCoord, requestingUnitFaction.Faction, requestingUnitCellsToMark);
                                }
                                else
                                {
                                    requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                                }
                                break;
                        }
                        requestingUnitActions.CurrentSelected = requestingUnitActions.NullAction;
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }
        #endregion

        return inputDeps;
    }

    public Actions.Component ValidateCellTarget(WorldIndexShared worldIndex, Actions.Component requestingUnitActions, Vector3f targetCoord, Vector3f requestingUnitCoord, long requestingUnitId, uint requestingUnitFaction, CellsToMark.Component requestingUnitCellsToMark)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((in SpatialEntityId cellId, in CellAttributesComponent.Component cellAtts, in CubeCoordinate.Component cCoord, in Position.Component position) =>
        {
            if (Vector3fext.ToUnityVector(targetCoord) == Vector3fext.ToUnityVector(cCoord.CubeCoordinate))
            {
                bool valid = m_PathFindingSystem.ValidateTargetWithCellAtt(worldIndex, requestingUnitCoord, targetCoord, requestingUnitActions.CurrentSelected, requestingUnitId, requestingUnitFaction, requestingUnitCellsToMark.CachedPaths, cellAtts.CellAttributes.Cell);

                if (valid)
                {
                    requestingUnitActions.LockedAction = SetLockedAction(worldIndex, requestingUnitId, requestingUnitActions.CurrentSelected, requestingUnitCoord, targetCoord, requestingUnitFaction, requestingUnitCellsToMark, position);
                }
                else
                {
                    requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                }
            }
        })
        .WithoutBurst()
        .Run();

        return requestingUnitActions;
    }

    public Actions.Component ValidateUnitTarget(Actions.Component requestingUnitActions, Vector3f targetCoord, Vector3f requestingUnitCoord, long requestingUnitId, uint requestingUnitFaction, CellsToMark.Component requestingUnitCellsToMark, WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((Entity e, in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord) =>
        {
            if (Vector3fext.ToUnityVector(targetCoord) == Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate))
            {
                bool valid = m_PathFindingSystem.ValidateTarget(worldIndex, requestingUnitCoord, targetCoord, requestingUnitActions.CurrentSelected, requestingUnitId, requestingUnitFaction, requestingUnitCellsToMark.CachedPaths);

                if (valid)
                {
                    requestingUnitActions.LockedAction = SetLockedAction(worldIndex, requestingUnitId, requestingUnitActions.CurrentSelected, requestingUnitCoord, targetCoord, requestingUnitFaction, requestingUnitCellsToMark);
                }
                else
                {
                    requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                }
            }
        })
        .WithoutBurst()
        .Run();
        return requestingUnitActions;
    }

    public uint CalculateCombinedCost(ActionTarget inActionTarget)
    {
        uint combinedCost = 0;

        combinedCost += inActionTarget.EnergyCost;

        if (inActionTarget.Mods.Count != 0)
        {
            if(inActionTarget.Mods[0].ModType == ModTypeEnum.path)
            {
                combinedCost += (uint)inActionTarget.Mods[0].CoordinatePositionPairs.Count;
            }
        }

        return combinedCost;
    }

    public Action SetLockedAction(WorldIndexShared worldIndex, long usingUnitId, Action selectedAction, Vector3f originCoord, Vector3f targetCoord, uint faction, CellsToMark.Component cellsToMark, Position.Component position = default)
    {
        Action locked = selectedAction;
        var t = locked.Targets[0];
        t.TargetCoordinate = targetCoord;
        locked.Targets[0] = t;
        var effect = locked.Effects[0];

        /*
        effect.OriginUnitFaction = faction;
        effect.OriginUnitId = usingUnitId;
        if (effect.ApplyToRestrictions == ApplyToRestrictionsEnum.self)
            effect.TargetCoordinates.Add(originCoord);
        effect.TargetCoordinates.Add(targetCoord);
        locked.Effects[0] = effect;
        */

        for (int i = 0; i < locked.Effects.Count; i++)
        {
            effect = locked.Effects[i];
            effect.OriginUnitFaction = faction;
            effect.OriginUnitId = usingUnitId;
            if (effect.ApplyToRestrictions == ApplyToRestrictionsEnum.self)
                effect.TargetCoordinates.Add(originCoord);
            else
                effect.TargetCoordinates.Add(targetCoord);

            effect.TargetPosition = new Vector3f((float)position.Coords.X, (float)position.Coords.Y, (float)position.Coords.Z);

            locked.Effects[i] = effect;
        }
        
        effect = locked.Effects[0];
        
        for (int mi = 0; mi < locked.Targets[0].Mods.Count; mi++)
        {
            var modType = locked.Targets[0].Mods[mi].ModType;
            var mod = locked.Targets[0].Mods[0];

            switch (modType)
            {
                case ModTypeEnum.aoe:
                    foreach (Vector3f v in CellGridMethods.CircleDraw(t.TargetCoordinate, (uint)mod.AoeNested.Radius))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                        effect.TargetCoordinates.Add(v);
                    }
                    break;
                case ModTypeEnum.path:
                    foreach (CellAttribute c in m_PathFindingSystem.FindPath(targetCoord, cellsToMark.CachedPaths).CellAttributes)
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(c.CubeCoordinate, c.Position));
                        effect.MoveAlongPathNested.CoordinatePositionPairs.Add(new CoordinatePositionPair(c.CubeCoordinate, c.Position));
                        //effect.TargetCoordinates.Add(c.CubeCoordinate);
                    }
                    mod.PathNested.OriginCoordinate = originCoord;
                    locked.Targets[0].Mods[0] = mod;
                    locked.CombinedCost = CalculateCombinedCost(t);
                    break;
                case ModTypeEnum.line:
                    foreach (Vector3f v in CellGridMethods.LineDraw(new List<Vector3f>(), originCoord, t.TargetCoordinate))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                        effect.TargetCoordinates.Add(v);
                    }
                    break;
                case ModTypeEnum.ring:
                    foreach (Vector3f v in CellGridMethods.RingDraw(t.TargetCoordinate, mod.RingNested.Radius))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                        effect.TargetCoordinates.Add(v);
                    }
                    break;
                case ModTypeEnum.cone:
                    foreach (Vector3f v in CellGridMethods.ConeDraw(originCoord, t.TargetCoordinate, mod.ConeNested.Radius, mod.ConeNested.Extent))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                        effect.TargetCoordinates.Add(v);
                    }
                    break;
            }
        }
        m_ResourceSystem.SubstactEnergy(worldIndex, faction, locked.CombinedCost);

        return locked;
    }
}
