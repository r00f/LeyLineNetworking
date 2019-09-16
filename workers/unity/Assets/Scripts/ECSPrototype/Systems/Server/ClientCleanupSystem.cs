using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unit;
using Generic;
using LeyLineHybridECS;

[DisableAutoCreation]
public class ClientCleanupSystem : ComponentSystem
{
    public struct GarbageCollectorData
    {
        public readonly int Length;
        public ComponentArray<GarbageCollectorComponent> GarbageData;
    }

    [Inject] GarbageCollectorData m_GarbageCollectorData;

    ComponentGroup GameStateGroup;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        GameStateGroup = Worlds.ClientWorld.CreateComponentGroup(
        ComponentType.Create<GameState.Component>()
        );

    }

    protected override void OnUpdate()
    {
        if (GameStateGroup.CalculateLength() == 0 || m_GarbageCollectorData.Length == 0)
        {
            Debug.Log("no GameState or GarbageCollector found");
            return;
        }

        var gameState = GameStateGroup.GetComponentDataArray<GameState.Component>()[0];
        var garbageCollector = m_GarbageCollectorData.GarbageData[0];

        if (gameState.CurrentState == GameStateEnum.planning)
        {
            if(garbageCollector.CurrentSinkTime >= 0)
            {
                garbageCollector.CurrentSinkTime -= Time.deltaTime;
                foreach (Rigidbody r in garbageCollector.GarbageRigidbodies)
                {
                    if(!r.isKinematic)
                        r.isKinematic = true;
                }

                foreach(GameObject g in garbageCollector.GarbageObjects)
                {
                    g.transform.position -= new Vector3(0, garbageCollector.SinkSpeed * Time.deltaTime, 0);
                }
            }
            else
            {
                for(int i = 0; i < garbageCollector.GarbageObjects.Count; i++)
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
        }
    }
}
