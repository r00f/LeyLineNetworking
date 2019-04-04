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
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class VisionSystem_Client : ComponentSystem
    {
        struct playerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<Player.PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<Generic.Vision.Component> VisionData;
        }
        [Inject]
        playerData m_Player;
        struct cellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Generic.CubeCoordinate.Component> Coordinate;
            public ComponentDataArray<IsVisible> Visible;
            public ComponentArray<IsVisibleReferences> VisibleRef;
        }
        [Inject]
        cellData m_Cells;

        struct gameStateData
        {
            public readonly int Length;
            public ComponentDataArray<Generic.GameState.Component> GameStateData;
        }
        [Inject]
        gameStateData m_GameState;

        protected override void OnUpdate()
        {
            //Debug.Log("CellsClient:" + m_Cells.Length);
            var GameState = m_GameState.GameStateData[0];
            if (GameState.CurrentState == Generic.GameStateEnum.moving)
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
                        //Debug.Log("val: " + m_Cells.Visible[e].Value + " up:" + m_Cells.Visible[e].RequireUpdate);
                    }
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
                    //Debug.Log("Ayaya");
                    

                    /*if (c_IsVisible.LerpSpeed == 0f)
                    {
                            if (isVisible == 1) {
                                go.SetActive(true);
                            }
                            else
                            {
                                go.SetActive(false);

                            }
                        c_IsVisible.RequireUpdate = 0;
                    }

                    else
                    {
                        */
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
                        //Debug.Log("Ayaya");    
                        go.SetActive(true);
                            
                        if (meshRenderer.material.color.a < 1)
                        {
                            meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a + c_IsVisible.LerpSpeed * Time.deltaTime);
                        }
                        else
                        {
                            c_IsVisible.RequireUpdate = 0;
                        }

                    //}
                }
              }
                //m_Cells.Visible[i] = c_IsVisible;
            }
        }
    }
}
