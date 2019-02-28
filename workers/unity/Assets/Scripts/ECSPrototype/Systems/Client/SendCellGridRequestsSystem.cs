using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cells;
using Unity.Mathematics;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SendCellGridRequestsSystem : ComponentSystem
{
    public struct CellsInRangeRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public readonly ComponentDataArray<ClientPath.Component> ClientPathData;
        public ComponentDataArray<CellsToMark.CommandSenders.CellsInRangeCommand> CellsInRangeSenders;
        public ComponentDataArray<CellsToMark.CommandSenders.FindAllPathsCommand> FindAllPathsSenders;
        public ComponentDataArray<ServerPath.CommandSenders.FindPathCommand> FindPathSenders;
    }

    [Inject] private CellsInRangeRequestData m_CellsInRangeRequest;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] private CellData m_CellData;

    public struct GameStateData
    {
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;

    protected override void OnUpdate()
    {
        if (m_GameStateData.GameState[0].CurrentState != GameStateEnum.planning)
            return;

        for (int i = 0; i < m_CellsInRangeRequest.Length; i++)
        {
            var unitMouseState = m_CellsInRangeRequest.MouseStateData[i];
            var targetEntityId = m_CellsInRangeRequest.EntityIds[i].EntityId;

            //only request onetime
            if (unitMouseState.CurrentState == MouseState.State.Clicked)
            {
                var cellsToMark = m_CellsInRangeRequest.CellsToMarkData[i];

                //fill both cellsInRange and CachedPathLists
                if (cellsToMark.CellsInRange.Count == 0)
                {
                    var cellsInRangerequestSender = m_CellsInRangeRequest.CellsInRangeSenders[i];
                    var coord = m_CellsInRangeRequest.CoordinateData[i].CubeCoordinate;

                    var request = CellsToMark.CellsInRangeCommand.CreateRequest
                    (
                        targetEntityId,
                        new CellsInRangeRequest(coord, 3)
                    );

                    cellsInRangerequestSender.RequestsToSend.Add(request);
                    m_CellsInRangeRequest.CellsInRangeSenders[i] = cellsInRangerequestSender;

                }
                else if (cellsToMark.CachedPaths.Count == 0)
                {
                    var findAllPathsRequestSender = m_CellsInRangeRequest.FindAllPathsSenders[i];

                    var request2 = CellsToMark.FindAllPathsCommand.CreateRequest
                    (
                        targetEntityId,
                        new FindAllPathsRequest(cellsToMark.CellsInRange[0].Cell, 3, cellsToMark.CellsInRange)
                    );

                    findAllPathsRequestSender.RequestsToSend.Add(request2);
                    m_CellsInRangeRequest.FindAllPathsSenders[i] = findAllPathsRequestSender;
                }
            }

            else
            {
                for (int ci = 0; ci < m_CellData.Length; ci++)
                {
                    var cellMousestate = m_CellData.MouseStateData[ci];

                    if (cellMousestate.ClickEvent == 1)
                    {
                        var destinationCell = m_CellData.CellAttributes[ci].CellAttributes.Cell;
                        var clientPath = m_CellsInRangeRequest.ClientPathData[i];

                        if (clientPath.Path.CellAttributes.Count != 0)
                        {
                            if (destinationCell.CubeCoordinate == clientPath.Path.CellAttributes[clientPath.Path.CellAttributes.Count - 1].CubeCoordinate)
                            {
                                Debug.Log("ClickEvent");
                                var findPathRequestSender = m_CellsInRangeRequest.FindPathSenders[i];

                                var request3 = ServerPath.FindPathCommand.CreateRequest
                                (
                                    targetEntityId,
                                    new FindPathRequest(destinationCell)
                                );

                                findPathRequestSender.RequestsToSend.Add(request3);
                                m_CellsInRangeRequest.FindPathSenders[i] = findPathRequestSender;
                            }
                        }
                    }
                }
            }
        }
    }
}
