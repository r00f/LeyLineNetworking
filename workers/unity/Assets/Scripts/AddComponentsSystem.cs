using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using LeyLineHybridECS;

public class AddComponentsSystem : ComponentSystem
{
    public struct Data
    {
        public readonly int Length;
        public EntityArray Entites;
        [ReadOnly] public ComponentDataArray<Cells.IsTaken.Component> EffectComponent;
        [ReadOnly] public ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
    }

    [Inject] private Data m_Data;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_Data.Length; i++)
        {
            var entity = m_Data.Entites[i];
            MouseState mouseState = new MouseState
            {
                CurrentState = MouseState.State.Neutral
            };
            PostUpdateCommands.AddComponent(entity, mouseState);
        }
    }
}
