using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Improbable;
using Unity.Jobs;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class MovementSystem : JobComponentSystem
    {
        ILogDispatcher logger;
        /*
EntityQuery m_GameStateData;
EntityQuery m_CellData;
EntityQuery m_UnitData;

protected override void OnCreate()
{
    base.OnCreate();

    m_GameStateData = GetEntityQuery(

    ComponentType.ReadOnly<GameState.Component>()
    );

    m_UnitData = GetEntityQuery(
    ComponentType.ReadOnly<SpatialEntityId>(),

    ComponentType.ReadWrite<Position.Component>(),
    ComponentType.ReadWrite<Actions.Component>(),
    ComponentType.ReadWrite<MovementVariables.Component>(),
    ComponentType.ReadWrite<CubeCoordinate.Component>(),
    ComponentType.ReadWrite<Vision.Component>(),
    ComponentType.ReadWrite<Energy.Component>()
    );
}
*/

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            /*
            Entities.ForEach(( in GameState.Component gameState) =>
            {
                if(gameState.CurrentState != GameStateEnum.planning)
                    UnitLoop(gameStateWorldIndex.Value, gameState);
            })
            .WithoutBurst()
            .Run();
            */

            /*
Entities.WithAll<StartExecuteTag>().ForEach((Entity e, ref Actions.Component actionsData, in SpatialEntityId unitId) =>
{
    if (actionsData.LockedAction.Index != -3 && actionsData.LockedAction.ActionExecuteStep == ExecuteStepEnum.move)
    {
        if (actionsData.LockedAction.Targets[0].Mods.Count != 0 && actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count != 0)
        {
            Debug.Log("CullMoveAction");
            actionsData = CompareCullableActions(actionsData, worldIndex.Value, unitId.EntityId.Id);
        }
    }
})
.WithoutBurst()
.Run();

var deltaTime = Time.DeltaTime;


JobHandle a = Entities.WithAll<MoveTag>().ForEach((Entity e, ref MovementVariables.Component m, ref Actions.Component actionsData, ref Energy.Component energy, ref Position.Component position, ref CubeCoordinate.Component coord, ref Vision.Component vision) =>
{
    if (actionsData.LockedAction.ActionExecuteStep == ExecuteStepEnum.move && actionsData.LockedAction.Targets[0].Mods.Count != 0 && actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count != 0)
    {
        energy.Harvesting = false;

        if (actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count != 0)
        {
            //instead of lerping towards next pos in path, set pos and wait for client to catch up
            if (m.TimeLeft >= 0)
            {
                m.TimeLeft -= deltaTime;
            }
            else
            {
                position.Coords = new Coordinates(actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].WorldPosition.X, actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].WorldPosition.Y, actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].WorldPosition.Z);

                if (actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count == 1)
                {
                    m.TimeLeft = 0;
                }
                else
                {
                    m.TimeLeft = actionsData.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell;
                }

                coord.CubeCoordinate = actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].CubeCoordinate;

                logger.HandleLog(LogType.Warning,
                new LogEvent("Set Unit ReqUpdate from MovementSystem")
                .WithField("AYAYA", "AYAYA"));

                vision.RequireUpdate = true;
                actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.RemoveAt(0);
            }
        }
    }
})
.Schedule(inputDeps);
*/

            return inputDeps;
        }

        /*
        public void UnitLoop(uint WorldIndex, GameState.Component gameState)
        {
            Entities.ForEach((Entity e, ref MovementVariables.Component m, ref Actions.Component actionsData, ref Energy.Component energy, ref Position.Component position, ref CubeCoordinate.Component coord, ref Vision.Component vision, in SpatialEntityId unitId) =>
            {

                if (actionsData.LockedAction.Index != -3 && actionsData.LockedAction.ActionExecuteStep == ExecuteStepEnum.move)
                {
                    if (actionsData.LockedAction.Targets[0].Mods.Count != 0 && actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count != 0)
                    {
                        if (gameState.CurrentState == GameStateEnum.interrupt)
                        {
                            actionsData = CompareCullableActions(actionsData, WorldIndex, unitId.EntityId.Id);
                        }
                        else if (gameState.CurrentState == GameStateEnum.move)
                        {
                            energy.Harvesting = false;

                            if (actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count != 0)
                            {
                                //instead of lerping towards next pos in path, set pos and wait for client to catch up
                                if (m.TimeLeft >= 0)
                                {
                                    m.TimeLeft -= Time.DeltaTime;
                                }
                                else
                                {
                                    position.Coords = new Coordinates(actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].WorldPosition.X, actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].WorldPosition.Y, actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].WorldPosition.Z);

                                    if (actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count == 1)
                                    {
                                        m.TimeLeft = 0;
                                    }
                                    else
                                    {
                                        m.TimeLeft = actionsData.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell;
                                    }

                                    coord.CubeCoordinate = actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs[0].CubeCoordinate;
                                    
                                    logger.HandleLog(LogType.Warning,
                                    new LogEvent("Set Unit ReqUpdate from MovementSystem")
                                    .WithField("AYAYA", "AYAYA"));
                                    
                                    vision.RequireUpdate = true;
                                    actionsData.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.RemoveAt(0);
                                }
                            }
                        }
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }


        private Actions.Component CompareCullableActions(Actions.Component unitActions, uint worldIndex, long unitId)
        {
            Entities.ForEach((ref Actions.Component otherUnitActions, in SpatialEntityId otherUnitId) =>
            {
                if (worldIndex == otherUnitWorldIndex.Value && unitId != otherUnitId.EntityId.Id && otherUnitActions.LockedAction.Index != -3 && unitActions.LockedAction.Index != -3)
                {
                    if (Vector3fext.ToUnityVector(otherUnitActions.LockedAction.Targets[0].TargetCoordinate) == Vector3fext.ToUnityVector(unitActions.LockedAction.Targets[0].TargetCoordinate))
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
                            }
                            else if (unitMoveTime > otherUnitMoveTime)
                            {
                                unitActions = CullPath(unitActions);
                            }
                        }
                        else if (otherUnitActions.LockedAction.Effects[0].EffectType == EffectTypeEnum.spawn_unit)
                        {
                            unitActions = CullPath(unitActions);
                        }
                    }
                }
            })
            .WithoutBurst()
            .Run();
            return unitActions;
        }

        private Actions.Component CullPath(Actions.Component unitActions)
        {
            var coordCount = 0;
            if(unitActions.LockedAction.Targets.Count != 0)
            {
                if (unitActions.LockedAction.Targets[0].Mods.Count != 0)
                    coordCount = unitActions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count;
            }

            if (coordCount != 0)
            {
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

            }
            else
            {
                unitActions.LockedAction = unitActions.NullAction;
            }
            return unitActions;
        }
                */
    }
}

