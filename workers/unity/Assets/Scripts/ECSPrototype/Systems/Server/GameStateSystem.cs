using Cell;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using Unity.Jobs;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
    public class GameStateSystem : JobComponentSystem
    {
        ResourceSystem m_ResourceSystem;
        ManalithSystem m_ManalithSystem;
        HandleCellGridRequestsSystem m_CellGridSystem;
        CleanupSystem m_CleanUpSystem;
        SpawnUnitsSystem m_SpawnSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        
        /*
        EntityQuery m_CellData;
        EntityQuery m_HeroData;
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;
        */

        /*
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
            //ComponentType.ReadOnly<Health.Component>(),
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
        */

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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Entities.ForEach((ref GameState.Component gameState, in WorldIndex.Component gameStateWorldIndex, in SpatialEntityId gameStateId) =>
            {
                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
#if UNITY_EDITOR
                        //check if game is ready to start (> everything has been initialized) instead of checking for a hardcoded number of units on map
                        if (gameState.PlayersOnMapCount == 1)
                        {
                            if (gameState.CurrentWaitTime <= 0)
                            {
                                gameState.WinnerFaction = 0;
                                gameState.TurnCounter = 0;
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                                //raise InitMapEvent
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.InitializeMapEvent.Event(),
                                gameStateId.EntityId);

                                gameState.InitMapWaitTime = 2f;

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
                                gameState.WinnerFaction = 0;
                                gameState.TurnCounter = 0;
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.InitializeMapEvent.Event(),
                                gameStateId.EntityId);
                                gameState.InitMapWaitTime = 2f;
                                gameState.CurrentState = GameStateEnum.cleanup;
                            }
                            else
                                gameState.CurrentWaitTime -= Time.DeltaTime;
                        }
#endif
                        break;
                    case GameStateEnum.planning:
                        if(gameState.CurrentRopeTime <= 0)
                        {
                            m_ComponentUpdateSystem.SendEvent(
                            new GameState.RopeEndEvent.Event(),
                            gameStateId.EntityId);
                        }
                        if (AllPlayersReady(gameStateWorldIndex.Value))
                        {
                            //Debug.Log("AllPlayersReady");
                            gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                            gameState.CurrentState = GameStateEnum.interrupt;
                        }
                        else if (AnyPlayerReady(gameStateWorldIndex.Value))
                        {
                            if(gameState.TurnCounter != 1)
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
                                UpdateMovedUnitCells(gameStateWorldIndex.Value);
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
#if UNITY_EDITOR

                            gameState.WinnerFaction = FindWinnerFaction(gameStateWorldIndex.Value, true);
#else

                            gameState.WinnerFaction = FindWinnerFaction(gameStateWorldIndex.Value);
#endif

                            gameState.CurrentState = GameStateEnum.game_over;
                        }
                        else
                        {
                            if (gameState.InitMapWaitTime > 0)
                                gameState.InitMapWaitTime -= Time.DeltaTime;
                            else
                            {
                                gameState.CurrentRopeTime = gameState.RopeTime;

                                Entities.WithAll<PlayerAttributes.Component>().ForEach((in SpatialEntityId playerId) =>
                                {
                                    m_ComponentUpdateSystem.SendEvent(
                                        new Vision.UpdateClientVisionEvent.Event(),
                                        playerId.EntityId);
                                })
                                .WithoutBurst()
                                .Run();

                                m_CleanUpSystem.DeleteDeadUnits(gameStateWorldIndex.Value);

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
                        }
                        break;
                    case GameStateEnum.calculate_energy:
                        gameState.CurrentState = GameStateEnum.planning;
                        break;
                    case GameStateEnum.game_over:
                        Entities.WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component playerVision) =>
                        {
                            if (!playerVision.RevealVision)
                            {
                                playerVision.RevealVision = true;
                                playerVision.RequireUpdate = true;
                            }
                        })
                        .WithoutBurst()
                        .Run();
                        break;
                }

                if (gameState.CurrentState != GameStateEnum.game_over && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    uint concededFaction = PlayerWithFactionConceded(gameStateWorldIndex.Value);

                    //if any player concedes in any step other than game over / waiting for players, go to gameoverstate
                    if (concededFaction != 0)
                    {
                        if (concededFaction == 1)
                            gameState.WinnerFaction = 2;
                        else
                            gameState.WinnerFaction = 1;

                        gameState.CurrentState = GameStateEnum.game_over;
                    }
                }

#if UNITY_EDITOR
                if (gameState.PlayersOnMapCount == 2 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    m_CleanUpSystem.DeleteAllUnits(gameStateWorldIndex.Value);
                    gameState.CurrentState = GameStateEnum.waiting_for_players;
                }
#else
                        if(gameState.PlayersOnMapCount == 0 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                        {
                            m_CleanUpSystem.DeleteAllUnits(gameStateWorldIndex.Value);
                            gameState.CurrentState = GameStateEnum.waiting_for_players;
                        }
#endif
            })
            .WithoutBurst()
            .Run();

            return inputDeps;
        }

        private bool AnyPlayerReady(uint gameStateWorldIndex)
        {
            bool b = false;

            Entities.ForEach((in PlayerState.Component playerState, in WorldIndex.Component playerWorldIndex) =>
            {
                if (playerWorldIndex.Value == gameStateWorldIndex)
                {
                    if (playerState.CurrentState == PlayerStateEnum.ready)
                        b = true;
                }
            })
            .WithoutBurst()
            .Run();

            return b;
        }

        private uint PlayerWithFactionConceded(uint gameStateWorldIndex)
        {
            uint f = 0;

            Entities.ForEach((in PlayerState.Component playerState, in WorldIndex.Component playerWorldIndex, in FactionComponent.Component playerFaction) =>
            {
                if (playerWorldIndex.Value == gameStateWorldIndex)
                {
                    if (playerState.CurrentState == PlayerStateEnum.conceded)
                    {
                        f = playerFaction.Faction;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            return f;
        }

        private float HighestExecuteTime(GameStateEnum step, uint gameStateWorldIndex, float minStepTime)
        {
            float highestTime = minStepTime;

            Entities.ForEach((in WorldIndex.Component unitWorldIndex, in Actions.Component unitAction) =>
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
            })
            .WithoutBurst()
            .Run();

            return highestTime;
        }

        private bool AllPlayersReady(uint gameStateWorldIndex)
        {
            bool b = true;

            Entities.ForEach((in PlayerState.Component playerState, in WorldIndex.Component playerWorldIndex) =>
            {
                if (playerWorldIndex.Value == gameStateWorldIndex)
                {
                    if (playerState.CurrentState != PlayerStateEnum.ready)
                        b = false;
                }
            })
            .WithoutBurst()
            .Run();

            return b;
        }

        private void UpdateMovedUnitCells(uint gameStateWorldIndex)
        {
            //Heap allocation - needs to be replaced
            Dictionary<Vector3f, long> unitDict = new Dictionary<Vector3f, long>();
            HashSet<Vector3f> unitCoordHash = new HashSet<Vector3f>();

            //Debug.Log(m_UnitData.CalculateEntityCount());

            Entities.ForEach((ref WorldIndex.Component worldIndex, ref CubeCoordinate.Component cubeCoord, ref SpatialEntityId entityId, ref Actions.Component actions) =>
            {
                if (gameStateWorldIndex == worldIndex.Value)
                {
                    if (actions.LockedAction.Index != -3 && actions.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                    {
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
            })
            .WithoutBurst()
            .Run();

            Entities.ForEach((ref WorldIndex.Component cellWorldIndex, ref CubeCoordinate.Component cellCubeCoordinate, ref CellAttributesComponent.Component cellAtt) =>
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
            })
            .WithoutBurst()
            .Run();
        }

        private uint FindWinnerFaction(uint gameStateWorldIndex, bool isEditor = false)
        {
            uint f = 0;
            if(!isEditor)
            {
                Entities.WithAll<Hero.Component>().ForEach((in WorldIndex.Component worldIndex, in FactionComponent.Component faction, in Health.Component health) =>
                {
                    if (worldIndex.Value == gameStateWorldIndex && health.CurrentHealth > 0)
                    {
                        f = faction.Faction;
                    }
                })
                .WithoutBurst()
                .Run();
            }
            else
            {
                Entities.WithAll<Hero.Component>().ForEach((in WorldIndex.Component worldIndex, in FactionComponent.Component faction, in Health.Component health) =>
                {
                    if (worldIndex.Value == gameStateWorldIndex)
                    {
                        if (faction.Faction == 1)
                            f = 2;
                        else
                            f = 1;
                    }
                })
                .WithoutBurst()
                .Run();
            }
            return f;
        }

        private bool CheckAnyHeroDead(uint gameStateWorldIndex)
        {
            bool b = false;
            Entities.WithAll<Hero.Component>().ForEach((in WorldIndex.Component worldIndex, in FactionComponent.Component faction, in Health.Component health) =>
            {
                if (worldIndex.Value == gameStateWorldIndex && health.CurrentHealth <= 0)
                {
                    b =true;
                }
            })
            .WithoutBurst()
            .Run();

            return b;
        }
    }
}


