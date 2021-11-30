using Unity.Entities;
using Improbable.Gdk.Core;
using Generic;
using Player;
using Unit;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Cell;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class SimulatedPlayerSystem : JobComponentSystem
    {

        ComponentUpdateSystem m_ComponentUpdateSystem;
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<GameState.Component>()
            );

            m_PlayerData = GetEntityQuery(
                ComponentType.ReadOnly<PlayerEnergy.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<Vision.Component>(),
                ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                ComponentType.ReadWrite<PlayerState.Component>(),
                ComponentType.ReadWrite<SimulatedPlayer.Component>()
            );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_GameStateData.CalculateEntityCount() != 1 || m_PlayerData.CalculateEntityCount() == 0)
                return inputDeps;

            var gameState = m_GameStateData.GetSingleton<GameState.Component>();
            var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
            var simulatedPlayerComp = m_PlayerData.GetSingleton<SimulatedPlayer.Component>();

            var ropeEndEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.RopeEndEvent.Event>();

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                if(simulatedPlayerComp.ActionsSelected)
                {
                    simulatedPlayerComp.ActionsSelected = false;
                    m_PlayerData.SetSingleton(simulatedPlayerComp);
                }
                if(playerState.CurrentState == PlayerStateEnum.ready)
                {
                    playerState.CurrentState = PlayerStateEnum.waiting;
                    m_PlayerData.SetSingleton(playerState);
                }
            }
            else if (playerState.CurrentState != PlayerStateEnum.ready && simulatedPlayerComp.ActionsSelected)
            {
                playerState.CurrentState = PlayerStateEnum.ready;
                m_PlayerData.SetSingleton(playerState);
            }

            if(ropeEndEvents.Count > 0)
            {
                playerState.CurrentState = PlayerStateEnum.ready;
                m_PlayerData.SetSingleton(playerState);
            }

            /*
            if(!simulatedPlayerComp.IsSimulatedPlayer)
            {
                simulatedPlayerComp.IsSimulatedPlayer = true;
                m_PlayerData.SetSingleton(simulatedPlayerComp);
            }
            */

            return inputDeps;
        }
    }
}


