using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class HandleCellGridRequestsSystem : ComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    public struct CellsInRangeRequestData
    {
        public readonly int Length;
        public ComponentDataArray<Unit.CellsToMark.CommandRequests.CellsInRangeCommand> ReceivedCellsInRangeRequests;
        public ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
    }

    [Inject] private CellsInRangeRequestData m_CellsInRangeRequestData;

    public struct FindAllPathsRequestData
    {
        public readonly int Length;
        public ComponentDataArray<Unit.CellsToMark.CommandRequests.FindAllPathsCommand> ReceivedFindAllPathsRequests;
        public ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
    }

    [Inject] private FindAllPathsRequestData m_FindAllPathsRequestData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<Cells.CellAttributesComponent.Component> CellsData;
    }

    [Inject] private CellData m_CellData;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_CellsInRangeRequestData.Length; i++)
        {
            var cellsToMarkData = m_CellsInRangeRequestData.CellsToMarkData[i];

            if(cellsToMarkData.CellsInRange.Count == 0)
            {
                var cellsInRangeRequests = m_CellsInRangeRequestData.ReceivedCellsInRangeRequests[i];

                foreach (var cellsInRangeRequest in cellsInRangeRequests.Requests)
                {
                    cellsToMarkData.CellsInRange = GetRadius(cellsInRangeRequest.Payload.Origin, cellsInRangeRequest.Payload.Range);
                    m_CellsInRangeRequestData.CellsToMarkData[i] = cellsToMarkData;
                }

            }

        }

        for (int ci = 0; ci < m_FindAllPathsRequestData.Length; ci++)
        {
            var cellsToMarkData = m_FindAllPathsRequestData.CellsToMarkData[ci];

            if (cellsToMarkData.CachedPaths.Count == 0 && cellsToMarkData.CellsInRange.Count != 0)
            {
                var findAllPathsRequests = m_FindAllPathsRequestData.ReceivedFindAllPathsRequests[ci];

                foreach (var findAllPathsRequest in findAllPathsRequests.Requests)
                {
                    cellsToMarkData.CachedPaths = GetAllPathsInRadius(findAllPathsRequest.Payload.Range, findAllPathsRequest.Payload.CellsInRange, findAllPathsRequest.Payload.Origin, cellsToMarkData.CachedPaths);
                    m_FindAllPathsRequestData.CellsToMarkData[ci] = cellsToMarkData;
                }
            }
        }
    }

    public int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }

    public List<Cells.CellAttributes> GetRadius(Vector3f originCellCubeCoordinate, uint radius)
    {
        //returns a list of offsetCoordinates
        var cellsInRadius = new List<Cells.CellAttributes>();
        //reserve first index for origin
        cellsInRadius.Add(new Cells.CellAttributes());

        for (int i = 0; i < m_CellData.Length; i++)
        {
            Vector3f cubeCoordinate = m_CellData.CoordinateData[i].CubeCoordinate;

            if (GetDistance(originCellCubeCoordinate, cubeCoordinate) < radius)
            {
                if(m_CellData.CellsData[i].CellAttributes.Cell.CubeCoordinate == originCellCubeCoordinate)
                {
                    cellsInRadius[0] = m_CellData.CellsData[i].CellAttributes;
                }
                else
                    cellsInRadius.Add(m_CellData.CellsData[i].CellAttributes);
            }
        }
        return cellsInRadius;
    }

    public Dictionary<Cells.CellAttribute, Unit.CellAttributeList> GetAllPathsInRadius(uint radius, List<Cells.CellAttributes> cellsInRange, Cells.CellAttribute origin, Dictionary<Cells.CellAttribute, Unit.CellAttributeList> cachedPaths)
    {
        var paths = CachePaths(cellsInRange, origin);

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

    public Dictionary<Cells.CellAttribute, Unit.CellAttributeList> CachePaths(List<Cells.CellAttributes> cellsInRange, Cells.CellAttribute origin)
    {
        var edges = GetGraphEdges(cellsInRange, origin);
        var paths = pathfinder.FindAllPaths(edges, origin);
        return paths;
    }

    /// <summary>
    /// Method returns graph representation of cell grid for pathfinding.
    /// </summary>
    public Dictionary<Cells.CellAttribute, Dictionary<Cells.CellAttribute, int>> GetGraphEdges(List<Cells.CellAttributes> cellsInRange, Cells.CellAttribute origin)
    {
        Dictionary<Cells.CellAttribute, Dictionary<Cells.CellAttribute, int>> ret = new Dictionary<Cells.CellAttribute, Dictionary<Cells.CellAttribute, int>>();

        //instead of looping over all cells in grid only loop over cells in CellsInMovementRange

        for (int i = 0; i < cellsInRange.Count; ++i)
        {
            Cells.CellAttributes cell = cellsInRange[i];
            
            var isTaken = cellsInRange[i].Cell.IsTaken;
            var movementCost = cellsInRange[i].Cell.MovementCost;
            var neighbours = cellsInRange[i].Neighbours;
            

            ret[cell.Cell] = new Dictionary<Cells.CellAttribute, int>();


            if (!isTaken || cell.Cell.CubeCoordinate == origin.CubeCoordinate)
            {
                foreach (var neighbour in neighbours)
                {
                    ret[cell.Cell][neighbour] = neighbour.MovementCost;
                }
            }


        }
        return ret;
    }
}
