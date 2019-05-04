using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Cell;
using Improbable.Gdk.ReactiveComponents;
using System.Collections.Generic;

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

        //List<CellAttributes> lastVisible = new List<CellAttributes>();

        protected override void OnUpdate()
        {
            var playerVision = m_Player.VisionData[0];
            
            for (int i = 0; i < m_IsVisible.Length; i++)
            {
                var isVisibleComp = m_IsVisible.Visible[i];
                MeshRenderer meshRenderer = m_IsVisible.VisibleRef[i].MeshRenderer;
                GameObject go = m_IsVisible.VisibleRef[i].GO;
                byte isVisible = m_IsVisible.Visible[i].Value;
                var coord = m_IsVisible.Coordinate[i];

                foreach(CellAttributes c in playerVision.Positives)
                {
                    if(c.Cell.CubeCoordinate == coord.CubeCoordinate && isVisible == 0)
                    {
                        isVisible = 1;
                        isVisibleComp.RequireUpdate = 1;
                    }
                }
                foreach (CellAttributes c in playerVision.Negatives)
                {
                    if (c.Cell.CubeCoordinate == coord.CubeCoordinate && isVisible == 1)
                    {
                        Debug.Log("Ayaya");
                        isVisible = 0;
                        isVisibleComp.RequireUpdate = 1;
                    }
                }

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
                                isVisibleComp.RequireUpdate = 0;
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
                                isVisibleComp.RequireUpdate = 0;
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
