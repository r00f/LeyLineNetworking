using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Cell;
using Improbable.Gdk.ReactiveComponents;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(VisionSystem_Server))]
    public class VisionSystem_Client : ComponentSystem
    {
        struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<Vision.Component> VisionData;
        }

        [Inject] PlayerData m_Player;

        struct IsVisibleData
        {
            public readonly int Length;
            public readonly ComponentDataArray<CubeCoordinate.Component> Coordinate;
            public ComponentDataArray<IsVisible> Visible;
            public ComponentArray<IsVisibleReferences> VisibleRef;
        }

        [Inject] IsVisibleData m_IsVisible;

        protected override void OnUpdate()
        {
            var playerVision = m_Player.VisionData[0];

            for (int e = m_IsVisible.Length - 1; e >= 0; e--)
            {
                var coordinate = m_IsVisible.Coordinate[e];
                var visible = m_IsVisible.Visible[e];

                visible.Value = 0;
                foreach (CellAttributes c in playerVision.CellsInVisionrange)
                {
                    if (c.Cell.CubeCoordinate == coordinate.CubeCoordinate)
                    {
                        visible.Value = 1;
                    }
                }
                //visible.RequireUpdate = 1;
                m_IsVisible.Visible[e] = visible;

            }

            for (int i = 0; i < m_IsVisible.Length; i++)
            {
                var isVisibleComp = m_IsVisible.Visible[i];
                MeshRenderer meshRenderer = m_IsVisible.VisibleRef[i].MeshRenderer;
                GameObject go = m_IsVisible.VisibleRef[i].GO;
                byte isVisible = m_IsVisible.Visible[i].Value;

                if (isVisibleComp.RequireUpdate == 1)
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
                                go.SetActive(false);
                                //isVisibleComp.RequireUpdate = 0;
                            }
                        }
                        else
                        {
                            go.SetActive(true);

                            if (meshRenderer.material.color.a < 1)
                            {
                                color.a = meshRenderer.material.color.a + isVisibleComp.LerpSpeed * Time.deltaTime;
                                meshRenderer.material.color = color;
                            }
                            else
                            {
                                //isVisibleComp.RequireUpdate = 0;
                            }
                        }
                    }
                    else
                    {
                        if (isVisible == 0)
                        {
                            go.SetActive(false);
                        }
                        else
                        {
                            go.SetActive(true);
                        }
                    }
                }
            }
        }
    }
}
