using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Entities;
using Generic;
using Unit;
using System.Collections.Generic;
using Unity.Collections;
using System.Collections;
using Player;
using System.Linq;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(UnitAnimationSystem))]
public class ActionEffectsSystem : ComponentSystem
{
    Settings settings;
    UISystem m_UISystem;
    PathFindingSystem m_PathFindingSystem;
    UnitAnimationSystem m_UnitAnimationSystem;
    HighlightingSystem m_HighlightingSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    EntityQuery m_GameStateData;
    EntityQuery m_PlayerData;
    GameObject GarbageCollection;
    EntityQuery m_GarbageCollection;

    protected override void OnCreate()
    {
        base.OnCreate();
        settings = Resources.Load<Settings>("Settings");

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<Vision.Component>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>()
        );


        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<CapsuleCollider>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<UnitComponentReferences>()
        );

        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<MarkerState>(),
        ComponentType.ReadWrite<MarkerGameObjects>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        m_UnitAnimationSystem = World.GetExistingSystem<UnitAnimationSystem>();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_UISystem = World.GetExistingSystem<UISystem>();
        m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();

        GarbageCollection = Object.FindObjectOfType<GarbageCollectorComponent>().gameObject;

        m_GarbageCollection = Worlds.DefaultWorld.CreateEntityQuery(
        ComponentType.ReadWrite<GarbageCollectorComponent>()
        );
    }

    protected override void OnUpdate()
    {

        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

        if (cleanUpStateEvents.Count > 0)
        {
            Entities.With(m_UnitData).ForEach((Entity e, UnitComponentReferences componentReferences, ref Health.Component health, ref CubeCoordinate.Component coord, ref Actions.Component actions) =>
            {
                if (componentReferences.UnitEffectsComp.AxaShield && componentReferences.UnitEffectsComp.AxaShield.Orbits.Count != 0)
                {
                    for (int i = componentReferences.UnitEffectsComp.AxaShield.Orbits.Count - 1; i >= 0; i--)
                    {
                        AxaShieldOrbit o = componentReferences.UnitEffectsComp.AxaShield.Orbits[i];

                        foreach (ParticleSystem loopPS in o.LoopingSystems)
                        {
                            loopPS.Stop();
                        }
                        foreach (ParticleSystem explosionPS in o.ExplosionSystems)
                        {
                            explosionPS.Play();
                        }
                        Object.Destroy(o.gameObject, .2f);
                        componentReferences.UnitEffectsComp.AxaShield.Orbits.Remove(o);
                    }
                }
                componentReferences.UnitEffectsComp.CombinedArmor = 0;
                componentReferences.UnitEffectsComp.CurrentArmor = 0;
                componentReferences.UnitEffectsComp.CurrentHealth = health.CurrentHealth;
            });
        }

        Entities.With(m_GameStateData).ForEach((ref GameState.Component gameState) =>
        {
            if (gameState.CurrentState != GameStateEnum.planning)
            {
                Entities.With(m_PlayerData).ForEach((ref Vision.Component playerVision) =>
                {
                    if (playerVision.RevealVision)
                    {
                        return;
                    }
                    var pVision = playerVision;

                    Entities.With(m_UnitData).ForEach((Entity e, UnitComponentReferences componentReferences, ref Health.Component health, ref CubeCoordinate.Component coord, ref Actions.Component actions) =>
                    {
                        if (pVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
                        {
                            //DOES THE SAME FOR EACH GETHITEFFECT IN LIST (TWO DAMAGE NUMBERS AT THE SAME TIME) - ADD BEHAVIOUR TO ADD DAMAGE NUBERS AND ONLY DISPLAY 1 EFFECT 
                            for (int i = 0; i < componentReferences.UnitEffectsComp.GetHitEffects.Count; i++)
                            {
                                componentReferences.UnitEffectsComp.HitPosition = componentReferences.CapsuleCollider.ClosestPoint(componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i).Value);
                                Vector3 dir = componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i).Value - componentReferences.UnitEffectsComp.HitPosition;

                                foreach (ActionEffect effect in componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i).Key.Effects)
                                {
                                    switch (effect.EffectType)
                                    {
                                        case EffectTypeEnum.deal_damage:

                                            uint damageAmount = effect.DealDamageNested.DamageAmount;

                                            if (componentReferences.UnitEffectsComp.CurrentArmor > 0)
                                            {
                                                //handle axa shield orbits
                                                if (componentReferences.UnitEffectsComp.AxaShield && componentReferences.UnitEffectsComp.AxaShield.Orbits.Count != 0)
                                                {
                                                    int remainingDamage = (int) damageAmount;

                                                    for (int j = componentReferences.UnitEffectsComp.AxaShield.Orbits.Count - 1; j >= 0; j--)
                                                    {
                                                        AxaShieldOrbit o = componentReferences.UnitEffectsComp.AxaShield.Orbits[j];

                                                        if (remainingDamage - 10 >= 0)
                                                        {
                                                            foreach (ParticleSystem loopPS in o.LoopingSystems)
                                                            {
                                                                loopPS.Stop();
                                                            }
                                                            foreach (ParticleSystem explosionPS in o.ExplosionSystems)
                                                            {
                                                                explosionPS.Play();
                                                            }
                                                            Object.Destroy(o.gameObject, .2f);
                                                            remainingDamage -= 10;
                                                            componentReferences.UnitEffectsComp.AxaShield.Orbits.Remove(o);
                                                        }
                                                    }
                                                }

                                                if (damageAmount < componentReferences.UnitEffectsComp.CurrentArmor)
                                                {
                                                    //do armor damage Stuff
                                                    Object.Instantiate(componentReferences.UnitEffectsComp.DefenseParticleSystem, componentReferences.UnitEffectsComp.HitPosition, Quaternion.identity);
                                                    componentReferences.UnitEffectsComp.CurrentArmor -= damageAmount;
                                                    m_UISystem.SetArmorDisplay(e, componentReferences.UnitEffectsComp.CurrentArmor, 5f);
                                                    m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.yellow);
                                                }
                                                else
                                                {
                                                    //shatter shield
                                                    uint remainingDamage = damageAmount - componentReferences.UnitEffectsComp.CurrentArmor;
                                                    Object.Instantiate(componentReferences.UnitEffectsComp.ShieldBrakeParticleSystem, componentReferences.UnitEffectsComp.HitPosition, Quaternion.identity);

                                                    m_UISystem.SetHealthFloatText(e, false, remainingDamage, Color.red, 0.4f);

                                                    m_UISystem.SetArmorDisplay(e, componentReferences.UnitEffectsComp.CurrentArmor, .5f, true);

                                                    componentReferences.UnitEffectsComp.CurrentArmor = 0;

                                                    if ((int) componentReferences.UnitEffectsComp.CurrentHealth - (int) remainingDamage > 0)
                                                    {
                                                        componentReferences.UnitEffectsComp.CurrentHealth -= remainingDamage;
                                                    }
                                                    else
                                                    {
                                                        componentReferences.UnitEffectsComp.CurrentHealth = 0;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                PlayerColor_ParticleSystem go = Object.Instantiate(componentReferences.UnitEffectsComp.BloodParticleSystem, componentReferences.UnitEffectsComp.HitPosition, Quaternion.LookRotation(dir));
                                                for (int ii = 0; ii < go.SetParticleSystem_BaseColor.Count; ii++)
                                                {
                                                    ParticleSystem.MainModule main = go.SetParticleSystem_BaseColor[ii].main;
                                                    main.startColor = componentReferences.UnitEffectsComp.PlayerColor;
                                                }

                                                //if the unit survives
                                                if ((int) componentReferences.UnitEffectsComp.CurrentHealth - (int) damageAmount > 0)
                                                {
                                                    m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.red);
                                                    componentReferences.UnitEffectsComp.CurrentHealth -= damageAmount;
                                                }
                                                else
                                                {
                                                    componentReferences.UnitEffectsComp.CurrentHealth = 0;
                                                }
                                            }


                                            if (componentReferences.HeadUIReferencesComp.UnitHeadHealthBarInstance)
                                            {
                                                if (componentReferences.HeadUIReferencesComp.IncomingDamage - damageAmount >= 0)
                                                {
                                                    componentReferences.HeadUIReferencesComp.IncomingDamage -= damageAmount;
                                                }
                                                else
                                                {
                                                    componentReferences.HeadUIReferencesComp.IncomingDamage = 0;
                                                }

                                                if (componentReferences.UnitEffectsComp.CurrentHealth + componentReferences.UnitEffectsComp.CurrentArmor >= health.MaxHealth)
                                                {
                                                    componentReferences.HeadUIReferencesComp.UnitHeadHealthBarInstance.BgFill.fillAmount = 1 - ((float) (componentReferences.UnitEffectsComp.CurrentHealth + componentReferences.UnitEffectsComp.CurrentArmor) / (health.MaxHealth + componentReferences.UnitEffectsComp.CombinedArmor));
                                                }
                                                else
                                                {
                                                    componentReferences.HeadUIReferencesComp.UnitHeadHealthBarInstance.BgFill.fillAmount = 1 - ((float) (componentReferences.UnitEffectsComp.CurrentHealth + componentReferences.UnitEffectsComp.CurrentArmor) / health.MaxHealth);
                                                }
                                            }

                                            componentReferences.AnimatorComp.Animator.SetTrigger("GetHit");

                                            break;

                                        case EffectTypeEnum.gain_armor:

                                            //enter defensive stance in animator

                                            uint armorAmount = componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i).Key.Effects[0].GainArmorNested.ArmorAmount;
                                            //do gainArmorStuff
                                            componentReferences.UnitEffectsComp.CombinedArmor += armorAmount;
                                            componentReferences.UnitEffectsComp.CurrentArmor += armorAmount;
                                            m_UISystem.SetArmorDisplay(e, componentReferences.UnitEffectsComp.CurrentArmor, .5f);
                                            m_UISystem.SetHealthFloatText(e, true, armorAmount, Color.yellow);
                                            break;
                                    }
                                }

                                componentReferences.UnitEffectsComp.CurrentGetHitEffect = componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i);
                                componentReferences.UnitEffectsComp.GetHitEffects.Remove(componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i).Key);
                            }

                            if (componentReferences.UnitEffectsComp.CurrentHealth == 0 && health.CurrentHealth == 0 && !componentReferences.AnimatorComp.Dead)
                            {
                                if (actions.LockedAction.Index == -3 || actions.LockedAction.ActionExecuteStep != componentReferences.UnitEffectsComp.CurrentGetHitEffect.Key.ActionExecuteStep)
                                {
                                    //NORMAL DEATH - INSTANTLY DIE
                                    if (componentReferences.UnitEffectsComp.DisplayDeathSkull)
                                        m_UISystem.TriggerUnitDeathUI(e);

                                    if (componentReferences.UnitEffectsComp.BodyPartBloodParticleSystem)
                                    {
                                        Death(componentReferences, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Key, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Value, componentReferences.UnitEffectsComp.PlayerColor, componentReferences.UnitEffectsComp.BodyPartBloodParticleSystem);
                                    }
                                    else
                                    {
                                        Death(componentReferences, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Key, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Value, componentReferences.UnitEffectsComp.PlayerColor);
                                    }

                                    //componentReferences.UnitEffectsComp.GetHitEffects.Remove(componentReferences.UnitEffectsComp.GetHitEffects.ElementAt(i).Key);
                                }
                                else
                                {
                                    //SECOND WIND DEATH - DIE WHEN ANIM IS DONE

                                    //if(componentReferences.AnimatorComp.) if character is not red turn it red
                                    if (componentReferences.UnitEffectsComp.SecondWindParticleSystemInstance)
                                    {
                                        ParticleSystem ps = componentReferences.UnitEffectsComp.SecondWindParticleSystemInstance;

                                        if (!ps.isPlaying)
                                            ps.Play();
                                    }

                                    if (componentReferences.AnimatorComp.AnimationEvents.EventTriggered)
                                    {
                                        if (componentReferences.UnitEffectsComp.SecondWindParticleSystemInstance)
                                        {
                                            ParticleSystem ps = componentReferences.UnitEffectsComp.SecondWindParticleSystemInstance;

                                            if (ps.isPlaying)
                                                ps.Stop();
                                        }

                                        if (componentReferences.UnitEffectsComp.DisplayDeathSkull)
                                            m_UISystem.TriggerUnitDeathUI(e);

                                        if (componentReferences.UnitEffectsComp.BodyPartBloodParticleSystem)
                                        {
                                            Death(componentReferences, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Key, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Value, componentReferences.UnitEffectsComp.PlayerColor, componentReferences.UnitEffectsComp.BodyPartBloodParticleSystem);
                                        }
                                        else
                                        {
                                            Death(componentReferences, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Key, componentReferences.UnitEffectsComp.CurrentGetHitEffect.Value, componentReferences.UnitEffectsComp.PlayerColor);
                                        }

                                    }
                                }
                            }
                        }

                        componentReferences.AnimatorComp.Animator.SetInteger("Armor", (int)componentReferences.UnitEffectsComp.CurrentArmor);
                    });
                });
            }
        });

    }

    public void TriggerActionEffect(Vision.Component playerVision, Action inAction, long unitID, Transform hitTransform, int spawnShieldOrbits = 0)
    {
        //Validate targets from CellgridMethods (ActionHelperMethods whenever we create it)
        HashSet<Vector3f> coordsToTrigger = new HashSet<Vector3f> { inAction.Targets[0].TargetCoordinate };

        if (inAction.Targets[0].Mods.Count != 0)
        {
            foreach (CoordinatePositionPair p in inAction.Targets[0].Mods[0].CoordinatePositionPairs)
            {
                coordsToTrigger.Add(p.CubeCoordinate);
            }
        }


        Entities.With(m_UnitData).ForEach((Entity e, UnitComponentReferences componentReferences, ref FactionComponent.Component faction, ref CubeCoordinate.Component cubeCoord) =>
        {
        if (playerVision.CellsInVisionrange.ContainsKey(cubeCoord.CubeCoordinate))
        {
            //only apply effect if target unit is valid or pass correct coordinates
            if (coordsToTrigger.Contains(componentReferences.UnitEffectsComp.LastStationaryCoordinate) && m_PathFindingSystem.ValidateUnitTarget(e, (UnitRequisitesEnum)(int)inAction.Effects[0].ApplyToRestrictions, unitID, faction.Faction))
            {
                if (componentReferences.UnitEffectsComp.AxaShield && spawnShieldOrbits != 0)
                {
                    //if we need to spawn shieldorbits spaWN SHIELDorbits
                    for (int i = 0; i < spawnShieldOrbits; i++)
                    {
                        AxaShieldOrbit go = Object.Instantiate(componentReferences.UnitEffectsComp.AxaShield.OrbitPrefab, componentReferences.UnitEffectsComp.AxaShield.transform.position, Quaternion.LookRotation(hitTransform.position - componentReferences.UnitEffectsComp.AxaShield.transform.position), componentReferences.UnitEffectsComp.AxaShield.transform);
                        go.GetComponent<MovementAnimComponent>().RotationAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), 0f);
                        componentReferences.UnitEffectsComp.AxaShield.Orbits.Add(go);
                    }
                }
                    componentReferences.UnitEffectsComp.GetHitEffects.Add(inAction, hitTransform.position);
                }
            }
        });
    }

    public void LaunchProjectile(Vision.Component playerVision, Projectile projectileFab, Transform spawnTransform, Vector3 targetPos, Action inAction, long unitId, Vector3f originCoord, float yOffset = 0)
    {
        bool AoEcontainsTarget = false;

        if (inAction.Targets[0].Mods.Count != 0 && !AoEcontainsTarget)
        {
            foreach (CoordinatePositionPair p in inAction.Targets[0].Mods[0].CoordinatePositionPairs)
            {
                if (playerVision.CellsInVisionrange.ContainsKey(p.CubeCoordinate))
                    AoEcontainsTarget = true;
            }
        }

        //if any coord in an AoE is visible or Launching unit is visible or target unit is visible, spawn a projectile
        if (AoEcontainsTarget || playerVision.CellsInVisionrange.ContainsKey(inAction.Targets[0].TargetCoordinate) || playerVision.CellsInVisionrange.ContainsKey(originCoord))
        {
            Projectile projectile = Object.Instantiate(projectileFab, spawnTransform.position, spawnTransform.rotation, spawnTransform.root);
            projectile.UnitId = unitId;
            projectile.Action = inAction;
            projectile.SpawnTransform = spawnTransform;


            //save targetPosition / targetYOffset on units?
            Vector3 offSetTarget = new Vector3(targetPos.x, targetPos.y + yOffset + projectile.TargetYOffset, targetPos.z);

            List<Vector3> travellingPoints = new List<Vector3>();

            //THIS USES SINUS CALC FOR STRAIGHT LINES -- CHANGE METHOD TO HANDLE STRAIGHT LINES WHITOUT CALCULATING SINUS STUFF
            travellingPoints.AddRange(m_HighlightingSystem.CalculateSinusPath(spawnTransform.position, offSetTarget, projectileFab.MaxHeight));

            Vector3 distance = offSetTarget - spawnTransform.position;

            foreach (SpringJoint s in projectileFab.SpringJoints)
            {
                s.maxDistance = distance.magnitude / projectileFab.SpringJoints.Count;
            }


            projectile.TravellingCurve = travellingPoints;
            projectile.IsTravelling = true;
        }
    }

    public void Death(UnitComponentReferences componentReferences, Action action, Vector3 position,Color playerColor, PlayerColor_ParticleSystem bodyPartParticle = null, float delay = 0)
    {
        var garbageCollector = m_GarbageCollection.ToComponentArray<GarbageCollectorComponent>()[0];

        if (bodyPartParticle)
        {
            int random = Random.Range(0, componentReferences.AnimatorComp.RagdollRigidBodies.Count);
            int random2 = Random.Range(0, componentReferences.AnimatorComp.RagdollRigidBodies.Count);

            for (int i = 0; i < componentReferences.AnimatorComp.RagdollRigidBodies.Count; i++)
            {
                if (i == random || i == random2)
                {
                    PlayerColor_ParticleSystem go = Object.Instantiate(bodyPartParticle, componentReferences.AnimatorComp.RagdollRigidBodies[i].position, Quaternion.identity, componentReferences.AnimatorComp.RagdollRigidBodies[i].transform);
                    for (int ii = 0; ii < go.SetParticleSystem_BaseColor.Count; ii++)
                    {
                        ParticleSystem.MainModule main = go.SetParticleSystem_BaseColor[ii].main;
                        main.startColor = playerColor;
                    }
                }
            }
        }

        foreach (GameObject go in componentReferences.AnimatorComp.DisableOnDeathObjects)
        {
            go.SetActive(false);
        }

        foreach (Transform t in componentReferences.AnimatorComp.Props)
        {
            t.parent = componentReferences.AnimatorComp.Animator.transform;
        }

        //dismemberment
        foreach (CharacterJoint j in componentReferences.AnimatorComp.DismemberJoints)
        {
            float rand = Random.Range(0f, 1f);
            if(rand <= componentReferences.AnimatorComp.DismemberPercentage)
            {
                j.transform.parent = componentReferences.AnimatorComp.Animator.transform;
                Object.Destroy(j);
            }
        }


        //Enable ragdoll behaviour
        componentReferences.AnimatorComp.Animator.transform.parent = GarbageCollection.transform;
        componentReferences.AnimatorComp.Animator.enabled = false;
        garbageCollector.GarbageObjects.Add(componentReferences.AnimatorComp.Animator.gameObject);


        HashSet<Rigidbody> ragdollHash = new HashSet<Rigidbody>();

        foreach (Rigidbody r in componentReferences.AnimatorComp.RagdollRigidBodies)
        {
            ragdollHash.Add(r);
            garbageCollector.GarbageRigidbodies.Add(r);
            r.isKinematic = false;
        }


        
        foreach (ActionEffect e in action.Effects)
        {
            if(e.EffectType == EffectTypeEnum.deal_damage)
            {
                //apply physics forces to ragdoll
                Explode(position, e.DealDamageNested.ExplosionRadius, e.DealDamageNested.ExplosionForce, e.DealDamageNested.UpForce, ragdollHash);
            }
        }

        componentReferences.AnimatorComp.Dead = true;
    }

    public void Explode(Vector3 explosionOrigin, float explosionRadius, float explosionForce, uint upForce, HashSet<Rigidbody> ragdollRigidbodies)
    {
        //Debug.Log("Explode");
        //Add ExplosionForce to all Rigidbodies in N range
        var cols = Physics.OverlapSphere(explosionOrigin, explosionRadius);
        var rigidbodies = new List<Rigidbody>();

        
        //var debugSphere = Object.Instantiate(settings.ExplosionDebugSphere, explosionOrigin, Quaternion.identity);
        //debugSphere.transform.localScale *= explosionRadius;
        

        foreach (var col in cols)
        {
            if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody) && ragdollRigidbodies.Contains(col.attachedRigidbody))
            {
                //Debug.Log(col.name);
                rigidbodies.Add(col.attachedRigidbody);
            }
        }

        foreach (Rigidbody r in rigidbodies)
        {
            //ADD MORITZ FANCY AYAYA physics explosion code
            //Debug.Log("RigidBodyInExplosionRange");
            r.AddForce(Vector3.up * upForce, ForceMode.Impulse);
            r.AddExplosionForce(explosionForce, explosionOrigin, explosionRadius);
        }

        
    }
}
