using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Improbable.Gdk.PlayerLifecycle;

public sealed class EntityArchetypes
{
    public static EntityArchetype UnitArchetype;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var entityManager = World.AllWorlds[0].EntityManager;

        UnitArchetype = entityManager.CreateArchetype(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadWrite<Actions.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadOnly<OwningWorker.Component>()
        );

    }

}
