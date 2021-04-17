using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Cell;
using Improbable;
using Player;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class AISystem : ComponentSystem
{
    EntityQuery m_AiUnitData;
    EntityQuery m_PlayerUnitData;
    CommandSystem m_CommandSystem;
    PathFindingSystem m_PathFindingSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_AiUnitData = GetEntityQuery(
        ComponentType.ReadWrite<AiUnit.Component>(),
        ComponentType.ReadWrite<Actions.Component>(),
        ComponentType.ReadOnly<Vision.Component>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadWrite<CellsToMark.Component>()
        );

        var PlayerUnitDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(ManalithUnit.Component),
                typeof(AiUnit.Component)
            },
            All = new ComponentType[]
            {
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Health.Component>()
            }
        };

        m_PlayerUnitData = GetEntityQuery(PlayerUnitDesc);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
    }

    protected override void OnUpdate()
    {
        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

        if (cleanUpStateEvents.Count > 0)
        {
            UpdateAIUnits();
        }
    }

    public void UpdateAIUnits()
    {
        Entities.With(m_AiUnitData).ForEach((Entity e, ref AiUnit.Component aiUnit, ref Vision.Component vision, ref Actions.Component actions, ref SpatialEntityId AIid, ref CellsToMark.Component unitCellsToMark, ref CubeCoordinate.Component unitCoord) =>
        {
            var unitWorldIndex = EntityManager.GetComponentData<WorldIndex.Component>(e);

            var extrarange = 0;

            if (actions.ActionsList[1].Targets[0].Mods.Count != 0)
            {
                if (actions.ActionsList[1].Targets[0].Mods[0].ModType == ModTypeEnum.aoe)
                    extrarange = actions.ActionsList[1].Targets[0].Mods[0].AoeNested.Radius;
            }

            unitCellsToMark.CellsInRange.Clear();
            unitCellsToMark.CachedPaths.Clear();

            if (aiUnit.CurrentState == AiUnitStateEnum.idle || CellGridMethods.GetDistance(unitCoord.CubeCoordinate, aiUnit.AggroedUnitCoordinate) > actions.ActionsList[1].Targets[0].Targettingrange + extrarange)
            {
                aiUnit = RandomizeUnitAggression(vision, aiUnit, actions, AIid);
            }
            else
            {
                aiUnit = RefreshUnitAggression(vision, aiUnit, actions, AIid);
            }

            if (aiUnit.CurrentState == AiUnitStateEnum.aggroed || aiUnit.CurrentState == AiUnitStateEnum.chasing)
            {
                //ATTACK ACTION
                if (CellGridMethods.GetDistance(unitCoord.CubeCoordinate, aiUnit.AggroedUnitCoordinate) <= actions.ActionsList[1].Targets[0].Targettingrange + extrarange)
                {
                    actions.CurrentSelected = actions.ActionsList[1];

                    if (extrarange != 0)
                    {
                        for (uint i = 0; i < 6; i++)
                        {
                            if (CellGridMethods.GetDistance(CellGridMethods.CubeNeighbour(aiUnit.AggroedUnitCoordinate, i), unitCoord.CubeCoordinate) <= actions.ActionsList[1].Targets[0].Targettingrange)
                            {
                                var request = new Actions.SetTargetCommand.Request
                                (
                                    AIid.EntityId,
                                    new SetTargetRequest(CellGridMethods.CubeNeighbour(aiUnit.AggroedUnitCoordinate, i))
                                );
                                m_CommandSystem.SendCommand(request);
                            }
                        }
                    }
                    else
                    {
                        var request = new Actions.SetTargetCommand.Request
                        (
                            AIid.EntityId,
                            new SetTargetRequest(aiUnit.AggroedUnitCoordinate)
                        );
                        m_CommandSystem.SendCommand(request);
                    }
                }
                else
                {
                    actions.CurrentSelected = actions.ActionsList[0];
                    unitCellsToMark.CellsInRange = m_PathFindingSystem.GetRadius(unitCoord.CubeCoordinate, (uint)actions.CurrentSelected.Targets[0].Targettingrange, unitWorldIndex.Value);
                    unitCellsToMark.CachedPaths = m_PathFindingSystem.GetAllPathsInRadius((uint)actions.ActionsList[0].Targets[0].Targettingrange, unitCellsToMark.CellsInRange, unitCellsToMark.CellsInRange[0].Cell);

                    var request = new Actions.SetTargetCommand.Request
                    (
                        AIid.EntityId,
                        new SetTargetRequest(ClosestPathTarget(aiUnit.AggroedUnitCoordinate, unitCellsToMark.CachedPaths))
                    );
                    m_CommandSystem.SendCommand(request);
                }
            }
        });
    }

    public AiUnit.Component RandomizeUnitAggression(Vision.Component aiVision, AiUnit.Component aiUnit, Actions.Component act, SpatialEntityId aiId)
    {
        aiUnit.AggroedUnitCoordinate = new Vector3f(999, 999, 999);
        aiUnit.AggroedUnitId = 0;
        bool anyUnitinVisionRange = false;

        Entities.With(m_PlayerUnitData).ForEach((ref CubeCoordinate.Component coord, ref SpatialEntityId id) =>
        {
            if (aiVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
            {
                aiUnit.AggroedUnitCoordinate = coord.CubeCoordinate;
                aiUnit.AggroedUnitId = id.EntityId.Id;
                anyUnitinVisionRange = true;
            }
        });


        switch (aiUnit.CurrentState)
        {
            case AiUnitStateEnum.idle:
                if(anyUnitinVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.aggroed;
                break;
            case AiUnitStateEnum.aggroed:
                if (anyUnitinVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.chasing;
                else
                    aiUnit.CurrentState = AiUnitStateEnum.idle;
                break;
            case AiUnitStateEnum.chasing:
                if (!anyUnitinVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.idle;
                break;
            case AiUnitStateEnum.attacking:
                break;
            case AiUnitStateEnum.returning:
                break;
        }


        return aiUnit;
    }

    public AiUnit.Component RefreshUnitAggression(Vision.Component aiVision, AiUnit.Component aiUnit, Actions.Component act, SpatialEntityId aiId)
    {
        bool aggroedUnitExists = false;

        Entities.With(m_PlayerUnitData).ForEach((ref CubeCoordinate.Component coord, ref SpatialEntityId id) =>
        {
            if (aiUnit.AggroedUnitId == id.EntityId.Id && aiVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
            {
                aggroedUnitExists = true;
                aiUnit.AggroedUnitCoordinate = coord.CubeCoordinate;
            }
        });

        
        switch (aiUnit.CurrentState)
        {
            case AiUnitStateEnum.idle:
                break;
            case AiUnitStateEnum.aggroed:
                    aiUnit.CurrentState = AiUnitStateEnum.chasing;
                break;
            case AiUnitStateEnum.chasing:
                break;
            case AiUnitStateEnum.attacking:
                break;
            case AiUnitStateEnum.returning:
                break;
        }

        if (aggroedUnitExists)
            return aiUnit;
        else
            return RandomizeUnitAggression(aiVision, aiUnit, act, aiId);
    }

    public Vector3f ClosestPathTarget(Vector3f targetCoord, Dictionary<CellAttribute, CellAttributeList> inDict)
    {
        return inDict.Keys.OrderBy(i => CellGridMethods.GetDistance(targetCoord, i.CubeCoordinate)).ToList()[0].CubeCoordinate;
    }
}
