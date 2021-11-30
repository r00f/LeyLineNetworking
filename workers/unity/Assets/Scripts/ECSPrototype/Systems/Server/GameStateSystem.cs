using Cell;
using Generic;
using Improbable.Gdk.Core;
using Player;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

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
        private BeginSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        ILogDispatcher logger;

        /*
        EntityQuery m_CellData;
        EntityQuery m_HeroData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameStateData;
        */

        EntityQuery m_UnitData;
        EntityQuery m_EffectStackData;

        protected override void OnCreate()
        {
            base.OnCreate();

            entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

            m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            //ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );

            m_EffectStackData = GetEntityQuery(
                 ComponentType.ReadWrite<EffectStack.Component>()
            );

            //RequireSingletonForUpdate<EffectStack.Component>();

            /*
            m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );

            m_HeroData = GetEntityQuery(
            ComponentType.ReadOnly<Hero.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            );


            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<Vision.Component>(),
            ComponentType.ReadOnly<PlayerAttributes.Component>(),
            ComponentType.ReadWrite<PlayerState.Component>()
            );

            m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<GameState.Component>()
            );
            */
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
            m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
            m_ManalithSystem = World.GetExistingSystem<ManalithSystem>();
            m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            m_CellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
            m_CleanUpSystem = World.GetExistingSystem<CleanupSystem>();
            m_SpawnSystem = World.GetExistingSystem<SpawnUnitsSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_EffectStackData.CalculateEntityCount() == 0)
            {
                //Debug.Log("EffectStackDataCount = 0");
                return inputDeps;
            }

            EntityCommandBuffer ECBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            var effectStack = m_EffectStackData.GetSingleton<EffectStack.Component>();

            Entities.ForEach((ref GameState.Component gameState, in WorldIndexShared gameStateWorldIndex, in SpatialEntityId gameStateId) =>
            {
                switch (gameState.CurrentState)
                {
                    case GameStateEnum.waiting_for_players:
                       /*
#if UNITY_EDITOR
                        //check if game is ready to start (> everything has been initialized) instead of checking for a hardcoded number of units on map
                        if (gameState.PlayersOnMapCount == 1)
                        {
                            if (gameState.CurrentWaitTime <= 0)
                            {
                                gameState.WinnerFaction = 0;
                                gameState.TurnCounter = 0;
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.InitializeMapEvent.Event(new InitializeMap(gameStateWorldIndex.Value)),
                                gameStateId.EntityId
                                );
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                                UpdateUnitsGameStateTag(GameStateEnum.cleanup, gameStateWorldIndex, ECBuffer);
                                gameState.CurrentState = GameStateEnum.cleanup;

                            }
                            else
                                gameState.CurrentWaitTime -= Time.DeltaTime;
                        }
#else
*/
                        if (gameState.PlayersOnMapCount == 2)
                        {
                            if (gameState.CurrentWaitTime <= 0)
                            {
                                gameState.WinnerFaction = 0;
                                gameState.TurnCounter = 0;
                                m_ComponentUpdateSystem.SendEvent(
                                new GameState.InitializeMapEvent.Event(new InitializeMap(gameStateWorldIndex.Value)),
                                gameStateId.EntityId
                                );
                                gameState.CurrentWaitTime = gameState.CalculateWaitTime;
                                UpdateUnitsGameStateTag(GameStateEnum.cleanup, gameStateWorldIndex, ECBuffer);
                                gameState.CurrentState = GameStateEnum.cleanup;
                            }
                            else
                                gameState.CurrentWaitTime -= Time.DeltaTime;
                        }
//#endif
                        break;
                    case GameStateEnum.planning:
                        if(gameState.CurrentRopeTime <= 0)
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
                            if(gameState.TurnCounter != 1)
                                gameState.CurrentRopeTime -= Time.DeltaTime;
                        }
                        break;
                    case GameStateEnum.start_execute:
                        UpdateUnitsGameStateTag(GameStateEnum.interrupt, gameStateWorldIndex, ECBuffer);
                        SetEffectStackInfo(effectStack, gameStateWorldIndex.Value, GameStateEnum.interrupt);
                        gameState.CurrentState = GameStateEnum.interrupt;
                        break;
                    case GameStateEnum.interrupt:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.interrupt, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.attack, gameStateWorldIndex, ECBuffer);
                                SetEffectStackInfo(effectStack, gameStateWorldIndex.Value, GameStateEnum.attack);
                                gameState.CurrentState = GameStateEnum.attack;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.attack:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.attack, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.move, gameStateWorldIndex, ECBuffer);
                                SetEffectStackInfo(effectStack, gameStateWorldIndex.Value, GameStateEnum.move);
                                gameState.CurrentState = GameStateEnum.move;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.move:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.move, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.skillshot, gameStateWorldIndex, ECBuffer);
                                UpdateMovedUnitCells(gameStateWorldIndex);
                                SetEffectStackInfo(effectStack, gameStateWorldIndex.Value, GameStateEnum.skillshot);
                                gameState.CurrentState = GameStateEnum.skillshot;
                                gameState.HighestExecuteTime = 0;
                            }
                        }
                        break;
                    case GameStateEnum.skillshot:
                        if (gameState.HighestExecuteTime == 0)
                        {
                            gameState.HighestExecuteTime = HighestExecuteTime(GameStateEnum.skillshot, gameStateWorldIndex, gameState.MinExecuteStepTime);
                        }
                        else
                        {
                            gameState.HighestExecuteTime -= Time.DeltaTime;
                            if (gameState.HighestExecuteTime <= .1f)
                            {
                                UpdateUnitsGameStateTag(GameStateEnum.cleanup, gameStateWorldIndex, ECBuffer);
                                SetEffectStackInfo(effectStack, gameStateWorldIndex.Value, GameStateEnum.cleanup);
                                gameState.CurrentState = GameStateEnum.cleanup;
                                gameState.HighestExecuteTime = 0;
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
                            CleanUpEffectStack(effectStack, gameStateWorldIndex.Value);
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
                                    m_ManalithSystem.UpdateManaliths(gameStateWorldIndex);
                                    m_ResourceSystem.CalculateIncome(gameStateWorldIndex);

                                    m_ComponentUpdateSystem.SendEvent(
                                    new GameState.CleanupStateEvent.Event(),
                                    gameStateId.EntityId);

                                    UpdateUnitsGameStateTag(GameStateEnum.planning, gameStateWorldIndex, ECBuffer);
                                    CleanUpUnitIncomingEffects(gameStateWorldIndex);
                                    CleanUpEffectStack(effectStack, gameStateWorldIndex.Value);
                                    SetEffectStackInfo(effectStack, gameStateWorldIndex.Value, GameStateEnum.planning);
                                    gameState.CurrentState = GameStateEnum.planning;
                                }
                            }
                        }
                        break;
                    case GameStateEnum.game_over:
                        RevealPlayerVisions(gameStateWorldIndex);
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
                /*
#if UNITY_EDITOR
                if (gameState.PlayersOnMapCount == 2 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                {
                    m_CleanUpSystem.DeleteAllUnits(gameStateWorldIndex);
                    gameState.InitMapWaitTime = 2f;
                    gameState.CurrentState = GameStateEnum.waiting_for_players;
                }
#else
*/
                        if(gameState.PlayersOnMapCount == 0 && gameState.CurrentState != GameStateEnum.waiting_for_players)
                        {
                            m_CleanUpSystem.DeleteAllUnits(gameStateWorldIndex);
                            gameState.InitMapWaitTime = 2f;
                            gameState.CurrentState = GameStateEnum.waiting_for_players;
                        }
//#endif
            })
            .WithoutBurst()
            .Run();

            return inputDeps;
        }

        void SetEffectStackInfo(EffectStack.Component effectStack, uint worldIndex, GameStateEnum gameState)
        {
            for(int i = 0; i < effectStack.GameStateEffectStacks.Count; i++)
            {
                if (effectStack.GameStateEffectStacks[i].WorldIndex == worldIndex)
                {
                    var e = effectStack.GameStateEffectStacks[i];
                    e.CurrentState = gameState;
                    e.EffectsExecuted = false;
                    effectStack.GameStateEffectStacks[i] = e;                }
            }
            m_EffectStackData.SetSingleton(effectStack);
        }

        void CleanUpEffectStack(EffectStack.Component effectStack, uint worldIndex)
        {
            for (int i = 0; i < effectStack.GameStateEffectStacks.Count; i++)
            {
                if (effectStack.GameStateEffectStacks[i].WorldIndex == worldIndex)
                {
                    for (int y = effectStack.GameStateEffectStacks[i].InterruptEffects.Count; y > 0; y--)
                    {
                        if (effectStack.GameStateEffectStacks[i].InterruptEffects[y - 1].TurnDuration == 0)
                        {
                            effectStack.GameStateEffectStacks[i].InterruptEffects.RemoveAt(y - 1);
                        }
                    }
                    for (int y = effectStack.GameStateEffectStacks[i].AttackEffects.Count; y > 0; y--)
                    {
                        if (effectStack.GameStateEffectStacks[i].AttackEffects[y - 1].TurnDuration == 0)
                        {
                            effectStack.GameStateEffectStacks[i].AttackEffects.RemoveAt(y - 1);
                        }
                    }
                    for (int y = effectStack.GameStateEffectStacks[i].MoveEffects.Count; y > 0; y--)
                    {
                        if (effectStack.GameStateEffectStacks[i].MoveEffects[y - 1].TurnDuration == 0)
                        {
                            effectStack.GameStateEffectStacks[i].MoveEffects.RemoveAt(y - 1);
                        }
                    }
                    for (int y = effectStack.GameStateEffectStacks[i].SkillshotEffects.Count; y > 0; y--)
                    {
                        if (effectStack.GameStateEffectStacks[i].SkillshotEffects[y - 1].TurnDuration == 0)
                        {
                            effectStack.GameStateEffectStacks[i].SkillshotEffects.RemoveAt(y - 1);
                        }
                    }
                }
            }
            m_EffectStackData.SetSingleton(effectStack);
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
                        float unitMoveTime = unitAction.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell * (unitAction.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs.Count + 1);
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

        private void UpdateMovedUnitCells(WorldIndexShared gameStateWorldIndex)
        {
            //Heap allocation - needs to be replaced
            Dictionary<Vector3f, long> unitDict = new Dictionary<Vector3f, long>();
            //HashSet<Vector3f> unitCoordHash = new HashSet<Vector3f>();

            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach((in CubeCoordinate.Component cubeCoord, in SpatialEntityId entityId, in Actions.Component actions, in Health.Component health, in IncomingActionEffects.Component incomingEffects) =>
            {
                if (incomingEffects.MoveEffects.Count != 0 && health.CurrentHealth > 0 && incomingEffects.MoveEffects[0].EffectType == EffectTypeEnum.move_along_path)
                {
                    //Debug.Log("AddMovedUnitToDict");
                    if (!unitDict.ContainsKey(cubeCoord.CubeCoordinate))
                    {
                        unitDict.Add(actions.LockedAction.Targets[0].Mods[0].PathNested.OriginCoordinate, entityId.EntityId.Id);
                        unitDict.Add(cubeCoord.CubeCoordinate, entityId.EntityId.Id);
                    }
                }
            })
            .WithoutBurst()
            .Run();

            Entities.WithSharedComponentFilter(gameStateWorldIndex).ForEach(( ref CellAttributesComponent.Component cellAtt, in CubeCoordinate.Component cellCubeCoordinate) =>
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
    }
}
