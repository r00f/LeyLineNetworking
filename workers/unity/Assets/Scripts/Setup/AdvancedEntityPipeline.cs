using System;
using System.Collections.Generic;
using System.IO;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.StandardTypes;
using Improbable.Gdk.Subscriptions;
using LeyLineHybridECS;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

public class AdvancedEntityPipeline : IEntityGameObjectCreator
{
    private const string GameobjectNameFormat = "{0}(SpatialOS {1}, Worker: {2})";
    private const string WorkerAttributeFormat = "workerId:{0}";
    private const string PlayerMetadata = "Player";

    private readonly GameObject cachedAuthPlayer;
    private readonly GameObject cachedNonAuthPlayer;

    private readonly IEntityGameObjectCreator fallback;
    private readonly string workerIdAttribute;
    private readonly Worker worker;

    private readonly Dictionary<EntityId, GameObject> gameObjectsCreated = new Dictionary<EntityId, GameObject>();

    private readonly Dictionary<string, GameObject> cachedPrefabs
    = new Dictionary<string, GameObject>();

    private readonly Vector3 workerOrigin;

    private readonly ILogDispatcher logger;


    private readonly Type[] componentsToAdd =
    {
    typeof(Transform),
    typeof(HeroTransform),
    typeof(MarkerGameObjects),
    typeof(UnitMarkerGameObjects),
    typeof(Unit_BaseDataSet),
    typeof(LineRendererComponent),
    typeof(AnimatorComponent),
    typeof(MovementAnimComponent),
    typeof(IsVisibleReferences),
    typeof(TeamColorMeshes),
    typeof(Healthbar),
    typeof(AnimatedPortraitReference),
    typeof(UnitComponentReferences),
    typeof(Moba_Camera),
    typeof(GarbageCollectorComponent)
    };

    public AdvancedEntityPipeline(Worker worker, string authPlayer, string nonAuthPlayer,
        IEntityGameObjectCreator fallback)
    {
        this.worker = worker;
        this.fallback = fallback;
        workerIdAttribute = string.Format(WorkerAttributeFormat, worker.WorkerId);
        cachedAuthPlayer = Resources.Load<GameObject>(authPlayer);
        cachedNonAuthPlayer = Resources.Load<GameObject>(nonAuthPlayer);
    }

    public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
    {
        if (!entity.HasComponent<Metadata.Component>())
        {
            return;
        }

        var prefabName = entity.GetComponent<Metadata.Component>().EntityType;

        if (prefabName.Equals(PlayerMetadata))
        {
            var playerState = entity.GetComponent<Player.PlayerState.Component>();
            if (entity.GetComponent<EntityAcl.Component>().ComponentWriteAcl
                .TryGetValue(playerState.ComponentId, out var playerStateWrite))
            {
                var authority = false;

                foreach (var attributeSet in playerStateWrite.AttributeSet)
                {
                    if (attributeSet.Attribute.Contains(workerIdAttribute))
                    {
                        authority = true;
                    }
                }

                var position = worker.Origin;
                var prefab = authority ? cachedAuthPlayer : cachedNonAuthPlayer;
                var gameObject = Object.Instantiate(prefab, position, Quaternion.identity);
                gameObject.name = GetGameObjectName(prefab, entity, worker);
                linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, componentsToAdd);
                return;
            }
        }
        else
        {
            if (!entity.HasComponent<Position.Component>())
            {
                cachedPrefabs[prefabName] = null;
                return;
            }

            var spatialOSPosition = entity.GetComponent<Position.Component>();
            var position = new Vector3((float)spatialOSPosition.Coords.X, (float)spatialOSPosition.Coords.Y, (float)spatialOSPosition.Coords.Z) +
                workerOrigin;
            var workerSpecificPath = Path.Combine("Prefabs", worker.WorkerType, prefabName);
            var commonPath = Path.Combine("Prefabs", "Common", prefabName);

            if (!cachedPrefabs.TryGetValue(prefabName, out var prefab))
            {
                prefab = Resources.Load<GameObject>(workerSpecificPath);
                if (prefab == null)
                {
                    prefab = Resources.Load<GameObject>(commonPath);
                }

                cachedPrefabs[prefabName] = prefab;
            }

            if (prefab == null)
            {
                return;
            }

            var gameObject = Object.Instantiate(prefab, position, Quaternion.identity);
            gameObject.name = $"{prefab.name}(SpatialOS: {entity.SpatialOSEntityId}, Worker: {worker.WorkerType})";

            gameObjectsCreated.Add(entity.SpatialOSEntityId, gameObject);
            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, componentsToAdd);
        }
    }

    private static string GetGameObjectName(GameObject prefab, SpatialOSEntity entity, Worker worker)
    {
        return string.Format(GameobjectNameFormat, prefab.name, entity.SpatialOSEntityId, worker.WorkerType);
    }

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
}

/*

    if (prefabName.Equals(PlayerMetadata))
    {
        var playerState = entity.GetComponent<Player.PlayerState.Component>();
        if (entity.GetComponent<EntityAcl.Component>().ComponentWriteAcl
            .TryGetValue(playerState.ComponentId, out var playerStateWrite))
        {
            var authority = false;

            foreach (var attributeSet in playerStateWrite.AttributeSet)
            {
                if (attributeSet.Attribute.Contains(workerIdAttribute))
                {
                    authority = true;
                }
            }

            var position = worker.Origin;
            var prefab = authority ? cachedAuthPlayer : cachedNonAuthPlayer;
            var gameObject = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            gameObject.name = GetGameObjectName(prefab, entity, worker);
            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, componentsToAdd);
            return;
        }
    }
*/
