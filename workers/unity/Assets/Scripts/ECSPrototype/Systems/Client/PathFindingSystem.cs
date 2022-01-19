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
    ILogDispatcher logger;

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
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<Actions.Component>(),

        );
        */
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
    }

    public List<KeyValuePair<Vector2i, MapCell>> GetMapRadius(CurrentMapState mapData, Vector3f originCellCubeCoordinate, uint radius)
    {
        var cellsInRadius = new List<KeyValuePair<Vector2i, MapCell>>();

        var coordList = CellGridMethods.CircleDraw(originCellCubeCoordinate, radius);

        foreach (Vector3f v in coordList)
        {
            cellsInRadius.Add(new KeyValuePair<Vector2i, MapCell>(CellGridMethods.CubeToAxial(v), mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(v)]));
        }

        return cellsInRadius;
    }

    public Dictionary<MapCell, MapCellList> GetAllMapPathsInRadius(uint radius, List<KeyValuePair<Vector2i, MapCell>> cellsInRange, Vector3f originCoord, CurrentMapState map)
    {
        MapCell origin = map.CoordinateCellDictionary[CellGridMethods.CubeToAxial(originCoord)];

        var paths = CacheMapPaths(map, cellsInRange, origin);
        var cachedPaths = new Dictionary<MapCell, MapCellList>();

        foreach (var key in paths.Keys)
        {
            var path = paths[key];

            int pathCost;

            if (key.IsTaken)
                continue;

            pathCost = (int)path.Cells.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                cachedPaths.Add(key, path);
            }

            path.Cells.Reverse();
        }
        return cachedPaths;
    }

    public List<MapCell> FindMapPath(CurrentMapState mapData, List<KeyValuePair<Vector2i, MapCell>> cellsInRange, MapCell origin, MapCell destination)
    {
        var edges = GetMapGraphEdges(mapData, cellsInRange, origin);
        var path = pathfinder.FindPath(edges, origin, destination);
        path.Reverse();
        return path;
    }

    public Dictionary<MapCell, Dictionary<MapCell, uint>> GetMapGraphEdges(CurrentMapState mapData, List<KeyValuePair<Vector2i, MapCell>> cellsInRange, MapCell origin)
    {
        Dictionary<MapCell, Dictionary<MapCell, uint>> ret = new Dictionary<MapCell, Dictionary<MapCell, uint>>();

        //instead of looping over all cells in grid only loop over cells in CellsInMovementRange

        for (int i = 0; i < cellsInRange.Count; ++i)
        {
            var isTaken = cellsInRange[i].Value.IsTaken;
            var movementCost = cellsInRange[i].Value.MovementCost;

            ret[cellsInRange[i].Value] = new Dictionary<MapCell, uint>();
            if (!isTaken || Vector3fext.ToUnityVector(cellsInRange[i].Value.Position) == Vector3fext.ToUnityVector(origin.Position))
            {
                for (int y = 0; y < 6; y++)
                {
                    var v = CellGridMethods.CubeNeighbour(CellGridMethods.AxialToCube(cellsInRange[i].Key), (uint) y);

                    if (Mathf.Abs(v.X) <= 14 && Mathf.Abs(v.Y) <= 14 && Mathf.Abs(v.Z) <= 14)
                        ret[cellsInRange[i].Value][mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(v)]] = mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(v)].MovementCost;
                }
            }
        }
        return ret;
    }

    public Dictionary<MapCell, MapCellList> CacheMapPaths(CurrentMapState mapData, List<KeyValuePair<Vector2i, MapCell>> cellsInRange, MapCell origin)
    {
        var edges = GetMapGraphEdges(mapData, cellsInRange, origin);
        var paths = pathfinder.FindAllMapPaths(edges, origin);
        return paths;
    }

    public MapCellList FindPathFromCachedPaths(CurrentMapState mapData, Vector3f inDestination, Dictionary<MapCell, MapCellList> cachedPaths)
    {
        MapCell destination = mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(inDestination)];

        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new MapCellList(new List<MapCell>());
    }

    public Dictionary<Vector3f, Vector3fList> GetAllPathCoordinatesInRadius(CurrentMapState currentMapState, uint radius, List<KeyValuePair<Vector2i, MapCell>> cellsInRange)
    {
        var cachedPaths = new Dictionary<Vector3f, Vector3fList>();

        if (cellsInRange.Count == 0)
            return cachedPaths;

        var paths = CacheMapPaths(currentMapState, cellsInRange, cellsInRange[0].Value);

        foreach (var key in paths.Keys)
        {
            var path = new Vector3fList { Coordinates = new List<Vector3f>() };

            foreach (MapCell c in paths[key].Cells)
            {
                path.Coordinates.Add(CellGridMethods.AxialToCube(c.AxialCoordinate));
            }

            long pathCost;

            if (key.IsTaken)
            {
                continue;
            }

            pathCost = paths[key].Cells.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                path.Coordinates.Reverse();
                cachedPaths.Add(CellGridMethods.AxialToCube(key.AxialCoordinate), path);
            }
        }

        return cachedPaths;
    }

    public Vector3fList FindPathCoordinates(Vector3f destination, Dictionary<Vector3f, Vector3fList> cachedPaths)
    {
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new Vector3fList();
    }

    public bool ValidateMapCellTarget(Vector3f originCoord, Action inAction, MapCell cellAtt)
    {
        bool valid = false;

        if (inAction.Index == -3)
            return valid;

        if (CellGridMethods.GetDistance(originCoord, CellGridMethods.AxialToCube(cellAtt.AxialCoordinate)) <= inAction.Targets[0].Targettingrange)
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

        return valid;
    }

    #region Old Methods
    /*

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

    public bool ValidateCellTarget(Vector3f originCoord, Action inAction, CellAttribute cellAtt)
    {
        bool valid = false;

        if (inAction.Index == -3)
            return valid;

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

        return valid;
    }

    public CellAttribute GetCellAttributeAtCoordinate(Vector3f coordinate, WorldIndexShared worldIndex)
    {
        var cell = new CellAttribute();
        //use a hashset instead of a list to improve contains performance
        Entities.WithSharedComponentFilter(worldIndex).ForEach((in CellAttributesComponent.Component cellAttributes, in CubeCoordinate.Component cubeCoordinate) =>
        {
            if (Vector3fext.ToUnityVector(cubeCoordinate.CubeCoordinate) == Vector3fext.ToUnityVector(coordinate))
            {
                cell = cellAttributes.CellAttributes.Cell;
            }
        })
        .WithoutBurst()
        .Run();

        return cell;
    }

    public List<CellAttributes> GetCellAttributesRadius(Vector3f originCellCubeCoordinate, uint radius, WorldIndexShared unitWorldIndex)
    {
        //returns a list of offsetCoordinates
        var cellsInRadius = new List<CellAttributes>
        {
            //reserve first index for origin
            new CellAttributes { Neighbours = new CellAttributeList(new List<CellAttribute>()) }
        };

        //get all cubeCordinates within range
        var coordList = CellGridMethods.CircleDraw(originCellCubeCoordinate, radius);

        HashSet<Vector3f> coordHash = new HashSet<Vector3f>();

        foreach (Vector3f v in coordList)
        {
            coordHash.Add(v);
        }

        //use a hashset instead of a list to improve contains performance
        Entities.WithSharedComponentFilter(unitWorldIndex).ForEach((in CellAttributesComponent.Component cellAttributes, in CubeCoordinate.Component cubeCoordinate) =>
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
        var cellsInRadius = new List<CellAttributes>
        {
            //reserve first index for origin
            new CellAttributes { Neighbours = new CellAttributeList(new List<CellAttribute>()) }
        };

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

    public Dictionary<Vector3f, Vector3fList> GetAllPathCoordinatesInRadius(uint radius, List<CellAttributes> cellsInRange)
    {
        var cachedPaths = new Dictionary<Vector3f, Vector3fList>();

        if (cellsInRange.Count == 0)
            return cachedPaths;

        var paths = CachePaths(cellsInRange, cellsInRange[0].Cell);

        foreach (var key in paths.Keys)
        {
            var path = new Vector3fList { Coordinates = new List<Vector3f>() };

            foreach (CellAttribute c in paths[key].CellAttributes)
            {
                path.Coordinates.Add(c.CubeCoordinate);
            }

            int pathCost;

            if (key.IsTaken)
            {
                continue;
            }

            pathCost = paths[key].CellAttributes.Sum(c => c.MovementCost);

            if (pathCost <= radius)
            {
                path.Coordinates.Reverse();
                cachedPaths.Add(key.CubeCoordinate, path);
            }
        }

        return cachedPaths;
    }

    public Dictionary<CellAttribute, CellAttributeList> GetAllPathsInRadius(uint radius, List<CellAttributes> cellsInRange)
    {
        var paths = CachePaths(cellsInRange, cellsInRange[0].Cell);
        var cachedPaths = new Dictionary<CellAttribute, CellAttributeList>();

        foreach (var key in paths.Keys)
        {
            var path = paths[key];

            int pathCost;

            if (key.IsTaken)
            {
                continue;
            }

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

        */
    /*
    public List<CellAttribute> FindPath(List<CellAttributes> cellsInRange, CellAttribute origin, CellAttribute destination)
    {
        var edges = GetGraphEdges(cellsInRange, origin);
        var path = pathfinder.FindPath(edges, origin, destination);
        path.Reverse();
        return path;
    }
    */
    #endregion



    public bool ValidateUnitTarget(ApplyToRestrictionsEnum restrictions, long usingUnitId, uint usingUnitFaction, long targetUnitId, uint targetUnitFaction)
    {
        bool valid = false;

        switch (restrictions)
        {
            case ApplyToRestrictionsEnum.any:
                valid = true;
                break;
            case ApplyToRestrictionsEnum.enemy:
                if (targetUnitFaction != usingUnitFaction)
                {
                    valid = true;
                }
                break;
            case ApplyToRestrictionsEnum.friendly:
                if (targetUnitFaction == usingUnitFaction)
                {
                    valid = true;
                }
                break;
            case ApplyToRestrictionsEnum.friendly_other:
                if (targetUnitFaction == usingUnitFaction && usingUnitId != targetUnitId)
                {
                    valid = true;
                }
                break;
            case ApplyToRestrictionsEnum.other:
                if (usingUnitId != targetUnitId)
                {
                    valid = true;
                }
                break;
            case ApplyToRestrictionsEnum.self:
                //maybe selfstate becomes irrelevant once a self-target is implemented.
                if (usingUnitId == targetUnitId)
                {
                    valid = true;
                }
                break;

            default:
                break;
        }

        return valid;
    }

    public bool ValidateTargetClient(CurrentMapState currentMapState, Vector3f originCoord, Vector3f coord, Action inAction, long usingUnitId, uint inFaction)
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
                    })
                    .WithoutBurst()
                    .Run();
                break;
            case TargetTypeEnum.unit:
                Entities.ForEach((in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
                {
                    if (Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (CellGridMethods.GetDistance(targetUnitCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                        {
                            valid = ValidateUnitTarget(inAction.Effects[0].ApplyToRestrictions, usingUnitId, inFaction, targetUnitId.EntityId.Id, targetUnitFaction.Faction);
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
        }

        return valid;
    }

    public bool ValidatePathTargetClient(CurrentMapState currentMapState, Vector3f originCoord, Vector3f coord, Action inAction, long usingUnitId, uint inFaction, Dictionary<MapCell, MapCellList> inCachedPaths)
    {
        bool valid = false;

        if (inAction.Index == -3)
            return valid;

        switch (inAction.Targets[0].TargetType)
        {
            case TargetTypeEnum.cell:

                if (inCachedPaths.Count != default)
                {
                    if (currentMapState.CoordinateCellDictionary.ContainsKey(CellGridMethods.CubeToAxial(coord)) && inCachedPaths.ContainsKey(currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(coord)]))
                        valid = true;
                }
                else
                {
                    Entities.ForEach((in SpatialEntityId cellId, in CellAttributesComponent.Component cellAtts, in CubeCoordinate.Component cellCoord) =>
                    {
                        var cell = cellAtts.CellAttributes.Cell;

                        if (Vector3fext.ToUnityVector(cellCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
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
                    })
                    .WithoutBurst()
                    .Run();
                }
                break;
            case TargetTypeEnum.unit:
                Entities.ForEach((in SpatialEntityId targetUnitId, in CubeCoordinate.Component targetUnitCoord, in FactionComponent.Component targetUnitFaction) =>
                {
                    if (Vector3fext.ToUnityVector(targetUnitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(coord))
                    {
                        if (CellGridMethods.GetDistance(targetUnitCoord.CubeCoordinate, originCoord) <= inAction.Targets[0].Targettingrange)
                        {
                            valid = ValidateUnitTarget(inAction.Effects[0].ApplyToRestrictions, usingUnitId, inFaction, targetUnitId.EntityId.Id, targetUnitFaction.Faction);
                        }
                    }
                })
                .WithoutBurst()
                .Run();
                break;
        }

        return valid;
    }

}
