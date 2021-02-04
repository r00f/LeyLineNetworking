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
        ComponentType.ReadWrite<AnimatorComponent>(),
        ComponentType.ReadWrite<UnitHeadUIReferences>(),
        ComponentType.ReadWrite<UnitEffects>()
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
        var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
        var playerVisionData = m_PlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);


        if (m_GameStateData.CalculateEntityCount() == 0 || m_PlayerData.CalculateEntityCount() == 0)
        {
            //Debug.Log("OOF");
            playerVisionData.Dispose();
            gameStates.Dispose();
            return;
        }

        var playerVisionHash = new HashSet<Vector3f>(playerVisionData[0].CellsInVisionrange);

        Entities.With(m_UnitData).ForEach((Entity e, AnimatorComponent animatorComponent, ref Health.Component health, ref CubeCoordinate.Component coord, ref Actions.Component actions) =>
        {
            var unitHeadUIRef = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);
            var unitCollider = EntityManager.GetComponentObject<CapsuleCollider>(e);
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);

            if(playerVisionHash.Contains(coord.CubeCoordinate))
            {
                if (gameStates[0].CurrentState == GameStateEnum.planning)
                {
                    //handle axa shield orbits
                    if (unitEffects.AxaShield && unitEffects.AxaShield.Orbits.Count != 0)
                    {
                        for (int i = unitEffects.AxaShield.Orbits.Count - 1; i >= 0; i--)
                        {
                            AxaShieldOrbit o = unitEffects.AxaShield.Orbits[i];

                            foreach (ParticleSystem loopPS in o.LoopingSystems)
                            {
                                loopPS.Stop();
                            }
                            foreach (ParticleSystem explosionPS in o.ExplosionSystems)
                            {
                                explosionPS.Play();
                            }
                            Object.Destroy(o.gameObject, .2f);
                            unitEffects.AxaShield.Orbits.Remove(o);
                        }
                    }

                    unitEffects.CombinedArmor = 0;
                    unitEffects.CurrentArmor = 0;
                    unitEffects.CurrentHealth = health.CurrentHealth;
                }

                //DOES THE SAME FOR EACH GETHITEFFECT IN LIST (TWO DAMAGE NUMBERS AT THE SAME TIME) - ADD BEHAVIOUR TO ADD DAMAGE NUBERS AND ONLY DISPLAY 1 EFFECT 
                for (int i = 0; i < unitEffects.GetHitEffects.Count; i++)
                {
                    unitEffects.HitPosition = unitCollider.ClosestPoint(unitEffects.GetHitEffects.ElementAt(i).Value);
                    Vector3 dir = unitEffects.GetHitEffects.ElementAt(i).Value - unitEffects.HitPosition;

                    foreach (ActionEffect effect in unitEffects.GetHitEffects.ElementAt(i).Key.Effects)
                    {
                        switch (effect.EffectType)
                        {
                            case EffectTypeEnum.deal_damage:

                                uint damageAmount = effect.DealDamageNested.DamageAmount;

                                if (unitEffects.CurrentArmor > 0)
                                {
                                    //handle axa shield orbits
                                    if (unitEffects.AxaShield && unitEffects.AxaShield.Orbits.Count != 0)
                                    {
                                        int remainingDamage = (int)damageAmount;

                                        for (int j = unitEffects.AxaShield.Orbits.Count - 1; j >= 0; j--)
                                        {
                                            AxaShieldOrbit o = unitEffects.AxaShield.Orbits[j];

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
                                                unitEffects.AxaShield.Orbits.Remove(o);
                                            }
                                        }
                                    }

                                    if (damageAmount < unitEffects.CurrentArmor)
                                    {
                                        //do armor damage Stuff
                                        Object.Instantiate(unitEffects.DefenseParticleSystem, unitEffects.HitPosition, Quaternion.identity);
                                        unitEffects.CurrentArmor -= damageAmount;
                                        m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, 5f);
                                        m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.yellow);
                                    }
                                    else
                                    {
                                        //shatter shield
                                        uint remainingDamage = damageAmount - unitEffects.CurrentArmor;
                                        Object.Instantiate(unitEffects.ShieldBrakeParticleSystem, unitEffects.HitPosition, Quaternion.identity);

                                        m_UISystem.SetHealthFloatText(e, false, remainingDamage, Color.red, 0.4f);

                                        m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, .5f, true);

                                        unitEffects.CurrentArmor = 0;

                                        if ((int)unitEffects.CurrentHealth - (int)remainingDamage > 0)
                                        {
                                            unitEffects.CurrentHealth -= remainingDamage;
                                        }
                                        else
                                        {
                                            unitEffects.CurrentHealth = 0;
                                        }
                                    }
                                }
                                else
                                {
                                    PlayerColor_ParticleSystem go = Object.Instantiate(unitEffects.BloodParticleSystem, unitEffects.HitPosition, Quaternion.LookRotation(dir));
                                    for(int ii =0; ii<go.SetParticleSystem_BaseColor.Count; ii++)
                                    {
                                        go.SetParticleSystem_BaseColor[ii].startColor = unitEffects.PlayerColor;
                                    }

                                    //if the unit survives
                                    if ((int)unitEffects.CurrentHealth - (int)damageAmount > 0)
                                    {
                                        m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.red);
                                        unitEffects.CurrentHealth -= damageAmount;
                                    }
                                    else
                                    {
                                        unitEffects.CurrentHealth = 0;
                                    }
                                }


                                if (unitHeadUIRef.UnitHeadHealthBarInstance)
                                {
                                    if(unitHeadUIRef.IncomingDamage - damageAmount >= 0)
                                    {
                                        unitHeadUIRef.IncomingDamage -= damageAmount;
                                    }
                                    else
                                    {
                                        unitHeadUIRef.IncomingDamage = 0;
                                    }

                                    if (unitEffects.CurrentHealth + unitEffects.CurrentArmor >= health.MaxHealth)
                                    {
                                        unitHeadUIRef.UnitHeadHealthBarInstance.BgFill.fillAmount = 1 - ((float)(unitEffects.CurrentHealth + unitEffects.CurrentArmor) / (health.MaxHealth + unitEffects.CombinedArmor));
                                    }
                                    else
                                    {
                                        unitHeadUIRef.UnitHeadHealthBarInstance.BgFill.fillAmount = 1 - ((float)(unitEffects.CurrentHealth + unitEffects.CurrentArmor) / health.MaxHealth);
                                    }
                                }

                                animatorComponent.Animator.SetTrigger("GetHit");


                                break;

                            case EffectTypeEnum.gain_armor:

                                //enter defensive stance in animator

                                uint armorAmount = unitEffects.GetHitEffects.ElementAt(i).Key.Effects[0].GainArmorNested.ArmorAmount;
                                //do gainArmorStuff
                                unitEffects.CombinedArmor += armorAmount;
                                unitEffects.CurrentArmor += armorAmount;
                                m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, .5f);
                                m_UISystem.SetHealthFloatText(e, true, armorAmount, Color.yellow);
                                break;
                        }
                    }

                    unitEffects.CurrentGetHitEffect = unitEffects.GetHitEffects.ElementAt(i);
                    unitEffects.GetHitEffects.Remove(unitEffects.GetHitEffects.ElementAt(i).Key);
                }

                if (unitEffects.CurrentHealth == 0 && health.CurrentHealth == 0 && !animatorComponent.Dead)
                {
                    if (actions.LockedAction.Index == -3 || actions.LockedAction.ActionExecuteStep != unitEffects.CurrentGetHitEffect.Key.ActionExecuteStep)
                    {
                        //NORMAL DEATH - INSTANTLY DIE
                        if (unitEffects.DisplayDeathSkull)
                            m_UISystem.TriggerUnitDeathUI(e);

                        if (unitEffects.BodyPartBloodParticleSystem)
                        {
                            Death(animatorComponent, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor, unitEffects.BodyPartBloodParticleSystem);
                        }
                        else
                        {
                            Death(animatorComponent, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor);
                        }

                        //unitEffects.GetHitEffects.Remove(unitEffects.GetHitEffects.ElementAt(i).Key);
                    }
                    else
                    {
                        //SECOND WIND DEATH - DIE WHEN ANIM IS DONE

                        //if(animatorComponent.) if character is not red turn it red
                        if (unitEffects.SecondWindParticleSystemInstance)
                        {
                            ParticleSystem ps = unitEffects.SecondWindParticleSystemInstance;

                            if (!ps.isPlaying)
                                ps.Play();
                        }

                        if (animatorComponent.AnimationEvents.EventTriggered)
                        {
                            if (unitEffects.SecondWindParticleSystemInstance)
                            {
                                ParticleSystem ps = unitEffects.SecondWindParticleSystemInstance;

                                if (ps.isPlaying)
                                    ps.Stop();
                            }

                            if (unitEffects.DisplayDeathSkull)
                                m_UISystem.TriggerUnitDeathUI(e);

                            if (unitEffects.BodyPartBloodParticleSystem)
                            {
                                Death(animatorComponent, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value,unitEffects.PlayerColor, unitEffects.BodyPartBloodParticleSystem);
                            }
                            else
                            {
                                Death(animatorComponent, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor);
                            }

                            //unitEffects.GetHitEffects.Remove(unitEffects.GetHitEffects.ElementAt(i).Key);

                        }
                    }
                }
            }

            animatorComponent.Animator.SetInteger("Armor", (int)unitEffects.CurrentArmor);
        });

        playerVisionData.Dispose();
        gameStates.Dispose();
    }

    public void TriggerActionEffect(Action inAction, long unitID, Transform hitTransform, int spawnShieldOrbits = 0)
    {
        var playerVisionData = m_PlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
        var playerVisionHash = new HashSet<Vector3f>(playerVisionData[0].CellsInVisionrange);

        //Validate targets from CellgridMethods (ActionHelperMethods whenever we create it)
        HashSet<Vector3f> coordsToTrigger = new HashSet<Vector3f> { inAction.Targets[0].TargetCoordinate };

        if (inAction.Targets[0].Mods.Count != 0)
        {
            foreach (CoordinatePositionPair p in inAction.Targets[0].Mods[0].CoordinatePositionPairs)
            {
                coordsToTrigger.Add(p.CubeCoordinate);
            }
        }


        Entities.With(m_UnitData).ForEach((Entity e, UnitEffects unitEffects, ref FactionComponent.Component faction, ref CubeCoordinate.Component cubeCoord) =>
        {
        if (playerVisionHash.Contains(cubeCoord.CubeCoordinate))
        {
            //only apply effect if target unit is valid or pass correct coordinates
            if (coordsToTrigger.Contains(unitEffects.LastStationaryCoordinate) && m_PathFindingSystem.ValidateUnitTarget(e, (UnitRequisitesEnum)(int)inAction.Effects[0].ApplyToRestrictions, unitID, faction.Faction))
            {
                if (unitEffects.AxaShield && spawnShieldOrbits != 0)
                {
                    //if we need to spawn shieldorbits spaWN SHIELDorbits
                    for (int i = 0; i < spawnShieldOrbits; i++)
                    {
                        AxaShieldOrbit go = Object.Instantiate(unitEffects.AxaShield.OrbitPrefab, unitEffects.AxaShield.transform.position, Quaternion.LookRotation(hitTransform.position - unitEffects.AxaShield.transform.position), unitEffects.AxaShield.transform);
                        go.GetComponent<MovementAnimComponent>().RotationAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), 0f);
                        unitEffects.AxaShield.Orbits.Add(go);
                    }
                }
                    unitEffects.GetHitEffects.Add(inAction, hitTransform.position);
                }
            }
        });

        playerVisionData.Dispose();

    }

    public void LaunchProjectile(Projectile projectileFab, Transform spawnTransform, Vector3 targetPos, Action inAction, long unitId, Vector3f originCoord, float yOffset = 0)
    {
        var playerVisionData = m_PlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
        var playerVisionHash = new HashSet<Vector3f>(playerVisionData[0].CellsInVisionrange);

        bool AoEcontainsTarget = false;

        if (inAction.Targets[0].Mods.Count != 0 && !AoEcontainsTarget)
        {
            foreach (CoordinatePositionPair p in inAction.Targets[0].Mods[0].CoordinatePositionPairs)
            {
                if (playerVisionHash.Contains(p.CubeCoordinate))
                    AoEcontainsTarget = true;
            }
        }

        //if any coord in an AoE is visible or Launching unit is visible or target unit is visible, spawn a projectile
        if (AoEcontainsTarget || playerVisionHash.Contains(inAction.Targets[0].TargetCoordinate) || playerVisionHash.Contains(originCoord))
        {
            //save targetPosition / targetYOffset on units?
            Vector3 offSetTarget = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);

            List<Vector3> travellingPoints = new List<Vector3>();

            //if(projectileFab.MaxHeight > 0)
            //{

            //THIS USES SINUS CALC FOR STRAIGHT LINES -- CHANGE METHOD TO HANDLE STRAIGHT LINES WHITOUT CALCULATING SINUS STUFF
            travellingPoints.AddRange(m_HighlightingSystem.CalculateSinusPath(spawnTransform.position, offSetTarget, projectileFab.MaxHeight));
            //Debug.Log(travellingPoints.Count);
            //}
            //else
            //{
            //travellingPoints.Add(spawnTransform.position);
            //travellingPoints.Add(offSetTarget);
            //}

            //Quaternion lookRotation = new Quaternion();
            Vector3 distance = offSetTarget - spawnTransform.position;
            //lookRotation.SetLookRotation(distance);

            /*

            if (projectileFab.BaseJoint)
            {
                projectileFab.BaseJoint.connectedBody = spawnTransform.GetComponent<Rigidbody>();
            }
            */

            foreach (SpringJoint s in projectileFab.SpringJoints)
            {
                s.maxDistance = distance.magnitude / projectileFab.SpringJoints.Count;
            }

            Projectile projectile = Object.Instantiate(projectileFab, spawnTransform.position, spawnTransform.rotation, spawnTransform.root);
            projectile.UnitId = unitId;
            projectile.Action = inAction;
            projectile.SpawnTransform = spawnTransform;
            projectile.TravellingCurve = travellingPoints;
            projectile.IsTravelling = true;
        }

        playerVisionData.Dispose();
    }

    public void Death(AnimatorComponent animatorComponent, Action action, Vector3 position,Color playerColor, PlayerColor_ParticleSystem bodyPartParticle = null, float delay = 0)
    {
        var garbageCollector = m_GarbageCollection.ToComponentArray<GarbageCollectorComponent>()[0];

        if (bodyPartParticle)
        {
            int random = Random.Range(0, animatorComponent.RagdollRigidBodies.Count);
            int random2 = Random.Range(0, animatorComponent.RagdollRigidBodies.Count);

            for (int i = 0; i < animatorComponent.RagdollRigidBodies.Count; i++)
            {
                if (i == random || i == random2)
                {
                    PlayerColor_ParticleSystem go = Object.Instantiate(bodyPartParticle, animatorComponent.RagdollRigidBodies[i].position, Quaternion.identity, animatorComponent.RagdollRigidBodies[i].transform);
                    for (int ii = 0; ii < go.SetParticleSystem_BaseColor.Count; ii++)
                    {
                        go.SetParticleSystem_BaseColor[ii].startColor = playerColor;
                    }
                }
            }
        }

        foreach (GameObject go in animatorComponent.DisableOnDeathObjects)
        {
            go.SetActive(false);
        }

        foreach (Transform t in animatorComponent.Props)
        {
            t.parent = animatorComponent.Animator.transform;
        }

        //dismemberment
        foreach (CharacterJoint j in animatorComponent.DismemberJoints)
        {
            float rand = Random.Range(0f, 1f);
            if(rand <= animatorComponent.DismemberPercentage)
            {
                j.transform.parent = animatorComponent.Animator.transform;
                Object.Destroy(j);
            }
        }


        //Enable ragdoll behaviour
        animatorComponent.Animator.transform.parent = GarbageCollection.transform;
        animatorComponent.Animator.enabled = false;
        garbageCollector.GarbageObjects.Add(animatorComponent.Animator.gameObject);


        HashSet<Rigidbody> ragdollHash = new HashSet<Rigidbody>();

        foreach (Rigidbody r in animatorComponent.RagdollRigidBodies)
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

        animatorComponent.Dead = true;
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
