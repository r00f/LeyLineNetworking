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
            public ComponentDataArray<Actions.Component> ActionsData;
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
                var unitId = m_UnitData.EntityIDs[i].EntityId.Id;
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

                            if(m_GameStateData.GameState[gi].CurrentState == GameStateEnum.interrupt)
                            {
                                //PATH CULLING
                                actionsData = CompareCullableActions(actionsData, gameStateWorldIndex, unitId);
                                m_UnitData.ActionsData[i] = actionsData;
                            }
                            else if (m_GameStateData.GameState[gi].CurrentState == GameStateEnum.move)
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

        private Actions.Component CompareCullableActions(Actions.Component unitActions, uint worldIndex, long unitId)
        {
            for (int i = 0; i < m_UnitData.Length; ++i)
            {
                var otherUnitId = m_UnitData.EntityIDs[i].EntityId.Id;
                var otherUnitActions = m_UnitData.ActionsData[i];
                var otherUnitWorldIndex = m_UnitData.WorldIndexData[i].Value;

                if (worldIndex == otherUnitWorldIndex && unitId != otherUnitId)
                {
                    if (otherUnitActions.LockedAction.Index != -3 && otherUnitActions.LockedAction.Targets[0].TargetCoordinate == unitActions.LockedAction.Targets[0].TargetCoordinate)
                    {
                        if (otherUnitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                        {
                            //Decide which path to cull
                            float unitMoveTime = unitActions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell * unitActions.LockedAction.Targets[0].Mods[0].Coordinates.Count;
                            float otherUnitMoveTime = otherUnitActions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell * otherUnitActions.LockedAction.Targets[0].Mods[0].Coordinates.Count;
                            if (unitMoveTime == otherUnitMoveTime)
                            {
                                if (Random.Range(0, 2) == 0)
                                {
                                    unitActions = CullPath(unitActions);
                                }
                                Debug.Log("MoveTimesTheSame");
                            }
                            else if (unitMoveTime > otherUnitMoveTime)
                            {
                                unitActions = CullPath(unitActions);
                                Debug.Log("UnitSlower, Cull path");
                            }
                        }
                        else if(otherUnitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.spawn_unit)
                        {
                            Debug.Log("SpawnCullPath");
                            unitActions = CullPath(unitActions);
                        }
                    }
                }
            }
            return unitActions;
        }

        private Actions.Component CullPath(Actions.Component unitActions)
        {
            var coordCount = unitActions.LockedAction.Targets[0].Mods[0].Coordinates.Count;
            unitActions.LockedAction.Targets[0].Mods[0].Coordinates.RemoveAt(coordCount - 1);
            coordCount = unitActions.LockedAction.Targets[0].Mods[0].Coordinates.Count;

            if(coordCount != 0)
            {
                Vector3f newTarget = unitActions.LockedAction.Targets[0].Mods[0].Coordinates[coordCount - 1];
                var t = unitActions.LockedAction.Targets[0];
                t.TargetCoordinate = newTarget;
                unitActions.LockedAction.Targets[0] = t;
                unitActions.LockedAction = unitActions.LockedAction;
            }
            else
            {
                unitActions.LockedAction = unitActions.NullAction;
            }
            return unitActions;
        }
    }
}

