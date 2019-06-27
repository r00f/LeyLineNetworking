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

            if (projectile.IsTravelling)
            {
                if (projectile.CurrentTargetId < projectile.TravellingCurve.Count -1)
                {
                    //use distance as well
                    float dist = Vector3.Distance(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1]);
                    projectile.MovementPercentage += Time.deltaTime * projectile.TravellingSpeed / dist;

                    if (projectile.MovementPercentage >= 1f)
                    {
                        projectile.MovementPercentage = 0;
                        projectile.CurrentTargetId++;
                    }
                    else
                    {
                        transform.position = Vector3.Lerp(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1], projectile.MovementPercentage);
                    }
                }
                else
                {
                    Debug.Log("Reached Destination");
                    TriggerUnitActionEffect(projectile.EffectOnDetonation, projectile.CoordinatesToTrigger);

                    //Stop looping Particlesystems and emit explosion burst

                    ParticleSystem bodyPs = projectile.BodyParticleSystem;
                    bodyPs.Stop();

                    ParticleSystem trailPs = projectile.TrailParticleSystem;
                    trailPs.Stop();

                    ParticleSystem explosionPs = projectile.ExplosionParticleSystem;
                    explosionPs.Emit(100);

                    GameObject.Destroy(projectile.gameObject, 1);
                    PostUpdateCommands.DestroyEntity(entity);
                }
            }
        }
    }


    void TriggerUnitActionEffect(EffectTypeEnum inEffectType, HashSet<Vector3f> inCubeCoordinates)
    {
        for (int i = 0; i < unitGroup.GetEntityArray().Length; i++)
        {
            var animatorComponent = unitGroup.GetComponentArray<AnimatorComponent>()[i];

            if (inCubeCoordinates.Contains(animatorComponent.LastStationaryCoordinate))
            {
                Debug.Log("Set Unit actionEffectTrigger");
                animatorComponent.ActionEffectTrigger = true;
            }
        }
    }
}
