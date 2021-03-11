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
            var aiVision = vision;
            var aiU = aiUnit;
            var act = actions;
            var aiId = AIid;

            var unitWorldIndex = EntityManager.GetComponentData<WorldIndex.Component>(e);

            aiUnit.AggroedUnitCoordinate = new Vector3f(0, 0, 0);
            aiUnit.AggroedUnitId = 0;
            aiU.IsAggroed = false;

            Entities.With(m_PlayerUnitData).ForEach((ref CubeCoordinate.Component coord, ref SpatialEntityId id) =>
            {
                if (aiVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
                {
                    aiU.AggroedUnitCoordinate = coord.CubeCoordinate;
                    aiU.AggroedUnitId = id.EntityId.Id;
                    aiU.IsAggroed = true;
                }
            });

            //if playerunit is in range of attack
            if(CellGridMethods.GetDistance(unitCoord.CubeCoordinate, aiU.AggroedUnitCoordinate) <= actions.ActionsList[1].Targets[0].Targettingrange)
            {
                actions.CurrentSelected = actions.ActionsList[1];
            }
            //else walk towards playerunit
            else
            {
                actions.CurrentSelected = actions.ActionsList[0];
                unitCellsToMark.CellsInRange = m_PathFindingSystem.GetRadius(unitCoord.CubeCoordinate, (uint) actions.CurrentSelected.Targets[0].Targettingrange, unitWorldIndex.Value);
                unitCellsToMark.CachedPaths = m_PathFindingSystem.GetAllPathsInRadiusIgnoringTaken((uint) actions.ActionsList[0].Targets[0].Targettingrange, unitCellsToMark.CellsInRange, unitCellsToMark.CellsInRange[0].Cell);
            }

            var request = new Actions.SetTargetCommand.Request
            (
                aiId.EntityId,
                new SetTargetRequest(aiU.AggroedUnitId)
            );

            m_CommandSystem.SendCommand(request);

            aiUnit = aiU;

        });
    }
}
