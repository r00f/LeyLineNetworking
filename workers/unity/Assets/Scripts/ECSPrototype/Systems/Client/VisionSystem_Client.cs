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
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        EntityQuery m_IsVisibleData;
        EntityQuery m_AuthorativePlayerData;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<GameState.Component>()
                );
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

            m_AuthorativePlayerData = GetEntityQuery(
                ComponentType.ReadOnly<Vision.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<PlayerState.ComponentAuthority>()
                );

            m_AuthorativePlayerData.SetFilter(PlayerState.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            if (m_GameStateData.CalculateEntityCount() == 0 || m_AuthorativePlayerData.CalculateEntityCount() == 0)
                return;

            //|| m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob)[0].CurrentState == GameStateEnum.planning ||

            var playerVisions = m_AuthorativePlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
            var playerFactions = m_AuthorativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

            var playerVision = playerVisions[0];
            var playerFaction = playerFactions[0].Faction;

            HashSet<Vector3f> visionCoordsHash = new HashSet<Vector3f>(playerVision.CellsInVisionrange);
            HashSet<Vector3f> positivesHash = new HashSet<Vector3f>(playerVision.Positives);
            HashSet<Vector3f> negativessHash = new HashSet<Vector3f>(playerVision.Negatives);

            Entities.With(m_UnitData).ForEach((ref FactionComponent.Component faction, ref CubeCoordinate.Component coord, ref IsVisible visible) =>
            {
                if (faction.Faction != playerFaction && visible.RequireUpdate == 0)
                {
                    if (visionCoordsHash.Contains(coord.CubeCoordinate))
                    {
                        visible.Value = 1;
                    }
                    else
                    {
                        visible.Value = 0;
                    }
                    visible.RequireUpdate = 1;
                }
            });

            Entities.With(m_IsVisibleData).ForEach((IsVisibleReferences isVisibleGOs, ref IsVisible isVisibleComp, ref CubeCoordinate.Component coord) =>
            {
                MeshRenderer meshRenderer = isVisibleGOs.MeshRenderer;
                List<GameObject> gameObjects = isVisibleGOs.GameObjects;
                Collider collider = isVisibleGOs.Collider;
                byte isVisible = isVisibleComp.Value;

                if (isVisibleComp.RequireUpdate == 0)
                {
                    //only compare when serverSide playerVision is updated
                    if (isVisible == 0)
                    {
                        if (positivesHash.Contains(coord.CubeCoordinate))
                        {
                            isVisibleComp.Value = 1;
                            isVisibleComp.RequireUpdate = 1;
                        }
                    }
                    else
                    {
                        if (negativessHash.Contains(coord.CubeCoordinate))
                        {
                            isVisibleComp.Value = 0;
                            isVisibleComp.RequireUpdate = 1;
                        }
                    }
                }
                else
                {
                    Color color = meshRenderer.material.color;

                    if (isVisibleComp.LerpSpeed != 0)
                    {
                        if (isVisible == 0)
                        {
                            if (meshRenderer.material.color.a > 0)
                            {
                                color.a = meshRenderer.material.color.a - isVisibleComp.LerpSpeed * Time.deltaTime;
                                meshRenderer.material.color = color;
                            }
                            else
                            {
                                foreach (GameObject g in gameObjects)
                                {
                                    g.SetActive(false);
                                }
                                isVisibleComp.RequireUpdate = 0;
                            }
                        }
                        else
                        {
                            foreach (GameObject g in gameObjects)
                            {
                                g.SetActive(true);
                            }

                            if (meshRenderer.material.color.a < 1)
                            {
                                color.a = meshRenderer.material.color.a + isVisibleComp.LerpSpeed * Time.deltaTime;
                                meshRenderer.material.color = color;
                            }
                            else
                            {
                                isVisibleComp.RequireUpdate = 0;
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
                            isVisibleComp.RequireUpdate = 0;
                        }
                        else
                        {
                            foreach (GameObject g in gameObjects)
                            {
                                g.SetActive(true);
                            }
                            collider.enabled = true;
                            isVisibleComp.RequireUpdate = 0;
                        }
                    }
                }
            });

            playerVisions.Dispose();
            playerFactions.Dispose();
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
