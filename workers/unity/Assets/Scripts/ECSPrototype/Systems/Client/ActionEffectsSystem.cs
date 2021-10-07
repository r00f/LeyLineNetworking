using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Entities;
using Generic;
using Unit;
using System.Collections.Generic;
using Unity.Jobs;
using Player;
using System.Linq;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(UnitAnimationSystem))]
public class ActionEffectsSystem : JobComponentSystem
{

    ILogDispatcher logger;

    Settings settings;
    UISystem m_UISystem;
    PathFindingSystem m_PathFindingSystem;
    UnitAnimationSystem m_UnitAnimationSystem;
    HighlightingSystem m_HighlightingSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    //EntityQuery m_UnitData;
    //EntityQuery m_CellData;
    EntityQuery m_GameStateData;
    //EntityQuery m_PlayerData;
    GameObject GarbageCollection;
    EntityQuery m_GarbageCollection;

    protected override void OnCreate()
    {
        base.OnCreate();
        settings = Resources.Load<Settings>("Settings");

        /*
        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<Vision.Component>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>()
        );


        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<CapsuleCollider>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<AnimatorComponent>(),
        ComponentType.ReadWrite<UnitEffects>(),
        ComponentType.ReadOnly<IsVisible>(),
        ComponentType.ReadWrite<UnitHeadUIReferences>()
        );

        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<MarkerState>(),
        ComponentType.ReadWrite<MarkerGameObjects>()
        );
        */

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
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;

        GarbageCollection = Object.FindObjectOfType<GarbageCollectorComponent>().gameObject;

        m_GarbageCollection = Worlds.DefaultWorld.CreateEntityQuery(
        ComponentType.ReadWrite<GarbageCollectorComponent>()
        );
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_GameStateData.CalculateEntityCount() == 0)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();

        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

        if (cleanUpStateEvents.Count > 0)
        {
            Entities.ForEach((Entity e, UnitComponentReferences componentReferences, ref Health.Component health, ref CubeCoordinate.Component coord, ref Actions.Component actions) =>
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
            })
            .WithoutBurst()
            .Run();
        }

        if (gameState.CurrentState != GameStateEnum.planning)
        {
            Entities.ForEach((Entity e, AnimatorComponent animator, ref Health.Component health, ref CubeCoordinate.Component coord, ref Actions.Component actions, ref IsVisible isVisible) =>
            {
                var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);
                var capsuleCollider = EntityManager.GetComponentObject<CapsuleCollider>(e);
                var headUI = EntityManager.GetComponentObject<UnitHeadUIReferences>(e);

                //DOES THE SAME FOR EACH GETHITEFFECT IN LIST (TWO DAMAGE NUMBERS AT THE SAME TIME) - ADD BEHAVIOUR TO ADD DAMAGE NUBERS AND ONLY DISPLAY 1 EFFECT 
                for (int i = 0; i < unitEffects.GetHitEffects.Count; i++)
                {
                    unitEffects.HitPosition = capsuleCollider.ClosestPoint(unitEffects.GetHitEffects.ElementAt(i).Value);
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
                                        int remainingDamage = (int) damageAmount;

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

                                        if (isVisible.Value == 1)
                                        {
                                            Object.Instantiate(unitEffects.DefenseParticleSystem, unitEffects.HitPosition, Quaternion.identity);
                                        }
                                        unitEffects.CurrentArmor -= damageAmount;
                                        m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, 5f);
                                        m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.yellow);
                                    }
                                    else
                                    {
                                        //shatter shield
                                        uint remainingDamage = damageAmount - unitEffects.CurrentArmor;

                                        if (isVisible.Value == 1)
                                        {
                                            Object.Instantiate(unitEffects.ShieldBrakeParticleSystem, unitEffects.HitPosition, Quaternion.identity);
                                        }

                                        m_UISystem.SetHealthFloatText(e, false, remainingDamage, Color.red, 0.4f);

                                        m_UISystem.SetArmorDisplay(e, unitEffects.CurrentArmor, .5f, true);

                                        unitEffects.CurrentArmor = 0;

                                        if ((int) unitEffects.CurrentHealth - (int) remainingDamage > 0)
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
                                    if (isVisible.Value == 1)
                                    {
                                        PlayerColor_ParticleSystem go = Object.Instantiate(unitEffects.BloodParticleSystem, unitEffects.HitPosition, Quaternion.LookRotation(dir));
                                        for (int ii = 0; ii < go.SetParticleSystem_BaseColor.Count; ii++)
                                        {
                                            ParticleSystem.MainModule main = go.SetParticleSystem_BaseColor[ii].main;
                                            main.startColor = unitEffects.PlayerColor;
                                        }
                                    }


                                    //if the unit survives
                                    if ((int) unitEffects.CurrentHealth - (int) damageAmount > 0)
                                    {
                                        m_UISystem.SetHealthFloatText(e, false, damageAmount, Color.red);
                                        unitEffects.CurrentHealth -= damageAmount;
                                    }
                                    else
                                    {
                                        unitEffects.CurrentHealth = 0;
                                    }
                                }

                                if (headUI.UnitHeadHealthBarInstance)
                                {
                                    if (headUI.IncomingDamage - damageAmount >= 0)
                                    {
                                        headUI.IncomingDamage -= damageAmount;
                                    }
                                    else
                                    {
                                        headUI.IncomingDamage = 0;
                                    }

                                    if (unitEffects.CurrentHealth + unitEffects.CurrentArmor >= health.MaxHealth)
                                    {
                                        headUI.UnitHeadHealthBarInstance.BgFill.fillAmount = 1 - ((float) (unitEffects.CurrentHealth + unitEffects.CurrentArmor) / (health.MaxHealth + unitEffects.CombinedArmor));
                                    }
                                    else
                                    {
                                        headUI.UnitHeadHealthBarInstance.BgFill.fillAmount = 1 - ((float) (unitEffects.CurrentHealth + unitEffects.CurrentArmor) / health.MaxHealth);
                                    }
                                }
                                if (animator.Animator)
                                    animator.Animator.SetTrigger("GetHit");

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

                if (unitEffects.CurrentHealth == 0 && !animator.Dead && isVisible.Value == 1)
                {
                    if (actions.LockedAction.Index == -3 || actions.LockedAction.ActionExecuteStep != unitEffects.CurrentGetHitEffect.Key.ActionExecuteStep)
                    {
                        //NORMAL DEATH - INSTANTLY DIE
                        if (unitEffects.DisplayDeathSkull)
                            m_UISystem.TriggerUnitDeathUI(e);

                        if (unitEffects.BodyPartBloodParticleSystem)
                        {
                            Death(ref animator, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor, unitEffects.BodyPartBloodParticleSystem);
                        }
                        else
                        {
                            Death(ref animator, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor);
                        }
                    }
                    else
                    {
                        //SECOND WIND DEATH - DIE WHEN ANIM IS DONE
                        if (unitEffects.SecondWindParticleSystemInstance)
                        {
                            ParticleSystem ps = unitEffects.SecondWindParticleSystemInstance;

                            if (!ps.isPlaying)
                                ps.Play();
                        }

                        if (animator.AnimationEvents.EventTriggered)
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
                                Death(ref animator, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor, unitEffects.BodyPartBloodParticleSystem);
                            }
                            else
                            {
                                Death(ref animator, unitEffects.CurrentGetHitEffect.Key, unitEffects.CurrentGetHitEffect.Value, unitEffects.PlayerColor);
                            }

                        }
                    }
                }

                if (animator.Animator)
                    animator.Animator.SetInteger("Armor", (int) unitEffects.CurrentArmor);
            })
            .WithoutBurst()
            .Run();
        }

        return inputDeps;
    }

    public void TriggerActionEffect(uint usingUnitFaction, Action usingUnitAction, long usingUnitID, Transform hitTransform, GameState.Component gameState, int spawnShieldOrbits = 0)
    {
        //Validate targets from CellgridMethods (ActionHelperMethods whenever we create it)
        HashSet<Vector3f> coordsToTrigger = new HashSet<Vector3f> { usingUnitAction.Targets[0].TargetCoordinate };

        if (usingUnitAction.Targets[0].Mods.Count != 0)
        {
            foreach (CoordinatePositionPair p in usingUnitAction.Targets[0].Mods[0].CoordinatePositionPairs)
            {
                coordsToTrigger.Add(p.CubeCoordinate);
            }
        }

        //ALL POSSIBLE TARGET UNITS
        Entities.ForEach((Entity targetEntity, UnitEffects targetUnitEffects, ref CubeCoordinate.Component targetCubeCoord) =>
        {
            if ((int)usingUnitAction.ActionExecuteStep < 2)
                AddGetHitEffect(targetEntity, targetUnitEffects.OriginCoordinate, coordsToTrigger, targetUnitEffects, spawnShieldOrbits, usingUnitID, hitTransform, usingUnitFaction, usingUnitAction, gameState);
            else
                AddGetHitEffect(targetEntity, targetUnitEffects.DestinationCoordinate, coordsToTrigger, targetUnitEffects, spawnShieldOrbits, usingUnitID, hitTransform, usingUnitFaction, usingUnitAction, gameState);
        })
        .WithoutBurst()
        .Run();
    }

    public void AddGetHitEffect(Entity targetEntity,  Vector3f targetUnitCoordinate, HashSet<Vector3f> targetCoordinates, UnitEffects targetUnitEffects, int spawnShieldOrbits, long usingUnitID, Transform usingUnitHitTransform, uint usingUnitfaction, Action usingUnitAction, GameState.Component gameState)
    {
        /*
        logger.HandleLog(LogType.Warning,
        new LogEvent("AddGetHitEffect")
        .WithField("InAction", usingUnitAction));
        logger.HandleLog(LogType.Warning,
        new LogEvent("AddGetHitEffect")
        .WithField("hitTransform", usingUnitHitTransform));
        */

        if (targetCoordinates.Contains(targetUnitCoordinate) && m_PathFindingSystem.ValidateUnitTarget(targetEntity, (UnitRequisitesEnum) (int) usingUnitAction.Effects[0].ApplyToRestrictions, usingUnitID, usingUnitfaction))
        {
            Vector3 getHitPosition;

            if (usingUnitHitTransform)
                getHitPosition = usingUnitHitTransform.position;
            else
                getHitPosition = CellGridMethods.CubeToPos(targetUnitCoordinate, gameState.MapCenter);

            if (targetUnitEffects.AxaShield && spawnShieldOrbits != 0)
            {
                //if we need to spawn shieldorbits spaWN SHIELDorbits
                for (int i = 0; i < spawnShieldOrbits; i++)
                {
                    AxaShieldOrbit go = Object.Instantiate(targetUnitEffects.AxaShield.OrbitPrefab, targetUnitEffects.AxaShield.transform.position, Quaternion.LookRotation(getHitPosition - targetUnitEffects.AxaShield.transform.position), targetUnitEffects.AxaShield.transform);
                    go.GetComponent<MovementAnimComponent>().RotationAxis = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), 0f);
                    targetUnitEffects.AxaShield.Orbits.Add(go);
                }
            }

            targetUnitEffects.GetHitEffects.Add(usingUnitAction, getHitPosition);
        }
    }

    public void LaunchProjectile(uint usingUnitFaction, Vision.Component playerVision, Projectile projectileFab, Transform spawnTransform, Vector3 targetPos, Action inAction, long unitId, Vector3f originCoord, float yOffset = 0)
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

        Projectile projectile = Object.Instantiate(projectileFab, spawnTransform.position, spawnTransform.rotation, spawnTransform.root);

        foreach(GameObject g in projectile.EnableIfVisibleObjects)
        {
            g.SetActive(false);
        }

        projectile.UnitId = unitId;
        projectile.Action = inAction;
        projectile.SpawnTransform = spawnTransform;
        projectile.UnitFaction = usingUnitFaction;

        //if any coord in an AoE is visible or Launching unit is visible or target unit is visible make projectile visible
        if (AoEcontainsTarget || playerVision.CellsInVisionrange.ContainsKey(inAction.Targets[0].TargetCoordinate) || playerVision.CellsInVisionrange.ContainsKey(originCoord))
        {
            foreach (GameObject g in projectile.EnableIfVisibleObjects)
            {
                g.SetActive(true);
            }
        }

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

    public void Death(ref AnimatorComponent animator, Action action, Vector3 position, Color playerColor, PlayerColor_ParticleSystem bodyPartParticle = null, float delay = 0)
    {
        var garbageCollector = m_GarbageCollection.ToComponentArray<GarbageCollectorComponent>()[0];

        if (bodyPartParticle)
        {
            int random = Random.Range(0, animator.RagdollRigidBodies.Count);
            int random2 = Random.Range(0, animator.RagdollRigidBodies.Count);

            for (int i = 0; i < animator.RagdollRigidBodies.Count; i++)
            {
                if (i == random || i == random2)
                {
                    PlayerColor_ParticleSystem go = Object.Instantiate(bodyPartParticle, animator.RagdollRigidBodies[i].position, Quaternion.identity, animator.RagdollRigidBodies[i].transform);
                    for (int ii = 0; ii < go.SetParticleSystem_BaseColor.Count; ii++)
                    {
                        ParticleSystem.MainModule main = go.SetParticleSystem_BaseColor[ii].main;
                        main.startColor = playerColor;
                    }
                }
            }
        }

        foreach (GameObject go in animator.DisableOnDeathObjects)
        {
            go.SetActive(false);
        }

        foreach (Transform t in animator.Props)
        {
            t.parent = animator.Animator.transform;
        }

        //dismemberment
        foreach (CharacterJoint j in animator.DismemberJoints)
        {
            float rand = Random.Range(0f, 1f);
            if(rand <= animator.DismemberPercentage)
            {
                j.transform.parent = animator.Animator.transform;
                Object.Destroy(j);
            }
        }


        //Enable ragdoll behaviour
        animator.Animator.transform.parent = GarbageCollection.transform;
        animator.Animator.enabled = false;
        garbageCollector.GarbageObjects.Add(animator.Animator.gameObject);


        HashSet<Rigidbody> ragdollHash = new HashSet<Rigidbody>();

        foreach (Rigidbody r in animator.RagdollRigidBodies)
        {
            ragdollHash.Add(r);
            garbageCollector.GarbageRigidbodies.Add(r);
            r.isKinematic = false;
        }

        if(action.Effects != null)
        {
            foreach (ActionEffect e in action.Effects)
            {
                if (e.EffectType == EffectTypeEnum.deal_damage)
                {
                    //apply physics forces to ragdoll
                    Explode(position, e.DealDamageNested.ExplosionRadius, e.DealDamageNested.ExplosionForce, e.DealDamageNested.UpForce, ragdollHash);
                }
            }
        }

        animator.Dead = true;
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
            //Debug.Log("RigidBodyInExplosionRange");
            r.AddForce(Vector3.up * upForce, ForceMode.Impulse);
            r.AddExplosionForce(explosionForce, explosionOrigin, explosionRadius);
        }

        
    }

}
