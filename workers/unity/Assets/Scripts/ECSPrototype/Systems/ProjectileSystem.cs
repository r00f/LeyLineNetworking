using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unit;
using Improbable;
using LeyLineHybridECS;

[DisableAutoCreation]
public class ProjectileSystem : ComponentSystem
{
    struct ProjectileData
    {
        public readonly int Length;
        public ComponentArray<Projectile> Projectiles;
        public ComponentArray<Transform> Transforms;
        public EntityArray Entities;
    }

    [Inject]
    ProjectileData m_ProjectileData;

    public struct MoveAnimData
    {
        public readonly int Length;
        public ComponentArray<MovementAnimComponent> MoveAnimComponents;
    }

    [Inject] MoveAnimData m_MoveAnimData;



    /*

    struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public ComponentDataArray<PlayerState.Component> PlayerStateData;
    }

    [Inject]
    PlayerData m_PlayerData;

    public struct UnitData
    {
        public readonly int Length;
        public ComponentArray<AnimatorComponent> AnimatorComponents;
    }

    [Inject] UnitData m_UnitData;

    struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameStatesData;
        public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
    }

    [Inject]
    GameStateData m_GameStateData;

    */


    ComponentGroup unitGroup;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

        unitGroup = Worlds.ClientWorld.CreateComponentGroup(
        ComponentType.Create<AnimatorComponent>()
        );

    }


    protected override void OnUpdate()
    {
        for (int i = 0; i < m_ProjectileData.Length; i++)
        {
            var projectile = m_ProjectileData.Projectiles[i];
            var entity = m_ProjectileData.Entities[i];
            var transform = m_ProjectileData.Transforms[i];

            if (projectile.ToungeEnd)
            {
                if (!projectile.Launched && projectile.TravellingCurve.Count != 0)
                {
                    Vector3 direction = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] - projectile.ToungeEnd.position;
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
                if(projectile.ArriveInstantly)
                {
                    if (!projectile.ToungeEnd)
                        transform.position = projectile.TravellingCurve[projectile.TravellingCurve.Count -1];
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
                        if(!projectile.ToungeEnd)
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

                if(projectile.DestinationReached)
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

                    if (!projectile.FlagForDestruction)
                    {
                        TriggerUnitActionEffect(projectile.EffectOnDetonation, projectile.CoordinatesToTrigger);

                        if (projectile.ExplosionParticleSystem)
                        {
                            ParticleSystem explosionPs = projectile.ExplosionParticleSystem;
                            explosionPs.Play();
                        }
                    }

                    if(projectile.ExplosionWaitTime > 0)
                    {
                        projectile.ExplosionWaitTime -= Time.deltaTime;
                    }
                    else
                    {
                        //Add ExplosionForce to all Rigidbodies in N range
                        var cols = Physics.OverlapSphere(projectile.PhysicsExplosionOrigin.position, projectile.ExplosionRadius);
                        var rigidbodies = new List<Rigidbody>();

                        foreach (var col in cols)
                        {
                            if (col.attachedRigidbody != null && !rigidbodies.Contains(col.attachedRigidbody) && col.gameObject.layer == 11 && !col.attachedRigidbody.isKinematic)
                            {
                                Debug.Log(col.name);
                                rigidbodies.Add(col.attachedRigidbody);
                            }
                        }
                        foreach (Rigidbody r in rigidbodies)
                        {
                            Debug.Log("RigidBodyInExplosionRange");
                            r.AddExplosionForce(projectile.ExplosionForce, projectile.PhysicsExplosionOrigin.position, projectile.ExplosionRadius);
                        }


                        if (projectile.DestroyAtDestination)
                            projectile.FlagForDestruction = true;

                        projectile.IsTravelling = false;

                    }
                }
            }

            if (projectile.FlagForDestruction && !projectile.QueuedForDestruction)
            {
                if(projectile.ToungeEnd)
                    projectile.gameObject.SetActive(false);
                GameObject.Destroy(projectile.gameObject, 0.5f);
                UpdateInjectedComponentGroups();
                projectile.QueuedForDestruction = true;
                //PostUpdateCommands.DestroyEntity(entity);
            }
        }

        for (int i = 0; i < m_MoveAnimData.Length; i++)
        {
            var moveAnim = m_MoveAnimData.MoveAnimComponents[i];
            foreach (Transform t in moveAnim.Transforms)
            {
                if (moveAnim.Continuous)
                {
                    if (moveAnim.DegreesPerSecond != 0)
                    {
                        float smooth = Time.deltaTime * moveAnim.DegreesPerSecond;
                        t.RotateAround(t.position, moveAnim.RotationAxis, smooth);
                    }
                }
                else
                {



                }
            }
        }
    }

    void TriggerUnitActionEffect(EffectTypeEnum inEffectType, HashSet<Vector3f> inCubeCoordinates)
    {
        for (int i = 0; i < unitGroup.GetEntityArray().Length; i++)
        {
            var animatorComponent = unitGroup.GetComponentArray<AnimatorComponent>()[i];

            if (inCubeCoordinates.Contains(animatorComponent.LastStationaryCoordinate) && !animatorComponent.ActionEffectTrigger)
            {
                Debug.Log("Set Unit actionEffectTrigger");
                animatorComponent.ActionEffectTrigger = true;
            }
        }
    }
}
