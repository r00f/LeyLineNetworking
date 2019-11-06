﻿using Unity.Entities;
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
            if (unitMouseState.ClickEvent == 1 && playerState.CurrentState != PlayerStateEnum.waiting_for_target)
            {
                playerState.SelectedUnitId = unitEntityId.Id;

                if (actionsData.BasicMove.Index != -3 && faction.Faction == playerFaction.Faction)
                {
                    SelectActionCommand(-2, unitEntityId.Id);
                }
                else
                {
                    //SelectActionCommand(0, unitEntityId.Id);
                }
            }

            #region Set Target Command
            //check if unit is the selected unit in playerState and if current selected action is not empty
            if (unitEntityId.Id == playerState.SelectedUnitId && actionsData.CurrentSelected.Index != -3)
            {
                if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.cell)
                {
                    Vector3 TargetCoord = new Vector3(999, 999, 999);

                    //IF WE WANT CELL AND CLICK UNIT USE UNIT COORD
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

                            m_CommandSystem.SendCommand(request);
                            //setTargetRequest.RequestsToSend.Add(request);
                            //m_SelectActionRequestData.SetTargetSenders[i] = setTargetRequest;
                        }

                        m_HighlightingSystem.ResetHighlights();
                    });
                }
                else if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                {
                    bool sent = false;
                    Entities.With(m_ClickedUnitData).ForEach((ref SpatialEntityId targetUnitEntityId) =>
                    {

                        var request = new Actions.SetTargetCommand.Request
                        (
                            unitEntityId,
                            new SetTargetRequest(targetUnitEntityId.EntityId.Id)
                        );

                        m_CommandSystem.SendCommand(request);
                        sent = true;
                        m_HighlightingSystem.ResetHighlights();
                    });
                    if (!sent)
                    {
                        Entities.With(m_CellData).ForEach((ref SpatialEntityId cellEntityId, ref CubeCoordinate.Component cCoord, ref MarkerState cellMarkerState) =>
                        {

                            var cellCoord = Vector3fext.ToUnityVector(cCoord.CubeCoordinate);


                            if (cellCoord != unitCoord)
                            {
                                var request = new Actions.SetTargetCommand.Request
                                (
                                    unitEntityId,
                                    new SetTargetRequest(cellEntityId.EntityId.Id)
                                );
                                m_CommandSystem.SendCommand(request);
                            }
                            m_HighlightingSystem.ResetHighlights();
                        });
                    }
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
        bool isSelfTarget = false;

        var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

        var playerState = playerStates[0];
        var playerHigh = playerHighlightingDatas[0];

        playerState.SelectedActionId = actionIndex;

        Entities.With(m_UnitData).ForEach((ref SpatialEntityId idComponent, ref Actions.Component actions) =>
        {
            if (idComponent.EntityId.Id == entityId)
            {
                var request = new Actions.SelectActionCommand.Request
                (
                    idComponent.EntityId,
                    new SelectActionRequest(actionIndex)
                );

                m_CommandSystem.SendCommand(request);
                if (actionIndex >= 0)
                {
                    Action act = actions.OtherActions[actionIndex];
                    if (act.Targets[0].TargetType == TargetTypeEnum.unit)
                    {
                        if (act.Targets[0].UnitTargetNested.UnitReq == UnitRequisitesEnum.self)
                        {
                            isSelfTarget = true;
                        }
                    }
                }
                m_HighlightingSystem.ResetHighlights();

                if (!isSelfTarget)
                {
                    m_HighlightingSystem.ClearPlayerState();
                    playerHigh = m_HighlightingSystem.GatherHighlightingInformation(entityId, actionIndex, playerHigh);
                    playerState = m_HighlightingSystem.FillUnitTargetsList(playerHigh, playerState);
                    playerStates[0] = playerState;
                    playerHighlightingDatas[0] = playerHigh;
                    m_PlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
                    m_PlayerData.CopyFromComponentDataArray(playerStates);
                    m_HighlightingSystem.UpdateSelectedUnit();
                }
                else
                {
                    playerHigh.TargetRestrictionIndex = 2;
                    m_HighlightingSystem.SetSelfTarget(entityId);
                }
            }
        });

        playerStates[0] = playerState;
        playerHighlightingDatas[0] = playerHigh;
        m_PlayerData.CopyFromComponentDataArray(playerStates);
        m_PlayerData.CopyFromComponentDataArray(playerHighlightingDatas);
        playerHighlightingDatas.Dispose();
        playerStates.Dispose();

    }
}
