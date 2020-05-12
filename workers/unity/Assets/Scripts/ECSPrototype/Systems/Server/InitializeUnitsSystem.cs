﻿using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using cakeslice;
using Generic;
using Unity.Collections;
using Unit;

public class InitializeUnitsSystem : ComponentSystem
{
    EntityQuery m_PlayerData;
    EntityQuery m_UnitData;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadOnly<Unit_BaseDataSet>(),
            ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<UnitComponentReferences>(),
            ComponentType.ReadWrite<LineRendererComponent>(),
            ComponentType.ReadWrite<TeamColorMeshes>(),
            ComponentType.ReadWrite<UnitEffects>(),
            ComponentType.ReadOnly<Health.Component>()
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
        var healthData = m_UnitData.ToComponentDataArray<Health.Component>(Allocator.TempJob);
        var unitFactionData = m_UnitData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        var unitStatData = m_UnitData.ToComponentArray<Unit_BaseDataSet>();
        var unitEffectsData = m_UnitData.ToComponentArray<UnitEffects>();
        var teamColorMesheData = m_UnitData.ToComponentArray<TeamColorMeshes>();
        var componentReferenceData = m_UnitData.ToComponentArray<UnitComponentReferences>();
        var lineRendererData = m_UnitData.ToComponentArray<LineRendererComponent>();
        var unitTransformData = m_UnitData.ToComponentArray<Transform>();

        for (int i = 0; i < unitFactionData.Length; i++)
        {
            var stats = unitStatData[i];
            var health = healthData[i];
            var unitEffects = unitEffectsData[i];
            var unitFactionComp = unitFactionData[i];
            var teamColorMeshes = teamColorMesheData[i];
            var componentReferences = componentReferenceData[i];
            var lineRenderer = lineRendererData[i];
            var unitTransform = unitTransformData[i];

            unitEffects.CurrentHealth = health.CurrentHealth;

            Color factionColor = new Color();

            switch (unitFactionComp.TeamColor)
            {
                case TeamColorEnum.blue:
                    //markerObjects.Outline.color = 1;
                    factionColor = settings.FactionColors[1];
                    break;
                case TeamColorEnum.red:
                    //markerObjects.Outline.color = 2;
                    factionColor = settings.FactionColors[2];
                    break;
            }

            for(int m = 0; m < teamColorMeshes.detailColorMeshes.Count; m++)
            {
                Renderer r = teamColorMeshes.detailColorMeshes[m];

                //set layerMask 
                if (m < teamColorMeshes.PartialColorMasks.Count)
                    r.material.SetTexture("_LayerMaskMap", teamColorMeshes.PartialColorMasks[m]);
                else
                    Debug.LogError("Not Enough PartialColorMasks set, set them on the unit TeamColorMeshes component!");

                //set layer1 color to factionColor
                r.material.SetColor("_BaseColor1", factionColor);

            }

            foreach(ParticleSystem p in teamColorMeshes.ParticleSystems)
            {
                ParticleSystem.MainModule main = p.main;
                main.startColor = new Color(factionColor.r, factionColor.g, factionColor.b, main.startColor.color.a);
            }

            foreach (Renderer r in teamColorMeshes.FullColorMeshes)
            {
                if (r.material.HasProperty("_UnlitColor"))
                    r.material.SetColor("_UnlitColor", new Color(factionColor.r, factionColor.g, factionColor.b, r.material.GetColor("_UnlitColor").a));
                else if (r.material.HasProperty("_BaseColor"))
                    r.material.SetColor("_BaseColor", new Color(factionColor.r, factionColor.g, factionColor.b, r.material.GetColor("_BaseColor").a));

                if (r is SpriteRenderer)
                {
                    r.material.color = factionColor;
                }

                if (r is TrailRenderer)
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
                if (stats.IsHero && playerFactionComp.Faction == unitFactionComp.Faction && heroTransform.Transform == null)
                {
                    heroTransform.Transform = unitTransform;
                }
            });
        }

        m_UnitData.CopyFromComponentDataArray(unitFactionData);
        unitFactionData.Dispose();
        healthData.Dispose();

    }
}
