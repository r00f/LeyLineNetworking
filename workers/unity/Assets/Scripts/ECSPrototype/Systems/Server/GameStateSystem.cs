using Cell;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using System.Linq;
using Unit;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace LeyLineHybridECS
{
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class GameStateSystem : JobComponentSystem
    {
        AISystem m_AISystem;
        ResourceSystem m_ResourceSystem;
        ManalithSystem m_ManalithSystem;
        HandleCellGridRequestsSystem m_CellGridSystem;
        CleanupSystem m_CleanUpSystem;
        InitializeWorldSystem m_SpawnSystem;
        ComponentUpdateSystem m_ComponentUpdateSystem;
        BeginSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        ILogDispatcher logger;
        EntityQuery m_CellData;
        EntityQuery m_UnitData;
        EntityQuery m_EffectStackData;
        EntityQuery m_InitMapEventSenderData;

        protected override void OnCreate()
        {
            base.OnCreate();

            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

            m_InitMapEventSenderData = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<InitMapEvent.Component>()
            );

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>()
            );

            m_EffectStackData = GetEntityQuery(
                 ComponentType.ReadWrite<EffectStack.Component>()
            );
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
            m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
            m_AISystem = World.GetExistingSystem<AISystem>();
            m_ManalithSystem = World.GetExistingSystem<ManalithSystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
            m_CleanUpSystem = World.GetExistingSystem<CleanupSystem>();
            m_SpawnSystem = World.GetExistingSystem<InitializeWorldSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityCommandBuffer ECBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            Entities.WithNone<WorldIndexShared, NewlyAddedSpatialOSEntity>().ForEach((Entity e, in WorldIndex.Component entityWorldIndex) =>
            {
                if(entityWorldIndex.Value != 0)
                {
                    if (EntityManager.HasComponent<GameState.Component>(e))
                    {
                        var mapData = EntityManager.GetComponentData<MapData.Component>(e);
                        EntityManager.AddComponent<GameStateServerVariables>(e);
                        EntityManager.AddComponentObject(e, new CurrentMapState { CoordinateCellDictionary = mapData.CoordinateCellDictionary });
                        //Debug.Log("Added CurrentMapState to GameLogic GameState: " + EntityManager.GetComponentObject<CurrentMapState>(e).CoordinateCellDictionary.Count + " MapCell Coord 0: " + EntityManager.GetComponentObject<CurrentMapState>(e).CoordinateCellDictionary.ElementAt(0).Value.AxialCoordinate.X + EntityManager.GetComponentObject<CurrentMapState>(e).CoordinateCellDictionary.ElementAt(0).Value.AxialCoordinate.Y);
                    }

                    EntityManager.AddSharedComponentData(e, new WorldIndexShared { Value = entityWorldIndex.Value });
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();

            if (m_InitMapEventSenderData.CalculateEntityCount() != 1)
                return inputDeps;

            var initMapEventSenderId = m_InitMapEventSenderData.GetSingleton<SpatialEntityId>();

            Entities.ForEach((CurrentMapState mapData, ref GameState.Component gameState, ref EffectStack.Component effectStack, ref GameStateServerVariables serverVariables, in Position.Component position, in WorldIndexShared gameStateWorldIndex, in SpatialEntityId gameStateId, in ClientWorkerIds.Component clientWorkerIds) =>
            {
                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
                        if (clientWorkerIds.PlayersOnMapCount == 2)
                        {
                            if (gameState.CurrentWaitTime <= 0)
                            {
                                gameState.WinnerFaction = 0;
                                gameState.TurnCounter = 0;

                                //if gameState.InitMapEventSent
                                if (!gameState.InitMapEventSent)
                                {
                                    //Debug.Log("SendInitMapEvent");
                                    m_ComponentUpdateSystem.SendEvent(
                                    new InitMapEvent.InitializeMapEvent.Event(new InitializeMap(gameStateWorldIndex.Value, new Vector2f((int) position.Coords.X, (int) position.Coords.Z))),
                                    initMapEventSenderId.EntityId
                                    );
                                    gameState.InitMapEventSent = true;
                                }
                                //calculate initialized cellcount with sharedWorldIndex
                                else
                                {
                                    gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                                    /*
                                    logger.HandleLog(LogType.Warning,
                                    new LogEvent("Exit WaitingForPlayers")
                                    .WithField("GamestateWorldIndex", gameStateWorldIndex.Value));
                                    */
                                    UpdateUnitsGameStateTag(GameStateEnum.cleanup, gameStateWorldIndex, ECBuffer);
                                    gameState.CurrentState = GameStateEnum.cleanup;
                                }
                            }
                            else
                                gameState.CurrentWaitTime -= Time.DeltaTime;
                        }
                        break;
                    case GameStateEnum.planning:
                        if (clientWorkerIds.PlayersOnMapCount == 0)
                        {
                            m_CleanUpSystem.DeleteMap(gameStateWorldIndex);
                            gameState.InitMapEventSent = false;
                            gameState.InitMapWaitTime = 2f;
                            gameState.CurrentState = GameStateEnum.waiting_for_players;
                        }
                        else if (clientWorkerIds.PlayersOnMapCount == 2)
                        {
                            if (gameState.CurrentRopeTime <= 0)
                            {
                                /*
                                logger.HandleLog(LogType.Warning,
                                new LogEvent("Send endRopeEvent")
                                .WithField("GamestateWorldIndex", gameStateWorldIndex.Value));
                                */
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.RopeEndEvent.Event(),
                                gameStateId.EntityId);
                            }
                            if (AllPlayersReady(gameStateWorldIndex))
                            {
                                /*
                                logger.HandleLog(LogType.Warning,
                                new LogEvent("All Players Ready - move to startExecute step")
                                .WithField("GamestateWorldIndex", gameStateWorldIndex.Value));
                                */
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                                UpdateUnitsGameStateTag(GameStateEnum.start_execute, gameStateWorldIndex, ECBuffer);
                                gameState.CurrentState = GameStateEnum.start_execute;
                            }
                            else if (AnyPlayerReady(gameStateWorldIndex))
                            {
                                if (gameState.TurnCounter != 1)
                                    gameState.CurrentRopeTime -= Time.DeltaTime;
                            }
                        }
                        break;
                    case GameStateEnum.start_execute:
                        UpdateUnitsGameStateTag(GameStateEnum.interrupt, gameStateWorldIndex, ECBuffer);
                        effectStack.EffectsExecuted = false;
                        gameState.CurrentState = GameStateEnum.interrupt;
                        break;
                    case GameStateEnum.interrupt:
                        if (serverVariables.HighestExecuteTime == 0)
                        {
                            serverVariables.HighestExecuteTime = HighestExecuteTime(GameStateEnum.interrupt, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            serverVariables.HighestExecuteTime -= Time.DeltaTime;
                            if (serverVariables.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.attack, gameStateWorldIndex, ECBuffer);
                                effectStack.EffectsExecuted = false;
                                gameState.CurrentState = GameStateEnum.attack;
                                serverVariables.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.attack:
                        if (serverVariables.HighestExecuteTime == 0)
                        {
                            serverVariables.HighestExecuteTime = HighestExecuteTime(GameStateEnum.attack, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            serverVariables.HighestExecuteTime -= Time.DeltaTime;
                            if (serverVariables.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.move, gameStateWorldIndex, ECBuffer);
                                effectStack.EffectsExecuted = false;
                                gameState.CurrentState = GameStateEnum.move;
                                serverVariables.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.move:
                        if (serverVariables.HighestExecuteTime == 0)
                        {
                            serverVariables.HighestExecuteTime = HighestExecuteTime(GameStateEnum.move, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            serverVariables.HighestExecuteTime -= Time.DeltaTime;
                            if (serverVariables.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.skillshot, gameStateWorldIndex, ECBuffer);
                                UpdateMovedUnitCells(mapData, gameStateWorldIndex, new Dictionary<Vector3f, long>());
                                effectStack.EffectsExecuted = false;
                                gameState.CurrentState = GameStateEnum.skillshot;
                                serverVariables.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.skillshot:
                        if (serverVariables.HighestExecuteTime == 0)
                        {
                            serverVariables.HighestExecuteTime = HighestExecuteTime(GameStateEnum.skillshot, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            serverVariables.HighestExecuteTime -= Time.DeltaTime;
                            if (serverVariables.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.cleanup, gameStateWorldIndex, ECBuffer);
                                effectStack.EffectsExecuted = false;
                                gameState.CurrentState = GameStateEnum.cleanup;
                                serverVariables.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.cleanup:
                        //check if any hero is dead to go into gameOver
                        if (CheckAnyHeroDead(gameStateWorldIndex))
                        {
#if UNITY_EDITOR
                            gameState.WinnerFaction = FindWinnerFaction(gameStateWorldIndex, true);
#else
                            gameState.WinnerFaction = FindWinnerFaction(gameStateWorldIndex);
#endif
                            effectStack = CleanUpEffectStack(effectStack);
                            gameState.CurrentState = GameStateEnum.game_over;
                        }
                        else
                        {
                            if (gameState.InitMapWaitTime > 0)
                                gameState.InitMapWaitTime -= Time.DeltaTime;
                            else
                            {
                                gameState.CurrentRopeTime = gameState.RopeTime;
                                SendUpdateClientVisionEvents(gameStateWorldIndex);
                                m_CleanUpSystem.DeleteDeadUnits(gameStateWorldIndex);

                                if (m_CleanUpSystem.CheckAllDeadUnitsDeleted(gameStateWorldIndex))
                                {
                                    gameState.TurnCounter++;
                                    m_CleanUpSystem.ClearAllLockedActions(gameStateWorldIndex);
                                    m_ManalithSystem.UpdateManaliths(gameStateWorldIndex, clientWorkerIds);
                                    m_ResourceSystem.CalculateIncome(gameStateWorldIndex);
                                    m_AISystem.UpdateAIUnits(gameStateWorldIndex);

                                    m_ComponentUpdateSystem.SendEvent(
                                    new GameState.CleanupStateEvent.Event(),
                                    gameStateId.EntityId);

                                    UpdateUnitsGameStateTag(GameStateEnum.planning, gameStateWorldIndex, ECBuffer);
                                    CleanUpUnitIncomingEffects(gameStateWorldIndex);
                                    effectStack = CleanUpEffectStack(effectStack);
                                    effectStack.EffectsExecuted = false;
                                    gameState.CurrentState = GameStateEnum.planning;
                                }
                            }
                        }
                        break;
                    case GameStateEnum.game_over:
                        //Cycle back to Start in order to stresstest
                        m_CleanUpSystem.DeleteMap(gameStateWorldIndex);
                        gameState.InitMapEventSent = false;
                        gameState.InitMapWaitTime = 2f;
                        gameState.CurrentState = GameStateEnum.waiting_for_players;
                        //RevealPlayerVisions(gameStateWorldIndex);
                        break;
                }

                if (gameState.CurrentState != GameStateEnum.game_over && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    uint concededFaction = PlayerWithFactionConceded(gameStateWorldIndex);

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
            })
            .WithoutBurst()
            .Run();

            return inputDeps;
        }

        EffectStack.Component CleanUpEffectStack(EffectStack.Component effectStack)
        {

            for (int y = effectStack.InterruptEffects.Count; y > 0; y--)
            {
                if (effectStack.InterruptEffects[y - 1].TurnDuration == 0)
                {
                    effectStack.InterruptEffects.RemoveAt(y - 1);
                }
            }
            for (int y = effectStack.AttackEffects.Count; y > 0; y--)
            {
                if (effectStack.AttackEffects[y - 1].TurnDuration == 0)
                {
                    effectStack.AttackEffects.RemoveAt(y - 1);
                }
            }
            for (int y = effectStack.MoveEffects.Count; y > 0; y--)
            {
                if (effectStack.MoveEffects[y - 1].TurnDuration == 0)
                {
                    effectStack.MoveEffects.RemoveAt(y - 1);
                }
            }
            for (int y = effectStack.SkillshotEffects.Count; y > 0; y--)
            {
                if (effectStack.SkillshotEffects[y - 1].TurnDuration == 0)
                {
                    effectStack.SkillshotEffects.RemoveAt(y - 1);
                }
            }

            return effectStack;
        }

        void CleanUpUnitIncomingEffects(WorldIndexShared gameStateWorldIndex)
        {
            EntityCommandBuffer ECBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<IncomingInterruptTag>().ForEach((Entity e, ref IncomingActionEffects.Component incomingEffects) =>
            {
                for (int i = incomingEffects.InterruptEffects.Count; i > 0; i--)
                {
                    if (incomingEffects.InterruptEffects[i - 1].UnitDuration == 0)
                    {
                        incomingEffects.InterruptEffects.RemoveAt(i - 1);
                    }
                }

                incomingEffects.InterruptEffects = incomingEffects.InterruptEffects;

                if (incomingEffects.InterruptEffects.Count == 0)
                    ECBuffer.RemoveComponent<IncomingInterruptTag>(e);
            })
            .WithoutBurst()
            .Run();

            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<IncomingAttackTag>().ForEach((Entity e, ref IncomingActionEffects.Component incomingEffects) =>
            {
                for (int i = incomingEffects.AttackEffects.Count; i > 0; i--)
                {
                    if (incomingEffects.AttackEffects[i - 1].UnitDuration == 0)
                    {
                        incomingEffects.AttackEffects.RemoveAt(i - 1);
                    }
                }

                incomingEffects.AttackEffects = incomingEffects.AttackEffects;

                if (incomingEffects.AttackEffects.Count == 0)
                    ECBuffer.RemoveComponent<IncomingAttackTag>(e);
            })
            .WithoutBurst()
            .Run();

            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<IncomingMoveTag>().ForEach((Entity e, ref IncomingActionEffects.Component incomingEffects) =>
            {
                for (int i = incomingEffects.MoveEffects.Count; i > 0; i--)
                {
                    if (incomingEffects.MoveEffects[i - 1].UnitDuration == 0)
                    {
                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("Remove incoming move")
                        .WithField("UnitIndex", e.Index));
                        */

                        incomingEffects.MoveEffects.RemoveAt(i - 1);
                    }
                }

                incomingEffects.MoveEffects = incomingEffects.MoveEffects;

                if (incomingEffects.MoveEffects.Count == 0)
                    ECBuffer.RemoveComponent<IncomingMoveTag>(e);
            })
            .WithoutBurst()
            .Run();

            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<IncomingSkillshotTag>().ForEach((Entity e, ref IncomingActionEffects.Component incomingEffects) =>
            {
                for (int i = incomingEffects.SkillshotEffects.Count; i > 0; i--)
                {
                    if (incomingEffects.SkillshotEffects[i - 1].UnitDuration == 0)
                    {
                        incomingEffects.SkillshotEffects.RemoveAt(i - 1);
                    }
                }

                incomingEffects.SkillshotEffects = incomingEffects.SkillshotEffects;

                if (incomingEffects.SkillshotEffects.Count == 0)
                    ECBuffer.RemoveComponent<IncomingSkillshotTag>(e);
            })
            .WithoutBurst()
            .Run();
        }

        void RevealPlayerVisions(WorldIndexShared gameStateWorldIndex)
        {
            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component playerVision) =>
            {
                if (!playerVision.RevealVision)
                {
                    playerVision.RevealVision = true;
                    playerVision.RequireUpdate = true;
                }
            })
            .WithoutBurst()
            .Run();
        }

        void SendUpdateClientVisionEvents(WorldIndexShared gameStateWorldIndex)
        {
            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<PlayerAttributes.Component>().ForEach((in SpatialEntityId playerId) =>
            {
                m_ComponentUpdateSystem.SendEvent(
                new Vision.UpdateClientVisionEvent.Event(),
                playerId.EntityId);
            })
            .WithoutBurst()
            .Run();
        }

        private bool AnyPlayerReady(WorldIndexShared gameStateWorldIndex)
        {
            bool b = false;
            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((in PlayerState.Component playerState) =>
            {
                if (playerState.CurrentState == PlayerStateEnum.ready)
                    b = true;
            })
            .WithoutBurst()
            .Run();

            return b;
        }

        void UpdateUnitsGameStateTag(GameStateEnum gameState, WorldIndexShared gameStateWorldIndex, EntityCommandBuffer entityCommandBuffer)
        {
            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((Entity e, in Actions.Component actions) =>
            {
                switch (gameState)
                {
                    case GameStateEnum.planning:
                        entityCommandBuffer.AddComponent<PlanningTag>(e);
                        break;
                    case GameStateEnum.start_execute:
                        entityCommandBuffer.RemoveComponent<PlanningTag>(e);
                        entityCommandBuffer.AddComponent<StartExecuteTag>(e);
                        break;
                    case GameStateEnum.interrupt:
                        entityCommandBuffer.RemoveComponent<StartExecuteTag>(e);
                        entityCommandBuffer.AddComponent<InterruptTag>(e);
                        if (actions.LockedAction.Index != -3 && actions.LockedAction.ActionExecuteStep == ExecuteStepEnum.interrupt)
                            entityCommandBuffer.AddComponent<LockedInterruptTag>(e);
                        break;
                    case GameStateEnum.attack:
                        entityCommandBuffer.RemoveComponent<InterruptTag>(e);
                        entityCommandBuffer.AddComponent<AttackTag>(e);
                        if (actions.LockedAction.ActionExecuteStep == ExecuteStepEnum.attack)
                            entityCommandBuffer.AddComponent<LockedAttackTag>(e);
                        break;
                    case GameStateEnum.move:
                        entityCommandBuffer.RemoveComponent<AttackTag>(e);
                        entityCommandBuffer.AddComponent<MoveTag>(e);
                        if (actions.LockedAction.ActionExecuteStep == ExecuteStepEnum.move)
                            entityCommandBuffer.AddComponent<LockedMoveTag>(e);
                        break;
                    case GameStateEnum.skillshot:
                        entityCommandBuffer.RemoveComponent<MoveTag>(e);
                        entityCommandBuffer.AddComponent<SkillshotTag>(e);
                        if (actions.LockedAction.ActionExecuteStep == ExecuteStepEnum.skillshot)
                            entityCommandBuffer.AddComponent<LockedSkillshotTag>(e);
                        break;
                    case GameStateEnum.cleanup:
                        entityCommandBuffer.RemoveComponent<PlanningTag>(e);
                        entityCommandBuffer.RemoveComponent<StartExecuteTag>(e);
                        entityCommandBuffer.RemoveComponent<InterruptTag>(e);
                        entityCommandBuffer.RemoveComponent<AttackTag>(e);
                        entityCommandBuffer.RemoveComponent<MoveTag>(e);
                        entityCommandBuffer.RemoveComponent<SkillshotTag>(e);
                        break;
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();
        }

        private uint PlayerWithFactionConceded(WorldIndexShared gameStateWorldIndex)
        {
            uint f = 0;
            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((in PlayerState.Component playerState, in FactionComponent.Component playerFaction) =>
            {
                if (playerState.CurrentState == PlayerStateEnum.conceded)
                {
                    f = playerFaction.Faction;
                }
            })
            .WithoutBurst()
            .Run();

            return f;
        }

        private float HighestExecuteTime(GameStateEnum gameState, WorldIndexShared gameStateWorldIndex, float minStepTime)
        {
            float highestTime = minStepTime;
            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((in Actions.Component unitAction) =>
            {
                if (unitAction.LockedAction.Index != -3 && (int) unitAction.LockedAction.ActionExecuteStep == (int) gameState - 3)
                {
                    if (unitAction.LockedAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
                    {
                        float unitMoveTime = unitAction.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell * (unitAction.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count - 1);
                        if (unitMoveTime > highestTime)
                            highestTime = unitMoveTime;
                    }
                    else
                    {
                        if (unitAction.LockedAction.TimeToExecute > highestTime)
                            highestTime = unitAction.LockedAction.TimeToExecute;
                    }
                }
            })
            .WithoutBurst()
            .Run();

            return highestTime;
        }

        private bool AllPlayersReady(WorldIndexShared gameStateWorldIndex)
        {
            bool b = true;

            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((in PlayerState.Component playerState) =>
            {
                /*
                logger.HandleLog(LogType.Warning,
                new LogEvent("Player with shared worldIndex found")
                .WithField("GamestateWorldIndex", gameStateWorldIndex.Value));
                */

                if (playerState.CurrentState != PlayerStateEnum.ready)
                    b = false;
            })
            .WithoutBurst()
            .Run();

            return b;
        }

        private void UpdateMovedUnitCells(CurrentMapState mapData, WorldIndexShared gameStateWorldIndex, Dictionary<Vector3f, long> unitDict)
        {
            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((in CubeCoordinate.Component cubeCoord, in SpatialEntityId entityId, in Health.Component health, in IncomingActionEffects.Component incomingEffects) =>
            {
                if (incomingEffects.MoveEffects.Count != 0 && health.CurrentHealth > 0 && incomingEffects.MoveEffects[0].EffectType == EffectTypeEnum.move_along_path)
                {
                    if (!unitDict.ContainsKey(cubeCoord.CubeCoordinate))
                        unitDict.Add(cubeCoord.CubeCoordinate, entityId.EntityId.Id);
                    if (!unitDict.ContainsKey(incomingEffects.MoveEffects[0].MoveAlongPathNested.OriginCoordinate))
                        unitDict.Add(incomingEffects.MoveEffects[0].MoveAlongPathNested.OriginCoordinate, entityId.EntityId.Id);
                }
            })
            .WithoutBurst()
            .Run();

            /*
            for(int i = 0; i < unitDict.Count; i++)
            {
                MapCell cell = mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitDict.ElementAt(i).Key)];

                if(cell.UnitOnCellId != unitDict.ElementAt(i).Value)
                {
                    cell.IsTaken = true;
                    cell.UnitOnCellId = unitDict.ElementAt(i).Value;
                }
                else if (cell.IsTaken)
                {
                    cell.IsTaken = false;
                    cell.UnitOnCellId = 0;
                }
                mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitDict.ElementAt(i).Key)] = cell;
            }
            */

            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((ref CellAttributesComponent.Component cellAtt, in CubeCoordinate.Component cellCubeCoordinate) =>
            {
                //Debug.Log("CellsWithWorldIndexShared");
                if (unitDict.ContainsKey(cellCubeCoordinate.CubeCoordinate))
                {
                    long id = unitDict[cellCubeCoordinate.CubeCoordinate];

                    if (cellAtt.CellAttributes.Cell.UnitOnCellId != id)
                    {
                        cellAtt.CellAttributes = SetCellAttributes(cellAtt.CellAttributes, true, id, gameStateWorldIndex);
                        cellAtt.CellAttributes = cellAtt.CellAttributes;
                    }
                    else if (cellAtt.CellAttributes.Cell.IsTaken)
                    {
                        cellAtt.CellAttributes = SetCellAttributes(cellAtt.CellAttributes, false, 0, gameStateWorldIndex);
                        cellAtt.CellAttributes = cellAtt.CellAttributes;
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        public CellAttributes SetCellAttributes(CellAttributes cellAttributes, bool isTaken, long entityId, WorldIndexShared worldIndex)
        {
            var cell = cellAttributes.Cell;
            cell.IsTaken = isTaken;
            cell.UnitOnCellId = entityId;

            CellAttributes cellAtt = new CellAttributes
            {
                Neighbours = cellAttributes.Neighbours,
                Cell = cell,
                CellMapColorIndex = cellAttributes.CellMapColorIndex
            };

            UpdateNeighbours(cellAtt.Cell, cellAtt.Neighbours, worldIndex);
            return cellAtt;
        }

        public void UpdateNeighbours(CellAttribute cell, CellAttributeList neighbours, WorldIndexShared worldIndex)
        {
            Entities.WithSharedComponentFilter(worldIndex).ForEach((ref CellAttributesComponent.Component cellAtt) =>
            {
                for (int n = 0; n < neighbours.CellAttributes.Count; n++)
                {
                    if (Vector3fext.ToUnityVector(neighbours.CellAttributes[n].CubeCoordinate) == Vector3fext.ToUnityVector(cellAtt.CellAttributes.Cell.CubeCoordinate))
                    {
                        for (int cn = 0; cn < cellAtt.CellAttributes.Neighbours.CellAttributes.Count; cn++)
                        {
                            if (Vector3fext.ToUnityVector(cellAtt.CellAttributes.Neighbours.CellAttributes[cn].CubeCoordinate) == Vector3fext.ToUnityVector(cell.CubeCoordinate))
                            {
                                cellAtt.CellAttributes.Neighbours.CellAttributes[cn] = cell;
                                cellAtt.CellAttributes = cellAtt.CellAttributes;
                            }
                        }
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        private uint FindWinnerFaction(WorldIndexShared gameStateWorldIndex, bool isEditor = false)
        {
            uint f = 0;
            if (!isEditor)
            {
                Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<Hero.Component>().ForEach((in FactionComponent.Component faction, in Health.Component health) =>
                {
                    if (health.CurrentHealth > 0)
                    {
                        f = faction.Faction;
                    }
                })
                .WithoutBurst()
                .Run();
            }
            else
            {
                Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<Hero.Component>().ForEach((in FactionComponent.Component faction, in Health.Component health) =>
                {
                    if (faction.Faction == 1)
                        f = 2;
                    else
                        f = 1;
                })
                .WithoutBurst()
                .Run();
            }
            return f;
        }

        private bool CheckAnyHeroDead(WorldIndexShared gameStateWorldIndex)
        {
            bool b = false;
            Entities.WithSharedComponentFilter(gameStateWorldIndex).WithAll<Hero.Component>().ForEach((in FactionComponent.Component faction, in Health.Component health) =>
            {
                if (health.CurrentHealth <= 0)
                {
                    b =true;
                }
            })
            .WithoutBurst()
            .Run();

            return b;
        }

        private uint InitializedCellCount(WorldIndexShared gameStateWorldIndex)
        {
            Entities.WithStoreEntityQueryInField(ref m_CellData).WithAll<CellAttributesComponent.Component>().WithSharedComponentFilter(gameStateWorldIndex).ForEach((Entity e) =>
            {

            })
            .Run();
            //Debug.Log(InitializedCellCount(gameStateWorldIndex));
            return (uint) m_CellData.CalculateEntityCount();
        }

    }

}
