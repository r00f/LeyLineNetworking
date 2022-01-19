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

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem))]
public class HandleCellGridRequestsSystem : JobComponentSystem
{
    PathFindingSystem m_PathFindingSystem;
    CommandSystem m_CommandSystem;
    ResourceSystem m_ResourceSystem;
    ILogDispatcher logger;

    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;

    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        #region select action
        Entities.WithNone<AiUnit.Component>().ForEach((Entity e, ref Actions.Component unitActions, in ClientActionRequest.Component clientActionRequest, in FactionComponent.Component unitFaction, in WorldIndexShared unitWorldIndex, in SpatialEntityId unitEntityId, in CubeCoordinate.Component unitCoord) =>
        {
            if(clientActionRequest.ActionId == -3 && (unitActions.CurrentSelected.Index != -3 || unitActions.LockedAction.Index != -3))
            {
                m_ResourceSystem.AddEnergy(unitWorldIndex, unitFaction.Faction, unitActions.LockedAction.CombinedCost);
                unitActions.CurrentSelected = unitActions.NullAction;
                unitActions.LastSelected = unitActions.NullAction;
                unitActions.LockedAction = unitActions.NullAction;
            }
            else if (clientActionRequest.ActionId != unitActions.LastSelected.Index && Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) == Vector3.zero)
            {
                m_ResourceSystem.AddEnergy(unitWorldIndex, unitFaction.Faction, unitActions.LockedAction.CombinedCost);
                unitActions.LockedAction = unitActions.NullAction;
                Action actionToSelect = unitActions.NullAction;

                if (m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction) > 0)
                {
                    if (clientActionRequest.ActionId >= 0)
                    {
                        var a = unitActions.ActionsList[clientActionRequest.ActionId];
                        a.CombinedCost = CalculateCombinedCost(unitActions.ActionsList[clientActionRequest.ActionId].Targets[0]);
                        unitActions.ActionsList[clientActionRequest.ActionId] = a;

                        if (m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction, unitActions.ActionsList[clientActionRequest.ActionId].CombinedCost) >= 0)
                        {
                            actionToSelect = unitActions.ActionsList[clientActionRequest.ActionId];
                        }
                    }
                }

                unitActions.CurrentSelected = actionToSelect;
                bool self = false;

                if (unitActions.CurrentSelected.Targets.Count != 0)
                {
                    if (unitActions.CurrentSelected.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                    {
                        self = true;
                        unitActions.LockedAction = SetLockedAction(new CurrentMapState(), unitWorldIndex, unitEntityId.EntityId.Id, unitActions.CurrentSelected, unitCoord.CubeCoordinate, unitCoord.CubeCoordinate, unitFaction.Faction);
                        unitActions.LastSelected = unitActions.CurrentSelected;
                        unitActions.CurrentSelected = unitActions.NullAction;
                    }

                    if (!unitActions.CurrentSelected.Equals(unitActions.LastSelected) && !self)
                    {
                        switch (unitActions.CurrentSelected.Targets[0].Higlighter)
                        {
                            case UseHighlighterEnum.pathing:
                                if (unitActions.CurrentSelected.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                                {
                                    int range = unitActions.CurrentSelected.Targets[0].Targettingrange;
                                    if (m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction, 0) < range)
                                    {
                                        range = m_ResourceSystem.CheckPlayerEnergy(unitWorldIndex, unitFaction.Faction, 0);
                                    }
                                }
                                break;
                        }
                    }
                }

                if (!self)
                    unitActions.LastSelected = unitActions.CurrentSelected;
            }
        })
        .WithoutBurst()
        .Run();
        #endregion 

        #region set target
        Entities.ForEach((ref Actions.Component requestingUnitActions, in ClientActionRequest.Component clientActionRequest, in CubeCoordinate.Component requestingUnitCoord, in FactionComponent.Component requestingUnitFaction, in SpatialEntityId requestingUnitId, in WorldIndexShared worldIndex) =>
        {
            if (requestingUnitActions.CurrentSelected.Index != -3 && requestingUnitActions.LockedAction.Index == -3 && Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) != Vector3.zero && requestingUnitActions.CurrentSelected.Targets[0].Targettingrange > 0)
            {
                if (ValidateTarget(worldIndex, requestingUnitActions.CurrentSelected, clientActionRequest.TargetCoordinate, requestingUnitCoord.CubeCoordinate, requestingUnitId.EntityId.Id, requestingUnitFaction.Faction))
                    requestingUnitActions.LockedAction = SetLockedAction(new CurrentMapState(), worldIndex, requestingUnitId.EntityId.Id, requestingUnitActions.CurrentSelected, requestingUnitCoord.CubeCoordinate, clientActionRequest.TargetCoordinate, requestingUnitFaction.Faction);
                else
                    requestingUnitActions.LockedAction = requestingUnitActions.NullAction;

                requestingUnitActions.CurrentSelected = requestingUnitActions.NullAction;
                requestingUnitActions.LastSelected = requestingUnitActions.NullAction;
            }
        })
        .WithoutBurst()
        .Run();
        #endregion
        return inputDeps;
    }

    public bool ValidateTarget(WorldIndexShared worldIndex, Action requestingUnitAction, Vector3f targetCoord, Vector3f requestingUnitCoord, long requestingUnitId, uint requestingUnitFaction)
    {
        bool valid = false;

        switch (requestingUnitAction.Targets[0].TargetType)
        {
            case TargetTypeEnum.cell:
                Entities.WithSharedComponentFilter(worldIndex).ForEach((in CurrentMapState currentMapState) =>
                {
                    valid = m_PathFindingSystem.ValidateMapCellTarget(requestingUnitCoord, requestingUnitAction, currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(targetCoord)]);
                })
                .WithoutBurst()
                .Run();
                break;
            case TargetTypeEnum.unit:
                Entities.WithAll<Health.Component>().WithSharedComponentFilter(worldIndex).ForEach((in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
                {
                    if (Vector3fext.ToUnityVector(targetCoord) == Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate))
                    {
                        valid = m_PathFindingSystem.ValidateUnitTarget(requestingUnitAction.Effects[0].ApplyToRestrictions, requestingUnitId, requestingUnitFaction, targetUnitId.EntityId.Id, targetUnitFaction.Faction);
                    }
                })
                .WithoutBurst()
                .Run();
                break;

        }

        return valid;
    }

    public bool ValidateCellTarget(WorldIndexShared worldIndex, Action requestingUnitAction, Vector3f targetCoord, Vector3f requestingUnitCoord)
    {
        bool valid = false;



        return valid;
    }

    /*
    public bool ValidateCellTarget(WorldIndexShared worldIndex, Actions.Component requestingUnitActions, Vector3f targetCoord, Vector3f requestingUnitCoord)
    {
        bool valid = false;

        Entities.WithSharedComponentFilter(worldIndex).ForEach((in CellAttributesComponent.Component cellAtts, in CubeCoordinate.Component cCoord) =>
        {
            if (Vector3fext.ToUnityVector(targetCoord) == Vector3fext.ToUnityVector(cCoord.CubeCoordinate))
            {
                valid = m_PathFindingSystem.ValidateCellTarget(requestingUnitCoord, requestingUnitActions.CurrentSelected, cellAtts.CellAttributes.Cell);
            }
        })
        .WithoutBurst()
        .Run();

        return valid;
    }
    */

    public bool ValidateUnitTarget(WorldIndexShared worldIndex, Actions.Component requestingUnitActions, Vector3f targetCoord, long requestingUnitId, uint requestingUnitFaction)
    {
        bool valid = false;

        Entities.WithAll<Health.Component>().WithSharedComponentFilter(worldIndex).ForEach((in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
        {
            if (Vector3fext.ToUnityVector(targetCoord) == Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate))
            {
                valid = m_PathFindingSystem.ValidateUnitTarget(requestingUnitActions.CurrentSelected.Effects[0].ApplyToRestrictions, requestingUnitId, requestingUnitFaction, targetUnitId.EntityId.Id, targetUnitFaction.Faction);
            }
        })
        .WithoutBurst()
        .Run();

        return valid;
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

    public Action SetLockedAction(CurrentMapState currentMapState, WorldIndexShared worldIndex, long usingUnitId, Action selectedAction, Vector3f originCoord, Vector3f targetCoord, uint faction)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((in CurrentMapState curMapState) =>
        {
            currentMapState = curMapState;
        })
        .WithoutBurst()
        .Run();

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

            effect.TargetPosition = GetPositionFromGameState(worldIndex, targetCoord);

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
                    /*
                    logger.HandleLog(LogType.Warning,
                        new LogEvent("WritePathIntoMod")
                        .WithField("WorldIndex", worldIndex.Value)
                        .WithField("TargetingRange", (uint) t.Targettingrange)
                        .WithField("OriginCoord", Vector3fext.ToUnityVector(originCoord))
                        .WithField("TargetCoord", Vector3fext.ToUnityVector(targetCoord))
                    );
                    */

                    //Hotfix for Bots targeting themselves with move action 
                    if(Vector3fext.ToUnityVector(originCoord) != Vector3fext.ToUnityVector(targetCoord))
                    {
                        foreach (MapCell c in m_PathFindingSystem.FindMapPath(currentMapState, m_PathFindingSystem.GetMapRadius(currentMapState, originCoord, (uint) t.Targettingrange), currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(originCoord)], currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(targetCoord)]))
                        {
                            mod.CoordinatePositionPairs.Add(new CoordinatePositionPair(CellGridMethods.AxialToCube(c.AxialCoordinate), currentMapState.CoordinateCellDictionary[c.AxialCoordinate].Position));
                            effect.MoveAlongPathNested.CoordinatePositionPairs.Add(new CoordinatePositionPair(CellGridMethods.AxialToCube(c.AxialCoordinate), currentMapState.CoordinateCellDictionary[c.AxialCoordinate].Position));
                        }

                        mod.PathNested.OriginCoordinate = originCoord;
                        effect.MoveAlongPathNested.OriginCoordinate = originCoord;
                        locked.Effects[0] = effect;
                        locked.Targets[0].Mods[0] = mod;
                        locked.CombinedCost = CalculateCombinedCost(t);
                    }
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

    public Vector3f GetPositionFromGameState(WorldIndexShared worldIndex, Vector3f coord)
    {
        var pos = new Vector3f();
        Entities.WithSharedComponentFilter(worldIndex).ForEach((in MapData.Component map) =>
        {
            pos = map.CoordinateCellDictionary[CellGridMethods.CubeToAxial(coord)].Position;
        })
        .WithoutBurst()
        .Run();

        return pos;
    }
}
