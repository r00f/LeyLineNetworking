using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class SendCellsInRangeRequestSystem : ComponentSystem
{
    public struct Data
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStateData;
        public readonly ComponentDataArray<Cells.CubeCoordinate.Component> CoordinateData;
        public readonly ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
        public ComponentDataArray<Unit.CellsToMark.CommandSenders.CellsInRangeCommand> CellsInRangeSenders;
    }

    [Inject] private Data data;

    bool requestSent;

    protected override void OnUpdate()
    {
        for (int i = 0; i < data.Length; i++)
        {
            var mouseState = data.MouseStateData[i];

            //only request onetime
            if(mouseState.CurrentState == MouseState.State.Clicked && !requestSent)
            {
                //Debug.Log("SENDREQUEST");
                var requestSender = data.CellsInRangeSenders[i];
                var targetEntityId = data.EntityIds[i].EntityId;
                var coord = data.CoordinateData[i].CubeCoordinate;

                var request = Unit.CellsToMark.CellsInRangeCommand.CreateRequest
                (
                    targetEntityId,
                    new Unit.CellsInRangeRequest(coord, 5)
                );

                // add it to the list of command requests to be sent at the end of the current update loop

                requestSender.RequestsToSend.Add(request);

                data.CellsInRangeSenders[i] = requestSender;

                requestSent = true;
            }
        }
    }
}
