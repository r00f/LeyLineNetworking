using System;
using System.Collections.Generic;
using System.IO;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
//using Improbable.Gdk.StandardTypes;
using Improbable.Gdk.Subscriptions;
using LeyLineHybridECS;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

public class AdvancedEntityPipeline : IEntityGameObjectCreator
{
    private const string PlayerEntityType = "Player";

    private readonly GameObject cachedAuthPlayer;
    private readonly GameObject cachedNonAuthPlayer;

    private readonly GameObjectCreatorFromMetadata fallback;

    private readonly string workerId;
    private readonly string workerType;
    private readonly Vector3 workerOrigin;
    private readonly Worker worker;

    private readonly Dictionary<EntityId, GameObject> gameObjectsCreated = new Dictionary<EntityId, GameObject>();

    private readonly Dictionary<string, GameObject> cachedPrefabs
        = new Dictionary<string, GameObject>();

    public event Action OnRemovedAuthoritativePlayer;

    private readonly Type[] playerComponentsToAdd =
    {
        typeof(Transform),
        typeof(Moba_Camera),
        typeof(HeroTransform)
    };


    private readonly Type[] componentsToAdd =
    {
    typeof(Transform),
    typeof(Collider),
    typeof(UnitEffects),
    typeof(AnimatorComponent),
    typeof(MarkerGameObjects),
    typeof(UnitMarkerGameObjects),
    typeof(Unit_BaseDataSet),
    typeof(LineRendererComponent),
    typeof(MovementAnimComponent),
    typeof(IsVisibleReferences),
    typeof(TeamColorMeshes),
    typeof(UnitHeadUIReferences),
    typeof(AnimatedPortraitReference),
    typeof(UnitComponentReferences),
    typeof(GarbageCollectorComponent),
    typeof(UnitHeadUI)
    };

    public AdvancedEntityPipeline(WorkerInWorld worker, string authPlayer, string nonAuthPlayer)
    {
        this.worker = worker;
        workerId = worker.WorkerId;
        workerType = worker.WorkerType;
        workerOrigin = worker.Origin;

        fallback = new GameObjectCreatorFromMetadata(workerType, workerOrigin, worker.LogDispatcher);
        cachedAuthPlayer = Resources.Load<GameObject>(authPlayer);
        cachedNonAuthPlayer = Resources.Load<GameObject>(nonAuthPlayer);
    }

    public void OnEntityCreated(string entityType, SpatialOSEntity entity, EntityGameObjectLinker linker)
    {
        switch (entityType)
        {
            case "Player":
                CreatePlayerGameObject(entity, linker);
                break;
            default:
                CreateDefaultGameObject(entity, linker, entityType);
                break;
        }
    }

    private void CreateDefaultGameObject(SpatialOSEntity entity, EntityGameObjectLinker linker, string entityType)
    {

        if (!entity.HasComponent<Position.Component>())
        {
            cachedPrefabs[entityType] = null;
            return;
        }

        var spatialOSPosition = entity.GetComponent<Position.Component>();
        var position = new Vector3((float)spatialOSPosition.Coords.X, (float)spatialOSPosition.Coords.Y, (float)spatialOSPosition.Coords.Z) +
            workerOrigin;
        var workerSpecificPath = Path.Combine("Prefabs", worker.WorkerType, entityType);
        var commonPath = Path.Combine("Prefabs", "Common", entityType);

        if (!cachedPrefabs.TryGetValue(entityType, out var prefab))
        {
            prefab = Resources.Load<GameObject>(workerSpecificPath);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(commonPath);
            }

            cachedPrefabs[entityType] = prefab;
        }

        if (prefab == null)
        {
            return;
        }

        var gameObject = Object.Instantiate(prefab, position, Quaternion.identity);

        if(gameObject.GetComponent<IsVisibleReferences>())
        {
            foreach (GameObject g in gameObject.GetComponent<IsVisibleReferences>().GameObjects)
                g.SetActive(false);
        }

        gameObject.name = $"{prefab.name}(SpatialOS: {entity.SpatialOSEntityId}, Worker: {worker.WorkerType})";

        gameObjectsCreated.Add(entity.SpatialOSEntityId, gameObject);
        linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, componentsToAdd);
    }

    private void CreatePlayerGameObject(SpatialOSEntity entity, EntityGameObjectLinker linker)
    {
        var owningWorker = entity.GetComponent<OwningWorker.Component>();
        //var serverPosition = entity.GetComponent<ServerMovement.Component>();

        //var position = serverPosition.Latest.Position.ToVector3() + workerOrigin;

        var prefab = owningWorker.WorkerId == workerId ? cachedAuthPlayer : cachedNonAuthPlayer;
        var gameObject = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);

        gameObjectsCreated.Add(entity.SpatialOSEntityId, gameObject);
        gameObject.name = $"{prefab.name}(SpatialOS {entity.SpatialOSEntityId}, Worker: {workerType})";
        linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, playerComponentsToAdd);
    }

    /*
    private static string GetGameObjectName(GameObject prefab, SpatialOSEntity entity, Worker worker)
    {
        return string.Format(GameobjectNameFormat, prefab.name, entity.SpatialOSEntityId, worker.WorkerType);
    }
    */

    public void OnEntityRemoved(EntityId entityId)
    {
        if (!gameObjectsCreated.TryGetValue(entityId, out var go))
        {
            fallback.OnEntityRemoved(entityId);
            return;
        }

        gameObjectsCreated.Remove(entityId);
        Object.Destroy(go);
    }

    public void PopulateEntityTypeExpectations(EntityTypeExpectations entityTypeExpectations)
    {
        
        entityTypeExpectations.RegisterEntityType(PlayerEntityType, new[]
        {
        typeof(OwningWorker.Component)
        });
        
        fallback.PopulateEntityTypeExpectations(entityTypeExpectations);
    }
}

