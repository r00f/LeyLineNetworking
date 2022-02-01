using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;
using Generic;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Collections.Generic;
using Player;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HighlightingSystem))]
    public class CellMarkerSystem : ComponentSystem
    {
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;
        Settings settings;
        ComponentUpdateSystem m_ComponentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>(),
                ComponentType.ReadOnly<MapData.Component>()
            );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            settings = Resources.Load<Settings>("Settings");
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            if (m_GameStateData.CalculateEntityCount() != 1)
                return;

            var mapData = m_GameStateData.GetSingleton<MapData.Component>();
            var mapInitializedEvent = m_ComponentUpdateSystem.GetEventsReceived<ClientWorkerIds.MapInitializedEvent.Event>();

            if (mapInitializedEvent.Count > 0)
            {
                var visibilityDesc = new RenderMeshDescription(
                   settings.ShadowMarkerMesh,
                   settings.ShadowMarkerMat,
                   shadowCastingMode: ShadowCastingMode.Off,
                   layer: 12,
                   receiveShadows: false);

                var highlightingDesc = new RenderMeshDescription(
                   settings.ShadowMarkerMesh,
                   settings.TargetCellMat,
                   shadowCastingMode: ShadowCastingMode.Off,
                   layer: 3,
                   receiveShadows: false);

                InstanciateRenderMeshEntities(mapData, visibilityDesc, new ComponentTypes(typeof(LocalToWorld), typeof(CubeCoordinate.Component), typeof(UnlitMaterialColor), typeof(IsVisible), typeof(RequireVisibleUpdate)), -0.5f, "VisibilityCell");
                InstanciateRenderMeshEntities(mapData, highlightingDesc, new ComponentTypes(typeof(LocalToWorld), typeof(CubeCoordinate.Component), typeof(MarkerState), typeof(MouseState), typeof(UnlitMaterialColor)), 0.1f, "ReachableCell");
            }

            Entities.ForEach((Entity e, RenderMesh renderMesh, ref UnlitMaterialColor matColor, ref MarkerState markerState, ref CubeCoordinate.Component cubeCoord, ref RequireMarkerUpdate reqUpdate) =>
            {
                var r = renderMesh;

                if (markerState.NumberOfTargets > 0)
                {
                    matColor.Value = ColorToFloat4(settings.TurnStepLineColors[markerState.TurnStepIndex], markerState.NumberOfTargets * 0.1f);
                    r.layer = 14;
                }
                else
                    r.layer = 3;

                EntityManager.SetSharedComponentData(e, r);
                EntityManager.RemoveComponent<RequireMarkerUpdate>(e);
            });
        }

        public float4 ColorToFloat4(Color color, float alphaModifier = 0)
        {
            return new float4(color.r, color.g, color.b, color.a + alphaModifier);
        }

        void InstanciateRenderMeshEntities(MapData.Component mapData, RenderMeshDescription desc, ComponentTypes componentsToAdd, float yOffset, string name = "CellEntity", bool ignoreTaken = false)
        {
            var prototype = EntityManager.CreateEntity();

            #if UNITY_EDITOR
            EntityManager.SetName(prototype, name);
            #endif

            RenderMeshUtility.AddComponents(prototype, EntityManager, desc);

            EntityManager.AddComponents(prototype, componentsToAdd);

            foreach (MapCell c in mapData.CoordinateCellDictionary.Values)
            {
                if (ignoreTaken && c.IsTaken)
                    continue;

                var instance = EntityManager.Instantiate(prototype);

                EntityManager.SetComponentData(instance, new CubeCoordinate.Component
                {
                    CubeCoordinate = CellGridMethods.AxialToCube(c.AxialCoordinate)
                });

                EntityManager.SetComponentData(instance, new LocalToWorld
                {
                    Value = float4x4.Translate(new float3(c.Position.X, c.Position.Y + yOffset, c.Position.Z))
                });

                if (EntityManager.HasComponent<UnlitMaterialColor>(instance))
                {
                    EntityManager.SetComponentData(instance, new UnlitMaterialColor
                    {
                        Value = new float4(1, 1, 1, 0)
                    });
                }

                if (EntityManager.HasComponent<IsVisible>(instance))
                {
                    EntityManager.SetComponentData(instance, new IsVisible
                    {
                        Value = 0,
                        LerpSpeed = 0.5f,
                    });
                }

                if (EntityManager.HasComponent<MarkerState>(instance))
                {
                    EntityManager.SetComponentData(instance, new MarkerState
                    {
                        CurrentTargetType = MarkerState.TargetType.Neutral,
                        IsSet = 0,
                        TargetTypeSet = 0,
                        CurrentState = MarkerState.State.Neutral,
                        IsUnit = 0
                    });
                }
            }
            EntityManager.DestroyEntity(prototype);
        }
    }
}

