using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using cakeslice;

public class InitializeUnitsSystem : ComponentSystem
{

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentArray<Transform> Transforms;
        public readonly ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
        public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        public ComponentArray<UnitComponentReferences> ComponentReferences;
        public ComponentArray<LineRendererComponent> LineRenderers;
        public ComponentArray<TeamColorMeshes> TeamColorMeshesData;
        public ComponentArray<UnitMarkerGameObjects> MarkerGameObjects;
    }

    [Inject] UnitData m_UnitData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        public ComponentArray<HeroTransform> HeroTransforms;
    }

    [Inject] PlayerData m_PlayerData;

    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var unitFactionComp = m_UnitData.FactionData[i];
            var teamColorMeshes = m_UnitData.TeamColorMeshesData[i];
            var componentReferences = m_UnitData.ComponentReferences[i];
            var lineRenderer = m_UnitData.LineRenderers[i];
            var unitTransform = m_UnitData.Transforms[i];
            var markerObjects = m_UnitData.MarkerGameObjects[i];


            Color factionColor = new Color();

            switch (unitFactionComp.TeamColor)
            {
                case Generic.TeamColorEnum.blue:
                    markerObjects.Outline.color = 1;
                    factionColor = Color.blue;
                    foreach (Renderer r in teamColorMeshes.detailColorMeshes)
                    {
                        r.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(0, 0.5f));
                    }
                    break;
                case Generic.TeamColorEnum.red:
                    markerObjects.Outline.color = 2;
                    factionColor = Color.red;
                    foreach (Renderer r in teamColorMeshes.detailColorMeshes)
                    {
                        r.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(0.5f, 0.5f));
                    }
                    break;
            }

            //lineRenderer.lineRenderer.startColor = factionColor;
            //lineRenderer.lineRenderer.endColor = factionColor;

            foreach (MeshRenderer r in teamColorMeshes.fullColorMeshes)
            {
                r.material.color = factionColor;
            }

            //Debug.Log(m_PlayerData.Length);

            for (int pi = 0; pi < m_PlayerData.Length; pi++)
            {

                var playerFactionComp = m_PlayerData.FactionData[pi];
                var heroTransform = m_PlayerData.HeroTransforms[pi];

                if(playerFactionComp.Faction == unitFactionComp.Faction && heroTransform.Transform == null)
                {
                    heroTransform.Transform = unitTransform;
                }

            }
        }
    }
}
