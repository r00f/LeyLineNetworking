using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unit;
using Improbable;
using LeyLineHybridECS;
using Generic;
using Player;
using Unity.Collections;
using Improbable.Gdk.Core;

//[DisableAutoCreation]
public class ProjectileSystem : ComponentSystem
{
    private EntityQuery m_GameStateData;
    private EntityQuery m_MoveAnimData;
    private EntityQuery m_ProjectileData;
    private EntityQuery m_UnitData;
    EntityQuery m_PlayerData;
    bool initialized;
    ActionEffectsSystem m_ActionEffectSystem;
    ILogDispatcher logger;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_ProjectileData = GetEntityQuery(
        ComponentType.ReadWrite<Projectile>(),
        ComponentType.ReadWrite<Transform>()
        );

        m_MoveAnimData = GetEntityQuery(
        ComponentType.ReadWrite<MovementAnimComponent>(),
        ComponentType.ReadWrite<Transform>()
        );



    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        logger = Worlds.ClientWorld.World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_ActionEffectSystem = Worlds.ClientWorld.World.GetExistingSystem<ActionEffectsSystem>();
    }

    private bool WorldsInitialized()
    {
        if (Worlds.ClientWorld != null)
        {
            if(!initialized)
            {
                m_UnitData = Worlds.ClientWorld.CreateEntityQuery(
                    ComponentType.ReadWrite<AnimatorComponent>()
                );

                m_GameStateData = Worlds.ClientWorld.CreateEntityQuery(
                    ComponentType.ReadOnly<GameState.Component>()
                );

                m_PlayerData = Worlds.ClientWorld.CreateEntityQuery(
                ComponentType.ReadOnly<Vision.Component>(),
                ComponentType.ReadOnly<PlayerState.HasAuthority>()
                );

                initialized = true;
            }
        }

        return initialized;
    }

    protected override void OnUpdate()
    {
        if(WorldsInitialized())
        {
            if (m_GameStateData.CalculateEntityCount() == 0)
                return;

            var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
            /*
            if(m_ProjectileData.CalculateEntityCount() > 0)
            {
                
                logger.HandleLog(LogType.Warning,
                new LogEvent("ProjectileCount")
                .WithField("Count", m_ProjectileData.CalculateEntityCount()));
                
            }
            */
            Entities.With(m_ProjectileData).ForEach((Entity entity, Projectile projectile, Transform transform) =>
            {
                if(projectile.CollisionDetection)
                {
                    if (projectile.CollisionDetection.HasCollided)
                    {
                        //Debug.Log("Projectile Collision Detected");
                        projectile.DestinationReached = true;
                        projectile.FlagForDestruction = true;
                    }
                }

                if (projectile.DestroyAfterSeconds != 0)
                {
                    if (projectile.DestroyAfterSeconds > 0.05f)
                    {
                        projectile.DestroyAfterSeconds -= Time.DeltaTime;
                    }
                    else
                    {
                        projectile.FlagForDestruction = true;
                    }
                }

                if (!projectile.Launched && projectile.TravellingCurve.Count != 0)
                {
                    foreach (Rigidbody r in projectile.RigidbodiesToLaunch)
                    {
                        projectile.MovementDelay *= Vector3.Distance(projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset, r.position);
                        Vector3 direction = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset - r.position;
                        r.AddForce(direction * projectile.LaunchForce, projectile.LaunchForceMode);
                    }
                    projectile.Launched = true;
                }

                if (projectile.DegreesPerSecond != 0 && !projectile.DestinationReached)
                    transform.RotateAround(transform.position, transform.forward, projectile.DegreesPerSecond * Time.DeltaTime);

                if (projectile.MovementDelay > 0)
                {
                    projectile.MovementDelay -= Time.DeltaTime;
                }
                else if (projectile.IsTravelling)
                {
                    if (projectile.ArriveInstantly)
                    {
                        if (projectile.RigidbodiesToLaunch.Count == 0)
                            transform.position = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset;

                        projectile.DestinationReached = true;
                    }
                    else if (projectile.CurrentTargetId < projectile.TravellingCurve.Count - 1 - projectile.TravellingCurveCutOff)
                    {
                        float dist = Vector3.Distance(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1]);
                        if (projectile.MovementPercentage >= 1f)
                        {
                            projectile.MovementPercentage = 0;
                            projectile.CurrentTargetId++;
                        }
                        else
                        {
                            projectile.MovementPercentage += Time.DeltaTime * projectile.TravellingSpeed / dist;
                            if (projectile.RigidbodiesToLaunch.Count == 0)
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
                            foreach (Rigidbody r in projectile.RigidbodiesToLaunch)
                            {
                                r.AddForce(Vector3.up * projectile.ContractUpForce, ForceMode.Acceleration);

                            }
                            foreach (SpringJoint s in projectile.SpringJoints)
                            {
                                s.maxDistance = Mathf.Lerp(s.maxDistance, 0, Time.DeltaTime * projectile.ContractSpeed);
                            }
                        }

                        if(projectile.ParticleSystemsStopWaitTime > 0)
                        {
                            projectile.ParticleSystemsStopWaitTime -= Time.DeltaTime;

                        }
                        else
                        {
                            foreach (ParticleSystem p in projectile.ParticleSystemsToStop)
                            {
                                p.Stop();
                            }
                        }

                        if (!projectile.EffectTriggered)
                        {
                            /*
                            logger.HandleLog(LogType.Warning,
                            new LogEvent("TriggerProjectileEvent")
                            .WithField("InAction", projectile.UnitId));
                            */

                            m_ActionEffectSystem.TriggerActionEffect(projectile.UnitFaction, projectile.Action, projectile.UnitId, projectile.PhysicsExplosionOrigin, gameStates[0], projectile.AxaShieldOrbitCount);

                            if (projectile.DestinationExplosionPrefab && projectile.ExplosionSpawnTransform)
                            {
                                Object.Instantiate(projectile.DestinationExplosionPrefab, projectile.ExplosionSpawnTransform.position, Quaternion.identity, projectile.transform.parent);
                            }

                            if (projectile.ExplosionEventEmitter)
                            {
                                projectile.ExplosionEventEmitter.Play();
                            }

                            if (projectile.ExplosionParticleSystem)
                            {
                                projectile.ExplosionParticleSystem.Play();
                            }

                            foreach (GameObject go in projectile.DisableAtDestinationObjects)
                            {
                                go.SetActive(false);
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
                    foreach (GameObject go in projectile.DisableBeforeDestructionObjects)
                    {
                        go.SetActive(false);
                    }
                    if(projectile)
                        GameObject.Destroy(projectile.gameObject, 0.5f);

                    projectile.QueuedForDestruction = true;
                }
            });

            Entities.With(m_MoveAnimData).ForEach((Entity entity, MovementAnimComponent anim, Transform transform) =>
            {
                if (anim.DegreesPerSecond != 0 && transform)
                    transform.RotateAround(transform.position, transform.right + anim.RotationAxis, anim.DegreesPerSecond * Time.DeltaTime);

                if(anim.RandomizeAxis != Vector3.zero)
                {
                    if (anim.RandomizeAxis.x != 0)
                    {
                        anim.RotationAxis.x = Mathf.Lerp(anim.RotationAxis.x, anim.RotationAxis.x + Random.Range(-.1f, .1f), Time.DeltaTime);
                    }
                    if (anim.RandomizeAxis.y != 0)
                    {
                        anim.RotationAxis.y = Mathf.Lerp(anim.RotationAxis.y, anim.RotationAxis.y + Random.Range(-.1f, .1f), Time.DeltaTime);
                    }
                    if (anim.RandomizeAxis.z != 0)
                    {
                        anim.RotationAxis.z = Mathf.Lerp(anim.RotationAxis.z, anim.RotationAxis.z += Random.Range(-.1f, .1f), Time.DeltaTime);
                    }
                }
            });

            gameStates.Dispose();
            //playerVisionData.Dispose();
        }
    }
}
