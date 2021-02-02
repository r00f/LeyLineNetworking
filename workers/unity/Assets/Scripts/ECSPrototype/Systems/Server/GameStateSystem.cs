using Cell;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class GameStateSystem : ComponentSystem
    {
        ResourceSystem m_ResourceSystem;
        ManalithSystem m_ManalithSystem;
        HandleCellGridRequestsSystem m_CellGridSystem;
        CleanupSystem m_CleanUpSystem;
        SpawnUnitsSystem m_SpawnSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;

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
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<Vision.Component>(),
            ComponentType.ReadOnly<PlayerAttributes.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<PlayerState.Component>()
            );

            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<GameState.Component>()
            );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
            m_ManalithSystem = World.GetExistingSystem<ManalithSystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
            m_CleanUpSystem = World.GetExistingSystem<CleanupSystem>();
            m_SpawnSystem = World.GetExistingSystem<SpawnUnitsSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.With(m_GameStateData).ForEach((ref GameState.Component gameState, ref WorldIndex.Component gameStateWorldIndex, ref SpatialEntityId gameStateId) =>
            {
                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
                        Debug.Log("WaitingForPlayers");
                        #if UNITY_EDITOR
                        //check if game is ready to start (> everything has been initialized) instead of checking for a hardcoded number of units on map
                        if (gameState.PlayersOnMapCount == 1)
                        {
                            if (gameState.CurrentWaitTime <= 0)
                            {
                                gameState.TurnCounter = 0;
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;

                                m_CleanUpSystem.DeleteNeutralUnits(gameStateWorldIndex.Value);
                                //raise InitMapEvent
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.InitializeMapEvent.Event(),
                                gameStateId.EntityId);

                                gameState.CurrentState = GameStateEnum.cleanup;
                            }
                            else
                                gameState.CurrentWaitTime -= Time.DeltaTime;
                        }
#else
                        if (gameState.PlayersOnMapCount == 2)
                        {
                            if (gameState.CurrentWaitTime <= 0)
                            {
                                gameState.TurnCounter = 0;
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;

                                m_CleanUpSystem.DeleteNeutralUnits(gameStateWorldIndex.Value);
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.InitializeMapEvent.Event(),
                                gameStateId.EntityId);
                                gameState.CurrentState = GameStateEnum.cleanup;
                            }
                            else
                                gameState.CurrentWaitTime -= Time.DeltaTime;
                        }
#endif
                        break;
                    case GameStateEnum.planning:
                        //DEBUGGING INSTA ROPE
                        //#if UNITY_EDITOR
                        //gameState.CurrentRopeTime -= Time.DeltaTime;
                        //#endif
                        if(gameState.CurrentRopeTime <= 0)
                        {
                            m_ComponentUpdateSystem.SendEvent(
                            new GameState.RopeEndEvent.Event(),
                            gameStateId.EntityId);
                        }
                        if (AllPlayersReady(gameStateWorldIndex.Value))
                        {
                            Debug.Log("AllPlayersReady");
                            gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                            gameState.CurrentState = GameStateEnum.interrupt;
                        }
                        else if (AnyPlayerReady(gameStateWorldIndex.Value))
                        {
                            gameState.CurrentRopeTime -= Time.DeltaTime;
                        }
                        break;
                    case GameStateEnum.interrupt:

                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.interrupt, gameStateWorldIndex.Value, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.AttackDamageDealt = false;
                                gameState.CurrentState = GameStateEnum.attack;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.attack:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.attack, gameStateWorldIndex.Value, gameState.MinExecuteStepTime);
                        }

                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.AttackDamageDealt = false;
                                gameState.CurrentState = GameStateEnum.move;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.move:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.move, gameStateWorldIndex.Value, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.AttackDamageDealt = false;
                                gameState.CurrentState = GameStateEnum.skillshot;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.skillshot:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.skillshot, gameStateWorldIndex.Value, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                gameState.AttackDamageDealt = false;
                                gameState.CurrentState = GameStateEnum.cleanup;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;

                    case GameStateEnum.cleanup:
                        //check if any hero is dead to go into gameOver
                        if (CheckAnyHeroDead(gameStateWorldIndex.Value))
                        {

                            Entities.With(m_PlayerData).ForEach((ref Vision.Component playerVision) =>
                            {
                                playerVision.RevealVision = true;
                                playerVision.RequireUpdate = true;
                            });

                            gameState.WinnerFaction = FindWinnerFaction(gameStateWorldIndex.Value);

                            gameState.CurrentState = GameStateEnum.game_over;
                        }
                        else
                        {
                            gameState.CurrentRopeTime = gameState.RopeTime;

                            Entities.With(m_PlayerData).ForEach((ref SpatialEntityId playerId, ref Vision.Component playerVision) =>
                            {
                                /*
                                if(playerVision.RevealVision)
                                {
                                    playerVision.RequireUpdate = true;
                                    playerVision.RevealVision = false;
                                }
                                */
                                m_ComponentUpdateSystem.SendEvent(
                                    new Vision.UpdateClientVisionEvent.Event(),
                                    playerId.EntityId);
                            });

                            UpdateMovedUnitCells(gameStateWorldIndex.Value);
                                
                            if (m_CleanUpSystem.CheckAllDeadUnitsDeleted(gameStateWorldIndex.Value))
                            {
                                gameState.TurnCounter++;
                                m_CleanUpSystem.ClearAllLockedActions(gameStateWorldIndex.Value);
                                m_ManalithSystem.UpdateManaliths(gameStateWorldIndex.Value);
                                m_ResourceSystem.CalculateIncome(gameStateWorldIndex.Value);

                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.CleanupStateEvent.Event(),
                                gameStateId.EntityId);

                                gameState.CurrentState = GameStateEnum.calculate_energy;
                            }
                        }
                        break;
                    case GameStateEnum.calculate_energy:
                        gameState.CurrentState = GameStateEnum.planning;
                        break;
                    case GameStateEnum.game_over:

                        break;
                }

#if UNITY_EDITOR
                if (gameState.PlayersOnMapCount == 2 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    gameState.CurrentState = GameStateEnum.waiting_for_players;
                }
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

        private float HighestExecuteTime(GameStateEnum step, uint gameStateWorldIndex, float minStepTime)
        {
            float highestTime = minStepTime;

            Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref Actions.Component unitAction) =>
            {
                var lockedAction = unitAction.LockedAction;

                if (unitWorldIndex.Value == gameStateWorldIndex)
                {
                    if ((int)lockedAction.ActionExecuteStep == (int)step - 2)
                    {
                        if (step == GameStateEnum.move && lockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
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

        private void UpdateMovedUnitCells(uint gameStateWorldIndex)
        {
            Dictionary<Vector3f, long> unitDict = new Dictionary<Vector3f, long>();
            HashSet<Vector3f> unitCoordHash = new HashSet<Vector3f>();

            //Debug.Log(m_UnitData.CalculateEntityCount());

            Entities.With(m_UnitData).ForEach((ref WorldIndex.Component worldIndex, ref CubeCoordinate.Component cubeCoord, ref SpatialEntityId entityId, ref Actions.Component actions) =>
            {
                if (gameStateWorldIndex == worldIndex.Value)
                {
                    if (actions.LockedAction.Index != -3 && actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                    {
                        //Debug.Log("UnitHasAPath");
                        if (!unitDict.ContainsKey(cubeCoord.CubeCoordinate))
                        {
                            unitDict.Add(actions.LockedAction.Targets[0].Mods[0].PathNested.OriginCoordinate, entityId.EntityId.Id);
                            unitDict.Add(cubeCoord.CubeCoordinate, entityId.EntityId.Id);
                        }
                            
                        if (!unitCoordHash.Contains(cubeCoord.CubeCoordinate))
                        {
                            unitCoordHash.Add(actions.LockedAction.Targets[0].Mods[0].PathNested.OriginCoordinate);
                            unitCoordHash.Add(cubeCoord.CubeCoordinate);
                        }
                    }
                }
            });

            Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWorldIndex, ref CubeCoordinate.Component cellCubeCoordinate, ref CellAttributesComponent.Component cellAtt) =>
            {
                if (gameStateWorldIndex == cellWorldIndex.Value)
                {
                    if (unitCoordHash.Contains(cellCubeCoordinate.CubeCoordinate))
                    {
                        long id = unitDict[cellCubeCoordinate.CubeCoordinate];

                        if (cellAtt.CellAttributes.Cell.UnitOnCellId != id)
                        {
                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, id, cellWorldIndex.Value);
                            cellAtt.CellAttributes = cellAtt.CellAttributes;
                        }
                        else if (cellAtt.CellAttributes.Cell.IsTaken)
                        {
                            cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, 0, cellWorldIndex.Value);
                            cellAtt.CellAttributes = cellAtt.CellAttributes;
                        }
                    }
                }
            });

            m_CleanUpSystem.DeleteDeadUnits(gameStateWorldIndex);
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


