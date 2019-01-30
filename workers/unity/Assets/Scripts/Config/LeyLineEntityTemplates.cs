using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Common;
using Unity.Entities;


public static class LeyLineEntityTemplates {

    
    private static readonly List<string> AllWorkerAttributes =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.SimulatedPlayer };

    public static EntityTemplate Cell(Vector3f position, bool isTaken)
    {
        
        var gameLogic = WorkerUtils.UnityGameLogic;
        //var clientLogic = WorkerUtils.UnityClient;

        var pos = new Position.Snapshot
        {
            Coords = new Coordinates
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z
            }
        };

        var taken = new Cells.IsTaken.Snapshot
        {
            IsTaken = isTaken
        };

        var template = new EntityTemplate();
        template.AddComponent(pos, gameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Cell" }, gameLogic);
        template.AddComponent(new Persistence.Snapshot(), gameLogic);
        template.AddComponent(taken, gameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;

    }
}
