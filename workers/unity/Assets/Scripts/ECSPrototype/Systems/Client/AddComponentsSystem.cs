using Cell;
using Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using LeyLineHybridECS;
using Player;
using Unit;
using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateAfter(typeof(SpawnUnitsSystem))]
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

    [Inject] CellAddedData m_CellAddedData;

    private struct UnitAddedData
    {
        public readonly int Length;
        public readonly EntityArray Entities;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates; 
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Transform> Transform;
        public readonly ComponentDataArray<Health.Component> UnitAttributeData;
        public ComponentArray<AnimatorComponent> AnimatorData;
        public SubtractiveComponent<WorldIndexStateData> WorldIndexState;
    }

    [Inject] UnitAddedData m_UnitAddedData;

    private struct PlayerAddedData
    {
        public readonly int Length;
        public readonly EntityArray Entities;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;

        public SubtractiveComponent<WorldIndexStateData> WorldIndexState;
    }

    [Inject] PlayerAddedData m_PlayerAddedData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
    }

    [Inject] PlayerData m_PlayerData;

    protected override void OnUpdate()
    {
        if (m_PlayerData.Length == 0)
            return;

        var authPlayerWorldIndex = m_PlayerData.WorldIndexData[0].Value;
        var playerFaction = m_PlayerData.Factions[0].Faction;

        if (authPlayerWorldIndex == 0)
            return;

        for (int i = 0; i < m_PlayerAddedData.Length; i++)
        {
            var pWorldIndex = m_PlayerAddedData.WorldIndexData[i];
            var entity = m_PlayerAddedData.Entities[i];

            if (pWorldIndex.Value == authPlayerWorldIndex)
            {
                HighlightingDataComponent highlightingData = new HighlightingDataComponent();
                PostUpdateCommands.AddComponent(entity, highlightingData);
            }

            PostUpdateCommands.AddComponent(m_PlayerAddedData.Entities[i], new WorldIndexStateData { WorldIndexState = pWorldIndex });
        }

        for (int i = 0; i < m_CellAddedData.Length; i++)
        {
            var cellWorldIndex = m_CellAddedData.WorldIndexData[i];
            var entity = m_CellAddedData.Entities[i];

            if (cellWorldIndex.Value == authPlayerWorldIndex)
            {
                IsVisible isVisible = new IsVisible
                {
                    Value = 0,
                    RequireUpdate = 1,
                    LerpSpeed = 0.5f,
                };

                MouseState mouseState = new MouseState
                {
                    CurrentState = MouseState.State.Neutral,
                };

                MouseVariables mouseVars = new MouseVariables
                {
                    Distance = 0.865f
                };

                MarkerState markerState = new MarkerState
                {
                    CurrentTargetType = MarkerState.TargetType.Neutral,
                    IsSet = 0,
                    TargetTypeSet = 0,
                    CurrentState = MarkerState.State.Neutral,
                    IsUnit = 0
                };

                PostUpdateCommands.AddComponent(entity, mouseVars);
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
            var faction = m_UnitAddedData.Factions[i].Faction;
            var coord = m_UnitAddedData.Coordinates[i].CubeCoordinate;
            var anim = m_UnitAddedData.AnimatorData[i];

            anim.LastStationaryCoordinate = coord;

            if (unitWorldIndex.Value == authPlayerWorldIndex)
            {
                MouseState mouseState = new MouseState
                {
                    CurrentState = MouseState.State.Neutral,
                    ClickEvent = 0
                };

                MouseVariables mouseVars = new MouseVariables
                {
                    yOffset = 1f,
                    Distance = 1.2f
                };

                IsVisible isVisible = new IsVisible();

                if (faction == playerFaction)
                {
                    isVisible.Value = 1;
                    isVisible.RequireUpdate = 1;
                }
                else
                {
                    isVisible.Value = 0;
                    isVisible.RequireUpdate = 1;
                }

                MarkerState markerState = new MarkerState
                {
                    CurrentTargetType = MarkerState.TargetType.Neutral,
                    IsSet = 0,
                    TargetTypeSet = 0,
                    CurrentState = MarkerState.State.Neutral,
                    IsUnit = 1
                };

                PostUpdateCommands.AddComponent(entity, mouseVars);
                PostUpdateCommands.AddComponent(entity, markerState);
                PostUpdateCommands.AddComponent(entity, mouseState);
                PostUpdateCommands.AddComponent(entity, isVisible);
            }

            PostUpdateCommands.AddComponent(m_UnitAddedData.Entities[i], new WorldIndexStateData { WorldIndexState = unitWorldIndex });
        }
    }
}
