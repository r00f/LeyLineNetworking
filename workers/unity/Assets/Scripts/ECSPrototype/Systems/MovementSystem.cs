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
    [UpdateAfter(typeof(HandleCellGridRequestsSystem))]
    public class MovementSystem : ComponentSystem
    {
        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<SpatialEntityId> EntityIDs;
            public ComponentDataArray<MovementVariables.Component> MovementVariables;
            public ComponentDataArray<CellsToMark.Component> CellsToMark;
            public ComponentDataArray<CubeCoordinate.Component> CubeCoordinates;
            public ComponentDataArray<ServerPath.Component> ServerPathData;
            public ComponentDataArray<Position.Component> Positions;
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

        [Inject] private HandleCellGridRequestsSystem m_CellGridSystem;

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
                var movementVariables = m_UnitData.MovementVariables[i];

                if (m_GameStateData.GameState[0].CurrentState == GameStateEnum.moving)
                {
                    if (currentPath.Path.CellAttributes.Count != 0)
                    {
                        for (int ci = 0; ci < m_CellData.Length; ci++)
                        {
                            var cellAtt = m_CellData.CellAttributes[ci];

                            if (cellsToMark.CellsInRange[0].Cell.CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate && cellAtt.CellAttributes.Cell.IsTaken)
                            {
                                //Debug.Log("ASYD");
                                cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, new EntityId());

                                m_CellData.CellAttributes[ci] = cellAtt;

                                //m_CellGridSystem.UpdateNeighbours(cellAtt.CellAttributes.Cell, cellAtt.CellAttributes.Neighbours);
                            }
                        }

                        //instead of lerping towards next pos in path, set pos and wait for client to catch up
                        if (position.Coords.ToUnityVector() != currentPath.Path.CellAttributes[0].Position.ToUnityVector())
                        {
                            if(movementVariables.TimeLeft > 0)
                            {
                                movementVariables.TimeLeft -= Time.deltaTime;
                                m_UnitData.MovementVariables[i] = movementVariables;
                            }
                            else
                            {
                                position.Coords = new Coordinates(currentPath.Path.CellAttributes[0].Position.X, currentPath.Path.CellAttributes[0].Position.Y, currentPath.Path.CellAttributes[0].Position.Z);
                                m_UnitData.Positions[i] = position;
                            }
                        }
                        else
                        {
                            movementVariables.TimeLeft = movementVariables.TravelTime;

                            if (currentPath.Path.CellAttributes.Count == 1)
                            {
                                movementVariables.TimeLeft = 0;
                                for (int ci = 0; ci < m_CellData.Length; ci++)
                                {
                                    var cellAtt = m_CellData.CellAttributes[ci];

                                    if (currentPath.Path.CellAttributes[0].CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate)
                                    {
                                        cellAtt.CellAttributes = cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, m_UnitData.EntityIDs[i].EntityId);

                                        m_CellData.CellAttributes[ci] = cellAtt;

                                        //m_CellGridSystem.UpdateNeighbours(cellAtt.CellAttributes.Cell, cellAtt.CellAttributes.Neighbours);
                                    }
                                }
                            }

                            m_UnitData.MovementVariables[i] = movementVariables;
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
    }
}
