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
using Unity.Jobs;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class PathFindingSystem : JobComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    EntityQuery m_CellData;
    EntityQuery m_UnitData;

    protected override void OnCreate()
    {
        base.OnCreate();
        /*
        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadWrite<CellAttributesComponent.Component>()
        );

        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        //ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<Actions.Component>(),
        ComponentType.ReadWrite<CellsToMark.Component>()
        );
        */
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
    }

    public List<CellAttributes> GetRadius(Vector3f originCellCubeCoordinate, uint radius, WorldIndexShared unitWorldIndex)
    {
        //returns a list of offsetCoordinates
        var cellsInRadius = new List<CellAttributes>();
        //reserve first index for origin
        cellsInRadius.Add(new CellAttributes { Neighbours = new CellAttributeList(new List<CellAttribute>()) });

        //get all cubeCordinates within range
        var coordList = CellGridMethods.CircleDraw(originCellCubeCoordinate, radius);

        HashSet<Vector3f> coordHash = new HashSet<Vector3f>();

        foreach (Vector3f v in coordList)
        {
            coordHash.Add(v);
        }

        //use a hashset instead of a list to improve contains performance

        Entities.WithSharedComponentFilter(unitWorldIndex).ForEach(( ref CellAttributesComponent.Component cellAttributes, in CubeCoordinate.Component cubeCoordinate) =>
        {
            if (Vector3fext.ToUnityVector(cubeCoordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCellCubeCoordinate))
            {
                cellsInRadius[0] = cellAttributes.CellAttributes;
            }
            else if (coordHash.Contains(cubeCoordinate.CubeCoordinate))
            {
                cellsInRadius.Add(cellAttributes.CellAttributes);
            }
        })
        .WithoutBurst()
        .Run();

        return cellsInRadius;
    }

    public List<CellAttributes> GetRadiusClient(Vector3f originCellCubeCoordinate, uint radius)
    {
        //returns a list of offsetCoordinates
        var cellsInRadius = new List<CellAttributes>();
        //reserve first index for origin
        cellsInRadius.Add(new CellAttributes { Neighbours = new CellAttributeList(new List<CellAttribute>()) });

        //get all cubeCordinates within range
        var coordList = CellGridMethods.CircleDraw(originCellCubeCoordinate, radius);

        HashSet<Vector3f> coordHash = new HashSet<Vector3f>();

        foreach (Vector3f v in coordList)
        {
            coordHash.Add(v);
        }

        //use a hashset instead of a list to improve contains performance

        Entities.ForEach((in CubeCoordinate.Component cubeCoordinate, in CellAttributesComponent.Component cellAttributes) =>
        {
            if (Vector3fext.ToUnityVector(cubeCoordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCellCubeCoordinate))
            {
                cellsInRadius[0] = cellAttributes.CellAttributes;
            }
            else if (coordHash.Contains(cubeCoordinate.CubeCoordinate))
            {
                cellsInRadius.Add(cellAttributes.CellAttributes);
            }
        })
        .WithoutBurst()
        .Run();

        return cellsInRadius;
    }

    public Dictionary<CellAttribute, CellAttributeList> GetAllPathsInRadius(uint radius, List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        var paths = CachePaths(cellsInRange, origin);
        var cachedPaths = new Dictionary<CellAttribute, CellAttributeList>();

        foreach (var key in paths.Keys)
        {
            var path = paths[key];

            int pathCost;

            if (key.IsTaken)
                continue;

            pathCost = path.CellAttributes.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                path.CellAttributes.Reverse();
                cachedPaths.Add(key, path);
            }
        }

        return cachedPaths;
    }

    public Dictionary<CellAttribute, CellAttributeList> GetAllPathsInRadius(uint radius, List<CellAttributes> cellsInRange, Vector3f originCoord)
    {
        CellAttribute origin = new CellAttribute();
        Entities.ForEach((in CubeCoordinate.Component coordinate, in CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCoord))
            {
                origin = cellAttribute.CellAttributes.Cell;
            }
        })
        .WithoutBurst()
        .Run();

        var paths = CachePaths(cellsInRange, origin);
        var cachedPaths = new Dictionary<CellAttribute, CellAttributeList>();

        foreach (var key in paths.Keys)
        {
            var path = paths[key];

            int pathCost;

            if (key.IsTaken)
                continue;

            pathCost = path.CellAttributes.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                cachedPaths.Add(key, path);
            }

            path.CellAttributes.Reverse();
        }

        return cachedPaths;

    }

    public Dictionary<CellAttribute, CellAttributeList> CachePaths(List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        var edges = GetGraphEdges(cellsInRange, origin);
        var paths = pathfinder.FindAllPaths(edges, origin);
        return paths;
    }

    public Dictionary<CellAttribute, Dictionary<CellAttribute, int>> GetGraphEdges(List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        Dictionary<CellAttribute, Dictionary<CellAttribute, int>> ret = new Dictionary<CellAttribute, Dictionary<CellAttribute, int>>();

        //instead of looping over all cells in grid only loop over cells in CellsInMovementRange

        for (int i = 0; i < cellsInRange.Count; ++i)
        {
            CellAttributes cell = cellsInRange[i];

            var isTaken = cellsInRange[i].Cell.IsTaken;
            var movementCost = cellsInRange[i].Cell.MovementCost;
            var neighbours = cellsInRange[i].Neighbours.CellAttributes;


            ret[cell.Cell] = new Dictionary<CellAttribute, int>();


            if (!isTaken || Vector3fext.ToUnityVector(cell.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(origin.CubeCoordinate))
            {
                if (neighbours != null)
                {
                    foreach (var neighbour in neighbours)
                    {
                        ret[cell.Cell][neighbour] = neighbour.MovementCost;
                    }
                }

            }
        }
        return ret;
    }

    public CellAttributeList FindPath(CellAttribute destination, Dictionary<CellAttribute, CellAttributeList> cachedPaths)
    {
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public CellAttributeList FindPath(Vector3f inDestination, Dictionary<CellAttribute, CellAttributeList> cachedPaths, WorldIndexShared worldIndex)
    {
        CellAttribute destination = new CellAttribute();

        Entities.WithSharedComponentFilter(worldIndex).ForEach((in CubeCoordinate.Component coordinate, in CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(inDestination))
            {
                destination = cellAttribute.CellAttributes.Cell;
            }
        })
        .WithoutBurst()
        .Run();

        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public CellAttributeList FindPathClient(Vector3f inDestination, Dictionary<CellAttribute, CellAttributeList> cachedPaths)
    {
        CellAttribute destination = new CellAttribute();

        Entities.ForEach((in CubeCoordinate.Component coordinate, in CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(inDestination))
            {
                destination = cellAttribute.CellAttributes.Cell;
            }
        })
        .WithoutBurst()
        .Run();

        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public bool ValidateTarget(WorldIndexShared worldIndex, Vector3f originCoord, Vector3f coord, Action inAction, long usingUnitId, uint inFaction, Dictionary<CellAttribute, CellAttributeList> inCachedPaths)
    {
        bool valid = false;

        if (inAction.Index == -3)
            return valid;

        switch (inAction.Targets[0].TargetType)
        {
            case TargetTypeEnum.cell:
                Entities.WithSharedComponentFilter(worldIndex).ForEach((in SpatialEntityId cellId, in CellAttributesComponent.Component cellAtts, in CubeCoordinate.Component cellCoord) =>
                {
                    var cell = cellAtts.CellAttributes.Cell;

                    if (Vector3fext.ToUnityVector(cellCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (inCachedPaths.Count != 0)
                        {
                            if (inCachedPaths.ContainsKey(cell))
                            {
                                valid = true;
                            }
                        }
                        else
                        {
                            if(CellGridMethods.GetDistance(cellCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                            {
                                if (inAction.Targets[0].CellTargetNested.RequireEmpty)
                                {
                                    if (!cell.IsTaken)
                                        valid = true;
                                }
                                else
                                {
                                    valid = true;
                                }
                            }
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
            case TargetTypeEnum.unit:
                Entities.WithSharedComponentFilter(worldIndex).ForEach((Entity e, in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
                {
                    if (Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (CellGridMethods.GetDistance(targetUnitCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                        {
                            valid = ValidateUnitTarget(e, inAction.Effects[0].ApplyToRestrictions, usingUnitId, inFaction);
                            //Debug.Log("UNIT VALID IN PATHFINDING SYS: " + valid);
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
        }
        
        return valid;
    }

    public bool ValidateTargetWithCellAtt(WorldIndexShared worldIndex, Vector3f originCoord, Vector3f coord, Action inAction, long usingUnitId, uint inFaction, Dictionary<CellAttribute, CellAttributeList> inCachedPaths, CellAttribute cellAtt)
    {
        bool valid = false;

        if (inAction.Index == -3)
            return valid;

        switch (inAction.Targets[0].TargetType)
        {
            case TargetTypeEnum.cell:

                var cell = cellAtt;

                if (Vector3fext.ToUnityVector(cellAtt.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                {
                    if (inCachedPaths.Count != 0)
                    {
                        if (inCachedPaths.ContainsKey(cell))
                        {
                            valid = true;
                        }
                    }
                    else
                    {
                        if (CellGridMethods.GetDistance(cellAtt.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                        {
                            if (inAction.Targets[0].CellTargetNested.RequireEmpty)
                            {
                                if (!cellAtt.IsTaken)
                                    valid = true;
                            }
                            else
                            {
                                valid = true;
                            }
                        }
                    }
                }
                break;
            case TargetTypeEnum.unit:
                Entities.WithSharedComponentFilter(worldIndex).ForEach((Entity e, in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
                {
                    if (Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (CellGridMethods.GetDistance(targetUnitCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                        {
                            valid = ValidateUnitTarget(e, inAction.Effects[0].ApplyToRestrictions, usingUnitId, inFaction);
                            //Debug.Log("UNIT VALID IN PATHFINDING SYS: " + valid);
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
        }

        return valid;
    }

    public bool ValidateTargetClient(Vector3f originCoord, Vector3f coord, Action inAction, long usingUnitId, uint inFaction, Dictionary<CellAttribute, CellAttributeList> inCachedPaths)
    {
        bool valid = false;

        if (inAction.Index == -3)
            return valid;

        switch (inAction.Targets[0].TargetType)
        {
            case TargetTypeEnum.cell:
                Entities.ForEach((in SpatialEntityId cellId, in CellAttributesComponent.Component cellAtts, in CubeCoordinate.Component cellCoord) =>
                {
                    var cell = cellAtts.CellAttributes.Cell;

                    if (Vector3fext.ToUnityVector(cellCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (inCachedPaths.Count != 0)
                        {
                            if (inCachedPaths.ContainsKey(cell))
                            {
                                valid = true;
                            }
                        }
                        else
                        {
                            if (CellGridMethods.GetDistance(cellCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                            {
                                if (inAction.Targets[0].CellTargetNested.RequireEmpty)
                                {
                                    if (!cell.IsTaken)
                                        valid = true;
                                }
                                else
                                {
                                    valid = true;
                                }
                            }
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
            case TargetTypeEnum.unit:
                Entities.ForEach((Entity e, in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
                {
                    if (Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (CellGridMethods.GetDistance(targetUnitCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                        {
                            valid = ValidateUnitTarget(e, inAction.Effects[0].ApplyToRestrictions, usingUnitId, inFaction);
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
        }

        return valid;
    }

    public bool ValidateUnitTarget(Entity targetEntity, ApplyToRestrictionsEnum restrictions, long usingUnitId, uint usingUnitFaction)
    {
        bool valid = false;
        bool isUnit = false;

        if (EntityManager.HasComponent<Actions.Component>(targetEntity))
            isUnit = true;

        if(isUnit)
        {
            var targetEntityfaction = EntityManager.GetComponentData<FactionComponent.Component>(targetEntity);
            var targetUnitId = EntityManager.GetComponentData<SpatialEntityId>(targetEntity);

            switch (restrictions)
            {
                case ApplyToRestrictionsEnum.any:
                    valid = true;
                    break;
                case ApplyToRestrictionsEnum.enemy:
                    if (targetEntityfaction.Faction != usingUnitFaction)
                    {
                        valid = true;
                    }
                    break;
                case ApplyToRestrictionsEnum.friendly:
                    if (targetEntityfaction.Faction == usingUnitFaction)
                    {
                        valid = true;
                    }
                    break;
                case ApplyToRestrictionsEnum.friendly_other:
                    if (targetEntityfaction.Faction == usingUnitFaction && usingUnitId != targetUnitId.EntityId.Id)
                    {
                        valid = true;
                    }
                    break;
                case ApplyToRestrictionsEnum.other:
                    if (usingUnitId != targetUnitId.EntityId.Id)
                    {
                        valid = true;
                    }
                    break;
                case ApplyToRestrictionsEnum.self:
                    //maybe selfstate becomes irrelevant once a self-target is implemented.
                    if (usingUnitId == targetUnitId.EntityId.Id)
                    {
                        valid = true;
                    }
                    break;

                default:
                    break;
            }
        }

        return valid;
    }
}
