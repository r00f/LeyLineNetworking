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

    UISystem m_UISystem;
    PathFindingSystem m_PathFindingSystem;
    UnitAnimationSystem m_UnitAnimationSystem;
    HighlightingSystem m_HighlightingSystem;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<Health.Component>(),
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
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);

            if(gameStates[0].CurrentState == GameStateEnum.planning)
            {
                unitEffects.CurrentArmor = 0;
                unitEffects.CurrentHealth = health.CurrentHealth;
            }

            //FeedBack to incoming Attacks / Heals
            if (unitEffects.ActionEffectTrigger)
            {
                float randomYoffset = Random.Range(0.5f, 1f);

                foreach(ActionEffect effect in unitEffects.Action.Effects)
                {
                    switch (effect.EffectType)
                    {
                        case EffectTypeEnum.deal_damage:

                            uint damageAmount = effect.DealDamageNested.DamageAmount;

                            Debug.Log("DealDamage: " + damageAmount);

                            if (unitEffects.CurrentArmor > 0)
                            {
                                //if we take less damage than our last amor amount
                                if (damageAmount < unitEffects.CurrentArmor)
                                {
                                    //do armor damage Stuff
                                    unitEffects.CurrentArmor -= damageAmount;
                                    m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, 5f);
                                    m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.yellow);
                                }
                                else
                                {
                                    uint remainingDamage = damageAmount - unitEffects.CurrentArmor;

                                    Object.Instantiate(unitEffects.BloodParticleSystem, animatorComponent.transform.position + new Vector3(0, randomYoffset, 0), Quaternion.identity);
                                    m_UISystem.SetHealthFloatText(e, false, remainingDamage, Color.red);
                                    animatorComponent.Animator.SetTrigger("GetHit");

                                    //shatter shield
                                    m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, .4f, true);

                                    unitEffects.CurrentArmor = 0;

                                    if ((int)unitEffects.CurrentHealth - (int)remainingDamage > 0)
                                    {
                                        unitEffects.CurrentHealth -= remainingDamage;
                                    }
                                    else
                                    {
                                        Debug.Log("unitEffects.CurrentHealth Negative Set To 0");
                                        unitEffects.CurrentHealth = 0;
                                    }
                                }
                            }
                            else
                            {
                                Object.Instantiate(unitEffects.BloodParticleSystem, animatorComponent.transform.position + new Vector3(0, randomYoffset, 0), Quaternion.identity);
                                m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.red);
                                animatorComponent.Animator.SetTrigger("GetHit");

                                //Debug.Log((int)unitEffects.CurrentHealth - (int)unitEffects.Action.Effects[0].DealDamageNested.DamageAmount);

                                if ((int)unitEffects.CurrentHealth - (int)damageAmount > 0)
                                {
                                    unitEffects.CurrentHealth -= damageAmount;
                                }
                                else
                                {
                                    Debug.Log("unitEffects.CurrentHealth Negative Set To 0");
                                    unitEffects.CurrentHealth = 0;
                                }
                            }

                            if (unitEffects.CurrentHealth == 0 && health.CurrentHealth == 0)
                            {
                                //Object.Instantiate(unitEffects.BloodParticleSystem, animatorComponent.transform.position + new Vector3(0, randomYoffset, 0), Quaternion.identity);
                                if (unitEffects.BodyPartBloodParticleSystem)
                                {
                                    m_UnitAnimationSystem.Death(animatorComponent, unitEffects.BodyPartBloodParticleSystem);
                                }
                                else
                                {
                                    m_UnitAnimationSystem.Death(animatorComponent);
                                }
                            }
                            break;

                        case EffectTypeEnum.gain_armor:
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
        });

        gameStates.Dispose();
    }

    public void TriggerActionEffect(Action action, long unitID/*EffectTypeEnum inEffectType,  uint healthAmount, uint armorAmount*/)
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
}
