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
public class SendActionRequestSystem : JobComponentSystem
{
    HighlightingSystem m_HighlightingSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    //EntityQuery m_CellData;
    EntityQuery m_UnitData;
    //EntityQuery m_ClickedUnitData;
    EntityQuery m_RightClickedUnitData;
    //EntityQuery m_SelectActionRequestData;
    CommandSystem m_CommandSystem;
    UISystem m_UISystem;
    EventSystem eventSystem;
    ILogDispatcher logger;

    protected override void OnCreate()
    {
        base.OnCreate();

        eventSystem = Object.FindObjectOfType<EventSystem>();

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<PlayerEnergy.Component>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>(),
        ComponentType.ReadWrite<PlayerState.Component>(),
        ComponentType.ReadWrite<PlayerPathing.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<HighlightingDataComponent>()
        );

        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<MouseState>(),
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>()
        );

        m_RightClickedUnitData = GetEntityQuery(
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<RightClickEvent>(),
        ComponentType.ReadWrite<AnimatorComponent>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>(),
        ComponentType.ReadOnly<MapData.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();


        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
        m_UISystem = World.GetExistingSystem<UISystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() != 1)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var playerHigh = m_PlayerData.GetSingleton<HighlightingDataComponent>();
        var playerFaction = m_PlayerData.GetSingleton<FactionComponent.Component>();
        var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
        var playerEnergy = m_PlayerData.GetSingleton<PlayerEnergy.Component>();
        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

        if (cleanUpStateEvents.Count > 0)
        {
            Entities.ForEach((ref ClientActionRequest.Component clientActionRequest) =>
            {
                clientActionRequest.TargetCoordinate = new Vector3f(0, 0, 0);
                clientActionRequest.ActionId = -3;
            })
            .WithoutBurst()
            .Run();
        }

        //if the current selected unit wants an unitTarget
        if (gameState.CurrentState == GameStateEnum.planning)
        {
            //Clear right Clicked unit actions
            Entities.ForEach((Entity e, AnimatorComponent anim, ref SpatialEntityId unitId, ref RightClickEvent rightClickEvent, in FactionComponent.Component faction, in ClientActionRequest.Component clientActionRequest) =>
            {
                if (faction.Faction == playerFaction.Faction)
                {
                    if (Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) != Vector3.zero)
                    {
                        if (anim.AnimationEvents)
                            anim.AnimationEvents.VoiceTrigger = true;
                        SelectActionCommand(-3, unitId.EntityId.Id);
                    }
                }
            })
            .WithoutBurst()
            .Run();

            Entities.ForEach((Entity e, AnimatorComponent anim, ref ClientActionRequest.Component clientActionRequest, in MouseState mouseState, in SpatialEntityId id, in CubeCoordinate.Component uCoord, in FactionComponent.Component faction) =>
            {
                //set unit action to basic move if is clicked and player has energy
                if (mouseState.ClickEvent == 1)
                {
                    if (playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                    {
                        if (faction.Faction == playerFaction.Faction)
                        {
                            if (anim.AnimationEvents)
                                anim.AnimationEvents.VoiceTrigger = true;
                        }
                    }
                }

                #region Set Target Command
                //check if unit is the selected unit in playerState and if current selected action is not empty
                if (id.EntityId.Id == playerState.SelectedUnitId && clientActionRequest.ActionId != -3 && Input.GetButtonUp("Fire1") && !eventSystem.IsPointerOverGameObject())
                {
                    clientActionRequest.TargetCoordinate = playerHigh.HoveredCoordinate;

                    if (anim.AnimationEvents)
                        anim.AnimationEvents.VoiceTrigger = true;

                    m_HighlightingSystem.ResetHighlights(ref playerState, playerHigh);
                }
                #endregion
            })
            .WithoutBurst()
            .Run();
        }

        m_PlayerData.SetSingleton(playerState);

        return inputDeps;
    }

    public void RevealPlayerVision()
    {
        Entities.ForEach((ref SpatialEntityId playerId,  ref Vision.Component playerVision, ref PlayerState.HasAuthority auth) =>
        {
            var request = new Vision.RevealVisionCommand.Request
            (
                playerId.EntityId,
                new RevealVisionRequest()
            );

            m_CommandSystem.SendCommand(request);
        })
        .WithoutBurst()
        .Run();
    }

    public void SelectActionCommand(int actionIndex, long entityId)
    {
        //Debug.Log("SelectActionCommand with index: " + actionIndex + " from entity with id: " + entityId);
        bool isSelfTarget = false;

        var gameStateEntity = m_GameStateData.GetSingletonEntity();
        var mapData = EntityManager.GetComponentObject<CurrentMapState>(gameStateEntity);

        var playerPathing = m_PlayerData.GetSingleton<PlayerPathing.Component>();
        var playerFaction = m_PlayerData.GetSingleton<FactionComponent.Component>();
        var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
        var playerHigh = m_PlayerData.GetSingleton<HighlightingDataComponent>();

        playerState.SelectedActionId = actionIndex;

        var lastSelectedActionTargets = new List<Vector3f>();

        if (playerState.UnitTargets.Keys.Contains(entityId))
        {
            lastSelectedActionTargets.AddRange(playerState.UnitTargets[entityId].CubeCoordinates.Keys);
        }

        m_HighlightingSystem.ResetHighlights(ref playerState, playerHigh);
        m_HighlightingSystem.ResetMarkerNumberOfTargets(lastSelectedActionTargets);

        Entities.ForEach((Entity e, UnitComponentReferences unitCompRef, ref ClientActionRequest.Component clientActionRequest, in SpatialEntityId idComponent, in Actions.Component actions, in CubeCoordinate.Component coord) =>
        {
            if (idComponent.EntityId.Id == entityId)
            {
                clientActionRequest.TargetCoordinate = new Vector3f(0, 0, 0);
                clientActionRequest.ActionId = actionIndex;
                Action act = actions.NullAction;

                if (actionIndex >= 0)
                {
                    act = actions.ActionsList[actionIndex];
                    if (act.Targets[0].TargetType == TargetTypeEnum.unit)
                    {
                        if (act.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                        {
                            isSelfTarget = true;
                        }
                    }
                }

                playerState.SelectedAction = act;

                if (actionIndex != -3)
                {
                    if (!isSelfTarget)
                    {
                        playerPathing.CoordinatesInRange.Clear();
                        playerPathing.CellsInRange.Clear();
                        playerPathing.CachedMapPaths.Clear();
                        playerPathing.CoordinatesInRange = playerPathing.CoordinatesInRange;
                        playerPathing.CellsInRange = playerPathing.CellsInRange;
                        playerPathing.CachedMapPaths = playerPathing.CachedMapPaths;
                        playerHigh = m_HighlightingSystem.GatherHighlightingInformation(e, actionIndex, playerHigh);
                        playerState = m_HighlightingSystem.FillUnitTargetsList(mapData, act, playerHigh, playerState, playerPathing, playerFaction.Faction);
                        playerPathing = m_HighlightingSystem.UpdateSelectedUnit(playerState, playerPathing, playerHigh, mapData);
                        m_HighlightingSystem.HighlightReachable(ref playerState, ref playerPathing);
                    }
                    else
                    {
                        if (unitCompRef.AnimatorComp.AnimationEvents)
                            unitCompRef.AnimatorComp.AnimationEvents.VoiceTrigger = true;
                        playerHigh.TargetRestrictionIndex = 2;
                        m_HighlightingSystem.SetSelfTarget(entityId, act, coord.CubeCoordinate, unitCompRef.LinerendererComp);
                        m_UISystem.DeactivateActionDisplay(e, .3f);
                    }

                    m_UISystem.InitializeUnitOverHeadActionDisplay(actionIndex, unitCompRef.BaseDataSetComp, unitCompRef.HeadUIRef);
                }
            }
        })
        .WithoutBurst()
        .Run();

        //
        playerHigh.ResetHighlightsBuffer = .05f;
        playerState.UnitTargets = playerState.UnitTargets;

        m_PlayerData.SetSingleton(playerPathing);
        m_PlayerData.SetSingleton(playerState);
        m_PlayerData.SetSingleton(playerHigh);


        /*
        playerState.UnitTargets = playerState.UnitTargets;
        playerStates[0] = playerState;
        playerHighlightingDatas[0] = playerHigh;
        playerPathings[0] = playerPathing;

        m_PlayerData.CopyFromComponentDataArray(playerPathings);
        m_PlayerData.CopyFromComponentDataArray(playerStates);
        m_PlayerData.CopyFromComponentDataArray(playerHighlightingDatas);

        playerHighlightingDatas.Dispose();
        playerStates.Dispose();
        playerPathings.Dispose();
        playerFactions.Dispose();
        */
    }
}
