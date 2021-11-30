using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cell;
using Player;
using UnityEngine;
using Improbable;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine.EventSystems;

//Update after playerState selected unit has been set
[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SimulatedActionRequestSystem : JobComponentSystem
{
    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    CommandSystem m_CommandSystem;
    ILogDispatcher logger;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<PlayerEnergy.Component>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>(),
        ComponentType.ReadWrite<PlayerState.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<SimulatedPlayer.Component>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() != 1)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var playerFaction = m_PlayerData.GetSingleton<FactionComponent.Component>();
        var playerEnergy = m_PlayerData.GetSingleton<PlayerEnergy.Component>();
        var simulatedPlayer = m_PlayerData.GetSingleton<SimulatedPlayer.Component>();

        Entities.WithAll<Actions.Component>().WithNone<SimulatedUnitRequestHandler, Manalith.Component, AiUnit.Component>().ForEach((Entity e) =>
        {
            EntityManager.AddComponentData(e, new SimulatedUnitRequestHandler());
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        if (gameState.CurrentState == GameStateEnum.planning)
        {
            bool allUnitsActionLockeReqSent = true;

            Entities.ForEach((Entity e, ref SimulatedUnitRequestHandler requestHandler, in Energy.Component energy, in Actions.Component actions, in CellsToMark.Component cellsToMark, in FactionComponent.Component faction, in SpatialEntityId id) =>
            {
                if (faction.Faction == playerFaction.Faction && !energy.Harvesting && !requestHandler.LockActionRequestSent)
                {
                    if (actions.CurrentSelected.Index == -3)
                    {
                        if (!requestHandler.SelectActionRequestSent)
                        {
                            var rAction = Random.Range(0, actions.ActionsList.Count);

                            if (actions.ActionsList[rAction].Targets[0].EnergyCost <= playerEnergy.Energy)
                            {
                                var request = new Actions.SelectActionCommand.Request
                                (
                                    id.EntityId,
                                    new SelectActionRequest(rAction)
                                );
                                m_CommandSystem.SendCommand(request);
                                requestHandler.SelectActionRequestSent = true;
                            }
                        }
                    }
                    else
                    {
                        if (actions.CurrentSelected.Effects[0].EffectType != EffectTypeEnum.move_along_path)
                        {
                            if (cellsToMark.CellsInRange.Count > 0)
                            {
                                var rCell = Random.Range(0, cellsToMark.CellsInRange.Count);
                                var request = new Actions.SetTargetCommand.Request
                                (
                                    id.EntityId,
                                    new SetTargetRequest(cellsToMark.CellsInRange[rCell].Cell.CubeCoordinate)
                                );
                                m_CommandSystem.SendCommand(request);
                            }
                        }
                        else
                        {
                            if (cellsToMark.CachedPaths.Count > 0)
                            {
                                var rCell = Random.Range(0, cellsToMark.CachedPaths.Count);
                                var request = new Actions.SetTargetCommand.Request
                                (
                                    id.EntityId,
                                    new SetTargetRequest(cellsToMark.CachedPaths.ElementAt(rCell).Key.CubeCoordinate)
                                );
                                m_CommandSystem.SendCommand(request);
                            }
                        }
                        requestHandler.LockActionRequestSent = true;
                    }
                    allUnitsActionLockeReqSent = false;
                }
            })
            .WithoutBurst()
            .Run();

            //if player has little to no energy left, pass the turn
            if (playerEnergy.Energy < 20)
                allUnitsActionLockeReqSent = true;

            simulatedPlayer.ActionsSelected = allUnitsActionLockeReqSent;
            m_PlayerData.SetSingleton(simulatedPlayer);
        }
        else
        {
            Entities.ForEach((Entity e, ref SimulatedUnitRequestHandler requestHandler) =>
            {
                requestHandler.LockActionRequestSent = false;
                requestHandler.SelectActionRequestSent = false;
            })
            .WithoutBurst()
            .Run();
        }
        return inputDeps;
    }
}
