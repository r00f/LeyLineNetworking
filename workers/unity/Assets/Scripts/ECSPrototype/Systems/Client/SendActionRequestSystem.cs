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

//Update after playerState selected unit has been set
[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SendActionRequestSystem : ComponentSystem
{
    HighlightingSystem m_HighlightingSystem;
    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    EntityQuery m_CellData;
    EntityQuery m_UnitData;
    EntityQuery m_ClickedUnitData;
    //EntityQuery m_SelectActionRequestData;
    CommandSystem m_CommandSystem;
    UISystem m_UISystem;
    ILogDispatcher logger;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<PlayerEnergy.Component>(),
        ComponentType.ReadOnly<PlayerState.ComponentAuthority>(),
        ComponentType.ReadWrite<PlayerState.Component>(),
        ComponentType.ReadWrite<PlayerPathing.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<HighlightingDataComponent>()
        );
        m_PlayerData.SetFilter(PlayerState.ComponentAuthority.Authoritative);

        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<MouseState>(),
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<ClientPath.Component>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>()
        );

        m_ClickedUnitData = GetEntityQuery(
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<ClickEvent>()
        );

        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<ClickEvent>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<CellAttributesComponent.Component>(),
        ComponentType.ReadOnly<MarkerState>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
        m_UISystem = World.GetExistingSystem<UISystem>();
    }

    protected override void OnUpdate()
    {
        if (m_PlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() == 0)
            return;


        var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

        var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerEnergys = m_PlayerData.ToComponentDataArray<PlayerEnergy.Component>(Allocator.TempJob);
        var playerHighs = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

        var playerHigh = playerHighs[0];
        var gameState = gameStates[0];
        var playerFaction = playerFactions[0];
        var playerState = playerStates[0];
        var playerEnergy = playerEnergys[0];

        //if the current selected unit wants an unitTarget
        if (gameState.CurrentState == GameStateEnum.planning)
        {
            Entities.With(m_UnitData).ForEach((Entity e, ref MouseState mouseState, ref SpatialEntityId id, ref WorldIndex.Component wIndex, ref ClientPath.Component path, ref Actions.Component actions, ref CubeCoordinate.Component uCoord) =>
            {
                var faction = EntityManager.GetComponentData<FactionComponent.Component>(e);
                var anim = EntityManager.GetComponentObject<AnimatorComponent>(e);
                var unitMouseState = mouseState;
                var unitEntityId = id.EntityId;
                var unitWorldIndex = wIndex.Value;
                var clientPath = path;
                var actionsData = actions;
                var unitCoord = Vector3fext.ToUnityVector(uCoord.CubeCoordinate);

                //set unit action to basic move if is clicked and player has energy
                if (unitMouseState.ClickEvent == 1)
                {
                    if (playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                    {
                        if (faction.Faction == playerFaction.Faction)
                        {
                            if (actionsData.BasicMove.Index != -3)
                            {
                                //Debug.Log("UNIT CLICKEVENT IN SENDACTIONREQSYSTEM");
                                anim.AnimationEvents.VoiceTrigger = true;
                                SelectActionCommand(-2, unitEntityId.Id);
                                m_UISystem.InitializeSelectedActionTooltip(0);
                            }
                        }
                    }
                }

                #region Set Target Command
                //check if unit is the selected unit in playerState and if current selected action is not empty
                if (unitEntityId.Id == playerState.SelectedUnitId && actionsData.CurrentSelected.Index != -3)
                {
                    if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.cell)
                    {
                        Entities.With(m_CellData).ForEach((ref SpatialEntityId cellEntityId, ref CubeCoordinate.Component cCoord, ref MarkerState cellMarkerState) =>
                        {
                            if (Vector3fext.ToUnityVector(playerHigh.HoveredCoordinate) == Vector3fext.ToUnityVector(cCoord.CubeCoordinate))
                            {
                                var request = new Actions.SetTargetCommand.Request
                                (
                                    unitEntityId,
                                    new SetTargetRequest(cellEntityId.EntityId.Id)
                                );
                                anim.AnimationEvents.VoiceTrigger = true;
                                m_CommandSystem.SendCommand(request);
                            }

                            m_HighlightingSystem.ResetHighlights();
                        });
                    }
                    else if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                    {
                        Entities.With(m_ClickedUnitData).ForEach((ref SpatialEntityId targetUnitEntityId) =>
                        {
                            var request = new Actions.SetTargetCommand.Request
                            (
                                unitEntityId,
                                new SetTargetRequest(targetUnitEntityId.EntityId.Id)
                            );

                            anim.AnimationEvents.VoiceTrigger = true;
                            m_CommandSystem.SendCommand(request);
                            m_HighlightingSystem.ResetHighlights();
                        });
                    }
                }

                mouseState = unitMouseState;
                id.EntityId = unitEntityId;
                wIndex.Value = unitWorldIndex;
                path = clientPath;
                actions = actionsData;
                uCoord.CubeCoordinate = Vector3fext.FromUnityVector(unitCoord);
                #endregion
            });
        }

        playerStates[0] = playerState;
        m_PlayerData.CopyFromComponentDataArray(playerStates);
        gameStates.Dispose();
        playerHighs.Dispose();
        playerFactions.Dispose();
        playerStates.Dispose();
        playerEnergys.Dispose();
    }

    public void SelectActionCommand(int actionIndex, long entityId)
    {
        bool isSelfTarget = false;

        var playerPathings = m_PlayerData.ToComponentDataArray<PlayerPathing.Component>(Allocator.TempJob);
        var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
        var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

        var playerPathing = playerPathings[0];
        var playerFaction = playerFactions[0];
        var playerState = playerStates[0];
        var playerHigh = playerHighlightingDatas[0];

        playerState.SelectedActionId = actionIndex;

        Entities.With(m_UnitData).ForEach((Entity e, ref SpatialEntityId idComponent, ref Actions.Component actions) =>
        {
            if (idComponent.EntityId.Id == entityId)
            {
                var request = new Actions.SelectActionCommand.Request
                (
                    idComponent.EntityId,
                    new SelectActionRequest(actionIndex)
                );

                m_CommandSystem.SendCommand(request);

                Action act = actions.NullAction;

                if (actionIndex >= 0)
                {
                    act = actions.OtherActions[actionIndex];
                    if (act.Targets[0].TargetType == TargetTypeEnum.unit)
                    {
                        if (act.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                        {
                            isSelfTarget = true;
                        }
                    }
                }
                else
                {
                    if (actionIndex == -2)
                        act = actions.BasicMove;
                    else if (actionIndex == -1)
                        act = actions.BasicAttack;
                }

                playerState.SelectedAction = act;

                m_HighlightingSystem.ResetHighlights();

                if (actionIndex != -3)
                {
                    if (!isSelfTarget)
                    {
                        /*
                        logger.HandleLog(LogType.Warning,
                        new LogEvent("SetAction")
                        .WithField("unitId", 1f));
                        */
                        playerPathing.CellsInRange.Clear();
                        playerPathing.CachedPaths.Clear();
                        playerPathing.CellsInRange = playerPathing.CellsInRange;
                        playerPathing.CachedPaths = playerPathing.CachedPaths;


                        playerHigh = m_HighlightingSystem.GatherHighlightingInformation(e, actionIndex, playerHigh);
                        playerState = m_HighlightingSystem.FillUnitTargetsList(act, playerHigh, playerState, playerPathing, playerFaction.Faction);
                        m_HighlightingSystem.SetNumberOfTargets(playerState);
                        playerPathing = m_HighlightingSystem.UpdateSelectedUnit(playerState, playerPathing, playerHigh);
                        m_HighlightingSystem.HighlightReachable();
                    }
                    else
                    {
                        playerHigh.TargetRestrictionIndex = 2;
                        m_HighlightingSystem.SetSelfTarget(entityId, (int)act.ActionExecuteStep);
                    }

                    playerState.UnitTargets = playerState.UnitTargets;
                    playerStates[0] = playerState;
                    playerHighlightingDatas[0] = playerHigh;
                    playerPathings[0] = playerPathing;
                    m_PlayerData.CopyFromComponentDataArray(playerPathings);
                    m_PlayerData.CopyFromComponentDataArray(playerStates);
                    m_PlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
                }
            }
        });

        playerHighlightingDatas.Dispose();
        playerStates.Dispose();
        playerPathings.Dispose();
        playerFactions.Dispose();
    }
}
