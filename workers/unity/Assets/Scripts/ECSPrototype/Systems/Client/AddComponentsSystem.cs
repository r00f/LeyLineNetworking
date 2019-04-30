using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cell;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem))]
public class AddComponentsSystem : ComponentSystem
{
    public struct WorldIndexStateData : ISystemStateComponentData
    {
        public WorldIndex.Component WorldIndexState;
    }

    private struct CellAddedData
    {
        public readonly int Length;
        public readonly EntityArray Entities;

        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Transform> Transform;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributesData;
        public SubtractiveComponent<WorldIndexStateData> WorldIndexState;
    }

    [Inject] private CellAddedData m_CellAddedData;

    private struct UnitAddedData
    {
        public readonly int Length;
        public readonly EntityArray Entities;

        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Transform> Transform;
        public readonly ComponentDataArray<Health.Component> UnitAttributeData;
        public SubtractiveComponent<WorldIndexStateData> WorldIndexState;
    }

    [Inject] private UnitAddedData m_UnitAddedData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Authoritative<Player.PlayerState.Component>> AuthorativeData;
    }

    [Inject] private PlayerData m_PlayerData;

    protected override void OnUpdate()
    {
        if (m_PlayerData.Length == 0)
            return;

        var playerWorldIndex = m_PlayerData.WorldIndexData[0].Value;

        if (playerWorldIndex == 0)
            return;

        for (int i = 0; i < m_CellAddedData.Length; i++)
        {
            var cellWorldIndex = m_CellAddedData.WorldIndexData[i];
            var entity = m_CellAddedData.Entities[i];

            if (cellWorldIndex.Value == playerWorldIndex)
            {
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

            PostUpdateCommands.AddComponent(m_CellAddedData.Entities[i], new WorldIndexStateData { WorldIndexState = cellWorldIndex });
        }
        
        for (int i = 0; i < m_UnitAddedData.Length; i++)
        {
            var unitWorldIndex = m_UnitAddedData.WorldIndexData[i];
            var entity = m_UnitAddedData.Entities[i];

            if (unitWorldIndex.Value == playerWorldIndex)
            {
                MouseState mouseState = new MouseState
                {
                    CurrentState = MouseState.State.Neutral
                };

                IsVisible isVisible = new IsVisible
                {
                    Value = 0,
                    RequireUpdate = 1,
                    LerpSpeed = 0f,
                };

                PostUpdateCommands.AddComponent(entity, mouseState);
                PostUpdateCommands.AddComponent(entity, isVisible);
            }

            PostUpdateCommands.AddComponent(m_UnitAddedData.Entities[i], new WorldIndexStateData { WorldIndexState = unitWorldIndex });
        }
    }
}
