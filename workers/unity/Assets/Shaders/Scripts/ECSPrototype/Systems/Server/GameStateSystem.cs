﻿using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Player;
using Cell;
using Improbable.Gdk.Core;
using System.Collections.Generic;
using Improbable;
using Unity.Collections;
using Improbable.Gdk.TransformSynchronization;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class GameStateSystem : ComponentSystem
    {
        HandleCellGridRequestsSystem m_CellGridSystem;
        CleanupSystem m_CleanUpSystem;
        SpawnUnitsSystem m_SpawnSystem;

        EntityQuery m_CellData;
        EntityQuery m_HeroData;
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );

            m_HeroData = GetEntityQuery(
            ComponentType.ReadOnly<Hero.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );

            m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );

            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<PlayerAttributes.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<PlayerState.Component>()
            );

            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<GameState.Component>()
            );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_CellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
            m_CleanUpSystem = World.GetExistingSystem<CleanupSystem>();
            m_SpawnSystem = World.GetExistingSystem<SpawnUnitsSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_GameStateData).ForEach((ref GameState.Component gameState, ref WorldIndex.Component gameStateWorldIndex) =>
            {
                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
#if UNITY_EDITOR
                       
                        if (gameState.PlayersOnMapCount == 1 && m_UnitData.CalculateEntityCount() == 1)
                        {
                            gameState.CurrentState = GameStateEnum.planning;
                        }
#else
                        if (gameState.PlayersOnMapCount == 2 && m_UnitData.CalculateEntityCount() == 2)
                        {
                            gameState.CurrentState = GameStateEnum.planning;
                        }
#endif
                        break;
                    case GameStateEnum.planning:
                        if (AllPlayersReady(gameStateWorldIndex.Value) || gameState.CurrentPlanningTime <= 0)
                        {
                            gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                            gameState.CurrentState = GameStateEnum.interrupt;
                        }
                        else if (AnyPlayerReady(gameStateWorldIndex.Value))
                        {
                            gameState.CurrentPlanningTime -= Time.deltaTime;
                        }
                        break;
                    case GameStateEnum.interrupt:

                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.interrupt, gameStateWorldIndex.Value);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.CurrentState = GameStateEnum.attack;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.attack:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.attack, gameStateWorldIndex.Value);
                        }

                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateIsTaken(gameStateWorldIndex.Value);
                                gameState.CurrentState = GameStateEnum.move;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.move:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.move, gameStateWorldIndex.Value);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateIsTaken(gameStateWorldIndex.Value);
                                gameState.CurrentState = GameStateEnum.skillshot;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.skillshot:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.skillshot, gameStateWorldIndex.Value);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.deltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.CurrentState = GameStateEnum.calculate_energy;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.calculate_energy:

                        gameState.CurrentPlanningTime = gameState.PlanningTime;
                        gameState.CurrentState = GameStateEnum.cleanup;
                        break;
                    case GameStateEnum.cleanup:
                        //check if any hero is dead to go into gameOver
                        if (CheckAnyHeroDead(gameStateWorldIndex.Value))
                        {
                            //UpdateIsTaken(gameStateWorldIndex);
                            gameState.WinnerFaction = FindWinnerFaction(gameStateWorldIndex.Value);
                            gameState.CurrentState = GameStateEnum.game_over;
                        }
                        else
                        {
                            //Debug.Log("Cleanup -> Planning");
                            //UpdateIsTaken(gameStateWorldIndex);
                            gameState.CurrentState = GameStateEnum.planning;
                        }
                        m_CleanUpSystem.DeleteDeadUnits(gameStateWorldIndex.Value);
                        break;
                    case GameStateEnum.game_over:
#if UNITY_EDITOR
                        gameState.CurrentState = GameStateEnum.waiting_for_players;
#endif
                        break;
                }
#if UNITY_EDITOR
#else

                if(gameState.PlayersOnMapCount == 0 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    gameState.CurrentState = GameStateEnum.waiting_for_players;
                }
#endif
            });
        }

        private bool AnyPlayerReady(uint gameStateWorldIndex)
        {
            bool b = false;

            Entities.With(m_PlayerData).ForEach((ref PlayerState.Component playerState, ref WorldIndex.Component playerWorldIndex) =>
            {
                if (playerWorldIndex.Value == gameStateWorldIndex)
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                        b = true;
                }
            });

            return b;
        }

        private float HighestExecuteTime(GameStateEnum step, uint gameStateWorldIndex)
        {
            float highestTime = 0.3f;

            Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref Actions.Component unitAction) =>
            {
                var lockedAction = unitAction.LockedAction;

                if (unitWorldIndex.Value == gameStateWorldIndex)
                {
                    if ((int)lockedAction.ActionExecuteStep == (int)step - 2)
                    {
                        if (step == GameStateEnum.move)
                        {
                            float unitMoveTime = lockedAction.Effects[0].MoveAlongPathNested.TimePerCell * (lockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count + 1);
                            if (unitMoveTime > highestTime)
                                highestTime = unitMoveTime;
                        }
                        else
                        {
                            if (lockedAction.TimeToExecute > highestTime)
                                highestTime = lockedAction.TimeToExecute;
                        }
                    }
                }
            });

            return highestTime;
        }

        private bool AllPlayersReady(uint gameStateWorldIndex)
        {
            bool b = true;

            Entities.With(m_PlayerData).ForEach((ref PlayerState.Component playerState, ref WorldIndex.Component playerWorldIndex) =>
            {

                if (playerWorldIndex.Value == gameStateWorldIndex)
                {
                    if (playerState.CurrentState != PlayerStateEnum.ready)
                        b = false;
                }
            });

            return b;
        }

        private void UpdateIsTaken(uint gameStateWorldIndex)
        {
            //Debug.Log("UpdateIsTaken");
            Dictionary<Vector3f, long> unitDict = new Dictionary<Vector3f, long>();
            HashSet<Vector3f> unitCoordHash = new HashSet<Vector3f>();

            Entities.With(m_UnitData).ForEach((ref WorldIndex.Component worldIndex, ref CubeCoordinate.Component cubeCoord, ref SpatialEntityId entityId, ref Actions.Component actions) =>
            {
                if (gameStateWorldIndex == worldIndex.Value)
                {
                    //if (actions.LockedAction.Index != -3 && actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                    //{
                        if (!unitDict.ContainsKey(cubeCoord.CubeCoordinate))
                            unitDict.Add(cubeCoord.CubeCoordinate, entityId.EntityId.Id);
                        if (!unitCoordHash.Contains(cubeCoord.CubeCoordinate))
                            unitCoordHash.Add(cubeCoord.CubeCoordinate);
                    //}
                }
            });

            Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWorldIndex, ref CubeCoordinate.Component cellCubeCoordinate, ref CellAttributesComponent.Component cellAtt) =>
            {
                if (gameStateWorldIndex == cellWorldIndex.Value)
                {
                    //unitCoordHash is filled with unitCoords that have a move planned died
                    if (unitCoordHash.Contains(cellCubeCoordinate.CubeCoordinate))
                    {
                        long id = unitDict[cellCubeCoordinate.CubeCoordinate];

                        if (cellAtt.CellAttributes.Cell.UnitOnCellId != id)
                        {
                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, id, cellWorldIndex.Value);
                            cellAtt.CellAttributes = cellAtt.CellAttributes;
                            //Debug.Log("SetTaken");
                        }
                        else if (cellAtt.CellAttributes.Cell.IsTaken && !cellAtt.CellAttributes.Cell.ObstructVision)
                        {
                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, 0, cellWorldIndex.Value);
                            cellAtt.CellAttributes = cellAtt.CellAttributes;
                            //Debug.Log("ReSetTaken: " + m_CellData.CellAttributes[i].CellAttributes.Cell.UnitOnCellId);
                        }
                    }
                }
            });
        }

        private uint FindWinnerFaction(uint gameStateWorldIndex)
        {
            uint f = 0;
            Entities.With(m_HeroData).ForEach((ref WorldIndex.Component worldIndex, ref FactionComponent.Component faction, ref Health.Component health) =>
            {
                if (worldIndex.Value == gameStateWorldIndex && health.CurrentHealth > 0)
                {
                    f = faction.Faction;
                }
            });
            return f;
        }

        private bool CheckAnyHeroDead(uint gameStateWorldIndex)
        {
            bool b = false;
            Entities.With(m_HeroData).ForEach((ref WorldIndex.Component worldIndex, ref FactionComponent.Component faction, ref Health.Component health) =>
            {
                if (worldIndex.Value == gameStateWorldIndex && health.CurrentHealth <= 0)
                {
                    b =true;
                }
            });
            return b;
        }
    }
}

