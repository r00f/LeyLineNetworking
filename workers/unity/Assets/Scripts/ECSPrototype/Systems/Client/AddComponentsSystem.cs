using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using LeyLineHybridECS;

public class AddComponentsSystem : ComponentSystem
{
    public struct CellData
    {
        public readonly int Length;
        public EntityArray Entites;
        [ReadOnly] public ComponentArray<Transform> Transform;
        [ReadOnly] public ComponentDataArray<Cells.CellAttributesComponent.Component> CellAttributesData;
        [ReadOnly] public ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
    }

    [Inject] private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public EntityArray Entites;
        [ReadOnly] public ComponentArray<Transform> Transform;
        [ReadOnly] public ComponentDataArray<Improbable.Gdk.Health.HealthComponent.Component> Health;
        [ReadOnly] public ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
    }

    [Inject] private UnitData m_UnitData;

    protected override void OnUpdate()
    {
        //adds IComponentData components to SpatialOS entities that don't need to be synced
        for (int i = 0; i < m_CellData.Length; i++)
        {
            var transform = m_CellData.Transform[i];
            var entity = m_CellData.Entites[i];

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

        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var transform = m_UnitData.Transform[i];
            var entity = m_UnitData.Entites[i];

            MouseState mouseState = new MouseState
            {
                CurrentState = MouseState.State.Neutral
            };

            PostUpdateCommands.AddComponent(entity, mouseState);
        }
    }
}
