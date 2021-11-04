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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithNone<WorldIndexShared>().ForEach((Entity e, in WorldIndex.Component entityWorldIndex) =>
        {
            EntityManager.AddSharedComponentData(e, new WorldIndexShared { Value = entityWorldIndex.Value });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithAll<WorldIndex.Component>().WithNone<WorldIndexShared>().ForEach((Entity e) =>
        {
           Debug.Log( EntityManager.GetName(e));
        })
        .WithoutBurst()
        .Run();

        if (m_NoWorldIndexSharedData.CalculateEntityCount() == 0)
            Debug.Log("No Entities without worldIndexShared found");
        else
            Debug.Log("Entity count without worldIndexShared = " + m_NoWorldIndexSharedData.CalculateEntityCount());



        return inputDeps;
    }
}
