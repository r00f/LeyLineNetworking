using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Entities;
using Generic;
using Unit;
using System.Collections.Generic;
using Unity.Collections;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(UnitAnimationSystem))]
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

    GameObject GarbageCollection;
    EntityQuery m_GarbageCollection;


    protected override void OnCreate()
    {
        base.OnCreate();
        settings = Resources.Load<Settings>("Settings");


        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<Collider>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<AnimatorComponent>(),
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

        if (m_GameStateData.CalculateEntityCount() == 0)
        {
            gameStates.Dispose();
            return;
        }
          
        Entities.With(m_UnitData).ForEach((Entity e, AnimatorComponent animatorComponent, ref Health.Component health) =>
        {
            var unitCollider = EntityManager.GetComponentObject<Collider>(e);
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);

            if(gameStates[0].CurrentState == GameStateEnum.planning)
            {
                unitEffects.CurrentArmor = 0;
                unitEffects.CurrentHealth = health.CurrentHealth;
            }
            //FeedBack to incoming Attacks / Heals
            if (unitEffects.ActionEffectTrigger)
            {
                unitEffects.HitPosition = unitCollider.ClosestPoint(unitEffects.AttackPosition);

                //float randomYoffset = Random.Range(0.5f, 1f);

                foreach(ActionEffect effect in unitEffects.Action.Effects)
                {
                    switch (effect.EffectType)
                    {
                        case EffectTypeEnum.deal_damage:

                            uint damageAmount = effect.DealDamageNested.DamageAmount;

                            if (unitEffects.CurrentArmor > 0)
                            {
                                //if we take less damage than our last amor amount
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
                                    Object.Instantiate(unitEffects.BloodParticleSystem, unitEffects.HitPosition, Quaternion.identity);
                                    m_UISystem.SetHealthFloatText(e, false, remainingDamage, Color.red);
                                    m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, .4f, true);
                                    unitEffects.CurrentArmor = 0;

                                    if ((int)unitEffects.CurrentHealth - (int)remainingDamage > 0)
                                    {
                                        unitEffects.CurrentHealth -= remainingDamage;
                                    }
                                    else
                                    {
                                        //Debug.Log("unitEffects.CurrentHealth Negative Set To 0");
                                        unitEffects.CurrentHealth = 0;
                                    }
                                }
                            }
                            else
                            {
                                Object.Instantiate(unitEffects.BloodParticleSystem, unitEffects.HitPosition, Quaternion.identity);
                                m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.red);
                                animatorComponent.Animator.SetTrigger("GetHit");

                                //Debug.Log((int)unitEffects.CurrentHealth - (int)unitEffects.Action.Effects[0].DealDamageNested.DamageAmount);

                                if ((int)unitEffects.CurrentHealth - (int)damageAmount > 0)
                                {
                                    unitEffects.CurrentHealth -= damageAmount;
                                }
                                else
                                {
                                    //Debug.Log("unitEffects.CurrentHealth Negative Set To 0");
                                    unitEffects.CurrentHealth = 0;
                                }
                            }

                            animatorComponent.Animator.SetTrigger("GetHit");

                            if (unitEffects.CurrentHealth == 0 && health.CurrentHealth == 0)
                            {
                                //Object.Instantiate(unitEffects.BloodParticleSystem, animatorComponent.transform.position + new Vector3(0, randomYoffset, 0), Quaternion.identity);
                                if (unitEffects.BodyPartBloodParticleSystem)
                                {
                                    Death(animatorComponent, unitEffects.Action, unitEffects.AttackPosition, unitEffects.BodyPartBloodParticleSystem);
                                }
                                else
                                {
                                    Death(animatorComponent, unitEffects.Action, unitEffects.AttackPosition);
                                }
                            }
                            break;

                        case EffectTypeEnum.gain_armor:

                            //enter defensive stance in animator
                            
                            uint armorAmount = unitEffects.Action.Effects[0].GainArmorNested.ArmorAmount;
                            //do gainArmorStuff
                            unitEffects.CurrentArmor += armorAmount;
                            m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, 3f);
                            m_UISystem.SetHealthFloatText(e, true, armorAmount, Color.yellow);
                            break;
                    }
                }
                unitEffects.ActionEffectTrigger = false;
            }

            animatorComponent.Animator.SetInteger("Armor", (int)unitEffects.CurrentArmor);
        });

        gameStates.Dispose();
    }

    public void TriggerActionEffect(Action action, long unitID, Transform hitTransform/*EffectTypeEnum inEffectType,  uint healthAmount, uint armorAmount*/)
    {
        //Validate targets from CellgridMethods (ActionHelperMethods whenever we create it)
        HashSet<Vector3f> coordsToTrigger = new HashSet<Vector3f> { action.Targets[0].TargetCoordinate };

        if (action.Targets[0].Mods.Count != 0)
        {
            foreach (CoordinatePositionPair p in action.Targets[0].Mods[0].CoordinatePositionPairs)
            {
                coordsToTrigger.Add(p.CubeCoordinate);
            }
        }


        Entities.With(m_UnitData).ForEach((Entity e, UnitEffects unitEffects, ref FactionComponent.Component faction) =>
        {

            //only apply effect if target unit is valid or pass correct coordinates
            if (coordsToTrigger.Contains(unitEffects.LastStationaryCoordinate) && m_PathFindingSystem.ValidateTarget(e, (UnitRequisitesEnum)(int)action.Effects[0].ApplyToRestrictions, unitID, faction.Faction))
            {
                unitEffects.Action = action;
                unitEffects.AttackPosition = hitTransform.position;
                //Debug.Log("Set Unit actionEffectTrigger from actionEffectsSystem");
                unitEffects.ActionEffectTrigger = true;
            }
        });

        /*
        Entities.With(m_CellData).ForEach((MarkerGameObjects references, ref CubeCoordinate.Component coord) =>
        {
            if (inCubeCoordinates.Contains(coord.CubeCoordinate))
            {
                references.EffectType = inEffectType;
            }
        });
        */
    }

    public void LaunchProjectile(Projectile projectileFab, Transform spawnTransform, Vector3 targetPos, Action inAction, long unitId, float yOffset = 0)
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

    public void Death(AnimatorComponent animatorComponent, Action action, Vector3 position, GameObject bodyPartParticle = null)
    {
        var garbageCollector = m_GarbageCollection.ToComponentArray<GarbageCollectorComponent>()[0];
        animatorComponent.Dead = true;

        if (bodyPartParticle)
        {
            int random = Random.Range(0, animatorComponent.RagdollRigidBodies.Count);
            int random2 = Random.Range(0, animatorComponent.RagdollRigidBodies.Count);

            for (int i = 0; i < animatorComponent.RagdollRigidBodies.Count; i++)
            {
                if (i == random || i == random2)
                {
                    Object.Instantiate(bodyPartParticle, animatorComponent.RagdollRigidBodies[i].position, Quaternion.identity, animatorComponent.RagdollRigidBodies[i].transform);
                }
            }
        }

        foreach (GameObject go in animatorComponent.ObjectsToDisable)
        {
            go.SetActive(false);
        }

        foreach (Transform t in animatorComponent.Props)
        {
            t.parent = animatorComponent.Animator.transform;
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
        //EGG DEATHEXPLOSION 
        /*
        if (animatorComponent.DeathExplosionPos)
        {
            foreach (Rigidbody r in animatorComponent.RagdollRigidBodies)
            {
                r.AddExplosionForce(animatorComponent.DeathExplosionForce, animatorComponent.DeathExplosionPos.position, animatorComponent.DeathExplosionRadius);
            }
        }
        */
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
