using Cell;
using Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Player;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unit;

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
            public readonly ComponentDataArray<FactionComponent.Component> Factions;
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
            public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates;
            public ComponentDataArray<IsVisible> Visible;
            public ComponentArray<IsVisibleReferences> VisibleRef;
        }

        [Inject] IsVisibleData m_IsVisible;

        struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates;
            public readonly ComponentDataArray<FactionComponent.Component> Factions;
            public ComponentDataArray<IsVisible> Visible;
        }

        [Inject] UnitData m_UnitData;



        protected override void OnUpdate()
        {
            for (int i = 0; i < m_UpdateVisionRequestData.Length; i++)
            {
                Debug.Log("updateClientVisionRequest");
            }

            var playerVision = m_PlayerData.VisionData[0];
            var playerFaction = m_PlayerData.Factions[0].Faction;

            //set opposing unit visibilty values when they enter / leave a players visionRange
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var faction = m_UnitData.Factions[i].Faction;
                var coord = m_UnitData.Coordinates[i];
                var visible = m_UnitData.Visible[i];

                if (faction != playerFaction && visible.RequireUpdate == 0)
                {
                    bool inVisionRange = false;

                    foreach (CellAttributes c in playerVision.CellsInVisionrange)
                    {
                        if(coord.CubeCoordinate == c.Cell.CubeCoordinate)
                        {
                            inVisionRange = true;
                        }
                    }

                    if (inVisionRange)
                    {
                        visible.Value = 1;
                    }
                    else
                    {
                        visible.Value = 0;
                    }

                    visible.RequireUpdate = 1;

                    m_UnitData.Visible[i] = visible;
                }
            }


            for (int i = 0; i < m_IsVisible.Length; i++)
            {
                var isVisibleComp = m_IsVisible.Visible[i];
                MeshRenderer meshRenderer = m_IsVisible.VisibleRef[i].MeshRenderer;
                List<GameObject> gameObjects = m_IsVisible.VisibleRef[i].GameObjects;
                Collider collider = m_IsVisible.VisibleRef[i].Collider;
                byte isVisible = m_IsVisible.Visible[i].Value;
                var coord = m_IsVisible.Coordinates[i];

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
                var coord = m_IsVisible.Coordinates[i];

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
