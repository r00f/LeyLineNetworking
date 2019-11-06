using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Unit;

public sealed class EntityArchetypes
{
    public static EntityArchetype UnitArchetype;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var entityManager = World.Active.EntityManager;

        UnitArchetype = entityManager.CreateArchetype(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadWrite<Actions.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>());
    }

}
