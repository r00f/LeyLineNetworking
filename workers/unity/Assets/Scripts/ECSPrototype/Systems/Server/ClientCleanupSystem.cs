using UnityEngine;
using Unity.Entities;
using Generic;
using Unity.Jobs;

public class ClientCleanupSystem : JobComponentSystem
{
    EntityQuery m_GarbageCollectorData;
    EntityQuery m_GameStateData;
    bool initialized;


    protected override void OnCreate()
    {
        base.OnCreate();

        m_GarbageCollectorData = GetEntityQuery(
        ComponentType.ReadWrite<GarbageCollectorComponent>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        initialized = false;
    }

    private bool WorldsInitialized()
    {
        if (!initialized)
        {
            if (Worlds.ClientWorldWorker != default && Worlds.ClientWorldWorker.World != null)
            {
                m_GameStateData = Worlds.ClientWorldWorker.World.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<GameState.Component>()
                );
                initialized = true;
            }
        }
        return initialized;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        
        if(WorldsInitialized())
        {
            if (m_GameStateData.CalculateEntityCount() != 1 || m_GarbageCollectorData.CalculateEntityCount() == 0)
            {
                return inputDeps;
            }

            var gameState = m_GameStateData.GetSingleton<GameState.Component>();
            var garbageCollectorEntity = m_GarbageCollectorData.GetSingletonEntity();
            var garbageCollector = EntityManager.GetComponentObject<GarbageCollectorComponent>(garbageCollectorEntity);

            if (gameState.CurrentState == GameStateEnum.planning)
            {
                if (garbageCollector.SinkDelay >= 0)
                {
                    garbageCollector.SinkDelay -= Time.DeltaTime;
                }
                else if (garbageCollector.CurrentSinkTime >= 0)
                {
                    garbageCollector.CurrentSinkTime -= Time.DeltaTime;
                    foreach (Rigidbody r in garbageCollector.GarbageRigidbodies)
                    {
                        if (!r.isKinematic)
                            r.isKinematic = true;
                    }

                    foreach (GameObject g in garbageCollector.GarbageObjects)
                    {
                        g.transform.position -= new Vector3(0, garbageCollector.SinkSpeed * Time.DeltaTime, 0);
                    }
                }
                else
                {
                    for (int i = 0; i < garbageCollector.GarbageObjects.Count; i++)
                    {
                        Object.Destroy(garbageCollector.GarbageObjects[i]);
                    }
                    garbageCollector.GarbageObjects.Clear();
                    garbageCollector.GarbageRigidbodies.Clear();
                }
            }
            else
            {
                if (garbageCollector.CurrentSinkTime != garbageCollector.MaxSinkTime)
                    garbageCollector.CurrentSinkTime = garbageCollector.MaxSinkTime;
                if (garbageCollector.SinkDelay != garbageCollector.MaxSinkDelay)
                    garbageCollector.SinkDelay = garbageCollector.MaxSinkDelay;
            }
        }

        return inputDeps;
    }
}
