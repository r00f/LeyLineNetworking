using Cell;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unit;
using Improbable;
using Unity.Collections;
using Improbable.Gdk.TransformSynchronization;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class VisionSystem_Client : ComponentSystem
    {
        ILogDispatcher logger;
        //EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        EntityQuery m_IsVisibleData;
        EntityQuery m_RequireVisibleUpdateData;
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
                ComponentType.ReadWrite<IsVisible>(),
                ComponentType.ReadWrite<IsVisibleReferences>()
                );

            m_RequireVisibleUpdateData = GetEntityQuery(
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
                {
                    ProjectorCamera = GameObject.FindGameObjectWithTag("Projector").GetComponent<Camera>();
                }
                else
                    return;
            }

            var updateVisionEvents = m_ComponentUpdateSystem.GetEventsReceived<Vision.UpdateClientVisionEvent.Event>();

            if(updateVisionEvents.Count > 0)
            {
                //Debug.Log("Update Vision Event");
                UpdateVision();
            }

            if(m_RequireVisibleUpdateData.CalculateEntityCount() > 0)
            {
                ProjectorCamera.enabled = true;
                
                //Debug.Log("Require Visible Update Data > 0");
                //REDUCED AMOUNT OF OBJECTS THAT ARE IN VISIBLEDATA 650 x if (isVisibleComp.RequireUpdate == 1) uses .5ms while doing nothing at all
                Entities.With(m_RequireVisibleUpdateData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component coord) =>
                {
                    if (isVisibleComp.LerpSpeed != 0)
                    {
                        if (isVisibleComp.Value == 0)
                        {
                            if (isVisibleGOs.InGameTileColor.a > 0)
                            {
                                isVisibleGOs.InGameTileColor.a -= isVisibleComp.LerpSpeed * Time.DeltaTime;
                                isVisibleGOs.MeshRenderer.material.SetColor("_UnlitColor", isVisibleGOs.InGameTileColor);
                            }
                            else
                            {
                                foreach (GameObject g in isVisibleGOs.GameObjects)
                                {
                                    g.SetActive(false);
                                }
                                PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                            }
                        }
                        else
                        {
                            foreach (GameObject g in isVisibleGOs.GameObjects)
                            {
                                g.SetActive(true);
                            }

                            if (isVisibleGOs.InGameTileColor.a < 1)
                            {
                                isVisibleGOs.InGameTileColor.a += isVisibleComp.LerpSpeed * Time.DeltaTime;
                                isVisibleGOs.MeshRenderer.material.SetColor("_UnlitColor", isVisibleGOs.InGameTileColor);
                            }
                            else
                            {
                                PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                            }
                        }
                    }
                    else
                    {
                        if (isVisibleComp.Value == 0)
                        {
                            foreach (GameObject g in isVisibleGOs.GameObjects)
                            {
                                g.SetActive(false);
                            }

                            if (isVisibleGOs.Collider)
                                isVisibleGOs.Collider.enabled = false;

                            PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                        }
                        else
                        {
                            foreach (GameObject g in isVisibleGOs.GameObjects)
                            {
                                g.SetActive(true);
                            }

                            if(isVisibleGOs.Collider)
                                isVisibleGOs.Collider.enabled = true;

                            PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                        }
                    }

                    if(isVisibleGOs.MiniMapTileInstance)
                        isVisibleGOs.MapTileColor.a = isVisibleGOs.MiniMapTileInstance.DarknessAlphaDefault - isVisibleGOs.InGameTileColor.a;

                    if (isVisibleGOs.MiniMapTileInstance && isVisibleGOs.MiniMapTileInstance.DarknessTileImage)
                    {
                        isVisibleGOs.MiniMapTileInstance.DarknessTileImage.color = isVisibleGOs.MapTileColor;

                        if (isVisibleGOs.MapTileColor.a >= 0)
                            isVisibleGOs.MiniMapTileInstance.DarknessTile.gameObject.SetActive(true);
                        else
                            isVisibleGOs.MiniMapTileInstance.DarknessTile.gameObject.SetActive(false);
                    }

                    if (isVisibleGOs.BigMapTileInstance && isVisibleGOs.BigMapTileInstance.DarknessTileImage)
                    {
                        isVisibleGOs.BigMapTileInstance.DarknessTileImage.color = isVisibleGOs.MapTileColor;

                        if (isVisibleGOs.MapTileColor.a >= 0)
                            isVisibleGOs.BigMapTileInstance.DarknessTile.gameObject.SetActive(true);
                        else
                            isVisibleGOs.BigMapTileInstance.DarknessTile.gameObject.SetActive(false);
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
                    if (playerVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
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

            Entities.With(m_IsVisibleData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component cubeCoord) =>
            {
                if(playerVision.CellsInVisionrange.ContainsKey(cubeCoord.CubeCoordinate))
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
