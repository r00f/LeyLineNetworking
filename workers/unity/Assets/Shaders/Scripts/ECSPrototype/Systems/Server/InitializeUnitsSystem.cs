using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using cakeslice;
using Generic;
using Unity.Collections;

public class InitializeUnitsSystem : ComponentSystem
{
    /*
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentArray<> Transforms;
        public readonly ComponentDataArray<> NewEntity;
        public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        public ComponentArray<UnitComponentReferences> ComponentReferences;
        public ComponentArray<LineRendererComponent> LineRenderers;
        public ComponentArray<TeamColorMeshes> TeamColorMeshesData;
        public ComponentArray<UnitMarkerGameObjects> MarkerGameObjects;
    }
    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.> FactionData;
        public ComponentArray<HeroTransform> HeroTransforms;
    }

    */

    EntityQuery m_PlayerData;
    EntityQuery m_UnitData;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<UnitComponentReferences>(),
            ComponentType.ReadWrite<LineRendererComponent>(),
            ComponentType.ReadWrite<TeamColorMeshes>(),
            ComponentType.ReadWrite<UnitMarkerGameObjects>()
            );

        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<HeroTransform>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );

        //var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName).GetComponent<Unit_BaseDataSet>();
        settings = Resources.Load<Settings>("Settings");

    }

    protected override void OnUpdate()
    {
        var unitFactionData = m_UnitData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        var teamColorMesheData = m_UnitData.ToComponentArray<TeamColorMeshes>();
        var componentReferenceData = m_UnitData.ToComponentArray<UnitComponentReferences>();
        var lineRendererData = m_UnitData.ToComponentArray<LineRendererComponent>();
        var unitTransformData = m_UnitData.ToComponentArray<Transform>();
        var markerObjectData = m_UnitData.ToComponentArray<UnitMarkerGameObjects>();

        for (int i = 0; i < unitFactionData.Length; i++)
        {
            var unitFactionComp = unitFactionData[i];
            var teamColorMeshes = teamColorMesheData[i];
            var componentReferences = componentReferenceData[i];
            var lineRenderer = lineRendererData[i];
            var unitTransform = unitTransformData[i];
            var markerObjects = markerObjectData[i];


            Color factionColor = new Color();

            switch (unitFactionComp.TeamColor)
            {
                case Generic.TeamColorEnum.blue:
                    markerObjects.Outline.color = 1;
                    factionColor = settings.FactionColors[1];
                    foreach (Renderer r in teamColorMeshes.detailColorMeshes)
                    {
                        r.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(0, 0.5f));
                    }
                    break;
                case Generic.TeamColorEnum.red:
                    markerObjects.Outline.color = 2;
                    factionColor = settings.FactionColors[2];
                    foreach (Renderer r in teamColorMeshes.detailColorMeshes)
                    {
                        r.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(0.5f, 0.5f));
                    }
                    break;
            }

            //lineRenderer.lineRenderer.startColor = factionColor;
            //lineRenderer.lineRenderer.endColor = factionColor;

            foreach (Renderer r in teamColorMeshes.FullColorMeshes)
            {
                r.material.color = factionColor;
                if(r is TrailRenderer)
                {
                    TrailRenderer tr = r as TrailRenderer;
                    float alpha = 1.0f;
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(factionColor, 0.0f), new GradientColorKey(factionColor, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
                    );
                    tr.colorGradient = gradient;

                }
            }
            Entities.With(m_PlayerData).ForEach((HeroTransform heroTransform, ref FactionComponent.Component playerFactionComp) =>
            {
                if (playerFactionComp.Faction == unitFactionComp.Faction && heroTransform.Transform == null)
                {
                    heroTransform.Transform = unitTransform;
                }
            });
        }
        m_UnitData.CopyFromComponentDataArray(unitFactionData);
        unitFactionData.Dispose();

    }
}
