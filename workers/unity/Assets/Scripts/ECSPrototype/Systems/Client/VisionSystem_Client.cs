﻿using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Cell;

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
            for (int i = m_Player.Length - 1; i >= 0; i--)
            {
                var playerVision = m_Player.VisionData[i];

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

                    visible.RequireUpdate = 1;
                    m_IsVisible.Visible[e] = visible;
                }
            }

            for (int i = 0; i < m_IsVisible.Length; i++)
            {
                var isVisibleComp = m_IsVisible.Visible[i];
                MeshRenderer meshRenderer = m_IsVisible.VisibleRef[i].MeshRenderer;
                GameObject go = m_IsVisible.VisibleRef[i].GO;
                byte isVisible = m_IsVisible.Visible[i].Value;
                //Debug.Log(isVisible);

                if (isVisibleComp.RequireUpdate == 1)
                {
                    if (isVisibleComp.LerpSpeed != 0)
                    {
                        if (isVisible == 0)
                        {
                            if (meshRenderer.material.color.a > 0)
                            {
                                meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a - isVisibleComp.LerpSpeed * Time.deltaTime);
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
                                meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a + isVisibleComp.LerpSpeed * Time.deltaTime);
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
