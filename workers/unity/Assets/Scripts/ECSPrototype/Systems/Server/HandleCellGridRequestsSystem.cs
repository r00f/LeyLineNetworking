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

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class HandleCellGridRequestsSystem : ComponentSystem
{
    PathFindingSystem m_PathFindingSystem;
    CommandSystem m_CommandSystem;
    TimerSystem m_TimerSystem;
    ResourceSystem m_ResourceSystem;

    EntityQuery m_GameStateData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    EntityQuery m_SetTargetRequestData;

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
            ComponentType.ReadOnly<Health.Component>(),
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
                            var a = unitActions.OtherActions[index];
                            a.CombinedCost = CalculateCombinedCost(unitActions.OtherActions[index].Targets[0]);
                            unitActions.OtherActions[index] = a;

                            if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.OtherActions[index].CombinedCost) >= 0)
                            {
                                actionToSelect = unitActions.OtherActions[index];
                            }
                        }
                        else
                        {
                            if (index == -2)
                            {
                                var a = unitActions.BasicMove;
                                a.CombinedCost = CalculateCombinedCost(unitActions.BasicMove.Targets[0]);
                                unitActions.BasicMove = a;

                                if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.BasicMove.CombinedCost) >= 0)
                                {
                                    actionToSelect = unitActions.BasicMove;
                                }
                            }
                            else if (index == -1)
                            {
                                var a = unitActions.BasicAttack;
                                a.CombinedCost = CalculateCombinedCost(unitActions.BasicAttack.Targets[0]);
                                unitActions.BasicAttack = a;

                                if (m_ResourceSystem.CheckPlayerEnergy(unitFaction.Faction, unitActions.BasicAttack.CombinedCost) >= 0)
                                {
                                    actionToSelect = unitActions.BasicAttack;
                                }
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
                                unitActions.LockedAction = SetLockedAction(unitActions.CurrentSelected, unitCoord.CubeCoordinate, unitCoord.CubeCoordinate, unitEntityId.EntityId.Id, unitFaction.Faction);
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
                    long id = setTargetRequest.Payload.TargetId;

                    if (unitActions.CurrentSelected.Index != -3 && unitActions.LockedAction.Index == -3)
                    {
                        switch (unitActions.CurrentSelected.Targets[0].TargetType)
                        {
                            case TargetTypeEnum.cell:

                                Entities.With(m_CellData).ForEach((ref SpatialEntityId cellId, ref CellAttributesComponent.Component cellAtts) =>
                                {
                                    var cell = cellAtts.CellAttributes.Cell;

                                    if (cellId.EntityId.Id == id)
                                    {
                                        bool isValidTarget = false;
                                        if (requestingUnitCellsToMark.CachedPaths.Count != 0)
                                        {
                                            if (requestingUnitCellsToMark.CachedPaths.ContainsKey(cell))
                                            {
                                                isValidTarget = true;
                                            }
                                        }
                                        else
                                        {
                                            bool valid = false;
                                            foreach (CellAttributes ca in requestingUnitCellsToMark.CellsInRange)
                                            {
                                                if (Vector3fext.ToUnityVector(ca.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(cell.CubeCoordinate))
                                                {
                                                    if (requestingUnitActions.CurrentSelected.Targets[0].CellTargetNested.RequireEmpty)
                                                    {
                                                        if (!cell.IsTaken) valid = true;
                                                    }
                                                    else
                                                    {
                                                        valid = true;
                                                    }
                                                }

                                            }

                                            isValidTarget = valid;

                                        }

                                        if (isValidTarget)
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.CurrentSelected;
                                            var locked = requestingUnitActions.LockedAction;
                                            var t = requestingUnitActions.LockedAction.Targets[0];
                                            t.TargetCoordinate = cell.CubeCoordinate;
                                            t.TargetId = id;
                                            requestingUnitActions.LockedAction.Targets[0] = t;

                                            for (int mi = 0; mi < requestingUnitActions.LockedAction.Targets[0].Mods.Count; mi++)
                                            {
                                                var modType = requestingUnitActions.LockedAction.Targets[0].Mods[mi].ModType;
                                                var mod = requestingUnitActions.LockedAction.Targets[0].Mods[0];
                                                switch (modType)
                                                {
                                                    case ModTypeEnum.aoe:
                                                        foreach (Vector3f v in CellGridMethods.CircleDraw(t.TargetCoordinate, (uint)mod.AoeNested.Radius))
                                                        {
                                                            //if(m_PathFindingSystem.ValidateUnitTarget(v)
                                                            //this adds all coords whitout validating targets and needs some kind of validation method
                                                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(v, new Vector3f()));
                                                        }
                                                        break;
                                                    case ModTypeEnum.path:
                                                        foreach (CellAttribute c in m_PathFindingSystem.FindPath(cell, requestingUnitCellsToMark.CachedPaths).CellAttributes)
                                                        {
                                                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(c.CubeCoordinate, c.Position));
                                                        }
                                                        requestingUnitActions.LockedAction.Targets[0].Mods[0] = mod;
                                                        locked.CombinedCost = CalculateCombinedCost(t);
                                                        break;
                                                    case ModTypeEnum.line:
                                                        foreach (Vector3f v in CellGridMethods.LineDraw(requestingUnitCoord.CubeCoordinate, t.TargetCoordinate))
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
                                                }
                                            }
                                            requestingUnitActions.LockedAction = locked;
                                            m_ResourceSystem.SubstactEnergy(requestingUnitFaction.Faction, locked.CombinedCost);
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
                                    if (targetUnitId.EntityId.Id == id)
                                    {
                                        bool isValidTarget = false;
                                        foreach (CellAttributes c in requestingUnitCellsToMark.CellsInRange)
                                        {
                                            if (Vector3fext.ToUnityVector(c.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate))
                                            {
                                                isValidTarget = m_PathFindingSystem.ValidateTarget(e, requestingUnitActions.CurrentSelected.Targets[0].UnitTargetNested.UnitReq, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction);
                                            }
                                        }
                                        if (isValidTarget)
                                        {
                                            requestingUnitActions.LockedAction = SetLockedAction(requestingUnitActions.CurrentSelected, requestingUnitCoord.CubeCoordinate, targetUnitCoord.CubeCoordinate, targetUnitId.EntityId.Id, requestingUnitFaction.Faction);
                                        }
                                        else
                                        {
                                            requestingUnitActions.LockedAction = requestingUnitActions.NullAction;
                                        }
                                    }
                                });
                                break;
                        }

                        unitActions = requestingUnitActions;
                        unitActions.CurrentSelected = unitActions.NullAction;
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

    public Action SetLockedAction(Action selectedAction, Vector3f originCoord, Vector3f unitCoord, long unitId, uint faction)
    {
        Action locked = selectedAction;
        var t = locked.Targets[0];
        t.TargetCoordinate = unitCoord;
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

                    break;
                case ModTypeEnum.line:
                    foreach (Vector3f v in CellGridMethods.LineDraw(originCoord, t.TargetCoordinate))
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
            Cell = cell
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
