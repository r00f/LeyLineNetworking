using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Gdk.GameObjectCreation;
using System.Collections.Generic;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using LeyLineHybridECS;

public static class WorkerUtils
{
    public const string UnityClient = "UnityClient";
    public const string UnityGameLogic = "UnityGameLogic";

    public static void AddClientSystems(World world)
    {
        world.GetOrCreateManager<MouseStateSystem>();
        world.GetOrCreateManager<CellStateSystem>();
        world.GetOrCreateManager<CellMarkerSystem>();
        world.GetOrCreateManager<AddComponentsSystem>();
        world.GetOrCreateManager<PlayerStateSystem>();
        world.GetOrCreateManager<SendCellsInRangeRequestSystem>();
        //world.GetOrCreateManager<HandleCellsInRangeResponseSystem>();
    }

    public static void AddGameLogicSystems(World world)
    {
        world.GetOrCreateManager<SpawnUnitsSystem>();
        world.GetOrCreateManager<SetPlayerFactionSystem>();
        world.GetOrCreateManager<SetGameStateSystem>();
        world.GetOrCreateManager<HandleCellsInRangeRequestSystem>();
    }

}
