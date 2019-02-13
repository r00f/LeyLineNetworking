using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Unit;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SendCellGridRequestsSystem : ComponentSystem
{
    public struct CellsInRangeRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<CurrentPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<CellsToMark.CommandSenders.CellsInRangeCommand> CellsInRangeSenders;
        public ComponentDataArray<CellsToMark.CommandSenders.FindAllPathsCommand> FindAllPathsSenders;
    }

    [Inject] private CellsInRangeRequestData m_CellsInRangeRequest;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Cells.CellAttributesComponent.Component> CellAttributes;
    }

    [Inject] private CellData m_CellData;

    public struct CurrentPathData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<CurrentPath.Component> PathData;
    }

    [Inject] private CurrentPathData m_CurrentPathData;

    public struct GameStateData
    {
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;

    public struct LineRendererData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Authoritative<CurrentPath.Component>> AuthorativeData;
        public ComponentDataArray<CurrentPath.Component> Paths;
        public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
        public ComponentArray<LineRendererComponent> LineRenderers;
    }

    [Inject] private LineRendererData m_LineRendererData;

    protected override void OnUpdate()
    {
        if (m_GameStateData.GameState[0].CurrentState != GameStateEnum.planning)
        {
            for (int li = 0; li < m_LineRendererData.Length; li++)
            {
                var lr = m_LineRendererData.LineRenderers[li];

                if (lr.lineRenderer.enabled)
                {
                    lr.lineRenderer.enabled = false;
                }

                if (m_GameStateData.GameState[0].CurrentState != GameStateEnum.calculate_energy)
                {
                    var path = m_LineRendererData.Paths[li];
                    if(path.Path.CellAttributes.Count > 0)
                    {
                        path.Path.CellAttributes.Clear();
                        m_LineRendererData.Paths[li] = path;
                    }

                }
            }
            return;
        }

        for (int i = 0; i < m_CellsInRangeRequest.Length; i++)
        {
            var mouseState = m_CellsInRangeRequest.MouseStateData[i];

            //only request onetime
            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                var cellsToMark = m_CellsInRangeRequest.CellsToMarkData[i];
                var targetEntityId = m_CellsInRangeRequest.EntityIds[i].EntityId;

                if (cellsToMark.CellsInRange.Count == 0)
                {
                    var cellsInRangerequestSender = m_CellsInRangeRequest.CellsInRangeSenders[i];
                    var coord = m_CellsInRangeRequest.CoordinateData[i].CubeCoordinate;

                    var request = CellsToMark.CellsInRangeCommand.CreateRequest
                    (
                        targetEntityId,
                        new CellsInRangeRequest(coord, 5)
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
                        new FindAllPathsRequest(cellsToMark.CellsInRange[0].Cell, 5, cellsToMark.CellsInRange)
                    );

                    findAllPathsRequestSender.RequestsToSend.Add(request2);
                    m_CellsInRangeRequest.FindAllPathsSenders[i] = findAllPathsRequestSender;
                }
            }
        }

        for (int pi = 0; pi < m_CurrentPathData.Length; pi++)
        {
            var mouseState = m_CurrentPathData.MouseStateData[pi];
            var path = m_CurrentPathData.PathData[pi];

            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                for (int ci = 0; ci < m_CellData.Length; ci++)
                {
                    var cellMousestate = m_CellData.MouseStateData[ci];
                    var cellAttributes = m_CellData.CellAttributes[ci];

                    if (cellMousestate.CurrentState == MouseState.State.Hovered)
                    {
                        var cellsToMark = m_CurrentPathData.CellsToMarkData[pi];
                        path.Path = FindPath(cellAttributes.CellAttributes.Cell, cellsToMark.CachedPaths);
                        m_CurrentPathData.PathData[pi] = path;
                    }
                }
            }
        }

        //Update LineRenderers

        for (int li = 0; li < m_LineRendererData.Length; li++)
        {
            var mouseState = m_LineRendererData.MouseStateData[li];
            var lr = m_LineRendererData.LineRenderers[li];

            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                var path = m_LineRendererData.Paths[li];
                var cellsToMark = m_LineRendererData.CellsToMarkData[li];
                //update Linerenderer
                lr.lineRenderer.enabled = true;
                lr.lineRenderer.positionCount = path.Path.CellAttributes.Count + 1;
                lr.lineRenderer.SetPosition(0, lr.transform.position + lr.offset);

                for (int pi = 1; pi <= path.Path.CellAttributes.Count; pi++)
                {
                    lr.lineRenderer.SetPosition(pi, path.Path.CellAttributes[pi - 1].Position.ToUnityVector() + lr.offset);
                }
            }

        }
    }

    public CellAttributeList FindPath(Cells.CellAttribute destination, Dictionary<Cells.CellAttribute, CellAttributeList> cachedPaths)
    {
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new CellAttributeList(new List<Cells.CellAttribute>());
    }

}
