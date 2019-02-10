using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LeyLineHybridECS
{

    public class PathFindingSystem : ComponentSystem
    {

        protected override void OnUpdate()
        {
        }
            /*
            public struct UnitsData
            {
                public readonly int Length;
                public readonly ComponentDataArray<MouseState> MouseStateData;
                public readonly ComponentArray<OccupiedCell> OccupiedCellData;
                public readonly ComponentDataArray<MovementData> MovementData;
                public readonly ComponentDataArray<Faction> FactionData;
                public ComponentArray<PathLists> PathListsData;

            }

            [Inject] private UnitsData m_UnitData;

            public struct PlayersEnergyData
            {
                public readonly int Length;
                public readonly ComponentDataArray<Faction> FactionData;
                public ComponentArray<PlayerEnergy> PlayerEnergyData;
            }

            [Inject] private PlayersEnergyData m_PlayerEnergyData;

            public struct CellsData
            {
                public readonly int Length;
                public ComponentDataArray<MouseState> MouseStateData;
                public ComponentDataArray<MarkerState> MarkerStateData;
                public readonly ComponentArray<Neighbours> NeighboursData;
                public readonly ComponentArray<Cell> CellData;
                public readonly ComponentArray<IsTaken> IsTakenData;
                public readonly ComponentArray<MovementCost> MovementCostData;
            }

            [Inject] private CellsData m_CellData;

            [Inject] private CellGridSystem m_CellGridSystem;

            bool cellsMarked;

            DijkstraPathfinding pathfinder = new DijkstraPathfinding();

            protected override void OnUpdate()
            {
                for (int i = 0; i < m_UnitData.Length; ++i)
                {
                    var factionData = m_UnitData.FactionData[i];
                    var pathListsData = m_UnitData.PathListsData[i];
                    var movementData = m_UnitData.MovementData[i];
                    var mouseState = m_UnitData.MouseStateData[i];
                    var occupiedCell = m_UnitData.OccupiedCellData[i];
                    float3 offset = new float3(0, 0.3f, 0);


                    if(Input.GetButtonDown("Fire2"))
                       // m_CellGridSystem.GetRadius(occupiedCell.Cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate, movementData.Range);

                    //if the GameState is UnitClicked and this Unit is clicked
                    if (GameStateSystem.CurrentState == GameStateSystem.State.UnitClicked && mouseState.CurrentState == MouseState.State.Clicked)
                    {
                        if (!cellsMarked)
                        {
                            pathListsData.EnergyRemoved = false;
                            //if unit has a path planned, restore energy
                            if (pathListsData.CurrentPath.Count() != 0)
                            {
                                for (int pi = 0; pi < m_PlayerEnergyData.Length; pi++)
                                {
                                    if (factionData.Value == m_PlayerEnergyData.FactionData[pi].Value)
                                    {
                                        m_PlayerEnergyData.PlayerEnergyData[pi].CurrentEnergy += pathListsData.CurrentPath.Count();
                                    }
                                }

                            }

                            //if pathsInRange are not initialized, initialize them
                            if (pathListsData.PathsInRange.Count == 0)
                            {

                                //pathListsData.CellsInMovementRange = m_CellGridSystem.GetRadius(occupiedCell.Cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate, movementData.Range);

                                pathListsData.LineRenderer.enabled = true;
                                pathListsData.LineRenderer.positionCount = pathListsData.CurrentPath.Count + 1;
                                pathListsData.LineRenderer.SetPosition(0, occupiedCell.Cell.GetComponent<Position3DDataComponent>().Value.Value + offset);
                                pathListsData.PathsInRange = GetAllPathsInRadius(movementData.Range, pathListsData.CellsInMovementRange, occupiedCell.Cell, pathListsData.CachedPaths);
                            }


                            for (int ci = 0; ci < m_CellData.Length; ++ci)
                            {
                                m_CellData.MouseStateData[ci] = new MouseState
                                {
                                    CurrentState = MouseState.State.Neutral
                                };

                                if (!m_CellData.IsTakenData[ci].Value && pathListsData.PathsInRange.Contains(m_CellData.CellData[ci]))
                                {
                                    m_CellData.MarkerStateData[ci] = new MarkerState
                                    {
                                        CurrentState = MarkerState.State.Reachable
                                    };
                                }
                            }
                            cellsMarked = true;
                        }


                        //if pathsInRange are initialized, Update current path and Linerenderer
                        else
                        {
                            for (int ci = 0; ci < m_CellData.Length; ++ci)
                            {
                                //check only the cell we are hovering over
                                if (m_CellData.MouseStateData[ci].CurrentState == MouseState.State.Hovered)
                                {
                                    //if it is reachable
                                    if (m_CellData.MarkerStateData[ci].CurrentState == MarkerState.State.Reachable)
                                    {
                                        //and it is not taken
                                        if (!m_CellData.IsTakenData[ci].Value)
                                        {
                                            //Update currenpath
                                            m_UnitData.PathListsData[i].CurrentPath.Clear();
                                            m_UnitData.PathListsData[i].CurrentPath.AddRange(FindPath(m_CellData.CellData[ci], m_UnitData.PathListsData[i].CachedPaths));
                                            m_UnitData.PathListsData[i].CurrentPath.Reverse();
                                            //remove energy
                                        }

                                        //update Linerenderer
                                        m_UnitData.PathListsData[i].LineRenderer.positionCount = m_UnitData.PathListsData[i].CurrentPath.Count + 1;
                                        m_UnitData.PathListsData[i].LineRenderer.SetPosition(0, m_UnitData.OccupiedCellData[i].Cell.GetComponent<Position3DDataComponent>().Value.Value + offset);

                                        for (int pi = 1; pi <= m_UnitData.PathListsData[i].CurrentPath.Count; pi++)
                                        {
                                            m_UnitData.PathListsData[i].LineRenderer.SetPosition(pi, m_UnitData.PathListsData[i].CurrentPath[pi - 1].GetComponent<Position3DDataComponent>().Value.Value + offset);
                                        }
                                    }
                                    //if it is not reachable
                                    else
                                    {
                                        //clear currentpath and LineRenderer
                                        m_UnitData.PathListsData[i].CurrentPath.Clear();
                                        m_UnitData.PathListsData[i].LineRenderer.positionCount = 0;
                                    }
                                }
                            }
                        }
                    }

                    //if we are in the WaitingForInput State
                    else if (GameStateSystem.CurrentState == GameStateSystem.State.WaitingForInput)
                    {
                        if (cellsMarked)
                        {
                            //remove energy
                            for (int pi = 0; pi < m_PlayerEnergyData.Length; pi++)
                            {
                                for (int ui = 0; ui < m_UnitData.Length; ui++)
                                {
                                    //if the player and the unit share the same Faction
                                    if (m_UnitData.FactionData[ui].Value == m_PlayerEnergyData.FactionData[pi].Value && !m_UnitData.PathListsData[ui].EnergyRemoved)
                                    {
                                        m_PlayerEnergyData.PlayerEnergyData[pi].CurrentEnergy -= m_UnitData.PathListsData[ui].CurrentPath.Count();
                                        m_UnitData.PathListsData[ui].EnergyRemoved = true;
                                    }

                                }
                            }

                            //unmark all reachable cells
                            for (int ci = 0; ci < m_CellData.Length; ++ci)
                            {
                                if (m_CellData.MarkerStateData[ci].CurrentState == MarkerState.State.Reachable)
                                {
                                    m_CellData.MarkerStateData[ci] = new MarkerState
                                    {
                                        CurrentState = MarkerState.State.Neutral
                                    };
                                }
                            }
                            cellsMarked = false;
                        }
                    }
                    //Once we exit the planning phase, clear all path data from the unit
                    else if (GameStateSystem.CurrentState != GameStateSystem.State.WaitingForInput && GameStateSystem.CurrentState != GameStateSystem.State.UnitClicked)
                    {
                        if (m_UnitData.PathListsData[i].PathsInRange.Count != 0)
                        {
                            m_UnitData.PathListsData[i].LineRenderer.enabled = false;
                            m_UnitData.PathListsData[i].CachedPaths.Clear();
                            m_UnitData.PathListsData[i].PathsInRange.Clear();
                        }
                    }
                }
            }

            HashSet<Cell> GetAllPathsInRadius(int radius, List<Cell> cellsInRange, Cell origin, Dictionary<Cell, List<Cell>> cachedPaths)
            {
                var paths = CachePaths(cellsInRange, origin);

                foreach (var key in paths.Keys)
                {
                    var path = paths[key];
                    int pathCost;

                    if (key.GetComponent<IsTaken>().Value)
                        continue;
                    pathCost = path.Sum(c => c.GetComponent<MovementCost>().Value);

                    if(pathCost <= radius)
                    cachedPaths.Add(key, path);
                }

                return new HashSet<Cell>(cachedPaths.Keys);

            }

            Dictionary<Cell, List<Cell>> CachePaths(List<Cell> cellsInRange, Cell origin)
            {
                var edges = GetGraphEdges(cellsInRange, origin);
                var paths = pathfinder.FindAllPaths(edges, origin);
                return paths;
            }

            /// <summary>
            /// Method returns graph representation of cell grid for pathfinding.
            /// </summary>
            Dictionary<Cell, Dictionary<Cell, int>> GetGraphEdges(List<Cell> cellsInRange, Cell origin)
            {

                Dictionary<Cell, Dictionary<Cell, int>> ret = new Dictionary<Cell, Dictionary<Cell, int>>();

                //instead of looping over all cells in grid only loop over cells in CellsInMovementRange

                for (int i = 0; i < cellsInRange.Count; ++i)
                {
                    Cell cell = cellsInRange[i];
                    var isTaken = cellsInRange[i].GetComponent<IsTaken>().Value;
                    var movementCost = cellsInRange[i].GetComponent<MovementCost>().Value;
                    var neighbours = cellsInRange[i].GetComponent<Neighbours>().NeighboursList;

                    ret[cell] = new Dictionary<Cell, int>();

                    if (!isTaken || cell == origin)
                    {
                        foreach (var neighbour in neighbours)
                        {
                            ret[cell][neighbour] = neighbour.GetComponent<MovementCost>().Value;
                        }
                    }
                }

                return ret;

            }

            List<Cell> FindPath(Cell destination, Dictionary<Cell, List<Cell>> cachedPaths)
            {
                if (cachedPaths.ContainsKey(destination))
                {
                    return cachedPaths[destination];
                }
                else
                    return null;
            }
            */
        }

}

