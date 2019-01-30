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
        [ReadOnly] public ComponentArray<Transform> Transform;
        [ReadOnly] public ComponentDataArray<Cells.IsTaken.Component> IsTakenComponent;
        [ReadOnly] public ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
    }

    [Inject] private Data m_Data;

    protected override void OnUpdate()
    {
        //adds IComponentData components to SpatialOS entities that don't need to be synced
        for (int i = 0; i < m_Data.Length; i++)
        {
            var transform = m_Data.Transform[i];
            var entity = m_Data.Entites[i];

            IsVisible isVisible = new IsVisible
            {
                Value = 0,
                RequireUpdate = 1,
                LerpSpeed = 0.5f,
            };

            MouseState mouseState = new MouseState
            {
                CurrentState = MouseState.State.Neutral
            };

            MarkerState markerState = new MarkerState
            {
                CurrentState = MarkerState.State.Neutral
            };

            PostUpdateCommands.AddComponent(entity, mouseState);
            PostUpdateCommands.AddComponent(entity, markerState);
            PostUpdateCommands.AddComponent(entity, isVisible);

        }
    }
}
