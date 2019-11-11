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

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class PathFindingSystem : ComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    EntityQuery m_CellData;
    EntityQuery m_UnitData;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadWrite<CellAttributesComponent.Component>()
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
    }

    protected override void OnUpdate()
    {
        
    }

    public List<CellAttributes> GetRadius(Vector3f originCellCubeCoordinate, uint radius, uint unitWorldIndex)
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

        Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWorldIndex, ref CubeCoordinate.Component cubeCoordinate, ref CellAttributesComponent.Component cellAttributes) =>
        {
            if (cellWorldIndex.Value == unitWorldIndex)
            {
                if (Vector3fext.ToUnityVector(cubeCoordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCellCubeCoordinate))
                {
                    cellsInRadius[0] = cellAttributes.CellAttributes;
                }
                else if (coordHash.Contains(cubeCoordinate.CubeCoordinate))
                {
                    cellsInRadius.Add(cellAttributes.CellAttributes);
                }
            }
        });

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
        Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component coordinate, ref CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(originCoord))
            {
                origin = cellAttribute.CellAttributes.Cell;
            }
        });

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

    public CellAttributeList FindPath(Vector3f inDestination, Dictionary<CellAttribute, CellAttributeList> cachedPaths)
    {
        CellAttribute destination = new CellAttribute();

        Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component coordinate, ref CellAttributesComponent.Component cellAttribute) =>
        {
            if (Vector3fext.ToUnityVector(coordinate.CubeCoordinate) == Vector3fext.ToUnityVector(inDestination))
            {
                destination = cellAttribute.CellAttributes.Cell;
            }
        });

        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public bool ValidateTarget(Entity e, UnitRequisitesEnum restrictions, long usingUnitId, uint inFaction)
    {
        bool valid = false;

        bool isUnit = false;

        if (EntityManager.HasComponent<Actions.Component>(e))
            isUnit = true;

        if(isUnit)
        {
            var faction = EntityManager.GetComponentData<FactionComponent.Component>(e);
            var unitId = EntityManager.GetComponentData<SpatialEntityId>(e);

            switch (restrictions)
            {
                case UnitRequisitesEnum.any:
                    valid = true;
                    break;
                case UnitRequisitesEnum.enemy:
                    if (faction.Faction != inFaction)
                    {
                        valid = true;
                    }
                    break;
                case UnitRequisitesEnum.friendly:
                    if (faction.Faction == inFaction)
                    {
                        valid = true;
                    }
                    break;
                case UnitRequisitesEnum.friendly_other:
                    if (faction.Faction == inFaction && usingUnitId != unitId.EntityId.Id)
                    {
                        valid = true;
                    }
                    break;
                case UnitRequisitesEnum.other:
                    if (usingUnitId != unitId.EntityId.Id)
                    {
                        valid = true;
                    }
                    break;
                case UnitRequisitesEnum.self:
                    //maybe selfstate becomes irrelevant once a self-target is implemented.
                    if (usingUnitId == unitId.EntityId.Id)
                    {
                        valid = true;
                    }
                    break;

                default:
                    break;
            }
        }
        else
        {
            var cellAttributes = EntityManager.GetComponentData<CellAttributesComponent.Component>(e).CellAttributes.Cell;

            if(cellAttributes.UnitOnCellId != 0)
            {


            }
        }

        return valid;
    }

    /*
    public bool ValidateUnitTarget(long targetUnitId, long usingUnitId, uint inFaction, UnitRequisitesEnum restrictions)
    {
        bool valid = false;

        Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component faction) =>
        {
            if (targetUnitId == unitId.EntityId.Id)
            {
                switch (restrictions)
                {
                    case UnitRequisitesEnum.any:
                        valid = true;
                        break;
                    case UnitRequisitesEnum.enemy:
                        if (faction.Faction != inFaction)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.friendly:
                        if (faction.Faction == inFaction)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.friendly_other:
                        if (faction.Faction == inFaction && usingUnitId != unitId.EntityId.Id)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.other:
                        if (usingUnitId != unitId.EntityId.Id)
                        {
                            valid = true;
                        }
                        break;
                    case UnitRequisitesEnum.self:
                        //maybe selfstate becomes irrelevant once a self-target is implemented.
                        if (usingUnitId == unitId.EntityId.Id)
                        {
                            valid = true;
                        }
                        break;

                    default:
                        break;
                }
            }
        });

        return valid;
    }
    */
}
