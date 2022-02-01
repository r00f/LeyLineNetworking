using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cell;
using Player;
using UnityEngine;
using System.Linq;
using Unity.Jobs;

//Update after playerState selected unit has been set
[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SimulatedActionRequestSystem : JobComponentSystem
{
    public struct ComponentsAddedIdentifierSim : ISystemStateComponentData
    {
    }

    PathFindingSystem m_PathFindingSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
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
        ComponentType.ReadOnly<PlayerState.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<SimPlayerSkipTime>(),
        ComponentType.ReadWrite<SimulatedPlayer.Component>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>(),
        ComponentType.ReadOnly<MapData.Component>(),
        ComponentType.ReadOnly<CurrentMapState>(),
        ComponentType.ReadOnly<SpatialEntityId>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithAll<PlayerState.HasAuthority>().WithNone<SimPlayerSkipTime>().ForEach((Entity e) =>
        {
            EntityManager.AddComponentData(e, new SimPlayerSkipTime());
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<ComponentsAddedIdentifierSim>().ForEach((Entity entity, in MapData.Component mapData) =>
        {
            var curMapState = new CurrentMapState { CoordinateCellDictionary = mapData.CoordinateCellDictionary };
            EntityManager.AddComponentObject(entity, curMapState);
            EntityManager.AddComponentData(entity, new ComponentsAddedIdentifierSim { });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        if (m_PlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() != 1)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var gameStateEntity = m_GameStateData.GetSingletonEntity();

        var currentMapState = EntityManager.GetComponentObject<CurrentMapState>(gameStateEntity);


        var gameStateId = m_GameStateData.GetSingleton<SpatialEntityId>();
        var playerFaction = m_PlayerData.GetSingleton<FactionComponent.Component>();
        var playerEnergy = m_PlayerData.GetSingleton<PlayerEnergy.Component>();
        var simulatedPlayer = m_PlayerData.GetSingleton<SimulatedPlayer.Component>();
        var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
        var simSkipTime = m_PlayerData.GetSingleton<SimPlayerSkipTime>();
        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();



        for (int i = 0; i < cleanUpStateEvents.Count; i++)
        {
            if (cleanUpStateEvents[i].EntityId.Id == gameStateId.EntityId.Id)
            {
                Entities.ForEach((Entity e, ref ClientActionRequest.Component clientActionRequest) =>
                {
                    clientActionRequest.ActionId = -3;
                    clientActionRequest.TargetCoordinate = new Vector3f(0, 0, 0);
                })
                .WithoutBurst()
                .Run();
            }
        }

        if (gameState.CurrentState == GameStateEnum.planning && playerState.CurrentState != PlayerStateEnum.ready )
        {
            if (simSkipTime.StartPlanningWaitTime > 0)
                simSkipTime.StartPlanningWaitTime -= Time.DeltaTime;
            else
            {
                simulatedPlayer.ActionsSelected = true;

                if (simSkipTime.SkipTurnWaitTime > 0f)
                {
                    simSkipTime.SkipTurnWaitTime -= Time.DeltaTime;
                    Entities.ForEach((Entity e, ref ClientActionRequest.Component clientActionRequest, in CubeCoordinate.Component coord, in Energy.Component energy, in Actions.Component actions, in FactionComponent.Component faction, in SpatialEntityId id) =>
                    {
                        if (faction.Faction == playerFaction.Faction && Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) == Vector3.zero && (!energy.Harvesting || EntityManager.HasComponent<Manalith.Component>(e)))
                        {
                            if (clientActionRequest.ActionId == -3)
                            {
                                var rAction = Random.Range(0, actions.ActionsList.Count);

                                if (actions.ActionsList[rAction].Targets[0].EnergyCost <= playerEnergy.Energy)
                                {
                                    clientActionRequest.TargetCoordinate = new Vector3f(0, 0, 0);
                                    clientActionRequest.ActionId = rAction;
                                }
                            }
                            else
                            {
                                var cachedCells = m_PathFindingSystem.GetMapRadius(currentMapState, coord.CubeCoordinate, (uint) actions.ActionsList[clientActionRequest.ActionId].Targets[0].Targettingrange);

                                if (cachedCells.Count > 0)
                                {
                                    if (actions.ActionsList[clientActionRequest.ActionId].Effects[0].EffectType != EffectTypeEnum.move_along_path)
                                    {
                                        var rCell = Random.Range(0, cachedCells.Count);
                                        if (m_PathFindingSystem.ValidateTargetClient(currentMapState, coord.CubeCoordinate, CellGridMethods.AxialToCube(cachedCells[rCell].Key), actions.ActionsList[clientActionRequest.ActionId], id.EntityId.Id, faction.Faction))
                                            clientActionRequest.TargetCoordinate = CellGridMethods.AxialToCube(cachedCells[rCell].Key);
                                    }
                                    else
                                    {
                                        var pathsInRange = m_PathFindingSystem.GetAllPathCoordinatesInRadius(currentMapState, (uint) actions.ActionsList[clientActionRequest.ActionId].Targets[0].Targettingrange, cachedCells);
                                        if (pathsInRange.Keys.Count > 0)
                                        {
                                            var rCell = Random.Range(0, pathsInRange.Count);
                                            clientActionRequest.TargetCoordinate = pathsInRange.ElementAt(rCell).Key;
                                        }
                                    }
                                }
                            }
                            simulatedPlayer.ActionsSelected = false;
                        }
                    })
                    .WithoutBurst()
                    .Run();
                    //if player has little to no energy left, pass the turn
                    if (playerEnergy.Energy < 20)
                        simulatedPlayer.ActionsSelected = true;
                }
                else
                    simulatedPlayer.ActionsSelected = true;

                m_PlayerData.SetSingleton(simulatedPlayer);
            }
        }
        else
        {
            simSkipTime.StartPlanningWaitTime = 10f;
            simSkipTime.SkipTurnWaitTime = 5f;
        }

        m_PlayerData.SetSingleton(simSkipTime);
        return inputDeps;
    }
}
