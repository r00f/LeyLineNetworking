using Unity.Entities;
using Unit;
using Improbable.Gdk.Core;
using Generic;
using LeyLineHybridECS;
using System.Collections.Generic;
using Unity.Jobs;
using Improbable.Gdk.PlayerLifecycle;
using UnityEngine;
using Cell;
using Improbable.Gdk.Core.Commands;
using Improbable;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
public class ExecuteActionsSystem : JobComponentSystem
{
    ILogDispatcher logger;
    PathFindingSystem m_PathFindingSystem;
    HandleCellGridRequestsSystem m_HandleCellGridSystem;
    ResourceSystem m_ResourceSystem;
    InitializeWorldSystem m_SpawnSystem;
    CommandSystem m_CommandSystem;

    EntityQuery m_GameStateData;
    EntityQuery m_CellData;
    EntityQuery m_UnitData;
    EntityQuery m_EffectStackData;


    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_EffectStackData = GetEntityQuery(
        ComponentType.ReadWrite<EffectStack.Component>()
        );

        RequireSingletonForUpdate<EffectStack.Component>();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_HandleCellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
        m_ResourceSystem = World.GetExistingSystem<ResourceSystem>();
        m_SpawnSystem = World.GetExistingSystem<InitializeWorldSystem>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //var effectStack = m_EffectStackData.GetSingleton<EffectStack.Component>();
        EntityCommandBuffer ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        EntityCommandBuffer.ParallelWriter ecbConcurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        Entities.ForEach((ref EffectStack.Component effectStack, in ClientWorkerIds.Component clientWorkerIds, in GameState.Component gameState, in WorldIndexShared gameStateWorldIndex) =>
        {
            effectStack = NormalizeEffectData(effectStack, gameStateWorldIndex, ecb);
            effectStack = DenormalizeEffectData(effectStack, gameState.CurrentState, clientWorkerIds, gameStateWorldIndex, ecb);
        })
        .WithoutBurst()
        .Run();

        #region DenormalizedUnitLoops
        /// <summary>
        ///
        /// Use Denormalized effect data to transform unit data
        ///
        /// </summary>

        JobHandle applyInterruptDataTransformationJob = Entities.WithAll<IncomingInterruptTag, InterruptTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, ref Health.Component health) =>
        {
            var combinedHealth = health.CurrentHealth + health.Armor;

            for (int i = 0; i < incomingEffects.InterruptEffects.Count; i++)
            {
                var incomingEffect = incomingEffects.InterruptEffects[i];

                switch (incomingEffect.EffectType)
                {
                    case EffectTypeEnum.deal_damage:
                        if ((int) combinedHealth - (int) incomingEffect.DealDamageNested.DamageAmount > 0)
                        {
                            combinedHealth -= incomingEffect.DealDamageNested.DamageAmount;
                            if (health.CurrentHealth > combinedHealth)
                            {
                                health.CurrentHealth = combinedHealth;
                                health.Armor = 0;
                            }
                            else
                                health.Armor = combinedHealth - health.CurrentHealth;
                        }
                        else
                        {
                            health.CurrentHealth = 0;
                            ecbConcurrent.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
                        }
                        break;
                    case EffectTypeEnum.gain_armor:
                        health.Armor += incomingEffect.GainArmorNested.ArmorAmount;
                        break;
                }

                if (incomingEffect.UnitDuration > 0)
                {
                    incomingEffect.UnitDuration--;
                    incomingEffects.InterruptEffects[i] = incomingEffect;
                }
            }
            incomingEffects.InterruptEffects = incomingEffects.InterruptEffects;
            ecbConcurrent.RemoveComponent<InterruptTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        JobHandle applyAttackDataTransformationJob = Entities.WithAll<IncomingAttackTag, AttackTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, ref Health.Component health) =>
        {
            var combinedHealth = health.CurrentHealth + health.Armor;

            for (int i = 0; i < incomingEffects.AttackEffects.Count; i++)
            {
                var incomingEffect = incomingEffects.AttackEffects[i];

                switch (incomingEffect.EffectType)
                {
                    case EffectTypeEnum.deal_damage:
                        if ((int) combinedHealth - (int) incomingEffect.DealDamageNested.DamageAmount > 0)
                        {
                            combinedHealth -= incomingEffect.DealDamageNested.DamageAmount;
                            if (health.CurrentHealth > combinedHealth)
                            {
                                health.CurrentHealth = combinedHealth;
                                health.Armor = 0;
                            }
                            else
                                health.Armor = combinedHealth - health.CurrentHealth;
                        }
                        else
                        {
                            health.CurrentHealth = 0;
                            ecbConcurrent.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
                        }
                        break;
                }

                if (incomingEffect.UnitDuration > 0)
                {
                    incomingEffect.UnitDuration--;
                    incomingEffects.AttackEffects[i] = incomingEffect;
                }
            }
            incomingEffects.AttackEffects = incomingEffects.AttackEffects;
            ecbConcurrent.RemoveComponent<AttackTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        JobHandle applyMoveDataTransformationJob = Entities.WithAll<IncomingMoveTag, MoveTag>().ForEach((Entity entity, int entityInQueryIndex, ref Position.Component position, ref CubeCoordinate.Component coord, ref UnitVision vision, ref IncomingActionEffects.Component incomingEffects, ref Health.Component health) =>
        {
            var combinedHealth = health.CurrentHealth + health.Armor;

            for (int i = 0; i < incomingEffects.MoveEffects.Count; i++)
            {
                var incomingEffect = incomingEffects.MoveEffects[i];

                switch (incomingEffect.EffectType)
                {
                    case EffectTypeEnum.deal_damage:
                        if ((int) combinedHealth - (int) incomingEffect.DealDamageNested.DamageAmount > 0)
                        {
                            combinedHealth -= incomingEffect.DealDamageNested.DamageAmount;
                            if (health.CurrentHealth > combinedHealth)
                            {
                                health.CurrentHealth = combinedHealth;
                                health.Armor = 0;
                            }
                            else
                                health.Armor = combinedHealth - health.CurrentHealth;
                        }
                        else
                        {
                            health.CurrentHealth = 0;
                            ecbConcurrent.AddComponent<IsDeadTag>(entityInQueryIndex,entity);
                        }
                        break;
                    case EffectTypeEnum.move_along_path:
                        //instead of moving the unit along a path on the server, teleport it to destination and handle path movement on the client
                        var pathCount = incomingEffect.MoveAlongPathNested.CoordinatePositionPairs.Count - 1;
                        position = new Position.Component
                        {
                            Coords = new Coordinates(incomingEffect.MoveAlongPathNested.CoordinatePositionPairs[pathCount].WorldPosition.X, incomingEffect.MoveAlongPathNested.CoordinatePositionPairs[pathCount].WorldPosition.Y, incomingEffect.MoveAlongPathNested.CoordinatePositionPairs[pathCount].WorldPosition.Z)
                        };
                        coord.CubeCoordinate = incomingEffect.MoveAlongPathNested.CoordinatePositionPairs[pathCount].CubeCoordinate;
                        vision.RequireUpdate = true;

                        /*l.HandleLog(LogType.Warning,
                        new LogEvent("UnitPosition after move action")
                        .WithField("Position", position.Coords.ToUnityVector()));
                        */
                        break;
                }

                if (incomingEffect.UnitDuration > 0)
                {
                    incomingEffect.UnitDuration--;
                    incomingEffects.MoveEffects[i] = incomingEffect;
                }
            }

            incomingEffects.MoveEffects = incomingEffects.MoveEffects;
            //EntityManager.RemoveComponent<MoveTag>(entity);
            ecbConcurrent.RemoveComponent<MoveTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        JobHandle applySkillshotDataTransformationJob = Entities.WithAll<IncomingSkillshotTag, SkillshotTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, ref Health.Component health) =>
        {
            var combinedHealth = health.CurrentHealth + health.Armor;

            for (int i = 0; i < incomingEffects.SkillshotEffects.Count; i++)
            {
                var incomingEffect = incomingEffects.SkillshotEffects[i];

                switch (incomingEffect.EffectType)
                {
                    case EffectTypeEnum.deal_damage:
                        if ((int) combinedHealth - (int) incomingEffect.DealDamageNested.DamageAmount > 0)
                        {
                            combinedHealth -= incomingEffect.DealDamageNested.DamageAmount;
                            if (health.CurrentHealth > combinedHealth)
                            {
                                health.CurrentHealth = combinedHealth;
                                health.Armor = 0;
                            }
                            else
                                health.Armor = combinedHealth - health.CurrentHealth;
                        }
                        else
                        {
                            health.CurrentHealth = 0;
                            ecbConcurrent.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
                        }
                        break;
                }

                if (incomingEffect.UnitDuration > 0)
                {
                    incomingEffect.UnitDuration--;
                    incomingEffects.SkillshotEffects[i] = incomingEffect;
                }
            }
            incomingEffects.SkillshotEffects = incomingEffects.SkillshotEffects;
            ecbConcurrent.RemoveComponent<SkillshotTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        #endregion

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applyInterruptDataTransformationJob);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applyAttackDataTransformationJob);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applyMoveDataTransformationJob);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applySkillshotDataTransformationJob);
        
        return inputDeps;
    }

    EffectStack.Component NormalizeEffectData(EffectStack.Component effectStack, WorldIndexShared gameStateWorldIndex, EntityCommandBuffer ecb)
    {
        #region NormalizeEffectData
        /// <summary>
        ///
        /// Units copy their locked action effect data into effectsDataBase when the correct step begins
        ///
        /// </summary>
        Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().WithAll<LockedInterruptTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
        {
            foreach (ActionEffect effect in actions.LockedAction.Effects)
            {
                effectStack.InterruptEffects.Add(effect);
            }
            ecb.RemoveComponent<LockedInterruptTag>(entity);
        })
        .WithoutBurst()
        .Run();

        Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().WithAll<LockedAttackTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
        {
            foreach (ActionEffect effect in actions.LockedAction.Effects)
            {
                effectStack.AttackEffects.Add(effect);
            }
            ecb.RemoveComponent<LockedAttackTag>(entity);
        })
        .WithoutBurst()
        .Run();

        Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().WithAll<LockedMoveTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
        {
            foreach (ActionEffect effect in actions.LockedAction.Effects)
            {
                effectStack.MoveEffects.Add(effect);
            }
            ecb.RemoveComponent<LockedMoveTag>(entity);
        })
        .WithoutBurst()
        .Run();

        Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().WithAll<LockedSkillshotTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
        {
            foreach (ActionEffect effect in actions.LockedAction.Effects)
            {
                effectStack.SkillshotEffects.Add(effect);
            }
            ecb.RemoveComponent<LockedSkillshotTag>(entity);
        })
        .WithoutBurst()
        .Run();

        #endregion

        return effectStack;
    }

    EffectStack.Component DenormalizeEffectData(EffectStack.Component effectStack, GameStateEnum gameState, ClientWorkerIds.Component clientWorkerIds, WorldIndexShared gameStateWorldIndex, EntityCommandBuffer ecb)
    {
        #region DenormalizeEffectData
        /// <summary>
        ///
        /// Copy Effect from EffectsStack back onto target units IncomingEffectsComponent
        ///
        /// </summary>
        if (!effectStack.EffectsExecuted)
        {
            switch (gameState)
            {
                case GameStateEnum.interrupt:

                    effectStack.InterruptEffects = CompareAndCullInterruptSpawnEffects(effectStack.InterruptEffects);

                    for (int y = 0; y < effectStack.InterruptEffects.Count; y++)
                    {
                        switch (effectStack.InterruptEffects[y].EffectType)
                        {
                            case EffectTypeEnum.spawn_unit:
                                var pos = new Position.Component
                                {
                                    Coords = new Coordinates
                                    {
                                        X = effectStack.InterruptEffects[y].TargetPosition.X,
                                        Y = effectStack.InterruptEffects[y].TargetPosition.Y,
                                        Z = effectStack.InterruptEffects[y].TargetPosition.Z
                                    }
                                };

                                string clientWorkerId = "";

                                if (effectStack.InterruptEffects[y].OriginUnitFaction == 1)
                                    clientWorkerId = clientWorkerIds.ClientWorkerId1;
                                else
                                    clientWorkerId = clientWorkerIds.ClientWorkerId2;

                                var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + effectStack.InterruptEffects[y].SpawnUnitNested.UnitName).GetComponent<UnitDataSet>();
                                var newUnit = LeyLineEntityTemplates.Unit(clientWorkerId, effectStack.InterruptEffects[y].SpawnUnitNested.UnitName, pos, effectStack.InterruptEffects[y].TargetCoordinates[0], effectStack.InterruptEffects[y].OriginUnitFaction, gameStateWorldIndex.Value, Stats, 0);
                                var createEntitiyRequest = new WorldCommands.CreateEntity.Request(newUnit);
                                m_CommandSystem.SendCommand(createEntitiyRequest);
                                break;
                        }
                    }

                    Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                    {
                        for (int y = 0; y < effectStack.InterruptEffects.Count; y++)
                        {
                            if (effectStack.InterruptEffects[y].TargetCoordinates != null && effectStack.InterruptEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                            {
                                bool valid = false;

                                switch (effectStack.InterruptEffects[y].ApplyToRestrictions)
                                {
                                    case ApplyToRestrictionsEnum.any:
                                        valid = true;
                                        break;
                                    case ApplyToRestrictionsEnum.enemy:
                                        if (unitFaction.Faction != effectStack.InterruptEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly:
                                        if (unitFaction.Faction == effectStack.InterruptEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly_other:
                                        if (unitFaction.Faction == effectStack.InterruptEffects[y].OriginUnitFaction && effectStack.InterruptEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.other:
                                        if (effectStack.InterruptEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.self:
                                        if (effectStack.InterruptEffects[y].OriginUnitId == id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;

                                    default:
                                        valid = false;
                                        break;
                                }

                                if (valid)
                                {
                                    var incEffect = new IncomingActionEffect
                                    {
                                        EffectType = effectStack.InterruptEffects[y].EffectType,
                                        UnitDuration = effectStack.InterruptEffects[y].UnitDuration,
                                        MoveAlongPathNested = effectStack.InterruptEffects[y].MoveAlongPathNested
                                    };

                                    switch (incEffect.EffectType)
                                    {
                                        case EffectTypeEnum.gain_armor:
                                            incEffect.GainArmorNested = effectStack.InterruptEffects[y].GainArmorNested;
                                            break;
                                        case EffectTypeEnum.spawn_unit:
                                            incEffect.SpawnUnitNested = effectStack.InterruptEffects[y].SpawnUnitNested;
                                            break;
                                        case EffectTypeEnum.deal_damage:
                                            incEffect.DealDamageNested = effectStack.InterruptEffects[y].DealDamageNested;
                                            break;
                                    }

                                    incomingEffects.InterruptEffects.Add(incEffect);
                                    incomingEffects.InterruptEffects = incomingEffects.InterruptEffects;

                                    if (incomingEffects.InterruptEffects.Count == 1)
                                    {
                                        ecb.AddComponent<IncomingInterruptTag>(entity);
                                    }
                                }
                            }
                        }
                    })
                    .WithoutBurst()
                    .Run();
                    effectStack.InterruptEffects = SubtractTurnTimers(effectStack.InterruptEffects);
                    break;
                case GameStateEnum.attack:
                    Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                    {
                        for (int y = 0; y < effectStack.AttackEffects.Count; y++)
                        {
                            if (effectStack.AttackEffects[y].TargetCoordinates != null && effectStack.AttackEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                            {
                                //var currentEffect = g.AttackEffects[y];
                                bool valid = false;

                                switch (effectStack.AttackEffects[y].ApplyToRestrictions)
                                {
                                    case ApplyToRestrictionsEnum.any:
                                        valid = true;
                                        break;
                                    case ApplyToRestrictionsEnum.enemy:
                                        if (unitFaction.Faction != effectStack.AttackEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly:
                                        if (unitFaction.Faction == effectStack.AttackEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly_other:
                                        if (unitFaction.Faction == effectStack.AttackEffects[y].OriginUnitFaction && effectStack.AttackEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.other:
                                        if (effectStack.AttackEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.self:
                                        if (effectStack.AttackEffects[y].OriginUnitId == id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;

                                    default:
                                        valid = false;
                                        break;
                                }

                                if (valid)
                                {
                                    var incEffect = new IncomingActionEffect
                                    {
                                        EffectType = effectStack.AttackEffects[y].EffectType,
                                        UnitDuration = effectStack.AttackEffects[y].UnitDuration,
                                        MoveAlongPathNested = effectStack.AttackEffects[y].MoveAlongPathNested
                                    };

                                    switch (incEffect.EffectType)
                                    {
                                        case EffectTypeEnum.gain_armor:
                                            incEffect.GainArmorNested = effectStack.AttackEffects[y].GainArmorNested;
                                            break;
                                        case EffectTypeEnum.spawn_unit:
                                            incEffect.SpawnUnitNested = effectStack.AttackEffects[y].SpawnUnitNested;
                                            break;
                                        case EffectTypeEnum.deal_damage:
                                            incEffect.DealDamageNested = effectStack.AttackEffects[y].DealDamageNested;
                                            break;
                                    }

                                    incomingEffects.AttackEffects.Add(incEffect);
                                    incomingEffects.AttackEffects = incomingEffects.AttackEffects;

                                    if (incomingEffects.AttackEffects.Count == 1)
                                    {
                                        ecb.AddComponent<IncomingAttackTag>(entity);
                                    }
                                }
                            }
                        }
                    })
                    .WithoutBurst()
                    .Run();
                    effectStack.AttackEffects = SubtractTurnTimers(effectStack.AttackEffects);
                    break;
                case GameStateEnum.move:
                    for (int y = 0; y < effectStack.MoveEffects.Count; y++)
                    {
                        switch (effectStack.MoveEffects[y].EffectType)
                        {
                            case EffectTypeEnum.spawn_unit:
                                var pos = new Position.Component
                                {
                                    Coords = new Coordinates
                                    {
                                        X = effectStack.MoveEffects[y].TargetPosition.X,
                                        Y = effectStack.MoveEffects[y].TargetPosition.Y,
                                        Z = effectStack.MoveEffects[y].TargetPosition.Z
                                    }
                                };

                                string clientWorkerId = "";

                                if (effectStack.MoveEffects[y].OriginUnitFaction == 1)
                                    clientWorkerId = clientWorkerIds.ClientWorkerId1;
                                else
                                    clientWorkerId = clientWorkerIds.ClientWorkerId2;

                                var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + effectStack.MoveEffects[y].SpawnUnitNested.UnitName).GetComponent<UnitDataSet>();
                                var newUnit = LeyLineEntityTemplates.Unit(clientWorkerId, effectStack.MoveEffects[y].SpawnUnitNested.UnitName, pos, effectStack.MoveEffects[y].TargetCoordinates[0], effectStack.MoveEffects[y].OriginUnitFaction, gameStateWorldIndex.Value, Stats, 0);
                                var createEntitiyRequest = new WorldCommands.CreateEntity.Request(newUnit);
                                m_CommandSystem.SendCommand(createEntitiyRequest);
                                break;
                        }
                    }

                    effectStack.MoveEffects = CompareAndCullMoveInterruptEffects(effectStack.InterruptEffects, effectStack.MoveEffects);
                    effectStack.MoveEffects = CompareAndCullMoveEffects(effectStack.MoveEffects);

                    Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                    {
                        for (int y = 0; y < effectStack.MoveEffects.Count; y++)
                        {
                            if (effectStack.MoveEffects[y].EffectType == EffectTypeEnum.move_along_path)
                            {
                                if (effectStack.MoveEffects[y].OriginUnitId == id.EntityId.Id)
                                {
                                    var incEffect = new IncomingActionEffect
                                    {
                                        EffectType = effectStack.MoveEffects[y].EffectType,
                                        UnitDuration = effectStack.MoveEffects[y].UnitDuration,
                                        MoveAlongPathNested = effectStack.MoveEffects[y].MoveAlongPathNested
                                    };

                                    incomingEffects.MoveEffects.Add(incEffect);
                                    incomingEffects.MoveEffects = incomingEffects.MoveEffects;

                                    if (incomingEffects.MoveEffects.Count == 1)
                                    {
                                        ecb.AddComponent<IncomingMoveTag>(entity);
                                    }
                                }
                            }
                            else if (effectStack.MoveEffects[y].TargetCoordinates != null && effectStack.MoveEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                            {
                                bool valid = false;
                                switch (effectStack.MoveEffects[y].ApplyToRestrictions)
                                {
                                    case ApplyToRestrictionsEnum.any:
                                        valid = true;
                                        break;
                                    case ApplyToRestrictionsEnum.enemy:
                                        if (unitFaction.Faction != effectStack.MoveEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly:
                                        if (unitFaction.Faction == effectStack.MoveEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly_other:
                                        if (unitFaction.Faction == effectStack.MoveEffects[y].OriginUnitFaction && effectStack.MoveEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.other:
                                        if (effectStack.MoveEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.self:
                                        if (effectStack.MoveEffects[y].OriginUnitId == id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;

                                    default:
                                        valid = false;
                                        break;
                                }

                                if (valid)
                                {
                                    var incEffect = new IncomingActionEffect
                                    {
                                        EffectType = effectStack.MoveEffects[y].EffectType,
                                        UnitDuration = effectStack.MoveEffects[y].UnitDuration,
                                        MoveAlongPathNested = effectStack.MoveEffects[y].MoveAlongPathNested
                                    };

                                    switch (incEffect.EffectType)
                                    {
                                        case EffectTypeEnum.gain_armor:
                                            incEffect.GainArmorNested = effectStack.MoveEffects[y].GainArmorNested;
                                            break;
                                        case EffectTypeEnum.spawn_unit:
                                            incEffect.SpawnUnitNested = effectStack.MoveEffects[y].SpawnUnitNested;
                                            break;
                                        case EffectTypeEnum.deal_damage:
                                            incEffect.DealDamageNested = effectStack.MoveEffects[y].DealDamageNested;
                                            break;
                                    }

                                    incomingEffects.MoveEffects.Add(incEffect);
                                    incomingEffects.MoveEffects = incomingEffects.MoveEffects;

                                    if (incomingEffects.MoveEffects.Count == 1)
                                    {
                                        ecb.AddComponent<IncomingMoveTag>(entity);
                                    }
                                }
                            }
                        }
                    })
                    .WithoutBurst()
                    .Run(); ;

                    effectStack.MoveEffects = SubtractTurnTimers(effectStack.MoveEffects);
                    break;
                case GameStateEnum.skillshot:
                    Entities.WithSharedComponentFilter(gameStateWorldIndex).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                    {
                        for (int y = 0; y < effectStack.SkillshotEffects.Count; y++)
                        {
                            if (effectStack.SkillshotEffects[y].TargetCoordinates != null && effectStack.SkillshotEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                            {
                                bool valid = false;
                                switch (effectStack.SkillshotEffects[y].ApplyToRestrictions)
                                {
                                    case ApplyToRestrictionsEnum.any:
                                        valid = true;
                                        break;
                                    case ApplyToRestrictionsEnum.enemy:
                                        if (unitFaction.Faction != effectStack.SkillshotEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly:
                                        if (unitFaction.Faction == effectStack.SkillshotEffects[y].OriginUnitFaction)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.friendly_other:
                                        if (unitFaction.Faction == effectStack.SkillshotEffects[y].OriginUnitFaction && effectStack.SkillshotEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.other:
                                        if (effectStack.SkillshotEffects[y].OriginUnitId != id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;
                                    case ApplyToRestrictionsEnum.self:
                                        if (effectStack.SkillshotEffects[y].OriginUnitId == id.EntityId.Id)
                                        {
                                            valid = true;
                                        }
                                        break;

                                    default:
                                        valid = false;
                                        break;
                                }

                                if (valid)
                                {
                                    var incEffect = new IncomingActionEffect
                                    {
                                        EffectType = effectStack.SkillshotEffects[y].EffectType,
                                        UnitDuration = effectStack.SkillshotEffects[y].UnitDuration,
                                        MoveAlongPathNested = effectStack.SkillshotEffects[y].MoveAlongPathNested
                                    };

                                    switch (incEffect.EffectType)
                                    {
                                        case EffectTypeEnum.gain_armor:
                                            incEffect.GainArmorNested = effectStack.SkillshotEffects[y].GainArmorNested;
                                            break;
                                        case EffectTypeEnum.spawn_unit:
                                            incEffect.SpawnUnitNested = effectStack.SkillshotEffects[y].SpawnUnitNested;
                                            break;
                                        case EffectTypeEnum.deal_damage:
                                            incEffect.DealDamageNested = effectStack.SkillshotEffects[y].DealDamageNested;
                                            break;
                                    }

                                    incomingEffects.SkillshotEffects.Add(incEffect);
                                    incomingEffects.SkillshotEffects = incomingEffects.SkillshotEffects;

                                    if (incomingEffects.SkillshotEffects.Count == 1)
                                    {
                                        ecb.AddComponent<IncomingSkillshotTag>(entity);
                                    }
                                }
                            }
                        }
                    })
                    .WithoutBurst()
                    .Run();

                    effectStack.SkillshotEffects = SubtractTurnTimers(effectStack.SkillshotEffects);
                    break;
            }
            effectStack.EffectsExecuted = true;
        }
        #endregion


        return effectStack;
    }

    List<ActionEffect> SubtractTurnTimers(List<ActionEffect> actionEffects)
    {
        for (int y = 0; y < actionEffects.Count; y++)
        {
            if (actionEffects[y].TurnDuration > 0)
            {
                var e = actionEffects[y];
                e.TurnDuration--;
                actionEffects[y] = e;
            }
        }

        return actionEffects;
    }

    List<ActionEffect> CompareAndCullInterruptSpawnEffects(List<ActionEffect> interruptEffects)
    {
        List<int> indexesToRemove = new List<int>();

        for (int i = 0; i < interruptEffects.Count; i++)
        {
            if (interruptEffects[i].EffectType == EffectTypeEnum.spawn_unit)
            {
                for (int y = 0; y < interruptEffects.Count; y++)
                {
                    if (y != i && interruptEffects[y].EffectType == EffectTypeEnum.spawn_unit)
                    {
                        if (Vector3fext.ToUnityVector(interruptEffects[i].TargetCoordinates[0]) == Vector3fext.ToUnityVector(interruptEffects[y].TargetCoordinates[0]))
                        {
                            if(!indexesToRemove.Contains(i))
                                indexesToRemove.Add(i);
                            if (!indexesToRemove.Contains(y))
                                indexesToRemove.Add(y);
                        }
                    }
                }
            }
        }

        indexesToRemove.Sort();
        //sort indexes to remove from high to low to prevent index out of range (remove 0 then remove last in list)
        indexesToRemove.Reverse();

        for (int i = 0; i < indexesToRemove.Count; i++)
        {
            /*
            logger.HandleLog
                (LogType.Warning,
                new LogEvent("Culling Spawn Actions")
                .WithField("index to remove", indexesToRemove[i])
                .WithField("interrupteffect count", interruptEffects.Count)
                );
                */
            interruptEffects.RemoveAt(indexesToRemove[i]);
        }

        return interruptEffects;
    }

    List<ActionEffect> CompareAndCullMoveInterruptEffects(in List<ActionEffect> interruptEffects, List<ActionEffect> moveEffects)
    {
        //find all effects with the same target coord
        for (int i = 0; i < interruptEffects.Count; i++)
        {
            if (interruptEffects[i].EffectType == EffectTypeEnum.spawn_unit)
            {
                for (int y = 0; y < moveEffects.Count; y++)
                {
                    if (moveEffects[y].EffectType == EffectTypeEnum.move_along_path)
                    {
                        var pathCount = moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.Count;
                        if (pathCount > 0 && Vector3fext.ToUnityVector(interruptEffects[i].TargetCoordinates[0]) == Vector3fext.ToUnityVector(moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs[pathCount - 1].CubeCoordinate))
                        {
                            moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.RemoveAt(pathCount - 1);
                        }
                    }
                }
            }
        }

        for (int i = moveEffects.Count - 1; i >= 0; i--)
        {
            if (moveEffects[i].EffectType == EffectTypeEnum.move_along_path && moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.Count == 0)
            {
                /*
                logger.HandleLog
                (LogType.Warning,
                new LogEvent("Removing Move Action from Cull Spawn / Move actions")
                .WithField("index to remove", i)
                .WithField("moveEffect count", moveEffects.Count)
                );
                */
                moveEffects.RemoveAt(i);
            }
        }
        return moveEffects;
    }

    List<ActionEffect> CompareAndCullMoveEffects(List<ActionEffect> moveEffects)
    {
        //Debug.Log("StartCullMethod");
        //find all effects with the same target coord
        bool culledAPath;
        culledAPath = false;

        for (int i = 0; i < moveEffects.Count; i++)
        {
            //Debug.Log("i index = " + i);
            if (moveEffects[i].EffectType == EffectTypeEnum.move_along_path)
            {
                var pathCount = moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.Count;
                for (int y = 0; y < moveEffects.Count; y++)
                {
                    //Debug.Log("Y index = " + y);
                    if (y != i && moveEffects[y].EffectType == EffectTypeEnum.move_along_path)
                    {
                        var otherPathCount = moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.Count;
                        if (pathCount != 0 && otherPathCount != 0 && Vector3fext.ToUnityVector(moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs[pathCount - 1].CubeCoordinate) == Vector3fext.ToUnityVector(moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs[otherPathCount-1].CubeCoordinate))
                        {
                            float unitMoveTime = moveEffects[i].MoveAlongPathNested.TimePerCell * pathCount;
                            float otherUnitMoveTime = moveEffects[y].MoveAlongPathNested.TimePerCell * otherPathCount;

                            if (unitMoveTime == otherUnitMoveTime)
                            {
                                if (Random.Range(0, 2) == 0)
                                {
                                    if (moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.Count != 0)
                                    {
                                        moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.RemoveAt(pathCount - 1);
                                        pathCount--;
                                    }
                                }
                                else
                                {
                                    if (moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.Count != 0)
                                        moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.RemoveAt(otherPathCount - 1);
                                }
                            }
                            else if (unitMoveTime > otherUnitMoveTime)
                            {
                                if (moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.Count != 0)
                                {
                                    moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.RemoveAt(pathCount - 1);
                                    pathCount--;
                                }
                            }
                            culledAPath = true;
                        }
                    }
                }
            }
        }

        if (culledAPath)
            return CompareAndCullMoveEffects(moveEffects);
        else
        {
            for (int i = moveEffects.Count - 1; i >= 0; i--)
            {
                if (moveEffects[i].EffectType == EffectTypeEnum.move_along_path && moveEffects[i].MoveAlongPathNested.CoordinatePositionPairs.Count == 0)
                {
                    moveEffects.RemoveAt(i);
                }
            }
            return moveEffects;
        }
    }
}
