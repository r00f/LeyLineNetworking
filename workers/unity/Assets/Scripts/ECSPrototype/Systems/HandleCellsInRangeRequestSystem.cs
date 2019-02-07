using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class HandleCellsInRangeRequestSystem : ComponentSystem
{
    public struct RequestData
    {
        public readonly int Length;
        public ComponentDataArray<Unit.CellsToMark.CommandRequests.CellsInRangeCommand> ReceivedCellsInRangeRequests;
        public ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
        //public ComponentDataArray<Unit.CellsToMark.CommandResponders.CellsInRangeCommand> CommandResponders;
    }

    [Inject] private RequestData m_RequestData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Cells.CubeCoordinate.Component> CoordinateData;
    }

    [Inject] private CellData m_CellData;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_RequestData.Length; i++)
        {
            var cellsToMarkData = m_RequestData.CellsToMarkData[i];
            var cellsInRangeRequests = m_RequestData.ReceivedCellsInRangeRequests[i];

            foreach (var cellsInRangeRequest in cellsInRangeRequests.Requests)
            {
                cellsToMarkData.CellsInRange = GetRadius(cellsInRangeRequest.Payload.Origin, cellsInRangeRequest.Payload.Range);
                m_RequestData.CellsToMarkData[i] = cellsToMarkData;
            }
        }
    }

    public int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }


    public List<Vector3f> GetRadius(Vector3f originCellCubeCoordinate, uint radius)
    {
        //returns a list of offsetCoordinates
        var coordsInRadius = new List<Vector3f>();

        for (int i = 0; i < m_CellData.Length; i++)
        {
            Vector3f cubeCoordinate = m_CellData.CoordinateData[i].CubeCoordinate;

            if (GetDistance(originCellCubeCoordinate, cubeCoordinate) < radius)
                coordsInRadius.Add(m_CellData.CoordinateData[i].CubeCoordinate);
        }

        return coordsInRadius;
    }
}
