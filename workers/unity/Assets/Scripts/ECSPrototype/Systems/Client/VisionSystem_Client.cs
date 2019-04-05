using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using Improbable;
using Improbable.Gdk.Core;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(VisionSystem_Server))]
    public class VisionSystem_Client : ComponentSystem
    {
        struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<Player.PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<Generic.Vision.Component> VisionData;
        }
        [Inject]
        PlayerData m_Player;

        struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Generic.CubeCoordinate.Component> Coordinate;
            public ComponentDataArray<IsVisible> Visible;
            public ComponentArray<IsVisibleReferences> VisibleRef;
        }
        [Inject]
        CellData m_Cells;

        protected override void OnUpdate()
        {
            for (int i = m_Player.Length - 1; i >= 0; i--)
            {
                var Vision = m_Player.VisionData[i];

                for (int e = m_Cells.Length - 1; e >= 0; e--)
                {
                    var coordinate = m_Cells.Coordinate[e];
                    var visible = m_Cells.Visible[e];
                    visible.Value = 0;
                    foreach (Cells.CellAttributes c in Vision.CellsInVisionrange)
                    {
                        if (c.Cell.CubeCoordinate == coordinate.CubeCoordinate)
                        {
                            visible.Value = 1;

                        }
                    }

                    visible.RequireUpdate = 1;
                    m_Cells.Visible[e] = visible;
                }
            }
            for (int i = 0; i < m_Cells.Length; i++)
            {
                var c_IsVisible = m_Cells.Visible[i];
                MeshRenderer meshRenderer = m_Cells.VisibleRef[i].MeshRenderer;
                GameObject go = m_Cells.VisibleRef[i].GO;
                byte isVisible = m_Cells.Visible[i].Value;
                //Debug.Log(isVisible);

                if (c_IsVisible.RequireUpdate == 1)
                {
                    if (isVisible == 0)
                    {
                        if (meshRenderer.material.color.a > 0)
                        {
                            meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a - c_IsVisible.LerpSpeed * Time.deltaTime);
                        }
                        else
                        {
                                
                                go.SetActive(false);
                                c_IsVisible.RequireUpdate = 0;
                        }

                    }
                    else
                    {
                        go.SetActive(true);
                            
                        if (meshRenderer.material.color.a < 1)
                        {
                            meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a + c_IsVisible.LerpSpeed * Time.deltaTime);
                        }
                        else
                        {
                            c_IsVisible.RequireUpdate = 0;
                        }

                }
              }
            }
        }
    }
}
