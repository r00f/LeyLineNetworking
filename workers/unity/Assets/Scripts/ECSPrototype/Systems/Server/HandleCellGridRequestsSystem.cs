using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using System.Linq;
using Generic;
using Cells;
using Unit;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(GameStateSystem)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(ResourceSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class HandleCellGridRequestsSystem : ComponentSystem
{
    DijkstraPathfinding pathfinder = new DijkstraPathfinding();

    public struct SelectActionRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<Actions.CommandRequests.SelectActionCommand> ReceivedSelectActionRequests;
        public ComponentDataArray<Actions.Component> ActionsData;
    }

    [Inject] private SelectActionRequestData m_SelectActionRequestData;

    public struct SetTargetRequestData
    {
        public readonly int Length;
        public ComponentDataArray<Actions.CommandRequests.SetTargetCommand> ReceivedSetTargetRequests;
        public ComponentDataArray<Actions.Component> ActionsData;
        public ComponentDataArray<ServerPath.Component> ServerPathData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;

    }

    [Inject] private SetTargetRequestData m_SetTargetRequestData;


    public struct ServerPathData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<ServerPath.Component> ServerPaths;
    }

    [Inject] private ServerPathData m_ServerPathData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] private CellData m_CellData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;
    [Inject] private ResourceSystem m_ResourceSystem;
    [Inject] private CellStateSystem m_CellStateSystem;

    protected override void OnUpdate()
    {
        #region select action

        for (int i = 0; i < m_SelectActionRequestData.Length; i++)
        {
            var actionData = m_SelectActionRequestData.ActionsData[i];
            var selectActionRequest = m_SelectActionRequestData.ReceivedSelectActionRequests[i];
            var cellsToMarkData = m_SelectActionRequestData.CellsToMarkData[i];
            var coord = m_SelectActionRequestData.CoordinateData[i].CubeCoordinate;
            var worldIndex = m_SelectActionRequestData.WorldIndexData[i].Value;

            cellsToMarkData.SetClientRange = false;
            m_SelectActionRequestData.CellsToMarkData[i] = cellsToMarkData;

            foreach (var sar in selectActionRequest.Requests)
            {
                int index = sar.Payload.ActionId;

                if(index >= 0)
                {
                    actionData.CurrentSelected = actionData.OtherActions[index];
                }
                else
                {
                    if(index == -3)
                    {
                        actionData.CurrentSelected = actionData.NullAction;
                    }
                    else if(index == -2)
                    {
                        actionData.CurrentSelected = actionData.BasicMove;

                    }
                    else if(index == -1)
                    {
                        actionData.CurrentSelected = actionData.BasicAttack;
                    }
                }
            }

            if(actionData.CurrentSelected.Targets.Count != 0)
            {
                if (!actionData.CurrentSelected.Equals(actionData.LastSelected) || cellsToMarkData.CellsInRange.Count == 0)
                {
                    cellsToMarkData.CellsInRange = GetRadius(coord, (uint)actionData.CurrentSelected.Targets[0].Targettingrange, worldIndex);

                    switch (actionData.CurrentSelected.Targets[0].Higlighter)
                    {
                        case UseHighlighterEnum.pathing:
                            cellsToMarkData.CachedPaths = GetAllPathsInRadius((uint)actionData.CurrentSelected.Targets[0].Targettingrange, cellsToMarkData.CellsInRange, cellsToMarkData.CellsInRange[0].Cell);
                            break;
                        case UseHighlighterEnum.no_pathing:
                            //do stuff

                            break;
                    }
                }
            }


            actionData.LastSelected = actionData.CurrentSelected;
            m_SelectActionRequestData.ActionsData[i] = actionData;

            cellsToMarkData.SetClientRange = true;
            m_SelectActionRequestData.CellsToMarkData[i] = cellsToMarkData;
        }

        #endregion

        #region set target

        for (int i = 0; i < m_SetTargetRequestData.Length; i++)
        {
            var actionData = m_SetTargetRequestData.ActionsData[i];
            var setTargetRequest = m_SetTargetRequestData.ReceivedSetTargetRequests[i];
            var serverPath = m_SetTargetRequestData.ServerPathData[i];
            var unitWorldIndex = m_SetTargetRequestData.WorldIndexData[i].Value;
            var cellsToMark = m_SetTargetRequestData.CellsToMarkData[i];

            foreach (var str in setTargetRequest.Requests)
            {
                long id = str.Payload.TargetId;

                switch (actionData.CurrentSelected.Targets[0].TargetType)
                {
                    case TargetTypeEnum.cell:
                        for(int ci = 0; ci < m_CellData.Length; ci++)
                        {
                            var cellId = m_CellData.EntityIds[ci].EntityId.Id;
                            var cell = m_CellData.CellAttributes[ci].CellAttributes.Cell;
                     
                            if (cellId == id)
                            {
                                //check if in range
                                //energy stuff inc
                                actionData.LockedAction = actionData.CurrentSelected;
                                var t = actionData.LockedAction.Targets[0];
                                t.CellTargetNested.TargetId = id;
                                actionData.LockedAction.Targets[0] = t;

                                foreach (TargetMod tm in actionData.LockedAction.Targets[0].Mods)
                                {
                                    if(tm.ModType == ModTypeEnum.aoe)
                                    {

                                    }
                                    if(tm.ModType == ModTypeEnum.path)
                                    {
                                        serverPath.Path = FindPath(cell, cellsToMark.CachedPaths);
                                        //Debug.Log(cell.CubeCoordinate);
                                        m_SetTargetRequestData.ServerPathData[i] = serverPath;
                                    }
                                }
                            }
                        }
                        break;
                    case TargetTypeEnum.unit:
                        //do stuff

                        break;
                }
            }
            
            m_SetTargetRequestData.ActionsData[i] = actionData;
        }

        #endregion

        #region set serverPath

        for (int i = 0; i < m_ServerPathData.Length; i++)
        {
            var serverPath = m_ServerPathData.ServerPaths[i];
            var unitWorldIndex = m_ServerPathData.WorldIndexData[i].Value;

            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                if (unitWorldIndex == gameStateWorldIndex)
                {
                    var gameState = m_GameStateData.GameState[gi].CurrentState;

                    if (gameState == Generic.GameStateEnum.calculate_energy)
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

        #endregion
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
