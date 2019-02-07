using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using Improbable.Worker.CInterop;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class HandleCellsInRangeResponseSystem : ComponentSystem
{

    public struct Data
    {
        public readonly int Length;
        public ComponentDataArray<Unit.CellsToMark.CommandResponses.CellsInRangeCommand> ReceivedCellsInRangeResponses;
        //public ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
    }

    [Inject] private Data data;

    protected override void OnUpdate()
    {
        for (int i = 0; i < data.Length; i++)
        {
            Debug.Log("HANDLERESPONSE");
            //var cellsInRange = data.CellsToMarkData[i];
            var responses = data.ReceivedCellsInRangeResponses[i];

            foreach (var response in responses.Responses)
            {
                if (response.StatusCode != StatusCode.Success)
                {
                    Debug.Log("COMMANDFAILURE");
                    // Handle command failure
                    continue;
                }
                var responsePayload = response.ResponsePayload;
                var requestPayload = response.RequestPayload;

                // Handle CellsInRange response
                //cellsInRange.CellsInRange = responsePayload.Value.CellsInRange;
                Debug.Log(responsePayload.Value.CellsInRange.Count);
                //data.CellsToMarkData[i] = cellsInRange;
            } 
        }
    }
}
