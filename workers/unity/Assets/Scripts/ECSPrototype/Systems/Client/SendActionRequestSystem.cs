using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cells;
using Player;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SendActionRequestSystem : ComponentSystem
{
    public struct SelectActionRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<ClientPath.Component> ClientPathData;
        public readonly ComponentDataArray<ServerPath.Component> ServerPathData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public ComponentDataArray<Actions.CommandSenders.SelectActionCommand> SelectActionSenders;
        public ComponentDataArray<Actions.CommandSenders.SetTargetCommand> SetTargetSenders;
    }

    [Inject] private SelectActionRequestData m_SelectActionRequestData;
    
    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Health.Component> HealthAttributes;
    }

    [Inject] private UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameState;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
    }

    [Inject] private GameStateData m_GameStateData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<PlayerEnergy.Component> PlayerEnergyData;
    }

    [Inject] private PlayerData m_PlayerData;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_SelectActionRequestData.Length; i++)
        {
            var unitMouseState = m_SelectActionRequestData.MouseStateData[i];
            var targetEntityId = m_SelectActionRequestData.EntityIds[i].EntityId;
            var unitWorldIndex = m_SelectActionRequestData.WorldIndexData[i].Value;
            var clientPath = m_SelectActionRequestData.ClientPathData[i];
            var serverPath = m_SelectActionRequestData.ServerPathData[i];
            var setTargetRequest = m_SelectActionRequestData.SetTargetSenders[i];
            var actionsData = m_SelectActionRequestData.ActionsData[i];


            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                if(unitWorldIndex == gameStateWorldIndex)
                {
                    if (m_GameStateData.GameState[gi].CurrentState != GameStateEnum.planning)
                        return;
                }
            }

            //only request onetime maybe check if currentAction is empty
            if (unitMouseState.ClickEvent == 1)
            {
                SelectActionCommand(-2, targetEntityId.Id);
            }

            else if(actionsData.CurrentSelected.Targets.Count != 0)
            {
                if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.cell)
                {
                    for (int ci = 0; ci < m_CellData.Length; ci++)
                    {
                        var cellMousestate = m_CellData.MouseStateData[ci];
                        var cellEntityId = m_CellData.EntityIds[ci].EntityId.Id;

                        if (cellMousestate.ClickEvent == 1)
                        {
                            var request = Actions.SetTargetCommand.CreateRequest
                            (
                                targetEntityId,
                                new SetTargetRequest(cellEntityId)
                            );

                            setTargetRequest.RequestsToSend.Add(request);
                            m_SelectActionRequestData.SetTargetSenders[i] = setTargetRequest;
                        }
                    }
                }
                else if (actionsData.CurrentSelected.Targets[0].TargetType == TargetTypeEnum.unit)
                {
                    for (int ci = 0; ci < m_UnitData.Length; ci++)
                    {
                        var unitMousestate = m_UnitData.MouseStateData[ci];
                        var unitEntityId = m_UnitData.EntityIds[ci].EntityId.Id;

                        if (unitMousestate.ClickEvent == 1)
                        {
                            var request = Actions.SetTargetCommand.CreateRequest
                            (
                                targetEntityId,
                                new SetTargetRequest(unitEntityId)
                            );

                            setTargetRequest.RequestsToSend.Add(request);
                            m_SelectActionRequestData.SetTargetSenders[i] = setTargetRequest;
                        }
                    }
                }
            }
        }
    }

    public void SelectActionCommand(int actionIndex, long entityId)
    {
        //Debug.Log(actionIndex + ", " + entityId);
        for (int i = 0; i < m_SelectActionRequestData.Length; i++)
        {
            var idComp = m_SelectActionRequestData.EntityIds[i].EntityId;
            var selectActionSender = m_SelectActionRequestData.SelectActionSenders[i];

            if (idComp.Id == entityId)
            {
                var request = Actions.SelectActionCommand.CreateRequest
                (
                idComp,
                new SelectActionRequest(actionIndex)
                );

                selectActionSender.RequestsToSend.Add(request);
                m_SelectActionRequestData.SelectActionSenders[i] = selectActionSender;
            }
        }
    }
}
