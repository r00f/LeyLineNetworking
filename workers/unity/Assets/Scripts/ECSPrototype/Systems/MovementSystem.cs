using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Cells;
using Improbable;

namespace LeyLineHybridECS
{
    public class MovementSystem : ComponentSystem
    {
        public struct UnitData
        {
            public readonly int Length;
            public ComponentDataArray<CellsToMark.Component> CellsToMark;
            public ComponentDataArray<CubeCoordinate.Component> CubeCoordinates;
            public ComponentDataArray<ServerPath.Component> ServerPathData;
            public ComponentDataArray<Position.Component> Positions;
            public ComponentDataArray<SpatialEntityId> EntityIDs;
        }

        [Inject] private UnitData m_UnitData;

        public struct CellData
        {
            public readonly int Length;
            public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        }

        [Inject] private CellData m_CellData;

        public struct GameStateData
        {
            public readonly ComponentDataArray<GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            //terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        }

        protected override void OnUpdate()
        {

            for (int i = 0; i < m_UnitData.Length; ++i)
            {
                var currentPath = m_UnitData.ServerPathData[i];
                var position = m_UnitData.Positions[i];
                var coord = m_UnitData.CubeCoordinates[i];
                var cellsToMark = m_UnitData.CellsToMark[i];

                if (m_GameStateData.GameState[0].CurrentState == GameStateEnum.moving)
                {

                    if (currentPath.Path.CellAttributes.Count != 0)
                    {

                        for (int ci = 0; ci < m_CellData.Length; ci++)
                        {
                            var cellAtt = m_CellData.CellAttributes[ci];

                            if (cellsToMark.CellsInRange[0].Cell.CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate && cellAtt.CellAttributes.Cell.IsTaken)
                            {
                                Debug.Log("ASYD");
                                cellAtt.CellAttributes = new CellAttributes
                                {
                                    Neighbours = cellAtt.CellAttributes.Neighbours,
                                    Cell = new CellAttribute
                                    {
                                        IsTaken = false,
                                        UnitOnCellId = new EntityId(),

                                        MovementCost = cellAtt.CellAttributes.Cell.MovementCost,
                                        CubeCoordinate = cellAtt.CellAttributes.Cell.CubeCoordinate,
                                        Position = cellAtt.CellAttributes.Cell.Position
                                    }
                                };

                                m_CellData.CellAttributes[ci] = cellAtt;

                                UpdateNeighbours(cellAtt.CellAttributes.Cell, cellAtt.CellAttributes.Neighbours);
                            }
                        }


                        if (position.Coords.ToUnityVector() != currentPath.Path.CellAttributes[0].Position.ToUnityVector())
                        {
                            Vector3 newPos = Vector3.MoveTowards(position.Coords.ToUnityVector(), currentPath.Path.CellAttributes[0].Position.ToUnityVector(), Time.deltaTime);
                            position.Coords = new Coordinates(newPos.x, newPos.y, newPos.z);
                            m_UnitData.Positions[i] = position;
                        }
                        else
                        {
                            if(currentPath.Path.CellAttributes.Count == 1)
                            {
                                for (int ci = 0; ci < m_CellData.Length; ci++)
                                {
                                    var cellAtt = m_CellData.CellAttributes[ci];

                                    if (currentPath.Path.CellAttributes[0].CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate)
                                    {
                                        cellAtt.CellAttributes = new CellAttributes
                                        {
                                            Neighbours = cellAtt.CellAttributes.Neighbours,
                                            Cell = new CellAttribute
                                            {
                                                IsTaken = true,
                                                UnitOnCellId = m_UnitData.EntityIDs[i].EntityId,

                                                MovementCost = cellAtt.CellAttributes.Cell.MovementCost,
                                                CubeCoordinate = cellAtt.CellAttributes.Cell.CubeCoordinate,
                                                Position = cellAtt.CellAttributes.Cell.Position
                                            }
                                        };

                                        m_CellData.CellAttributes[ci] = cellAtt;

                                        UpdateNeighbours(cellAtt.CellAttributes.Cell, cellAtt.CellAttributes.Neighbours);
                                    }
                                }
                            }
                            coord.CubeCoordinate = currentPath.Path.CellAttributes[0].CubeCoordinate;
                            m_UnitData.CubeCoordinates[i] = coord;
                            currentPath.Path.CellAttributes.RemoveAt(0);
                        }
                    }
     
                }
                else if (m_GameStateData.GameState[0].CurrentState == GameStateEnum.calculate_energy)
                {
                    cellsToMark.CachedPaths = new Dictionary<CellAttribute, CellAttributeList>();
                    cellsToMark.CellsInRange = new List<CellAttributes>();

                    m_UnitData.CellsToMark[i] = cellsToMark;
                }
            }
        }


        public void UpdateNeighbours(CellAttribute cell, CellAttributeList neighbours)
        {
            for (int ci = 0; ci < m_CellData.Length; ci++)
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
