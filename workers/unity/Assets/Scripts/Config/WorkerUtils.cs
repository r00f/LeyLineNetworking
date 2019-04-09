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
        world.GetOrCreateManager<SendCellGridRequestsSystem>();
        world.GetOrCreateManager<ClientMovementSystem>();
        world.GetOrCreateManager<InitializeUnitsSystem>();
        world.GetOrCreateManager<ClientPathVisualsSystem>();
        world.GetOrCreateManager<VisionSystem_Client>();
        world.GetOrCreateManager<UISystem>();
    }

    public static void AddGameLogicSystems(World world)
    {
        world.GetOrCreateManager<GameStateSystem>();
        world.GetOrCreateManager<InitializePlayerSystem>();
        world.GetOrCreateManager<SpawnUnitsSystem>();
        world.GetOrCreateManager<HandleCellGridRequestsSystem>();
        world.GetOrCreateManager<MovementSystem>();
        world.GetOrCreateManager<ManalithSystem>();
        world.GetOrCreateManager<UnitLifeCycleSystem>();
        world.GetOrCreateManager<VisionSystem_Server>();
        world.GetOrCreateManager<ResourceSystem>();
    }

}
