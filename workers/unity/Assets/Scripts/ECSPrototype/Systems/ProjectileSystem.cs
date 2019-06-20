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

            if (projectile.isTravelling)
            {
                if (projectile.currentTargetId < projectile.travellingCurve.Count)
                {
                    if (projectile.transform.position != projectile.travellingCurve[projectile.currentTargetId])
                    {
                        projectile.transform.position = Vector3.MoveTowards(projectile.transform.position, projectile.travellingCurve[projectile.currentTargetId], Time.deltaTime * projectile.travellingSpeed);
                    }
                    else
                    {
                        projectile.currentTargetId++;
                    }
                }
                else
                {
                    Debug.Log("Reached Destination");
                    m_ActionEffectSystem.TriggerActionEffect(projectile.effectonDetonation, projectile.CoordinatesToTrigger);
                    PostUpdateCommands.DestroyEntity(entity);
                }
            }
        }
    }


}
