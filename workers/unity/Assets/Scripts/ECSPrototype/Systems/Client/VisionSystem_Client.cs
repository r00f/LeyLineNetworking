using Cell;
using Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Player;
using Unity.Entities;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class VisionSystem_Client : ComponentSystem
    {
        struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<Vision.Component> VisionData;
        }

        [Inject] PlayerData m_PlayerData;

        struct UpdateVisionRequestData
        {
            public readonly int Length;
            public ComponentDataArray<Vision.CommandRequests.UpdateClientVisionCommand> UpdateClientVisionRequests;
        }

        [Inject] UpdateVisionRequestData m_UpdateVisionRequestData;

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
            for (int i = 0; i < m_UpdateVisionRequestData.Length; i++)
            {
                Debug.Log("updateClientVisionRequest");
            }

            var playerVision = m_PlayerData.VisionData[0];

            for (int i = 0; i < m_IsVisible.Length; i++)
            {
                var isVisibleComp = m_IsVisible.Visible[i];
                MeshRenderer meshRenderer = m_IsVisible.VisibleRef[i].MeshRenderer;
                GameObject go = m_IsVisible.VisibleRef[i].GO;
                byte isVisible = m_IsVisible.Visible[i].Value;
                var coord = m_IsVisible.Coordinate[i];

                if (isVisibleComp.RequireUpdate == 0)
                {
                    //only compare when serverSide playerVision is updated
                    if (isVisible == 0)
                    {
                        foreach (CellAttributes c in playerVision.Positives)
                        {
                            if (c.Cell.CubeCoordinate == coord.CubeCoordinate)
                            {
                                isVisibleComp.Value = 1;
                                isVisibleComp.RequireUpdate = 1;
                            }
                        }
                    }
                    else
                    {
                        foreach (CellAttributes c in playerVision.Negatives)
                        {
                            if (c.Cell.CubeCoordinate == coord.CubeCoordinate)
                            {
                                isVisibleComp.Value = 0;
                                isVisibleComp.RequireUpdate = 1;
                            }
                        }
                    }
                    m_IsVisible.Visible[i] = isVisibleComp;
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
                    m_IsVisible.Visible[i] = isVisibleComp;
                }
            }
        }

        public void UpdateVision()
        {
            var playerVision = m_PlayerData.VisionData[0];

            for (int i = 0; i < m_IsVisible.Length; i++)
            {
                var isVisibleComp = m_IsVisible.Visible[i];
                byte isVisible = m_IsVisible.Visible[i].Value;
                var coord = m_IsVisible.Coordinate[i];

                if (isVisibleComp.RequireUpdate == 0)
                {
                    //only compare when serverSide playerVision is updated
                    if (isVisible == 0)
                    {
                        foreach (CellAttributes c in playerVision.Positives)
                        {
                            if (c.Cell.CubeCoordinate == coord.CubeCoordinate)
                            {
                                isVisibleComp.Value = 1;
                                isVisibleComp.RequireUpdate = 1;
                            }
                        }
                    }
                    else
                    {
                        foreach (CellAttributes c in playerVision.Negatives)
                        {
                            if (c.Cell.CubeCoordinate == coord.CubeCoordinate)
                            {
                                isVisibleComp.Value = 0;
                                isVisibleComp.RequireUpdate = 1;
                            }
                        }
                    }
                    m_IsVisible.Visible[i] = isVisibleComp;
                }
            }
        }
    }
}
