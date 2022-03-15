using Generic;
using Improbable.Gdk.Core;
using Player;
using Unity.Entities;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class VisionSystemClient : ComponentSystem
    {
        ILogDispatcher logger;
        EntityQuery m_UnitData;
        EntityQuery m_IsVisibleData;
        EntityQuery m_CellRequireVisibleUpdateData;
        EntityQuery m_UnitRequireVisibleUpdateData;
        EntityQuery m_AuthorativePlayerData;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        Camera ProjectorCamera;
        public UIReferences UIRef { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadWrite<IsVisible>()
                );

            m_IsVisibleData = GetEntityQuery(
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadWrite<IsVisible>()
                );

            m_CellRequireVisibleUpdateData = GetEntityQuery(
                ComponentType.ReadWrite<UnlitMaterialColor>(),
                ComponentType.ReadWrite<RequireVisibleUpdate>(),
                ComponentType.ReadWrite<IsVisible>()
            );

            m_UnitRequireVisibleUpdateData = GetEntityQuery(
                ComponentType.ReadWrite<RequireVisibleUpdate>(),
                ComponentType.ReadWrite<IsVisible>(),
                ComponentType.ReadWrite<IsVisibleReferences>()
            );

            m_AuthorativePlayerData = GetEntityQuery(
                ComponentType.ReadOnly<Vision.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<PlayerState.HasAuthority>()
                );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            UIRef = Object.FindObjectOfType<UIReferences>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            if (m_AuthorativePlayerData.CalculateEntityCount() == 0)
                return;

            if (!ProjectorCamera)
            {
                if (GameObject.FindGameObjectWithTag("Projector"))
                    ProjectorCamera = GameObject.FindGameObjectWithTag("Projector").GetComponent<Camera>();
                else
                    return;
            }

            var updateVisionEvents = m_ComponentUpdateSystem.GetEventsReceived<Vision.UpdateClientVisionEvent.Event>();

            if(updateVisionEvents.Count > 0)
                UpdateVision();

            if (m_UnitRequireVisibleUpdateData.CalculateEntityCount() > 0)
            {
                Entities.With(m_UnitRequireVisibleUpdateData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component coord) =>
                {
                    if (isVisibleComp.Value == 0)
                    {
                        foreach (GameObject g in isVisibleGOs.GameObjects)
                            g.SetActive(false);

                        if (isVisibleGOs.Collider)
                            isVisibleGOs.Collider.enabled = false;

                        PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                    }
                    else
                    {
                        foreach (GameObject g in isVisibleGOs.GameObjects)
                            g.SetActive(true);

                        if (isVisibleGOs.Collider)
                            isVisibleGOs.Collider.enabled = true;

                        PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                    }
                });
            }

            if (m_CellRequireVisibleUpdateData.CalculateEntityCount() > 0)
            {
                ProjectorCamera.enabled = true;

                Entities.With(m_CellRequireVisibleUpdateData).ForEach((Entity e, ref UnlitMaterialColor matColor, ref IsVisible isVisibleComp, ref CubeCoordinate.Component coord) =>
                {
                    if (isVisibleComp.Value == 0)
                    {
                        if (matColor.Value.w > 0)
                            matColor.Value.w -= isVisibleComp.LerpSpeed * Time.DeltaTime;
                        else
                            PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                    }
                    else
                    {
                        if (matColor.Value.w < 1)
                            matColor.Value.w += isVisibleComp.LerpSpeed * Time.DeltaTime;
                        else
                            PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                    }
                });
            }
            else
            {
                ProjectorCamera.enabled = false;
            }
        }

        public void UpdateVision()
        {
            var playerVision = m_AuthorativePlayerData.GetSingleton<Vision.Component>();
            var playerFaction = m_AuthorativePlayerData.GetSingleton<FactionComponent.Component>();

            Entities.With(m_UnitData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref CubeCoordinate.Component coord, ref IsVisible visible, ref FactionComponent.Component faction) =>
            {
                if (faction.Faction != playerFaction.Faction)
                {
                    if (playerVision.CellsInVisionrange.Contains(CellGridMethods.CubeToAxial(coord.CubeCoordinate)))
                    {
                        if (isVisibleGOs.MiniMapTileInstance && isVisibleGOs.MiniMapTileInstance.gameObject.activeSelf == false)
                        {
                            if (isVisibleGOs.MiniMapTileInstance.BecomeVisibleMapEffect && UIRef.MinimapComponent.isActiveAndEnabled)
                            {
                                var ping = Object.Instantiate(isVisibleGOs.MiniMapTileInstance.BecomeVisibleMapEffect, isVisibleGOs.MiniMapTileInstance.TileRect.position, Quaternion.identity, UIRef.MinimapComponent.MiniMapEffectsPanel.transform);
                                ParticleSystem.MainModule main = ping.ParticleSystem.main;
                                ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                                main.startColor = isVisibleGOs.MiniMapTileInstance.TileColor;
                                size.sizeMultiplier = UIRef.MinimapComponent.BecomeVisiblePingSize;
                                ping.ParticleSystem.Play();
                                Object.Destroy(ping.gameObject, 2f);
                            }
                            isVisibleGOs.MiniMapTileInstance.gameObject.SetActive(true);
                        }

                        if (isVisibleGOs.BigMapTileInstance && isVisibleGOs.BigMapTileInstance.gameObject.activeSelf == false)
                        {
                            if (isVisibleGOs.BigMapTileInstance.BecomeVisibleMapEffect && UIRef.BigMapComponent.isActiveAndEnabled)
                            {
                                var ping = Object.Instantiate(isVisibleGOs.BigMapTileInstance.BecomeVisibleMapEffect, isVisibleGOs.BigMapTileInstance.TileRect.position, Quaternion.identity, UIRef.BigMapComponent.MiniMapEffectsPanel.transform);
                                ParticleSystem.MainModule main = ping.ParticleSystem.main;
                                ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                                main.startColor = isVisibleGOs.MiniMapTileInstance.TileColor;
                                size.sizeMultiplier = UIRef.BigMapComponent.BecomeVisiblePingSize;
                                ping.ParticleSystem.Play();
                                ping.FMODEmitter.Play();
                                Object.Destroy(ping.gameObject, 2f);
                            }
                            isVisibleGOs.BigMapTileInstance.gameObject.SetActive(true);
                        }
                        visible.Value = 1;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                    }
                    else
                    {
                        if (isVisibleGOs.MiniMapTileInstance)
                            isVisibleGOs.MiniMapTileInstance.gameObject.SetActive(false);
                        if(isVisibleGOs.BigMapTileInstance)
                            isVisibleGOs.BigMapTileInstance.gameObject.SetActive(false);
                        visible.Value = 0;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                    }
                }
            });

            Entities.With(m_IsVisibleData).ForEach((Entity e, ref IsVisible isVisibleComp, ref CubeCoordinate.Component cubeCoord) =>
            {
                if(playerVision.CellsInVisionrange.Contains(CellGridMethods.CubeToAxial(cubeCoord.CubeCoordinate)))
                {
                    if (isVisibleComp.Value == 0)
                    {
                        isVisibleComp.Value = 1;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                    }
                }
                else
                {
                    if (isVisibleComp.Value == 1)
                    {
                        isVisibleComp.Value = 0;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                    }
                }
            });
        }
    }
}
