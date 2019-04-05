using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;
using Cells;
using Unit;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem))]
public class HandleCellGridRequestsSystem : ComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    public struct CellsInRangeRequestData
    {
        public readonly int Length;
        public ComponentDataArray<CellsToMark.CommandRequests.CellsInRangeCommand> ReceivedCellsInRangeRequests;
        public ComponentDataArray<CellsToMark.Component> CellsToMarkData;
    }

    [Inject] private CellsInRangeRequestData m_CellsInRangeRequestData;

    public struct FindAllPathsRequestData
    {
        public readonly int Length;
        public ComponentDataArray<CellsToMark.CommandRequests.FindAllPathsCommand> ReceivedFindAllPathsRequests;
        public ComponentDataArray<CellsToMark.Component> CellsToMarkData;
    }

    [Inject] private FindAllPathsRequestData m_FindAllPathsRequestData;

    public struct FindPathRequestData
    {
        public readonly int Length;
        public ComponentDataArray<ServerPath.CommandRequests.FindPathCommand> ReceivedFindPathRequests;
        public ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<ServerPath.Component> ServerPathData;
    }

    [Inject] private FindPathRequestData m_FindPathRequestData;

    public struct ServerPathData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<ServerPath.Component> ServerPaths;
    }

    [Inject] private ServerPathData m_ServerPathData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] private CellData m_CellData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Generic.GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;

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
                    cellsToMarkData.CellsInRange = GetRadius(cellsInRangeRequest.Payload.Origin, cellsInRangeRequest.Payload.Range, cellsInRangeRequest.Payload.WorldIndex);
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
                    cellsToMarkData.CachedPaths = GetAllPathsInRadius(findAllPathsRequest.Payload.Range, findAllPathsRequest.Payload.CellsInRange, findAllPathsRequest.Payload.Origin);
                    m_FindAllPathsRequestData.CellsToMarkData[ci] = cellsToMarkData;
                }
            }
        }

        
        for(int i = 0; i < m_ServerPathData.Length; i++)
        {
            var serverPath = m_ServerPathData.ServerPaths[i];
            var unitWorldIndex = m_ServerPathData.WorldIndexData[i].Value;

            for(int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                if(unitWorldIndex == gameStateWorldIndex)
                {
                    var gameState = m_GameStateData.GameState[gi].CurrentState;

                    if(gameState == Generic.GameStateEnum.calculate_energy)
                    {
                        serverPath.Path = new CellAttributeList
                        {
                            CellAttributes = new List<CellAttribute>()
                        };
                        m_ServerPathData.ServerPaths[i] = serverPath;

                    }

                }

            }
        }
        

        for (int i = 0; i < m_FindPathRequestData.Length; i++)
        {
            var serverPathData = m_FindPathRequestData.ServerPathData[i];
            var findPathRequests = m_FindPathRequestData.ReceivedFindPathRequests[i];
            var cellsToMarkData = m_FindPathRequestData.CellsToMarkData[i];

            if (cellsToMarkData.CachedPaths.Count != 0)
            {
                foreach (var findPathRequest in findPathRequests.Requests)
                {
                    //Debug.Log("findPathRequest");
                    serverPathData.Path = FindPath(findPathRequest.Payload.Destination, cellsToMarkData.CachedPaths);
                    m_FindPathRequestData.ServerPathData[i] = serverPathData;
                }
            }

        }
    }

    public int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }

    public float GetAngle(Vector3f originPos, Vector3f targetPos)
    {
        Vector3f dir = targetPos - originPos;
        float Angle = Mathf.Atan2(dir.X, dir.Z) * Mathf.Rad2Deg;
        return Angle;
    }

    public List<CellAttributes> GetRadius(Vector3f originCellCubeCoordinate, uint radius, uint unitWorldIndex)
    {
        //returns a list of offsetCoordinates
        var cellsInRadius = new List<CellAttributes>();
        //reserve first index for origin
        cellsInRadius.Add(new CellAttributes());

        for (int i = 0; i < m_CellData.Length; i++)
        {
            uint cellWorldIndex = m_CellData.WorldIndexData[i].Value;

            if (cellWorldIndex == unitWorldIndex)
            {
                Vector3f cubeCoordinate = m_CellData.CoordinateData[i].CubeCoordinate;

                if (GetDistance(originCellCubeCoordinate, cubeCoordinate) <= radius)
                {
                    if (m_CellData.CellAttributes[i].CellAttributes.Cell.CubeCoordinate == originCellCubeCoordinate)
                    {
                        cellsInRadius[0] = m_CellData.CellAttributes[i].CellAttributes;
                    }
                    else
                        cellsInRadius.Add(m_CellData.CellAttributes[i].CellAttributes);
                }
            }
        }
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

    public Dictionary<CellAttribute, CellAttributeList> CachePaths(List<CellAttributes> cellsInRange, CellAttribute origin)
    {
        var edges = GetGraphEdges(cellsInRange, origin);
        var paths = pathfinder.FindAllPaths(edges, origin);
        return paths;
    }

    /// <summary>
    /// Method returns graph representation of cell grid for pathfinding.
    /// </summary>
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

    public CellAttributeList FindPath(CellAttribute destination, Dictionary<CellAttribute, CellAttributeList> cachedPaths)
    {
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<CellAttribute>());
    }

    public CellAttributes SetCellAttributes(CellAttributes cellAttributes, bool isTaken, EntityId entityId, uint worldIndex)
    {
        CellAttributes cellAtt = new CellAttributes
        {
            Neighbours = cellAttributes.Neighbours,

            Cell = new CellAttribute
            {
                IsTaken = isTaken,
                UnitOnCellId = entityId,

                MovementCost = cellAttributes.Cell.MovementCost,
                Position = cellAttributes.Cell.Position,
                CubeCoordinate = cellAttributes.Cell.CubeCoordinate,

            }

        };

        UpdateNeighbours(cellAtt.Cell, cellAtt.Neighbours, worldIndex);

        return cellAtt;
    }

    public void UpdateNeighbours(CellAttribute cell, CellAttributeList neighbours, uint worldIndex)
    {
        for (int ci = 0; ci < m_CellData.Length; ci++)
        {
            var cellWordlIndex = m_CellData.WorldIndexData[ci].Value;
            if(worldIndex == cellWordlIndex)
            {
                var cellAtt = m_CellData.CellAttributes[ci];
                for (int n = 0; n < neighbours.CellAttributes.Count; n++)
                {
                    if (neighbours.CellAttributes[n].CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate)
                    {
                        for (int cn = 0; cn < cellAtt.CellAttributes.Neighbours.CellAttributes.Count; cn++)
                        {
                            if (cellAtt.CellAttributes.Neighbours.CellAttributes[cn].CubeCoordinate == cell.CubeCoordinate)
                            {
                                cellAtt.CellAttributes.Neighbours.CellAttributes[cn] = new CellAttribute
                                {
                                    IsTaken = cell.IsTaken,
                                    CubeCoordinate = cellAtt.CellAttributes.Neighbours.CellAttributes[cn].CubeCoordinate,
                                    Position = cellAtt.CellAttributes.Neighbours.CellAttributes[cn].Position,
                                    MovementCost = cellAtt.CellAttributes.Neighbours.CellAttributes[cn].MovementCost
                                };
                                m_CellData.CellAttributes[ci] = cellAtt;
                            }
                        }
                    }
                }
            }
        }
    }
}
