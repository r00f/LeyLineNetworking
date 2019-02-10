using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SendCellGridRequestsSystem : ComponentSystem
{
    public struct CellsInRangeRequestData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<Unit.CellsToMark.CommandSenders.CellsInRangeCommand> CellsInRangeSenders;
        public ComponentDataArray<Unit.CellsToMark.CommandSenders.FindAllPathsCommand> FindAllPathsSenders;
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
        public readonly ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<Unit.CurrentPath.Component> PathData;
    }

    [Inject] private CurrentPathData m_CurrentPathData;

    public struct LineRendererData
    {
        public readonly int Length;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Unit.CurrentPath.Component> Paths;
        public readonly ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
        public ComponentArray<LineRendererComponent> LineRenderers;
    }

    [Inject] private LineRendererData m_LineRendererData;

    protected override void OnUpdate()
    {
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

                    var request = Unit.CellsToMark.CellsInRangeCommand.CreateRequest
                    (
                        targetEntityId,
                        new Unit.CellsInRangeRequest(coord, 5)
                    );

                    cellsInRangerequestSender.RequestsToSend.Add(request);
                    m_CellsInRangeRequest.CellsInRangeSenders[i] = cellsInRangerequestSender;

                }
                else if (cellsToMark.CachedPaths.Count == 0)
                {
                    var findAllPathsRequestSender = m_CellsInRangeRequest.FindAllPathsSenders[i];

                    var request2 = Unit.CellsToMark.FindAllPathsCommand.CreateRequest
                    (
                        targetEntityId,
                        new Unit.FindAllPathsRequest(cellsToMark.CellsInRange[0].Cell, 5, cellsToMark.CellsInRange, cellsToMark.CachedPaths)
                    );

                    findAllPathsRequestSender.RequestsToSend.Add(request2);
                    m_CellsInRangeRequest.FindAllPathsSenders[i] = findAllPathsRequestSender;
                }
            }
        }

        for (int pi = 0; pi < m_CurrentPathData.Length; pi++)
        {
            var mouseState = m_CurrentPathData.MouseStateData[pi];

            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                for (int ci = 0; ci < m_CellData.Length; ci++)
                {
                    var cellMousestate = m_CellData.MouseStateData[ci];
                    var cellAttributes = m_CellData.CellAttributes[ci];

                    if (cellMousestate.CurrentState == MouseState.State.Hovered)
                    {
                        //Debug.Log("Update currentPath");
                        var cellsToMark = m_CurrentPathData.CellsToMarkData[pi];
                        var path = m_CurrentPathData.PathData[pi];
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

            if(mouseState.CurrentState == MouseState.State.Clicked)
            {
                var path = m_LineRendererData.Paths[li];
                var lr = m_LineRendererData.LineRenderers[li];
                var cellsToMark = m_LineRendererData.CellsToMarkData[li];
                //update Linerenderer

                lr.lineRenderer.positionCount = path.Path.CellAttributes.Count + 1;
                lr.lineRenderer.SetPosition(0, lr.transform.position + lr.offset);

                for (int pi = 1; pi <= path.Path.CellAttributes.Count; pi++)
                {
                    lr.lineRenderer.SetPosition(pi, path.Path.CellAttributes[pi - 1].Position.ToUnityVector() + lr.offset);
                }

                //clear currentpath and LineRenderer
                //m_UnitData.PathListsData[i].CurrentPath.Clear();
                //m_UnitData.PathListsData[i].LineRenderer.positionCount = 0;
            }

        }
        
    }

    public Unit.CellAttributeList FindPath(Cells.CellAttribute destination, Dictionary<Cells.CellAttribute, Unit.CellAttributeList> cachedPaths)
    {
        if (cachedPaths.ContainsKey(destination))
        {
            return cachedPaths[destination];
        }
        else
            return new Unit.CellAttributeList(new List<Cells.CellAttribute>());
    }

}
