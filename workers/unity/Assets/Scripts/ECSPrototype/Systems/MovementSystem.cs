using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Cell;
using Improbable;
using System.Linq;
using Unity.Collections;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class MovementSystem : ComponentSystem
    {
        ILogDispatcher logger;

        EntityQuery m_GameStateData;
        EntityQuery m_CellData;
        EntityQuery m_UnitData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );

            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<GameState.Component>()
            );

            m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CellsToMark.Component>(),
            ComponentType.ReadWrite<Position.Component>(),
            ComponentType.ReadWrite<Actions.Component>(),
            ComponentType.ReadWrite<MovementVariables.Component>(),
            ComponentType.ReadWrite<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<Vision.Component>()
            );

        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;

        }

        protected override void OnUpdate()
        {
            //var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
            //var gameStateWorldIndexes = m_GameStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
           
            /*#region unit data

            var unitCellsToMarks = m_UnitData.ToComponentDataArray<CellsToMark.Component>(Allocator.TempJob);
            var movementVarData = m_UnitData.ToComponentDataArray<MovementVariables.Component>(Allocator.TempJob);
            int i = 0;
            #endregion
            */

            Entities.With(m_UnitData).ForEach((Entity e, ref MovementVariables.Component m, ref Actions.Component actionsData, ref WorldIndex.Component unitWorldIndex, ref Position.Component position, ref CubeCoordinate.Component coord, ref Vision.Component vision) =>
            {
                var movementVariables = m;
                var unitWindex = unitWorldIndex.Value;
                var actions = actionsData;
                var unitID = EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
                var pos = position;
                var vis = vision;
                var cor = coord;
                
                if (actionsData.LockedAction.Index == -2)
                {
                    var currentPath = actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs;
                    Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
                    {
                        if (unitWindex == gameStateWorldIndex.Value)
                        {
                            if (gameState.CurrentState == GameStateEnum.interrupt)
                            {
                                //PATH CULLING
                                actions = CompareCullableActions(actions, gameStateWorldIndex.Value, unitID);
                                //actionsDatas[i] = actionsData;
                            }
                            else if (gameState.CurrentState == GameStateEnum.move)
                            {
                                if (currentPath.Count != 0 /*&& cellsToMark.CellsInRange.Count != 0*/)
                                {
                                    //instead of lerping towards next pos in path, set pos and wait for client to catch up
                                    if (movementVariables.TimeLeft >= 0)
                                    {
                                        movementVariables.TimeLeft -= Time.deltaTime;
                                    }
                                    else
                                    {
                                        pos.Coords = new Coordinates(currentPath[0].WorldPosition.X, currentPath[0].WorldPosition.Y, currentPath[0].WorldPosition.Z);

                                        if (currentPath.Count == 1)
                                        {
                                            movementVariables.TimeLeft = 0;
                                        }
                                        else
                                        {
                                            movementVariables.TimeLeft = actions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell;
                                        }
                                        cor.CubeCoordinate = currentPath[0].CubeCoordinate;
                                        /*
                                        logger.HandleLog(LogType.Warning,
                                        new LogEvent("Set Unit ReqUpdate from MovementSystem")
                                        .WithField("AYAYA", "AYAYA"));
                                        */
                                        vis.RequireUpdate = true;
                                        currentPath.RemoveAt(0);
                                    }
                                }
                            }
                            /*else if (gameStates[gi].CurrentState == GameStateEnum.calculate_energy)
                            {
                                cellsToMark.CachedPaths = new Dictionary<CellAttribute, CellAttributeList>();
                                cellsToMark.CellsInRange = new List<CellAttributes>();
                                unitCellsToMarks[i] = cellsToMark;
                            }*/
                        }
                    });
                    position = pos;
                    m = movementVariables;
                    vision = vis;
                    coord = cor;
                    actionsData = actions;
                }
            });
        }

        private Actions.Component CompareCullableActions(Actions.Component unitActions, uint worldIndex, long unitId)
        {
            Entities.With(m_UnitData).ForEach((ref SpatialEntityId otherUnitId, ref Actions.Component otherUnitActions, ref WorldIndex.Component otherUnitWorldIndex) =>
            {
                if (worldIndex == otherUnitWorldIndex.Value && unitId != otherUnitId.EntityId.Id)
                {
                    if (otherUnitActions.LockedAction.Index != -3 && Vector3fext.ToUnityVector(otherUnitActions.LockedAction.Targets[0].TargetCoordinate) == Vector3fext.ToUnityVector(unitActions.LockedAction.Targets[0].TargetCoordinate))
                    {
                        if (otherUnitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                        {
                            //Decide which path to cull
                            float unitMoveTime = unitActions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell * unitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count;
                            float otherUnitMoveTime = otherUnitActions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell * otherUnitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count;
                            if (unitMoveTime == otherUnitMoveTime)
                            {
                                if (Random.Range(0, 2) == 0)
                                {
                                    unitActions = CullPath(unitActions);
                                }
                                //Debug.Log("MoveTimesTheSame");
                            }
                            else if (unitMoveTime > otherUnitMoveTime)
                            {
                                unitActions = CullPath(unitActions);
                                //Debug.Log("UnitSlower, Cull path");
                            }
                        }
                        else if (otherUnitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.spawn_unit)
                        {
                            //Debug.Log("SpawnCullPath");
                            unitActions = CullPath(unitActions);
                        }
                    }
                }
            });
            return unitActions;
        }

        private Actions.Component CullPath(Actions.Component unitActions)
        {
            var coordCount = unitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count;
            unitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.RemoveAt(coordCount - 1);
            coordCount = unitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count;

            if(coordCount != 0)
            {
                Vector3f newTarget = unitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[coordCount - 1].CubeCoordinate;
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

