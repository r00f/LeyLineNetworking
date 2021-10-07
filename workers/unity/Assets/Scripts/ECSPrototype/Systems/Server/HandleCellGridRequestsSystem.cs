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

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class HandleCellGridRequestsSystem : ComponentSystem
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

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<GameState.Component>()
        );

        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            //ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<Actions.Component>(),
            ComponentType.ReadWrite<CellsToMark.Component>()
            );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
    }

    protected override void OnUpdate()
    {
        #region select action

        var selectActionRequests = m_CommandSystem.GetRequests<Actions.SelectActionCommand.ReceivedRequest>();

        for (int i = 0; i < selectActionRequests.Count; i++)
        {
            var selectActionRequest = selectActionRequests[i];

            Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitEntityId, ref WorldIndex.Component unitWorldIndex, ref Actions.Component unitActions, ref CubeCoordinate.Component unitCoord, ref FactionComponent.Component unitFaction, ref CellsToMark.Component unitCellsToMark) =>
            {
                //if this unit has sent a selectActionCommand
                if (unitEntityId.EntityId.Id == selectActionRequest.EntityId.Id)
                {
                    unitCellsToMark.SetClientRange = false;

                    m_ResourceSystem.AddEnergy(unitFaction.Faction, unitActions.LockedAction.CombinedCost);

                    if (unitActions.LockedAction.Effects.Count != 0)
                    {
                        if (unitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.gain_armor)
                        {
                            m_ResourceSystem.RemoveArmor(unitActions.LockedAction.Targets[0].TargetId, unitActions.LockedAction.Effects[0].GainArmorNested.ArmorAmount);
                        }
                    }

                    unitActions.LockedAction = unitActions.NullAction;

                    int index = selectActionRequest.Payload.ActionId;
                    Action actionToSelect = unitActions.NullAction;

                    if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction) > 0)
                    {
                        if (index >= 0)
                        {
                            var a = unitActions.ActionsList[index];
                            a.CombinedCost = CalculateCombinedCost(unitActions.ActionsList[index].Targets[0]);
                            unitActions.ActionsList[index] = a;

                            if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.ActionsList[index].CombinedCost) >= 0)
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
                                //Set target instantly
                                self = true;
                                unitActions.LockedAction = SetLockedAction(unitActions.CurrentSelected, unitCoord.CubeCoordinate, unitCoord.CubeCoordinate,  unitFaction.Faction, unitCellsToMark, unitEntityId.EntityId.Id);
                                unitActions.CurrentSelected = unitActions.NullAction;
                            }
                        }
                        if ((!unitActions.CurrentSelected.Equals(unitActions.LastSelected) || unitCellsToMark.CellsInRange.Count == 0) && !self)
                        {
                            unitCellsToMark.CellsInRange = m_PathFindingSystem.GetRadius(unitCoord.CubeCoordinate, (uint)unitActions.CurrentSelected.Targets[0].Targettingrange, unitWorldIndex.Value);
                            unitCellsToMark.CachedPaths.Clear();


                            switch (unitActions.CurrentSelected.Targets[0].Higlighter)
                            {
                                case UseHighlighterEnum.pathing:
                                    uint range = (uint)unitActions.CurrentSelected.Targets[0].Targettingrange;
                                    if (unitActions.CurrentSelected.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                                    {
                                        if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, 0) < (uint)unitActions.CurrentSelected.Targets[0].Targettingrange)
                                        {
                                            range = (uint)m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, 0);
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
            });
        }

        #endregion

        #region set target

        //VALIDATE BEHAVIOUR NEEDS TO BE MOVED TO PATHFINDING SYSTEM SO WE CAN USE IT ON CLIENT ASWELL
        var setTargetRequests = m_CommandSystem.GetRequests<Actions.SetTargetCommand.ReceivedRequest>();

        for (int i = 0; i < setTargetRequests.Count; i++)
        {
            var setTargetRequest = setTargetRequests[i];

            Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitEntityId, ref WorldIndex.Component unitWorldIndex, ref Actions.Component unitActions, ref CubeCoordinate.Component unitCoord, ref FactionComponent.Component unitFaction, ref CellsToMark.Component unitCellsToMark) =>
            {
                var requestingUnitId = unitEntityId;
                var requestingUnitActions = unitActions;
                var requestingUnitCoord = unitCoord;
                var requestingUnitFaction = unitFaction;
                var requestingUnitCellsToMark = unitCellsToMark;

                if (unitEntityId.EntityId.Id == setTargetRequest.EntityId.Id)
                {
                    var coord = setTargetRequest.Payload.TargetCoordinate;

                    if (unitActions.CurrentSelected.Index != -3 && unitActions.LockedAction.Index == -3)
                    {
                        switch (unitActions.CurrentSelected.Targets[0].TargetType)
                        {
                            case TargetTypeEnum.cell:
                                Entities.With(m_CellData).ForEach((ref SpatialEntityId cellId, ref CellAttributesComponent.Component cellAtts, ref CubeCoordinate.Component cCoord) =>
                                {
                                    var cell = cellAtts.CellAttributes.Cell;

                                    if (Vector3fext.ToUnityVector(coord) == Vector3fext.ToUnityVector(cCoord.CubeCoordinate))
                                    {
                                        bool valid = m_PathFindingSystem.ValidateTarget(requestingUnitCoord.CubeCoordinate, cell.CubeCoordinate, requestingUnitActions.CurrentSelected, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction, requestingUnitCellsToMark.CachedPaths, cellAtts.CellAttributes.Cell);

                                        if (valid)
                                        {
                                            requestingUnitActions.LockedAction = SetLockedAction(requestingUnitActions.CurrentSelected, requestingUnitCoord.CubeCoordinate, cell.CubeCoordinate, requestingUnitFaction.Faction, requestingUnitCellsToMark);
                                        }
                                        else
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                                        }
                                    }
                                });
                                break;
                            case TargetTypeEnum.unit:
                                Entities.With(m_UnitData).ForEach((Entity e, ref SpatialEntityId targetUnitId, ref CubeCoordinate.Component targetUnitCoord) =>
                                {
                                    if (Vector3fext.ToUnityVector(coord) == Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate))
                                    {
                                        bool valid = m_PathFindingSystem.ValidateTarget(requestingUnitCoord.CubeCoordinate, targetUnitCoord.CubeCoordinate, requestingUnitActions.CurrentSelected, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction, requestingUnitCellsToMark.CachedPaths);

                                        if (valid)
                                        {
                                            requestingUnitActions.LockedAction = SetLockedAction(requestingUnitActions.CurrentSelected, requestingUnitCoord.CubeCoordinate, targetUnitCoord.CubeCoordinate, requestingUnitFaction.Faction, requestingUnitCellsToMark, targetUnitId.EntityId.Id);
                                        }
                                        else
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                                        }
                                    }
                                });
                                break;
                        }

                        requestingUnitActions.CurrentSelected = requestingUnitActions.NullAction;
                        unitActions = requestingUnitActions;
                    }
                }
            });

        }
        #endregion
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

    public Action SetLockedAction(Action selectedAction, Vector3f originCoord, Vector3f targetCoord, uint faction, CellsToMark.Component cellsToMark, long unitId = 0)
    {
        Action locked = selectedAction;
        var t = locked.Targets[0];
        t.TargetCoordinate = targetCoord;
        t.TargetId = unitId;
        locked.Targets[0] = t;

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
                    }
                    break;
                case ModTypeEnum.path:
                    foreach (CellAttribute c in m_PathFindingSystem.FindPath(targetCoord, cellsToMark.CachedPaths).CellAttributes)
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(c.CubeCoordinate, c.Position));
                    }
                    mod.PathNested.OriginCoordinate = originCoord;
                    locked.Targets[0].Mods[0] = mod;
                    locked.CombinedCost = CalculateCombinedCost(t);

                    break;
                case ModTypeEnum.line:
                    foreach (Vector3f v in CellGridMethods.LineDraw(new List<Vector3f>(), originCoord, t.TargetCoordinate))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                    }
                    break;
                case ModTypeEnum.ring:
                    foreach (Vector3f v in CellGridMethods.RingDraw(t.TargetCoordinate, mod.RingNested.Radius))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                    }
                    break;
                case ModTypeEnum.cone:
                    foreach (Vector3f v in CellGridMethods.ConeDraw(originCoord, t.TargetCoordinate, mod.ConeNested.Radius, mod.ConeNested.Extent))
                    {
                        mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                    }
                    break;
            }
        }

        m_ResourceSystem.AddArmor(locked.Targets[0].TargetId, locked.Effects[0].GainArmorNested.ArmorAmount);
        m_ResourceSystem.SubstactEnergy(faction, locked.CombinedCost);

        return locked;
    }

    public CellAttributes SetCellAttributes(CellAttributes cellAttributes, bool isTaken, long entityId, uint worldIndex)
    {
        var cell = cellAttributes.Cell;
        cell.IsTaken = isTaken;
        cell.UnitOnCellId = entityId;

        CellAttributes cellAtt = new CellAttributes
        {
            Neighbours = cellAttributes.Neighbours,
            Cell = cell,
            CellMapColorIndex = cellAttributes.CellMapColorIndex
        };

        UpdateNeighbours(cellAtt.Cell, cellAtt.Neighbours, worldIndex);
        return cellAtt;
    }

    public void UpdateNeighbours(CellAttribute cell, CellAttributeList neighbours, uint worldIndex)
    {
        Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWordlIndex, ref CellAttributesComponent.Component cellAtt) =>
        {
            if (worldIndex == cellWordlIndex.Value)
            {
                for (int n = 0; n < neighbours.CellAttributes.Count; n++)
                {
                    if (Vector3fext.ToUnityVector(neighbours.CellAttributes[n].CubeCoordinate) == Vector3fext.ToUnityVector(cellAtt.CellAttributes.Cell.CubeCoordinate))
                    {
                        for (int cn = 0; cn < cellAtt.CellAttributes.Neighbours.CellAttributes.Count; cn++)
                        {
                            if (Vector3fext.ToUnityVector(cellAtt.CellAttributes.Neighbours.CellAttributes[cn].CubeCoordinate) == Vector3fext.ToUnityVector(cell.CubeCoordinate))
                            {
                                cellAtt.CellAttributes.Neighbours.CellAttributes[cn] = cell;
                                cellAtt.CellAttributes = cellAtt.CellAttributes;
                            }
                        }
                    }
                }
            }
        });
    }

}
