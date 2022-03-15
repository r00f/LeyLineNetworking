using Unity.Entities;
using LeyLineHybridECS;
using Unity.Rendering;

public static class WorkerUtils
{
    public const string UnityClient = "UnityClient";
    public const string UnityGameLogic = "UnityGameLogic";
    public const string MapSpawn = "MapSpawn";
    public const string SimulatedPlayerCoordinator = "SimulatedPlayerCoordinator";

    public static void AddClientSystems(World world)
    {
        //world.GetOrCreateSystem<MatrixPreviousSystem>();
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

        world.GetOrCreateSystem<HybridRendererSystem>();
        world.GetOrCreateSystem<InitializeUnitsSystem>();
        world.GetOrCreateSystem<UnitLifeCycleSystemClient>();
        world.GetOrCreateSystem<MouseStateSystem>();
        world.GetOrCreateSystem<CellMarkerSystem>();
        world.GetOrCreateSystem<AddComponentsSystem>();
        world.GetOrCreateSystem<PlayerStateSystem>();
        world.GetOrCreateSystem<SendActionRequestSystem>();
        world.GetOrCreateSystem<UnitAnimationSystem>();
        world.GetOrCreateSystem<VisionSystemClient>();
        world.GetOrCreateSystem<HighlightingSystem>();
        world.GetOrCreateSystem<ActionEffectsSystem>();
        world.GetOrCreateSystem<PathFindingSystem>();
        world.GetOrCreateSystem<ManalithSystemClient>();
        world.GetOrCreateSystem<UISystem>();
        world.GetOrCreateSystem<ActionPreviewSystem>();
    }

    public static void AddGameLogicSystems(World world)
    {
        world.GetOrCreateSystem<GameStateSystem>();
        world.GetOrCreateSystem<HandleCellGridRequestsSystem>();
        world.GetOrCreateSystem<ManalithSystem>();
        world.GetOrCreateSystem<UnitLifeCycleSystemServer>();
        world.GetOrCreateSystem<ResourceSystem>();
        world.GetOrCreateSystem<ExecuteActionsSystem>();
        world.GetOrCreateSystem<CleanupSystem>();
        world.GetOrCreateSystem<PathFindingSystem>();
        world.GetOrCreateSystem<VisionSystemServer>();
        world.GetOrCreateSystem<AISystem>();
    }

    public static void AddMapSpawnSystems(World world)
    {
        world.GetOrCreateSystem<InitializePlayerSystem>();
        world.GetOrCreateSystem<InitializeWorldSystem>();
    }

    public static void AddSimulatedPlayerSystems(World world)
    {
        world.GetOrCreateSystem<SimulatedPlayerSystem>();
        world.GetOrCreateSystem<SimulatedActionRequestSystem>();
        world.GetOrCreateSystem<PathFindingSystem>();
        world.GetOrCreateSystem<UnitLifeCycleSystemClient>();
    }
}
