using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using LeyLineHybridECS;
using System.Collections.Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem))]
public class AddComponentsSystem : ComponentSystem
{
    public struct CellData
    {
        public readonly int Length;
        public EntityArray Entites;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Transform> Transform;
        public readonly ComponentDataArray<Cells.CellAttributesComponent.Component> CellAttributesData;
        public readonly ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
    }

    [Inject] private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public EntityArray Entites;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Transform> Transform;
        public readonly ComponentDataArray<Improbable.Gdk.Health.HealthComponent.Component> Health;
        public readonly ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
    }

    [Inject] private UnitData m_UnitData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Authoritative<Player.PlayerState.Component>> AuthorativeData;
    }

    [Inject] private PlayerData m_PlayerData;

    protected override void OnUpdate()
    {
        var playerWorldIndex = m_PlayerData.WorldIndexData[0].Value;

        //adds IComponentData components to SpatialOS entities that don't need to be synced
        for (int i = 0; i < m_CellData.Length; i++)
        {
            var cellWorldIndex = m_CellData.WorldIndexData[i].Value;
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

            if (cellWorldIndex == playerWorldIndex)
            {
                PostUpdateCommands.AddComponent(entity, mouseState);
                PostUpdateCommands.AddComponent(entity, markerState);
                PostUpdateCommands.AddComponent(entity, isVisible);
            }
        }

        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;
            var transform = m_UnitData.Transform[i];
            var entity = m_UnitData.Entites[i];

            MouseState mouseState = new MouseState
            {
                CurrentState = MouseState.State.Neutral
            };

            if (unitWorldIndex == playerWorldIndex)
            {
                PostUpdateCommands.AddComponent(entity, mouseState);
            }
        }
    }
}
