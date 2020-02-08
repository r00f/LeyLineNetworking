using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;
using LeyLineHybridECS;
using Unity.Collections;

//[DisableAutoCreation]
public class ClientCleanupSystem : ComponentSystem
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

    private bool WorldsInitialized()
    {
        if (Worlds.ClientWorld != null)
        {
            if (!initialized)
            {
                m_GameStateData = Worlds.ClientWorld.CreateEntityQuery(
                    ComponentType.ReadOnly<GameState.Component>()
                );
                initialized = true;
            }
        }

        return initialized;
    }

    protected override void OnUpdate()
    {
        if(WorldsInitialized())
        {
            if (m_GameStateData.CalculateEntityCount() == 0 || m_GarbageCollectorData.CalculateEntityCount() == 0)
            {
                //Debug.Log("no GameState or GarbageCollector found");
                return;
            }

            var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
            var gameState = gameStates[0];
            var garbageCollector = m_GarbageCollectorData.ToComponentArray<GarbageCollectorComponent>()[0];

            if (gameState.CurrentState == GameStateEnum.planning)
            {
                if(garbageCollector.SinkDelay >= 0)
                {
                    garbageCollector.SinkDelay -= Time.deltaTime;
                }
                else if(garbageCollector.CurrentSinkTime >= 0)
                {
                    garbageCollector.CurrentSinkTime -= Time.deltaTime;
                    foreach (Rigidbody r in garbageCollector.GarbageRigidbodies)
                    {
                        if (!r.isKinematic)
                            r.isKinematic = true;
                    }

                    foreach (GameObject g in garbageCollector.GarbageObjects)
                    {
                        g.transform.position -= new Vector3(0, garbageCollector.SinkSpeed * Time.deltaTime, 0);
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

            gameStates.Dispose();
        }
    }
}
