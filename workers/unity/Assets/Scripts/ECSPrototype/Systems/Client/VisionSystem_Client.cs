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
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
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

        protected override void OnCreate()
        {
            base.OnCreate();

            /*
            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>()
            );
            */

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
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            ProjectorCamera = GameObject.FindGameObjectWithTag("Projector").GetComponent<Camera>();
        }

        protected override void OnUpdate()
        {
            if (m_AuthorativePlayerData.CalculateEntityCount() == 0)
                return;

            //var gameStateData = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
            //var gameState = gameStateData[0];

            var updateVisionEvents = m_ComponentUpdateSystem.GetEventsReceived<Vision.UpdateClientVisionEvent.Event>();

            //if(gameState.CurrentState == GameStateEnum.move || gameState.CurrentState == GameStateEnum.cleanup)
                
            //else
                //ProjectorCamera.enabled = false;

            for (var i = 0; i < updateVisionEvents.Count; i++)
            {
                UpdateVision();
            }

            if(m_RequireVisibleUpdateData.CalculateEntityCount() > 0)
            {
                ProjectorCamera.enabled = true;
                //REDUCED AMOUNT OF OBJECTS THAT ARE IN VISIBLEDATA 650 x if (isVisibleComp.RequireUpdate == 1) uses .5ms while doing nothing at all
                Entities.With(m_RequireVisibleUpdateData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component coord) =>
                {
                    MeshRenderer meshRenderer = isVisibleGOs.MeshRenderer;
                    List<GameObject> gameObjects = isVisibleGOs.GameObjects;
                    Collider collider = isVisibleGOs.Collider;
                    byte isVisible = isVisibleComp.Value;

                    //use RequireVisibleUpdate flag comp instead of RequireUpdate check
                    //if (isVisibleComp.RequireUpdate == 1)
                    //{
                    Color color = new Color();

                    if (meshRenderer.material.HasProperty("_UnlitColor"))
                        color = meshRenderer.material.GetColor("_UnlitColor");

                    if (isVisibleComp.LerpSpeed != 0)
                    {
                        if (isVisible == 0)
                        {
                            if (meshRenderer.material.GetColor("_UnlitColor").a > 0)
                            {
                                color.a = meshRenderer.material.GetColor("_UnlitColor").a - isVisibleComp.LerpSpeed * Time.DeltaTime;
                                meshRenderer.material.SetColor("_UnlitColor", color);
                            }
                            else
                            {
                                foreach (GameObject g in gameObjects)
                                {
                                    g.SetActive(false);
                                }
                                //REMOVE FLAG
                                PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                                //isVisibleComp.RequireUpdate = 0;
                            }
                        }
                        else
                        {
                            foreach (GameObject g in gameObjects)
                            {
                                g.SetActive(true);
                            }

                            if (meshRenderer.material.GetColor("_UnlitColor").a < 1)
                            {
                                color.a = meshRenderer.material.GetColor("_UnlitColor").a + isVisibleComp.LerpSpeed * Time.DeltaTime;
                                meshRenderer.material.SetColor("_UnlitColor", color);
                            }
                            else
                            {
                                //REMOVE FLAG
                                PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);
                                //isVisibleComp.RequireUpdate = 0;
                            }
                        }
                    }
                    else
                    {
                        if (isVisible == 0)
                        {
                            foreach (GameObject g in gameObjects)
                            {
                                g.SetActive(false);
                            }
                            collider.enabled = false;
                            //REMOVE REQUIRE UPDATE FLAG COMPONENT
                            PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);

                            //isVisibleComp.RequireUpdate = 0;
                        }
                        else
                        {
                            foreach (GameObject g in gameObjects)
                            {
                                g.SetActive(true);
                            }
                            collider.enabled = true;
                            //REMOVE REQUIRE UPDATE FLAG COMPONENT
                            PostUpdateCommands.RemoveComponent<RequireVisibleUpdate>(e);

                            //isVisibleComp.RequireUpdate = 0;
                        }
                    }
                    //}
                });
            }
            else
            {
                ProjectorCamera.enabled = false;
            }


            //gameStateData.Dispose();
        }

        public void UpdateVision()
        {
            var playerVisions = m_AuthorativePlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
            var playerFactions = m_AuthorativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

            var playerVision = playerVisions[0];
            var playerFaction = playerFactions[0].Faction;

            HashSet<Vector3f> visionCoordsHash = new HashSet<Vector3f>(playerVision.CellsInVisionrange);

            Entities.With(m_UnitData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref FactionComponent.Component faction, ref CubeCoordinate.Component coord, ref IsVisible visible) =>
            {
                UpdateUnitMapTilePosition(coord.CubeCoordinate, ref isVisibleGOs);
                if (faction.Faction != playerFaction)
                {
                    if (visionCoordsHash.Contains(coord.CubeCoordinate))
                    {
                        if (isVisibleGOs.MiniMapTileInstance)
                        {
                            isVisibleGOs.MiniMapTileInstance.gameObject.SetActive(true);
                            if (isVisibleGOs.MiniMapTileInstance.UnitBecomeVisiblePingPS)
                                isVisibleGOs.MiniMapTileInstance.UnitBecomeVisiblePingPS.Play();
                        }
                        visible.Value = 1;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                    }
                    else
                    {
                        if (isVisibleGOs.MiniMapTileInstance)
                            isVisibleGOs.MiniMapTileInstance.gameObject.SetActive(false);
                        visible.Value = 0;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                    }

                }
            });

            Entities.With(m_IsVisibleData).ForEach((Entity e, IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component cubeCoord) =>
            {
                if(visionCoordsHash.Contains(cubeCoord.CubeCoordinate))
                {
                    if (isVisibleComp.Value == 0)
                    {
                        if (isVisibleGOs.MiniMapTileInstance)
                        {
                            isVisibleGOs.MiniMapTileInstance.TileImage.color = isVisibleGOs.MiniMapTileInstance.TileColor;
                        }
                        isVisibleComp.Value = 1;
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                        //EntityManager.AddComponentData(e,  RequireVisibleUpdate);
                        //ADD REQUIRE UPDATE FLAG COMPONENT
                        //isVisibleComp.RequireUpdate = 1;
                    }
                }
                else
                {
                    if (isVisibleComp.Value == 1)
                    {
                        if (isVisibleGOs.MiniMapTileInstance)
                            isVisibleGOs.MiniMapTileInstance.TileImage.color = isVisibleGOs.MiniMapTileInstance.TileInvisibleColor;
                        isVisibleComp.Value = 0;
                        //ADD REQUIRE UPDATE FLAG COMPONENT
                        PostUpdateCommands.AddComponent(e, new RequireVisibleUpdate());
                        //isVisibleComp.RequireUpdate = 1;
                    }
                }
            });

            playerFactions.Dispose();
            playerVisions.Dispose();
        }

        void UpdateUnitMapTilePosition(Vector3f coord, ref IsVisibleReferences isVisibleRef)
        {
            float offsetMultiplier = 5.8f;
            //Instantiate MiniMapTile into Map
            Vector3 pos = CellGridMethods.CubeToPos(coord, new Vector2f(0f, 0f));
            Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);
            if(isVisibleRef.MiniMapTileInstance)
                isVisibleRef.MiniMapTileInstance.TileRect.anchoredPosition = invertedPos;
        }

            /*
            public void UpdateVision()
            {
                var playerVisions = m_AuthorativePlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
                var playerVision = playerVisions[0];

                Entities.With(m_IsVisibleData).ForEach((IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component cubeCoord) =>
                {
                    var coord = Vector3fext.ToUnityVector(cubeCoord.CubeCoordinate);
                    byte isVisible = isVisibleComp.Value;

                    if (isVisibleComp.RequireUpdate == 0)
                    {
                        //only compare when serverSide playerVision is updated
                        if (isVisible == 0)
                        {
                            foreach (Vector3f c in playerVision.Positives)
                            {
                                if (Vector3fext.ToUnityVector(c) == coord)
                                {
                                    isVisibleComp.Value = 1;
                                    isVisibleComp.RequireUpdate = 1;
                                }
                            }
                        }
                        else
                        {
                            foreach (Vector3f c in playerVision.Negatives)
                            {
                                if (Vector3fext.ToUnityVector(c) == coord)
                                {
                                    isVisibleComp.Value = 0;
                                    isVisibleComp.RequireUpdate = 1;
                                }
                            }
                        }
                    }
                });

                playerVisions.Dispose();
            }
            */
        }
}
