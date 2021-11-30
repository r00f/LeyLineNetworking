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

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(InitializePlayerSystem)), UpdateBefore(typeof(HandleCellGridRequestsSystem))]
public class ExecuteActionsSystem : JobComponentSystem
{
    ILogDispatcher logger;
    PathFindingSystem m_PathFindingSystem;
    HandleCellGridRequestsSystem m_HandleCellGridSystem;
    ResourceSystem m_ResourceSystem;
    TimerSystem m_TimerSystem;
    SpawnUnitsSystem m_SpawnSystem;
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
        m_TimerSystem = World.GetExistingSystem<TimerSystem>();
        m_SpawnSystem = World.GetExistingSystem<SpawnUnitsSystem>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var effectStack = m_EffectStackData.GetSingleton<EffectStack.Component>();
        EntityCommandBuffer.ParallelWriter ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        for (int i = 0; i < effectStack.GameStateEffectStacks.Count; i++)
        {
            var gameStateEffectStack = effectStack.GameStateEffectStacks[i];

            #region NormalizeEffectData
            /// <summary>
            ///
            /// Units copy their locked action effect data into effectsDataBase when the correct step begins
            ///
            /// </summary>
            JobHandle writeInterruptEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().WithAll<LockedInterruptTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
            {
                foreach (ActionEffect effect in actions.LockedAction.Effects)
                {
                    effectStack.GameStateEffectStacks[i].InterruptEffects.Add(effect);
                }
                ecb.RemoveComponent<LockedInterruptTag>(entityInQueryIndex, entity);
            })
            .Schedule(inputDeps);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(writeInterruptEffectDataJob);

            JobHandle writeAttackEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().WithAll<LockedAttackTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
            {
                foreach (ActionEffect effect in actions.LockedAction.Effects)
                {
                    effectStack.GameStateEffectStacks[i].AttackEffects.Add(effect);
                }
                ecb.RemoveComponent<LockedAttackTag>(entityInQueryIndex, entity);
            })
            .Schedule(inputDeps);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(writeAttackEffectDataJob);

            JobHandle writeMoveEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().WithAll<LockedMoveTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
            {
                foreach (ActionEffect effect in actions.LockedAction.Effects)
                {
                    //Debug.Log("Write MoveEffect into effectstack with Type = " + effect.EffectType.ToString());
                    effectStack.GameStateEffectStacks[i].MoveEffects.Add(effect);
                }
                ecb.RemoveComponent<LockedMoveTag>(entityInQueryIndex, entity);
            })
            .Schedule(inputDeps);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(writeMoveEffectDataJob);

            JobHandle writeSkillshotEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().WithAll<LockedSkillshotTag>().ForEach((Entity entity, int entityInQueryIndex, in Actions.Component actions) =>
            {
                foreach (ActionEffect effect in actions.LockedAction.Effects)
                {
                    effectStack.GameStateEffectStacks[i].SkillshotEffects.Add(effect);
                }
                ecb.RemoveComponent<LockedSkillshotTag>(entityInQueryIndex, entity);
            })
            .Schedule(inputDeps);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(writeSkillshotEffectDataJob);
            #endregion

            writeInterruptEffectDataJob.Complete();
            writeMoveEffectDataJob.Complete();

            #region DenormalizeEffectData
            /// <summary>
            ///
            /// Copy Effect from EffectsStack back onto target units IncomingEffectsComponent
            ///
            /// </summary>
            if (!effectStack.GameStateEffectStacks[i].EffectsExecuted)
            {
                switch (effectStack.GameStateEffectStacks[i].CurrentState)
                {
                    case GameStateEnum.interrupt:

                        CompareAndCullInterruptSpawnEffects(ref gameStateEffectStack.InterruptEffects);

                        for (int y = 0; y < effectStack.GameStateEffectStacks[i].InterruptEffects.Count; y++)
                        {
                            switch (effectStack.GameStateEffectStacks[i].InterruptEffects[y].EffectType)
                            {
                                case EffectTypeEnum.spawn_unit:
                                    var pos = new Position.Component
                                    {
                                        Coords = new Coordinates
                                        {
                                            X = effectStack.GameStateEffectStacks[i].InterruptEffects[y].TargetPosition.X,
                                            Y = effectStack.GameStateEffectStacks[i].InterruptEffects[y].TargetPosition.Y,
                                            Z = effectStack.GameStateEffectStacks[i].InterruptEffects[y].TargetPosition.Z
                                        }
                                    };
                                    var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + effectStack.GameStateEffectStacks[i].InterruptEffects[y].SpawnUnitNested.UnitName).GetComponent<UnitDataSet>();
                                    var newUnit = LeyLineEntityTemplates.Unit(effectStack.GameStateEffectStacks[i].InterruptEffects[y].SpawnUnitNested.UnitName, pos, effectStack.GameStateEffectStacks[i].InterruptEffects[y].TargetCoordinates[0], effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitFaction, effectStack.GameStateEffectStacks[i].WorldIndex, Stats, 0);
                                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(newUnit);
                                    m_CommandSystem.SendCommand(createEntitiyRequest);
                                    break;
                            }
                        }

                        JobHandle denormalizeInterruptEffectsJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                        {
                            for (int y = 0; y < effectStack.GameStateEffectStacks[i].InterruptEffects.Count; y++)
                            {
                                if (effectStack.GameStateEffectStacks[i].InterruptEffects[y].TargetCoordinates != null && effectStack.GameStateEffectStacks[i].InterruptEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                                {
                                    bool valid = false;

                                    switch (effectStack.GameStateEffectStacks[i].InterruptEffects[y].ApplyToRestrictions)
                                    {
                                        case ApplyToRestrictionsEnum.any:
                                            valid = true;
                                            break;
                                        case ApplyToRestrictionsEnum.enemy:
                                            if (unitFaction.Faction != effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly_other:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitFaction && effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.other:
                                            if (effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.self:
                                            if (effectStack.GameStateEffectStacks[i].InterruptEffects[y].OriginUnitId == id.EntityId.Id)
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
                                            EffectType = effectStack.GameStateEffectStacks[i].InterruptEffects[y].EffectType,
                                            UnitDuration = effectStack.GameStateEffectStacks[i].InterruptEffects[y].UnitDuration,
                                            MoveAlongPathNested = effectStack.GameStateEffectStacks[i].InterruptEffects[y].MoveAlongPathNested
                                        };

                                        switch (incEffect.EffectType)
                                        {
                                            case EffectTypeEnum.gain_armor:
                                                incEffect.GainArmorNested = effectStack.GameStateEffectStacks[i].InterruptEffects[y].GainArmorNested;
                                                break;
                                            case EffectTypeEnum.spawn_unit:
                                                incEffect.SpawnUnitNested = effectStack.GameStateEffectStacks[i].InterruptEffects[y].SpawnUnitNested;
                                                break;
                                            case EffectTypeEnum.deal_damage:
                                                incEffect.DealDamageNested = effectStack.GameStateEffectStacks[i].InterruptEffects[y].DealDamageNested;
                                                break;
                                        }

                                        incomingEffects.InterruptEffects.Add(incEffect);
                                        incomingEffects.InterruptEffects = incomingEffects.InterruptEffects;

                                        if (incomingEffects.InterruptEffects.Count == 1)
                                        {
                                            ecb.AddComponent<IncomingInterruptTag>(entityInQueryIndex, entity);
                                        }
                                    }
                                }
                            }
                        })
                        .Schedule(writeInterruptEffectDataJob);

                        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(denormalizeInterruptEffectsJob);

                        SubtractTurnTimers(ref gameStateEffectStack.InterruptEffects);
                        break;
                    case GameStateEnum.attack:
                        JobHandle denormalizeAttackEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                        {
                            for (int y = 0; y < effectStack.GameStateEffectStacks[i].AttackEffects.Count; y++)
                            {
                                if (effectStack.GameStateEffectStacks[i].AttackEffects[y].TargetCoordinates != null && effectStack.GameStateEffectStacks[i].AttackEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                                {
                                    //var currentEffect = effectStack.GameStateEffectStacks[i].AttackEffects[y];
                                    bool valid = false;

                                    switch (effectStack.GameStateEffectStacks[i].AttackEffects[y].ApplyToRestrictions)
                                    {
                                        case ApplyToRestrictionsEnum.any:
                                            valid = true;
                                            break;
                                        case ApplyToRestrictionsEnum.enemy:
                                            if (unitFaction.Faction != effectStack.GameStateEffectStacks[i].AttackEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].AttackEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly_other:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].AttackEffects[y].OriginUnitFaction && effectStack.GameStateEffectStacks[i].AttackEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.other:
                                            if (effectStack.GameStateEffectStacks[i].AttackEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.self:
                                            if (effectStack.GameStateEffectStacks[i].AttackEffects[y].OriginUnitId == id.EntityId.Id)
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
                                            EffectType = effectStack.GameStateEffectStacks[i].AttackEffects[y].EffectType,
                                            UnitDuration = effectStack.GameStateEffectStacks[i].AttackEffects[y].UnitDuration,
                                            MoveAlongPathNested = effectStack.GameStateEffectStacks[i].AttackEffects[y].MoveAlongPathNested
                                        };

                                        switch (incEffect.EffectType)
                                        {
                                            case EffectTypeEnum.gain_armor:
                                                incEffect.GainArmorNested = effectStack.GameStateEffectStacks[i].AttackEffects[y].GainArmorNested;
                                                break;
                                            case EffectTypeEnum.spawn_unit:
                                                incEffect.SpawnUnitNested = effectStack.GameStateEffectStacks[i].AttackEffects[y].SpawnUnitNested;
                                                break;
                                            case EffectTypeEnum.deal_damage:
                                                incEffect.DealDamageNested = effectStack.GameStateEffectStacks[i].AttackEffects[y].DealDamageNested;
                                                break;
                                        }

                                        incomingEffects.AttackEffects.Add(incEffect);
                                        incomingEffects.AttackEffects = incomingEffects.AttackEffects;

                                        if (incomingEffects.AttackEffects.Count == 1)
                                        {
                                            ecb.AddComponent<IncomingAttackTag>(entityInQueryIndex, entity);
                                        }
                                    }
                                }
                            }
                        })
                        .Schedule(writeAttackEffectDataJob);
                        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(denormalizeAttackEffectDataJob);
                        SubtractTurnTimers(ref gameStateEffectStack.AttackEffects);
                        break;
                    case GameStateEnum.move:

                        for (int y = 0; y < effectStack.GameStateEffectStacks[i].MoveEffects.Count; y++)
                        {
                            switch (effectStack.GameStateEffectStacks[i].MoveEffects[y].EffectType)
                            {
                                case EffectTypeEnum.spawn_unit:
                                    var pos = new Position.Component
                                    {
                                        Coords = new Coordinates
                                        {
                                            X = effectStack.GameStateEffectStacks[i].MoveEffects[y].TargetPosition.X,
                                            Y = effectStack.GameStateEffectStacks[i].MoveEffects[y].TargetPosition.Y,
                                            Z = effectStack.GameStateEffectStacks[i].MoveEffects[y].TargetPosition.Z
                                        }
                                    };
                                    var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + effectStack.GameStateEffectStacks[i].MoveEffects[y].SpawnUnitNested.UnitName).GetComponent<UnitDataSet>();
                                    var newUnit = LeyLineEntityTemplates.Unit(effectStack.GameStateEffectStacks[i].MoveEffects[y].SpawnUnitNested.UnitName, pos, effectStack.GameStateEffectStacks[i].MoveEffects[y].TargetCoordinates[0], effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitFaction, effectStack.GameStateEffectStacks[i].WorldIndex, Stats, 0);
                                    var createEntitiyRequest = new WorldCommands.CreateEntity.Request(newUnit);
                                    m_CommandSystem.SendCommand(createEntitiyRequest);
                                    break;
                            }
                        }

                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("Before Cull Method")
                        .WithField("MoveEffectCount", gameStateEffectStack.MoveEffects.Count));
                        */

                        gameStateEffectStack.MoveEffects = CompareAndCullMoveInterruptEffects(effectStack.GameStateEffectStacks[i].InterruptEffects, gameStateEffectStack.MoveEffects);
                        gameStateEffectStack.MoveEffects = CompareAndCullMoveEffects(gameStateEffectStack.MoveEffects);

                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("After Cull Complete")
                        .WithField("MoveEffectCount", gameStateEffectStack.MoveEffects.Count));
                        */

                        /*
                        Debug.Log("MoveCount = " + effectStack.GameStateEffectStacks[i].MoveEffects.Count + ", World = " + effectStack.GameStateEffectStacks[i].WorldIndex);


                        foreach (ActionEffect e in effectStack.GameStateEffectStacks[i].MoveEffects)
                        {
                            Debug.Log("MoveEffectTypes before denormalization: " + e.EffectType.ToString());
                        }
                        */

                        JobHandle denormalizeMoveEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                        {
                            for (int y = 0; y < effectStack.GameStateEffectStacks[i].MoveEffects.Count; y++)
                            {
                                if (effectStack.GameStateEffectStacks[i].MoveEffects[y].EffectType == EffectTypeEnum.move_along_path)
                                {
                                    if (effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitId == id.EntityId.Id)
                                    {
                                        var incEffect = new IncomingActionEffect
                                        {
                                            EffectType = effectStack.GameStateEffectStacks[i].MoveEffects[y].EffectType,
                                            UnitDuration = effectStack.GameStateEffectStacks[i].MoveEffects[y].UnitDuration,
                                            MoveAlongPathNested = effectStack.GameStateEffectStacks[i].MoveEffects[y].MoveAlongPathNested
                                        };

                                        incomingEffects.MoveEffects.Add(incEffect);
                                        incomingEffects.MoveEffects = incomingEffects.MoveEffects;

                                        if (incomingEffects.MoveEffects.Count == 1)
                                        {
                                            ecb.AddComponent<IncomingMoveTag>(entityInQueryIndex, entity);
                                        }
                                    }
                                }
                                else if (effectStack.GameStateEffectStacks[i].MoveEffects[y].TargetCoordinates != null && effectStack.GameStateEffectStacks[i].MoveEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                                {
                                    bool valid = false;
                                    switch (effectStack.GameStateEffectStacks[i].MoveEffects[y].ApplyToRestrictions)
                                    {
                                        case ApplyToRestrictionsEnum.any:
                                            valid = true;
                                            break;
                                        case ApplyToRestrictionsEnum.enemy:
                                            if (unitFaction.Faction != effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly_other:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitFaction && effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.other:
                                            if (effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.self:
                                            if (effectStack.GameStateEffectStacks[i].MoveEffects[y].OriginUnitId == id.EntityId.Id)
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
                                            EffectType = effectStack.GameStateEffectStacks[i].MoveEffects[y].EffectType,
                                            UnitDuration = effectStack.GameStateEffectStacks[i].MoveEffects[y].UnitDuration,
                                            MoveAlongPathNested = effectStack.GameStateEffectStacks[i].MoveEffects[y].MoveAlongPathNested
                                        };

                                        switch (incEffect.EffectType)
                                        {
                                            case EffectTypeEnum.gain_armor:
                                                incEffect.GainArmorNested = effectStack.GameStateEffectStacks[i].MoveEffects[y].GainArmorNested;
                                                break;
                                            case EffectTypeEnum.spawn_unit:
                                                incEffect.SpawnUnitNested = effectStack.GameStateEffectStacks[i].MoveEffects[y].SpawnUnitNested;
                                                break;
                                            case EffectTypeEnum.deal_damage:
                                                incEffect.DealDamageNested = effectStack.GameStateEffectStacks[i].MoveEffects[y].DealDamageNested;
                                                break;
                                        }

                                        incomingEffects.MoveEffects.Add(incEffect);
                                        incomingEffects.MoveEffects = incomingEffects.MoveEffects;

                                        if (incomingEffects.MoveEffects.Count == 1)
                                        {
                                            ecb.AddComponent<IncomingMoveTag>(entityInQueryIndex, entity);
                                        }
                                    }
                                }
                            }
                        })
                        .Schedule(writeMoveEffectDataJob);
                        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(denormalizeMoveEffectDataJob);
                        denormalizeMoveEffectDataJob.Complete();
                        SubtractTurnTimers(ref gameStateEffectStack.MoveEffects);
                        break;
                    case GameStateEnum.skillshot:
                        JobHandle denormalizeSkillshotEffectDataJob = Entities.WithSharedComponentFilter(new WorldIndexShared { Value = effectStack.GameStateEffectStacks[i].WorldIndex }).WithNone<IsDeadTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, in SpatialEntityId id, in FactionComponent.Component unitFaction, in CubeCoordinate.Component unitCoord) =>
                        {
                            for (int y = 0; y < effectStack.GameStateEffectStacks[i].SkillshotEffects.Count; y++)
                            {
                                if (effectStack.GameStateEffectStacks[i].SkillshotEffects[y].TargetCoordinates != null && effectStack.GameStateEffectStacks[i].SkillshotEffects[y].TargetCoordinates.Contains(unitCoord.CubeCoordinate))
                                {
                                    bool valid = false;
                                    switch (effectStack.GameStateEffectStacks[i].SkillshotEffects[y].ApplyToRestrictions)
                                    {
                                        case ApplyToRestrictionsEnum.any:
                                            valid = true;
                                            break;
                                        case ApplyToRestrictionsEnum.enemy:
                                            if (unitFaction.Faction != effectStack.GameStateEffectStacks[i].SkillshotEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].SkillshotEffects[y].OriginUnitFaction)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.friendly_other:
                                            if (unitFaction.Faction == effectStack.GameStateEffectStacks[i].SkillshotEffects[y].OriginUnitFaction && effectStack.GameStateEffectStacks[i].SkillshotEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.other:
                                            if (effectStack.GameStateEffectStacks[i].SkillshotEffects[y].OriginUnitId != id.EntityId.Id)
                                            {
                                                valid = true;
                                            }
                                            break;
                                        case ApplyToRestrictionsEnum.self:
                                            if (effectStack.GameStateEffectStacks[i].SkillshotEffects[y].OriginUnitId == id.EntityId.Id)
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
                                            EffectType = effectStack.GameStateEffectStacks[i].SkillshotEffects[y].EffectType,
                                            UnitDuration = effectStack.GameStateEffectStacks[i].SkillshotEffects[y].UnitDuration,
                                            MoveAlongPathNested = effectStack.GameStateEffectStacks[i].SkillshotEffects[y].MoveAlongPathNested
                                        };

                                        switch (incEffect.EffectType)
                                        {
                                            case EffectTypeEnum.gain_armor:
                                                incEffect.GainArmorNested = effectStack.GameStateEffectStacks[i].SkillshotEffects[y].GainArmorNested;
                                                break;
                                            case EffectTypeEnum.spawn_unit:
                                                incEffect.SpawnUnitNested = effectStack.GameStateEffectStacks[i].SkillshotEffects[y].SpawnUnitNested;
                                                break;
                                            case EffectTypeEnum.deal_damage:
                                                incEffect.DealDamageNested = effectStack.GameStateEffectStacks[i].SkillshotEffects[y].DealDamageNested;
                                                break;
                                        }

                                        incomingEffects.SkillshotEffects.Add(incEffect);
                                        incomingEffects.SkillshotEffects = incomingEffects.SkillshotEffects;

                                        if (incomingEffects.SkillshotEffects.Count == 1)
                                        {
                                            ecb.AddComponent<IncomingSkillshotTag>(entityInQueryIndex, entity);
                                        }
                                    }
                                }
                            }
                        })
                        .Schedule(writeSkillshotEffectDataJob);
                        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(denormalizeSkillshotEffectDataJob);
                        SubtractTurnTimers(ref gameStateEffectStack.SkillshotEffects);
                        break;
                }

                

                gameStateEffectStack.EffectsExecuted = true;
                effectStack.GameStateEffectStacks[i] = gameStateEffectStack;
                //effectStack.GameStateEffectStacks = effectStack.GameStateEffectStacks;
                m_EffectStackData.SetSingleton(effectStack);
            }
            #endregion
        }

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

                switch(incomingEffect.EffectType)
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
                            ecb.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
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
            ecb.RemoveComponent<InterruptTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        JobHandle applyAttackDataTransformationJob = Entities.WithAll<IncomingAttackTag, AttackTag>().ForEach((Entity entity, int entityInQueryIndex, ref IncomingActionEffects.Component incomingEffects, ref Health.Component health) =>
        {
            var combinedHealth = health.CurrentHealth + health.Armor;

            for (int i = 0; i < incomingEffects.AttackEffects.Count; i++)
            {
                var incomingEffect = incomingEffects.AttackEffects[i];

                switch(incomingEffect.EffectType)
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
                            ecb.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
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
            ecb.RemoveComponent<AttackTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        JobHandle applyMoveDataTransformationJob = Entities.WithAll<IncomingMoveTag, MoveTag>().ForEach((Entity entity, int entityInQueryIndex, ref Position.Component position, ref CubeCoordinate.Component coord, ref Vision.Component vision, ref IncomingActionEffects.Component incomingEffects, ref Health.Component health) =>
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
                            ecb.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
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
                        /*
                        l.HandleLog(LogType.Warning,
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
            ecb.RemoveComponent<MoveTag>(entityInQueryIndex, entity);
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
                            ecb.AddComponent<IsDeadTag>(entityInQueryIndex, entity);
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
            ecb.RemoveComponent<SkillshotTag>(entityInQueryIndex, entity);
        })
        .Schedule(inputDeps);

        #endregion

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applyInterruptDataTransformationJob);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applyAttackDataTransformationJob);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applyMoveDataTransformationJob);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(applySkillshotDataTransformationJob);

        return inputDeps;
    }

    void SubtractTurnTimers(ref List<ActionEffect> actionEffects)
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
    }

    void CompareAndCullInterruptSpawnEffects(ref List<ActionEffect> interruptEffects)
    {
        if (interruptEffects.Count == 0)
            return;

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

        if (indexesToRemove.Count == 0)
            return;

        indexesToRemove.Sort();

        for (int i = indexesToRemove.Count-1; i >= 0; i--)
        {
            interruptEffects.RemoveAt(indexesToRemove[i]);
        }
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
                        var otherPathCount = moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.Count;
                        if (Vector3fext.ToUnityVector(interruptEffects[i].TargetCoordinates[0]) == Vector3fext.ToUnityVector(moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs[otherPathCount - 1].CubeCoordinate))
                        {
                            if (moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.Count != 0)
                                moveEffects[y].MoveAlongPathNested.CoordinatePositionPairs.RemoveAt(otherPathCount - 1);
                        }
                    }
                }
            }
        }
        for (int i = moveEffects.Count; i > 0; i--)
        {
            if (moveEffects[i - 1].EffectType == EffectTypeEnum.move_along_path && moveEffects[i - 1].MoveAlongPathNested.CoordinatePositionPairs.Count == 0)
            {
                moveEffects.RemoveAt(i - 1);
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
            for (int i = moveEffects.Count; i > 0; i--)
            {
                if (moveEffects[i - 1].EffectType == EffectTypeEnum.move_along_path && moveEffects[i - 1].MoveAlongPathNested.CoordinatePositionPairs.Count == 0)
                {
                    moveEffects.RemoveAt(i - 1);
                }
            }
            return moveEffects;
        }
    }
}
