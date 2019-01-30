using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Gdk.GameObjectCreation;
using System.Collections.Generic;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;

public static class WorkerUtils
{
    public const string UnityClient = "UnityClient";
    public const string UnityGameLogic = "UnityGameLogic";

    public const string SimulatedPlayerCoorindator = "SimulatedPlayerCoordinator";
    public const string SimulatedPlayer = "SimulatedPlayer";

    public static void AddClientSystems(World world)
    {
        world.GetOrCreateManager<MouseStateSystem>();
        world.GetOrCreateManager<AddComponentsSystem>();
    }

    public static void AddGameLogicSystems(World world)
    {

    }

}
