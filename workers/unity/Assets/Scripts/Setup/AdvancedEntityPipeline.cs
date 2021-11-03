using System;
using System.Collections.Generic;
using FMODUnity;
using Generic;
//using Fps.Movement;
//using Fps.SchemaExtensions;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using LeyLineHybridECS;
using Unity.Entities;
using UnityEngine;

public class AdvancedEntityPipeline : IEntityGameObjectCreator
{
    private const string PlayerEntityType = "Player";

    private readonly string workerType;
    private readonly Vector3 workerOrigin;

    private readonly Dictionary<EntityId, GameObject> gameObjectsCreated = new Dictionary<EntityId, GameObject>();

    public event Action OnRemovedAuthoritativePlayer;

    private readonly Type[] playerComponentsToAdd =
    {
            typeof(Transform),
            typeof(Moba_Camera),
            typeof(HeroTransform),
            typeof(PlayerEffects)
    };

    private readonly Type[] componentsToAdd =
    {
            typeof(Transform),
            typeof(Collider),
            typeof(UnitEffects),
            typeof(AnimatorComponent),
            typeof(MarkerGameObjects),
            typeof(UnitMarkerGameObjects),
            typeof(UnitDataSet),
            typeof(LineRendererComponent),
            typeof(MovementAnimComponent),
            typeof(IsVisibleReferences),
            typeof(TeamColorMeshes),
            typeof(UnitHeadUIReferences),
            typeof(AnimatedPortraitReference),
            typeof(UnitComponentReferences),
            typeof(GarbageCollectorComponent),
            typeof(UnitHeadUI),
            typeof(ManalithInitializer),
            typeof(MeshColor),
            typeof(MeshGradientColor),
            typeof(ManalithObject),
            typeof(StudioEventEmitter)
    };

    public AdvancedEntityPipeline(WorkerInWorld worker)
    {
        workerType = worker.WorkerType;
        workerOrigin = worker.Origin;
    }

    public void PopulateEntityTypeExpectations(EntityTypeExpectations entityTypeExpectations)
    {
        entityTypeExpectations.RegisterDefault(new[]
        {
                typeof(Position.Component)
            });

        entityTypeExpectations.RegisterEntityType(PlayerEntityType, new[]
        {
               typeof(OwningWorker.Component)
            });
    }

    public void OnEntityCreated(SpatialOSEntityInfo entityInfo, GameObject prefab, EntityManager entityManager, EntityGameObjectLinker linker)
    {
        Type[] compsToAdd;
        Vector3 spawnPosition;
        switch (entityInfo.EntityType)
        {
            case PlayerEntityType:
                compsToAdd = playerComponentsToAdd;
                spawnPosition = entityManager.GetComponentData<Position.Component>(entityInfo.Entity)
                    .Coords.ToUnityVector();
                break;
            default:
                compsToAdd = componentsToAdd;
                spawnPosition = entityManager.GetComponentData<Position.Component>(entityInfo.Entity)
                        .Coords.ToUnityVector();
                break;
        }

        var gameObject = UnityEngine.Object.Instantiate(prefab, spawnPosition + workerOrigin, Quaternion.identity);

        gameObjectsCreated.Add(entityInfo.SpatialOSEntityId, gameObject);
        gameObject.name = $"{prefab.name}(SpatialOS: {entityInfo.SpatialOSEntityId}, Worker: {workerType})";
        linker.LinkGameObjectToSpatialOSEntity(entityInfo.SpatialOSEntityId, gameObject, compsToAdd);
    }

    public void OnEntityRemoved(EntityId entityId)
    {
        if (!gameObjectsCreated.TryGetValue(entityId, out var gameObject))
        {
            return;
        }

        gameObjectsCreated.Remove(entityId);
        UnityEngine.Object.Destroy(gameObject);
    }

}
