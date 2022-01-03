using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Cell;
using Unity.Jobs;
using Player;

[DisableAutoCreation]
public class InitializeUnitsSystem : JobComponentSystem
{
    EntityQuery m_PlayerData;
    //EntityQuery m_UnitData;
    //EntityQuery m_ManalithUnitData;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        /*
        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadOnly<UnitDataSet>(),
            ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<UnitComponentReferences>(),
            ComponentType.ReadWrite<LineRendererComponent>(),
            ComponentType.ReadWrite<TeamColorMeshes>(),
            ComponentType.ReadWrite<UnitEffects>(),
            ComponentType.ReadWrite<AnimatorComponent>(),
            ComponentType.ReadOnly<MovementVariables.Component>()
            );


        m_ManalithUnitData = GetEntityQuery(
            ComponentType.ReadWrite<Transform>(),
            ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
            ComponentType.ReadOnly<MovementVariables.Component>()
            );
            */
        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<HeroTransform>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );

        settings = Resources.Load<Settings>("Settings");
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithAll<NewlyAddedSpatialOSEntity>().ForEach((Transform t, ref StartRotation.Component startRotation, ref Manalith.Component manalith) =>
        {
            t.rotation = Quaternion.Euler(new Vector3(0, startRotation.Value, 0));
        })
        .WithoutBurst()
        .Run();

        if (m_PlayerData.CalculateEntityCount() == 0)
            return inputDeps;

        var playerEntity = m_PlayerData.GetSingletonEntity();
        var playerFactionComp = m_PlayerData.GetSingleton<FactionComponent.Component>();
        var heroTransform = EntityManager.GetComponentObject<HeroTransform>(playerEntity);

        Entities.WithAll<NewlyAddedSpatialOSEntity>().ForEach((Entity e, AnimatorComponent anim, ref StartRotation.Component startRotation, in FactionComponent.Component unitFactionComp, in Health.Component health) =>
        {
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);
            var teamColorMeshes = EntityManager.GetComponentObject<TeamColorMeshes>(e);
            var unitTransform = EntityManager.GetComponentObject<Transform>(e);
            var stats = EntityManager.GetComponentObject<UnitDataSet>(e);

            anim.RotateTransform.rotation = Quaternion.Euler(new Vector3(0, startRotation.Value, 0));
            unitEffects.CurrentHealth = health.CurrentHealth;

            if (anim.Animator)
                anim.AnimStateEffectHandlers.AddRange(anim.Animator.GetBehaviours<AnimStateEffectHandler>());

            teamColorMeshes.color = settings.FactionColors[(int)unitFactionComp.Faction];
            unitEffects.PlayerColor = settings.FactionColors[(int)unitFactionComp.Faction];

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
            var unitTrans = unitTransform;

            if (stats.IsHero)
            {
                if (playerFactionComp.Faction == unitFactionComp.Faction && heroTransform.Transform == null)
                {
                    heroTransform.Transform = unitTransform;
                }
            }
        })
        .WithoutBurst()
        .Run();


        return inputDeps;
    }
}
