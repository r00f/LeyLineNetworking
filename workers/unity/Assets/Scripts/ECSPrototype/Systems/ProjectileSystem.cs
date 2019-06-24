using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using Player;
using Generic;
using Improbable.Gdk.ReactiveComponents;

public class ProjectileSystem : ComponentSystem
{
    struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameStatesData;
        public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
    }

    [Inject]
    GameStateData m_GameStateData;

    struct ProjectileData
    {
        public readonly int Length;
        public ComponentArray<Projectile> Projectiles;
        public ComponentArray<Transform> Transforms;
        public EntityArray Entities;
    }

    [Inject]
    ProjectileData m_ProjectileData;

    struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public ComponentDataArray<PlayerState.Component> PlayerStateData;
    }

    [Inject]
    PlayerData m_PlayerData;

    [Inject]
    ActionEffectsSystem m_ActionEffectSystem;


    protected override void OnUpdate()
    {
        
        if (m_PlayerData.Length != 0 && m_GameStateData.Length != 0)
        {
            var gameState = m_GameStateData.GameStatesData[0].CurrentState;
            if (gameState != GameStateEnum.planning)
            {
                var playerstate = m_PlayerData.PlayerStateData[0];
                var endStepReady = playerstate.EndStepReady;
                if (m_ProjectileData.Length == 0)
                {
                    endStepReady = true;
                }
                else {
                    endStepReady = false;
                }

                playerstate.EndStepReady = endStepReady;
                m_PlayerData.PlayerStateData[0] = playerstate;
            }
        }
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
                    m_ActionEffectSystem.TriggerActionEffect(projectile.EffectOnDetonation, projectile.CoordinatesToTrigger);

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


}
