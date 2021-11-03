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
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithNone<WorldIndexShared, PlayerAttributes.Component>().ForEach((Entity e, in WorldIndex.Component entityWorldIndex) =>
        {
            //Debug.Log("Convert WorldIndex comp to WorldIndex Shared");
            EntityManager.AddSharedComponentData(e, new WorldIndexShared { Value = entityWorldIndex.Value });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();
        return inputDeps;
    }
}
