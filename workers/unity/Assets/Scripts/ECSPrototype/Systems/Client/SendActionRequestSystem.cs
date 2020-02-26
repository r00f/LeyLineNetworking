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

    protected override void OnCreate()
    {
        base.OnCreate();

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<PlayerEnergy.Component>(),
        ComponentType.ReadOnly<PlayerState.ComponentAuthority>(),
        ComponentType.ReadWrite<PlayerState.Component>(),
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
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();
        m_UISystem = World.GetExistingSystem<UISystem>();
    }

    protected override void OnUpdate()
    {
        if (m_PlayerData.CalculateEntityCount() == 0)
            return;

        var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerEnergys = m_PlayerData.ToComponentDataArray<PlayerEnergy.Component>(Allocator.TempJob);

        var playerFaction = playerFactions[0];
        var playerState = playerStates[0];
        var playerEnergy = playerEnergys[0];

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

            Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
            {
                if (unitWorldIndex == gameStateWorldIndex.Value)
                {
                    if (gameState.CurrentState != GameStateEnum.planning)
                        return;
                }
            });

            //if the current selected unit wants an unitTarget

            //set unit action to basic move if is clicked and player has energy
            if (unitMouseState.ClickEvent == 1)
            {
                if(playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                {
                    playerState.SelectedUnitId = unitEntityId.Id;

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
                else if(!playerState.TargetValid)
                {
                    SelectActionCommand(-3, unitEntityId.Id);
                }
            }

            #region Set Target Command
            //check if unit is the selected unit in playerState and if current selected action is not empty
            if (unitEntityId.Id == playerState.SelectedUnitId && actionsData.CurrentSelected.Index != -3)
            {
                if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.cell)
                {
                    Vector3 TargetCoord = new Vector3(999, 999, 999);

                    if (!actionsData.CurrentSelected.Targets[0].CellTargetNested.RequireEmpty)
                    {
                        Entities.With(m_ClickedUnitData).ForEach((ref CubeCoordinate.Component clickedUnitCoord) =>
                        {
                            var coord = Vector3fext.ToUnityVector(clickedUnitCoord.CubeCoordinate);
                            TargetCoord = coord;
                        });
                    }

                    //Now only loops over the clicked cell (ClickEvent component)
                    Entities.With(m_CellData).ForEach((ref SpatialEntityId cellEntityId, ref CubeCoordinate.Component cCoord, ref MarkerState cellMarkerState) =>
                    {
                        var cellCoord = Vector3fext.ToUnityVector(cCoord.CubeCoordinate);

                        if (cellCoord != unitCoord || TargetCoord == cellCoord)
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
            actions =  actionsData;
            uCoord.CubeCoordinate = Vector3fext.FromUnityVector(unitCoord);
            #endregion
        });

        playerStates[0] = playerState;
        m_PlayerData.CopyFromComponentDataArray(playerStates);
        playerFactions.Dispose();
        playerStates.Dispose();
        playerEnergys.Dispose();
    }

    public void SelectActionCommand(int actionIndex, long entityId)
    {

        //Debug.Log("SelectActionCommand");
        bool isSelfTarget = false;

        var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
        var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

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
                //playerHigh.LastHoveredCoordinate = new Vector3f(999f, 999f, 999f);

                if (actionIndex != -3)
                {
                    if (!isSelfTarget)
                    {
                        m_HighlightingSystem.ClearPlayerState();
                        playerHigh = m_HighlightingSystem.GatherHighlightingInformation(e, actionIndex, playerHigh);
                        playerState = m_HighlightingSystem.FillUnitTargetsList(act, playerHigh, playerState, playerFaction.Faction);
                        playerState.TargetValid = false;
                        m_HighlightingSystem.UpdateSelectedUnit(ref playerState, playerHigh);
                        m_HighlightingSystem.SetNumberOfTargets(playerState);
                        playerStates[0] = playerState;
                        playerHighlightingDatas[0] = playerHigh;
                        m_PlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
                        m_PlayerData.CopyFromComponentDataArray(playerStates);
                    }
                    else
                    {
                        playerHigh.TargetRestrictionIndex = 2;
                        m_HighlightingSystem.SetSelfTarget(entityId);
                    }
                }

            }
        });

        playerStates[0] = playerState;
        playerHighlightingDatas[0] = playerHigh;
        m_PlayerData.CopyFromComponentDataArray(playerStates);
        m_PlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
        playerHighlightingDatas.Dispose();
        playerStates.Dispose();
        playerFactions.Dispose();
    }
}
