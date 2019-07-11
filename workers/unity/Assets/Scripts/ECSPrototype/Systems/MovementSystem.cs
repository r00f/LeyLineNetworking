using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Cell;
using Improbable;
using Improbable.Gdk.ReactiveComponents;

namespace LeyLineHybridECS
{
    [UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class MovementSystem : ComponentSystem
    {
        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<SpatialEntityId> EntityIDs;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public ComponentDataArray<MovementVariables.Component> MovementVariables;
            public ComponentDataArray<CellsToMark.Component> CellsToMark;
            public ComponentDataArray<CubeCoordinate.Component> CubeCoordinates;
            public ComponentDataArray<Position.Component> Positions;
            public ComponentDataArray<Vision.Component> Vision;
        }

        [Inject] UnitData m_UnitData;

        public struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<CellAttributesComponent.Component>> AuthorativeData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        }

        [Inject] CellData m_CellData;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<GameState.Component> GameState;
        }

        [Inject] GameStateData m_GameStateData;

        [Inject] HandleCellGridRequestsSystem m_CellGridSystem;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            //terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        }

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_UnitData.Length; ++i)
            {
                var actionsData = m_UnitData.ActionsData[i];
                var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
                var position = m_UnitData.Positions[i];
                var coord = m_UnitData.CubeCoordinates[i];
                var movementVariables = m_UnitData.MovementVariables[i];
                var vision = m_UnitData.Vision[i];
                
                if (actionsData.LockedAction.Index == -2)
                {
                    var currentPath = m_UnitData.ActionsData[i].LockedAction.Targets[0].Mods[0].Coordinates;

                    for (int gi = 0; gi < m_GameStateData.Length; gi++)
                    {
                        var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                        if (unitWorldIndex == gameStateWorldIndex)
                        {
                            var cellsToMark = m_UnitData.CellsToMark[i];

                            if (m_GameStateData.GameState[gi].CurrentState == GameStateEnum.moving)
                            {
                                if (currentPath.Count != 0 && cellsToMark.CellsInRange.Count != 0)
                                {
                                    for (int ci = 0; ci < m_CellData.Length; ci++)
                                    {
                                        var cellAtt = m_CellData.CellAttributes[ci];
                                        var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;

                                        if (cellsToMark.CellsInRange[0].Cell.CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate && cellAtt.CellAttributes.Cell.IsTaken && cellWorldIndex == unitWorldIndex)
                                        {
                                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, 0, cellWorldIndex);
                                            m_CellData.CellAttributes[ci] = cellAtt;
                                        }
                                    }

                                    //instead of lerping towards next pos in path, set pos and wait for client to catch up
                                    if (movementVariables.TimeLeft > 0)
                                    {
                                        movementVariables.TimeLeft -= Time.deltaTime;
                                        m_UnitData.MovementVariables[i] = movementVariables;
                                    }
                                    else
                                    {
                                        //convert cube Coordinate to position
                                        Vector3 worldPos = m_CellGridSystem.CoordinateToWorldPosition(unitWorldIndex, currentPath[0]);
                                        position.Coords = new Coordinates(worldPos.x, worldPos.y, worldPos.z);
                                        m_UnitData.Positions[i] = position;

                                        if (currentPath.Count == 1)
                                        {
                                            for (int ci = 0; ci < m_CellData.Length; ci++)
                                            {
                                                var cellAtt = m_CellData.CellAttributes[ci];
                                                var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;

                                                if (currentPath[0] == cellAtt.CellAttributes.Cell.CubeCoordinate && cellWorldIndex == unitWorldIndex)
                                                {
                                                    cellAtt.CellAttributes = cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, m_UnitData.EntityIDs[i].EntityId.Id, cellWorldIndex);
                                                    m_CellData.CellAttributes[ci] = cellAtt;
                                                }
                                            }
                                            movementVariables.TimeLeft = 0;
                                        }
                                        else
                                        {
                                            movementVariables.TimeLeft = movementVariables.TravelTime;
                                        }

                                        coord.CubeCoordinate = currentPath[0];
                                        vision.RequireUpdate = true;
                                        m_UnitData.Vision[i] = vision;
                                        m_UnitData.CubeCoordinates[i] = coord;
                                        m_UnitData.MovementVariables[i] = movementVariables;
                                        currentPath.RemoveAt(0);
                                    }
                                }
                            }
                            else if (m_GameStateData.GameState[gi].CurrentState == GameStateEnum.calculate_energy)
                            {
                                cellsToMark.CachedPaths = new Dictionary<CellAttribute, CellAttributeList>();
                                cellsToMark.CellsInRange = new List<CellAttributes>();
                                m_UnitData.CellsToMark[i] = cellsToMark;
                            }
                        }
                    }
                }
            }
        }
    }
}

