using UnityEngine;
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
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<AnimatorComponent>(),
            ComponentType.ReadOnly<MovementVariables.Component>()
            );

        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<HeroTransform>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );

        settings = Resources.Load<Settings>("Settings");
    }

    protected override void OnUpdate()
    {
        Entities.With(m_UnitData).ForEach((Entity e, AnimatorComponent anim, ref FactionComponent.Component unitFactionComp, ref Health.Component health, ref MovementVariables.Component move) =>
        {
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);
            var teamColorMeshes = EntityManager.GetComponentObject<TeamColorMeshes>(e);
            var unitTransform = EntityManager.GetComponentObject<Transform>(e);
            var stats = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);

            anim.RotateTransform.rotation = Quaternion.Euler(new Vector3(0, move.StartRotation, 0));
            unitEffects.CurrentHealth = health.CurrentHealth;

            switch (unitFactionComp.TeamColor)
            {
                case TeamColorEnum.blue:
                    //markerObjects.Outline.color = 1;
                    teamColorMeshes.color = settings.FactionColors[1];
                    unitEffects.PlayerColor = settings.FactionColors[1];
                    break;
                case TeamColorEnum.red:
                    //markerObjects.Outline.color = 2;
                    teamColorMeshes.color = settings.FactionColors[2];
                    unitEffects.PlayerColor = settings.FactionColors[2];
                    break;
            }

            if (unitFactionComp.Faction == 0)
            {
                teamColorMeshes.color = settings.FactionColors[0];
                unitEffects.PlayerColor = settings.FactionColors[0];
            }




            for (int m = 0; m < teamColorMeshes.detailColorMeshes.Count; m++)
            {
                Renderer r = teamColorMeshes.detailColorMeshes[m];

                //set layerMask 
                if (m < teamColorMeshes.PartialColorMasks.Count)
                    r.material.SetTexture("_LayerMaskMap", teamColorMeshes.PartialColorMasks[m]);
                else
                    Debug.LogError("Not Enough PartialColorMasks set, set them on the unit TeamColorMeshes component!");

                //set layer1 color to factionColor
                r.material.SetColor("_BaseColor1", teamColorMeshes.color);
            }

            foreach (TrailRenderer tr in teamColorMeshes.TrailRenderers)
            {
                tr.startColor = teamColorMeshes.color;
                tr.endColor = teamColorMeshes.color;
            }

            foreach (Renderer r in teamColorMeshes.EmissionColorMeshes)
            {
                if (r.materials[r.materials.Length - 1].HasProperty("_EmissiveColor"))
                    r.materials[r.materials.Length - 1].SetColor("_EmissiveColor", teamColorMeshes.color * r.materials[r.materials.Length - 1].GetFloat("_EmissiveIntensity"));

                if (r.materials[r.materials.Length - 1].HasProperty("_EmissionColor"))
                    r.materials[r.materials.Length - 1].SetColor("_EmissionColor", teamColorMeshes.color * r.materials[r.materials.Length - 1].GetFloat("_EmissiveIntensity"));
            }

            foreach (Light l in teamColorMeshes.Lights)
            {
                l.color = teamColorMeshes.color;
            }

            foreach (ParticleSystemRenderer p in teamColorMeshes.EmissiveTrailParticleSystems)
            {
                p.trailMaterial.SetColor("_EmissiveColor", teamColorMeshes.color * p.trailMaterial.GetFloat("_EmissiveIntensity"));
            }

            foreach (LineRenderer l in teamColorMeshes.LineRenderers)
            {
                l.material.SetColor("_EmissiveColor", teamColorMeshes.color * l.material.GetFloat("_EmissiveIntensity"));
            }

            foreach (ParticleSystem p in teamColorMeshes.ParticleSystems)
            {
                ParticleSystem.MainModule main = p.main;
                main.startColor = new Color(teamColorMeshes.color.r, teamColorMeshes.color.g, teamColorMeshes.color.b, main.startColor.color.a);
            }

            foreach (Renderer r in teamColorMeshes.FullColorMeshes)
            {
                if (r.material.HasProperty("_UnlitColor"))
                    r.material.SetColor("_UnlitColor", new Color(teamColorMeshes.color.r, teamColorMeshes.color.g, teamColorMeshes.color.b, r.material.GetColor("_UnlitColor").a));
                else if (r.material.HasProperty("_BaseColor"))
                    r.material.SetColor("_BaseColor", new Color(teamColorMeshes.color.r, teamColorMeshes.color.g, teamColorMeshes.color.b, r.material.GetColor("_BaseColor").a));

                if (r is SpriteRenderer)
                {
                    r.material.color = teamColorMeshes.color;
                }

                if (r is TrailRenderer)
                {
                    TrailRenderer tr = r as TrailRenderer;
                    float alpha = 1.0f;
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(teamColorMeshes.color, 0.0f), new GradientColorKey(teamColorMeshes.color, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
                    );
                    tr.colorGradient = gradient;
                }
            }

            var unitFComp = unitFactionComp;
            var unitTrans = unitTransform;

            if (stats.IsHero)
            {
                Entities.With(m_PlayerData).ForEach((HeroTransform heroTransform, ref FactionComponent.Component playerFactionComp) =>
                {
                    if (playerFactionComp.Faction == unitFComp.Faction && heroTransform.Transform == null)
                    {
                        heroTransform.Transform = unitTransform;
                    }
                });
            }
        });
    }
}
