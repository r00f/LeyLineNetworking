using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cell;
using Player;
using UnityEngine;
using Improbable.Gdk.ReactiveComponents;
using Improbable;

//Update after playerState selected unit has been set
[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(MouseStateSystem))]
public class SendActionRequestSystem : ComponentSystem
{
    public struct SelectActionRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<ClientPath.Component> ClientPathData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public ComponentDataArray<Actions.CommandSenders.SelectActionCommand> SelectActionSenders;
        public ComponentDataArray<Actions.CommandSenders.SetTargetCommand> SetTargetSenders;
    }

    [Inject] SelectActionRequestData m_SelectActionRequestData;
    
    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates;
        public readonly ComponentDataArray<ClickEvent> ClickEvents;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        public readonly ComponentDataArray<MarkerState> MarkerStateData;
    }

    [Inject] CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<ClickEvent> ClickEvents;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<Health.Component> HealthAttributes;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates;
    }

    [Inject] UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameState;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
    }

    [Inject] GameStateData m_GameStateData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
        public ComponentDataArray<HighlightingDataComponent> HighlightingData;
        public ComponentDataArray<PlayerState.Component> PlayerStateData;
    }

    [Inject] PlayerData m_PlayerData;

    [Inject] HighlightingSystem m_HighlightingSystem;

    protected override void OnUpdate()
    {
        if (m_PlayerData.Length == 0)
            return;

        var playerState = m_PlayerData.PlayerStateData[0];
        var playerEnergy = m_PlayerData.PlayerEnergyData[0];

        for (int i = 0; i < m_SelectActionRequestData.Length; i++)
        {
            var unitMouseState = m_SelectActionRequestData.MouseStateData[i];
            var unitEntityId = m_SelectActionRequestData.EntityIds[i].EntityId;
            var unitWorldIndex = m_SelectActionRequestData.WorldIndexData[i].Value;
            var clientPath = m_SelectActionRequestData.ClientPathData[i];
            var setTargetRequest = m_SelectActionRequestData.SetTargetSenders[i];
            var actionsData = m_SelectActionRequestData.ActionsData[i];
            var unitCoord = m_SelectActionRequestData.Coordinates[i].CubeCoordinate;

            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                if (unitWorldIndex == gameStateWorldIndex)
                {
                    if (m_GameStateData.GameState[gi].CurrentState != GameStateEnum.planning)
                        return;
                }
            }

            //if the current selected unit wants an unitTarget

            //set unit action to basic move if is clicked and player has energy
            if (unitMouseState.ClickEvent == 1 && playerState.CurrentState != PlayerStateEnum.waiting_for_target)
            {
                if(actionsData.BasicMove.Index != -3)
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
                    Vector3f TargetCoord = new Vector3f(999,999,999);

                    if (!actionsData.CurrentSelected.Targets[0].CellTargetNested.RequireEmpty)
                    {
                        for (int ui = 0; ui < m_UnitData.Length; ui++)
                        {
                            var coord = m_UnitData.Coordinates[ui].CubeCoordinate;
                            TargetCoord = coord;
                        }

                    }
                
                    //Now only loops over the clicked cell (ClickEvent component)
                    for (int ci = 0; ci < m_CellData.Length; ci++)
                    {
                        var cellEntityId = m_CellData.EntityIds[ci].EntityId.Id;
                        var cellCoord = m_CellData.Coordinates[ci].CubeCoordinate;
                        var cellMarkerState = m_CellData.MarkerStateData[ci].CurrentState;

                        if (cellCoord != unitCoord || TargetCoord == cellCoord)
                        {
                            var request = new Actions.SetTargetCommand.Request
                            (
                                unitEntityId,
                                new SetTargetRequest(cellEntityId)
                            );
                            setTargetRequest.RequestsToSend.Add(request);
                            m_SelectActionRequestData.SetTargetSenders[i] = setTargetRequest;
                        }

                        m_HighlightingSystem.ResetHighlights();
                    }
                }
                else if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                {
                    bool sent = false;
                    for (int ui = 0; ui < m_UnitData.Length; ui++)
                    {
                        var targetUnitEntityId = m_UnitData.EntityIds[ui].EntityId.Id;

                        var request = new Actions.SetTargetCommand.Request
                        (
                            unitEntityId,
                            new SetTargetRequest(targetUnitEntityId)
                        );

                        sent = true;
                        setTargetRequest.RequestsToSend.Add(request);
                        m_SelectActionRequestData.SetTargetSenders[i] = setTargetRequest;
                        m_HighlightingSystem.ResetHighlights();
                    }
                    if (!sent)
                    {
                        for (int ci = 0; ci < m_CellData.Length; ci++)
                        {
                            var cellEntityId = m_CellData.EntityIds[ci].EntityId.Id;
                            var cellCoord = m_CellData.Coordinates[ci].CubeCoordinate;
                            var cellMarkerState = m_CellData.MarkerStateData[ci].CurrentState;

                            if (cellCoord != unitCoord)
                            {
                                var request = new Actions.SetTargetCommand.Request
                                (
                                    unitEntityId,
                                    new SetTargetRequest(cellEntityId)
                                );
                                setTargetRequest.RequestsToSend.Add(request);
                                m_SelectActionRequestData.SetTargetSenders[i] = setTargetRequest;
                            }
                            m_HighlightingSystem.ResetHighlights();
                        }
                    }
                }
            }
            #endregion
        }
    }

    public void SelectActionCommand(int actionIndex, long entityId)
    {
        UpdateInjectedComponentGroups();
        bool isSelfTarget = false;
        var playerState = m_PlayerData.PlayerStateData[0];
        var highlightingData = m_PlayerData.HighlightingData[0];
        playerState.SelectedActionId = actionIndex;
        m_PlayerData.PlayerStateData[0] = playerState;


        //Debug.Log("SelectActionCommand");

        for (int i = 0; i < m_SelectActionRequestData.Length; i++)
        {
            var actions = m_SelectActionRequestData.ActionsData[i];
            var idCompomnent = m_SelectActionRequestData.EntityIds[i].EntityId;
            var selectActionSender = m_SelectActionRequestData.SelectActionSenders[i];

            if (idCompomnent.Id == entityId)
            {
                var request = new Actions.SelectActionCommand.Request
                (
                idCompomnent,
                new SelectActionRequest(actionIndex)
                );

                if (selectActionSender.RequestsToSend.Count == 0)
                {
                    selectActionSender.RequestsToSend.Add(request);
                }
                m_SelectActionRequestData.SelectActionSenders[i] = selectActionSender;


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
                    m_HighlightingSystem.GatherHighlightingInformation(entityId, actionIndex);
                    m_HighlightingSystem.FillUnitTargetsList(highlightingData);
                    //m_HighlightingSystem.SetNumberOfTargets();
                    m_HighlightingSystem.UpdateSelectedUnit();
                    m_HighlightingSystem.HighlightReachable();
                }
                else
                {
                    highlightingData.TargetRestrictionIndex = 2;
                    m_PlayerData.HighlightingData[0] = highlightingData;
                    m_HighlightingSystem.SetSelfTarget(entityId);
                }
            }
        }
    }
}
