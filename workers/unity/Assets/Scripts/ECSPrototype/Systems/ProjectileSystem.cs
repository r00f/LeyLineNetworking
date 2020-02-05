using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unit;
using Improbable;
using LeyLineHybridECS;
using Generic;
using Player;
using Unity.Collections;

//[DisableAutoCreation]
public class ProjectileSystem : ComponentSystem
{
    private EntityQuery m_GameStateDate;
    private EntityQuery m_MoveAnimData;
    private EntityQuery m_ProjectileData;
    private EntityQuery m_UnitData;
    bool initialized;
    ActionEffectsSystem m_ActionEffectSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ProjectileData = GetEntityQuery(
        ComponentType.ReadWrite<Projectile>(),
        ComponentType.ReadWrite<Transform>()
        );

    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_ActionEffectSystem = Worlds.ClientWorld.World.GetExistingSystem<ActionEffectsSystem>();
    }

    private bool WorldsInitialized()
    {
        if (Worlds.ClientWorld != null)
        {
            if(!initialized)
            {
                m_MoveAnimData = Worlds.ClientWorld.CreateEntityQuery(
                    ComponentType.ReadWrite<MovementAnimComponent>()
                );

                m_UnitData = Worlds.ClientWorld.CreateEntityQuery(
                    ComponentType.ReadWrite<AnimatorComponent>()
                );

                m_GameStateDate = Worlds.ClientWorld.CreateEntityQuery(
                    ComponentType.ReadOnly<GameState.Component>(),
                    ComponentType.ReadOnly<GameState.ComponentAuthority>()
                );

                m_GameStateDate.SetFilter(GameState.ComponentAuthority.Authoritative);
                initialized = true;
            }
        }

        return initialized;
    }

    protected override void OnUpdate()
    {
        if(WorldsInitialized())
        {
            Entities.With(m_ProjectileData).ForEach((Entity entity, Projectile projectile, Transform transform) =>
            {
                if(projectile.DestroyAfterSeconds != 0)
                {
                    if (projectile.DestroyAfterSeconds > 0.05f)
                    {
                        projectile.DestroyAfterSeconds -= Time.deltaTime;
                    }
                    else
                    {
                        projectile.FlagForDestruction = true;
                    }
                }
                if (projectile.ToungeEnd)
                {
                    if (!projectile.Launched && projectile.TravellingCurve.Count != 0)
                    {
                        projectile.MovementDelay *= Vector3.Distance(projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset, projectile.ToungeEnd.position);
                        Vector3 direction = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset - projectile.ToungeEnd.position;
                        projectile.ToungeEnd.AddForce(direction * projectile.LaunchForce, ForceMode.Impulse);
                        projectile.Launched = true;
                    }
                }

                if (projectile.MovementDelay > 0)
                {
                    projectile.MovementDelay -= Time.deltaTime;
                }
                else if (projectile.IsTravelling)
                {
                    if (projectile.ArriveInstantly)
                    {
                        if (!projectile.ToungeEnd)
                            transform.position = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset;

                        projectile.DestinationReached = true;
                    }
                    else if (projectile.CurrentTargetId < projectile.TravellingCurve.Count - 2)
                    {
                        float dist = Vector3.Distance(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1]);
                        if (projectile.MovementPercentage >= 1f)
                        {
                            projectile.MovementPercentage = 0;
                            projectile.CurrentTargetId++;
                        }
                        else
                        {
                            projectile.MovementPercentage += Time.deltaTime * projectile.TravellingSpeed / dist;
                            if (!projectile.ToungeEnd)
                            {
                                Vector3 pos = Vector3.Lerp(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1], projectile.MovementPercentage);
                                transform.position = pos;
                            }
                        }
                    }
                    else
                    {
                        projectile.DestinationReached = true;
                    }

                    if (projectile.DestinationReached)
                    {
                        //tounge contraction
                        if (projectile.SpringJoints.Count != 0)
                        {
                            projectile.ToungeEnd.AddForce(Vector3.up * projectile.ContractUpForce, ForceMode.Acceleration);
                            foreach (SpringJoint s in projectile.SpringJoints)
                            {
                                s.maxDistance = Mathf.Lerp(s.maxDistance, 0, Time.deltaTime * projectile.ContractSpeed);
                            }
                        }

                        //Stop looping Particlesystems and emit explosion burst
                        if (projectile.BodyParticleSystem)
                        {
                            ParticleSystem bodyPs = projectile.BodyParticleSystem;
                            bodyPs.Stop();
                        }
                        if (projectile.TrailParticleSystem)
                        {
                            ParticleSystem trailPs = projectile.TrailParticleSystem;
                            trailPs.Stop();
                        }


                        if (!projectile.EffectTriggered)
                        {
                            //Debug.Log("TriggerActionEffect from Projetile");
                            m_ActionEffectSystem.TriggerActionEffect(projectile.Action, projectile.UnitId, projectile.PhysicsExplosionOrigin);

                            if (projectile.ExplosionParticleSystem)
                            {
                                ParticleSystem explosionPs = projectile.ExplosionParticleSystem;
                                explosionPs.Play();
                            }
                            projectile.EffectTriggered = true;
                        }

                        if (projectile.DestroyAtDestination)
                        {
                            if (!projectile.FlagForDestruction)
                            {
                                projectile.FlagForDestruction = true;
                            }
                        }

                        //Explode(projectile);
                        projectile.IsTravelling = false;
                    }
                }

                if (projectile.FlagForDestruction && !projectile.QueuedForDestruction)
                {
                    if (projectile.ToungeEnd)
                        projectile.gameObject.SetActive(false);
                    GameObject.Destroy(projectile.gameObject, 0.5f);
                    projectile.QueuedForDestruction = true;
                    //PostUpdateCommands.DestroyEntity(entity);
                }
            });
        }
    }

    /*
    void TriggerUnitActionEffect(EffectTypeEnum inEffectType, HashSet<Vector3f> inCubeCoordinates)
    {
        var animatorComponents = m_UnitData.ToComponentArray<AnimatorComponent>(); 

        for(int i = 0; i < animatorComponents.Length; i++)
        {
            var animatorComponent = animatorComponents[i];

            if (inCubeCoordinates.Contains(animatorComponent.LastStationaryCoordinate) && !animatorComponent.ActionEffectTrigger)
            {
                animatorComponent.ActionEffectTrigger = true;
            }
        }
    }
    */
}
