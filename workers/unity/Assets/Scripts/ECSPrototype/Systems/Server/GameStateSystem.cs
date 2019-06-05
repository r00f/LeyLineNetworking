using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Player;
using Cell;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class GameStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
            public ComponentDataArray<GameState.Component> GameStateData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] Data m_Data;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<PlayerAttributes.Component> AttributesData;
            public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] UnitData m_UnitData;

        public struct SpawnCellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<CellAttributesComponent.Component>> AuthorativeData;
            public readonly ComponentDataArray<IsSpawn.Component> IsSpawnData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<UnitToSpawn.Component> UnitToSpawnData;
        }

        [Inject] SpawnCellData m_SpawnCellData;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Data.Length; i++)
            {
                var gameState = m_Data.GameStateData[i];
                var gameStateWorldIndex = m_Data.WorldIndexData[i].Value;

                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
                        #if UNITY_EDITOR

                        gameState.CurrentState = GameStateEnum.spawning;
                        m_Data.GameStateData[i] = gameState;

                        #else
                        if (AllSpawnsInitialized(gameStateWorldIndex))
                        {
                            gameState.CurrentState = GameStateEnum.spawning;
                            m_Data.GameStateData[i] = gameState;
                        }
                        #endif
                        break;
                    case GameStateEnum.planning:
                        if (AllPlayersReady(gameStateWorldIndex) || gameState.CurrentPlanningTime <= 0)
                        {
                            gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                            gameState.CurrentState = GameStateEnum.spawning;
                            m_Data.GameStateData[i] = gameState;
                        }
                        else if(AnyPlayerReady(gameStateWorldIndex))
                        {
                            gameState.CurrentPlanningTime -= Time.deltaTime;
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.spawning:
                        gameState.CurrentState = GameStateEnum.defending;
                        m_Data.GameStateData[i] = gameState;
                        break;
                    case GameStateEnum.defending:
                        gameState.CurrentState = GameStateEnum.attacking;
                        m_Data.GameStateData[i] = gameState;
                        break;
                    case GameStateEnum.attacking:
                        gameState.CurrentState = GameStateEnum.moving;
                        m_Data.GameStateData[i] = gameState;
                        break;
                    case GameStateEnum.moving:
                        if (NoUnitMoving(gameStateWorldIndex))
                        {
                            gameState.CurrentState = GameStateEnum.calculate_energy;
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.calculate_energy:
                        if (gameState.CurrentWaitTime > 0)
                        {
                            gameState.CurrentWaitTime -= Time.deltaTime;
                            m_Data.GameStateData[i] = gameState;
                        }
                        else
                        {
                            gameState.CurrentPlanningTime = gameState.PlanningTime;
                            gameState.CurrentState = GameStateEnum.cleanup;
                            m_Data.GameStateData[i] = gameState;
                        }
                        break;
                    case GameStateEnum.cleanup:
                        gameState.CurrentState = GameStateEnum.planning;
                        m_Data.GameStateData[i] = gameState;
                        break;
                    case GameStateEnum.game_over:
                        break;
                }

                #if UNITY_EDITOR

                #else

                if(gameState.PlayersOnMapCount < 2 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    gameState.CurrentState = GameStateEnum.waiting_for_players;
                    m_Data.GameStateData[i] = gameState;
                }

                #endif
            }

        }
        private bool AllSpawnsInitialized(uint gameStateWorldIndex)
        {
            for (int i = 0; i < m_SpawnCellData.Length; i++)
            {
                var cellWorldIndex = m_SpawnCellData.WorldIndexData[i].Value;
                var unitToSpawn = m_SpawnCellData.UnitToSpawnData[i];

                if (cellWorldIndex == gameStateWorldIndex)
                {
                    if (unitToSpawn.UnitName.Length == 0)
                        return false;
                }
            }
            return true;
        }

        private bool AnyPlayerReady(uint gameStateWorldIndex)
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var playerWorldIndex = m_PlayerData.WorldIndexData[i].Value;

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    var playerState = m_PlayerData.PlayerStateData[i].CurrentState;
                    if (playerState == PlayerStateEnum.ready)
                        return true;
                }
            }
            return false;
        }


        private bool AllPlayersReady(uint gameStateWorldIndex)
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var playerWorldIndex = m_PlayerData.WorldIndexData[i].Value;

                if (playerWorldIndex == gameStateWorldIndex)
                {
                    var playerState = m_PlayerData.PlayerStateData[i].CurrentState;
                    if (playerState != PlayerStateEnum.ready)
                        return false;
                }
            }
            return true;
        }

        private bool NoUnitMoving(uint gameStateWorldIndex)
        {
            //loop through all Units to check if idle
            for (int i = 0; i < m_UnitData.Length; i++)
            {
                var unitWorldIndex = m_UnitData.WorldIndexData[i].Value;

                if (unitWorldIndex == gameStateWorldIndex)
                {
                    var lockedAction = m_UnitData.ActionsData[i].LockedAction;

                    if(lockedAction.Index == -2)
                    {
                        if (lockedAction.Targets[0].Mods[0].Coordinates.Count != 0)
                            return false;
                    }
                }
            }
            return true;
        }
    }
}


