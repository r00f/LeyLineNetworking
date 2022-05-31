using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Generic;
using Unity.Jobs;
using Improbable.Gdk.Core;
using FMODUnity;

public class ProjectileSystem : JobComponentSystem
{
    private EntityQuery m_GameStateData;
    bool initialized;
    ActionEffectsSystem m_ActionEffectSystem;
    ILogDispatcher logger;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        initialized = false;
        if (Worlds.ClientWorldWorker != default)
        {
            logger = Worlds.ClientWorldWorker.World.GetExistingSystem<WorkerSystem>().LogDispatcher;
            m_ActionEffectSystem = Worlds.ClientWorldWorker.World.GetExistingSystem<ActionEffectsSystem>();
        }
    }

    private bool WorldsInitialized()
    {
        if (!initialized && Worlds.ClientWorldWorker != default && Worlds.ClientWorldWorker.World != null)
        {
            m_GameStateData = Worlds.ClientWorldWorker.World.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GameState.Component>()
            );
            initialized = true;
        }
        return initialized;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (WorldsInitialized())
        {
            if (m_GameStateData.CalculateEntityCount() == 0)
                return inputDeps;

            var gameState = m_GameStateData.GetSingleton<GameState.Component>();

            Entities.ForEach((Entity entity, Projectile projectile, Transform transform) =>
            {
                if(projectile.CollisionDetection)
                {
                    if (projectile.CollisionDetection.HasCollided)
                    {
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
                    if(projectile.EventEmitter && projectile.PlaySoundFX)
                    {
                        projectile.EventEmitter.Play();
                    }

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
                            if(!projectile.IsPreviewProjectile)
                                m_ActionEffectSystem.TriggerActionEffect(projectile.UnitFaction, projectile.Action, projectile.UnitId, projectile.PhysicsExplosionOrigin, gameState, projectile.AxaShieldOrbitCount);

                            if (projectile.DestinationExplosionPrefab && projectile.ExplosionSpawnTransform)
                            {
                                StudioEventEmitter explosion = Object.Instantiate(projectile.DestinationExplosionPrefab, projectile.ExplosionSpawnTransform.position, Quaternion.identity, projectile.transform.parent);
                                if (projectile.PlaySoundFX)
                                    explosion.Play();
                            }

                            if (projectile.ExplosionEventEmitter && projectile.PlaySoundFX)
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
                        Object.Destroy(projectile.gameObject, 0.5f);

                    projectile.QueuedForDestruction = true;
                }
            })
            .WithoutBurst()
            .Run();

            Entities.ForEach((Entity entity, MovementAnimComponent anim, Transform transform) =>
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
            })
            .WithoutBurst()
            .Run();
        }

        return inputDeps;
    }
}
