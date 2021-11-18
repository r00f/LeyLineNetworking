using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class InitializeWorldsSystem : JobComponentSystem
{
    EntityQuery m_NoWorldIndexSharedData;
    ILogDispatcher logger;

    protected override void OnCreate()
    {
        base.OnCreate();
        var noWorldIndexSharedDesc = new EntityQueryDesc
        {
            All  = new ComponentType[]
            {
            typeof(WorldIndex.Component)
            },

            None = new ComponentType[]
            {
            typeof(WorldIndexShared)
            }
        };
        m_NoWorldIndexSharedData = GetEntityQuery(noWorldIndexSharedDesc);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithNone<WorldIndexShared, PlayerAttributes.Component, NewlyAddedSpatialOSEntity>().ForEach((Entity e, in WorldIndex.Component entityWorldIndex) =>
        {
            /*
            if(EntityManager.HasComponent<GameState.Component>(e))
            {
                logger.HandleLog(LogType.Warning,
                new LogEvent("Add WorldIndexShared to gameState")
                .WithField("Index", entityWorldIndex.Value));
            }
            */
            EntityManager.AddSharedComponentData(e, new WorldIndexShared { Value = entityWorldIndex.Value });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        return inputDeps;
    }
}
