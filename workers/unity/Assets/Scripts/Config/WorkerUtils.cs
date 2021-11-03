using Unity.Entities;
using LeyLineHybridECS;

public static class WorkerUtils
{
    public const string UnityClient = "UnityClient";
    public const string UnityGameLogic = "UnityGameLogic";


    public static void AddClientSystems(World world)
    {
        world.GetOrCreateSystem<InitializeUnitsSystem>();
        world.GetOrCreateSystem<MouseStateSystem>();
        world.GetOrCreateSystem<CellMarkerSystem>();
        world.GetOrCreateSystem<AddComponentsSystem>();
        world.GetOrCreateSystem<PlayerStateSystem>();
        world.GetOrCreateSystem<SendActionRequestSystem>();
        world.GetOrCreateSystem<UnitAnimationSystem>();
        world.GetOrCreateSystem<VisionSystem_Client>();
        world.GetOrCreateSystem<UISystem>();
        world.GetOrCreateSystem<HighlightingSystem>();
        world.GetOrCreateSystem<ActionEffectsSystem>();
        world.GetOrCreateSystem<PathFindingSystem>();
        world.GetOrCreateSystem<ManalithSystemClient>();
    }

    public static void AddGameLogicSystems(World world)
    {
        world.GetOrCreateSystem<InitializeWorldsSystem>();
        world.GetOrCreateSystem<InitializePlayerSystem>();
        world.GetOrCreateSystem<GameStateSystem>();
        world.GetOrCreateSystem<SpawnUnitsSystem>();
        world.GetOrCreateSystem<HandleCellGridRequestsSystem>();
        world.GetOrCreateSystem<MovementSystem>();
        world.GetOrCreateSystem<ManalithSystem>();
        world.GetOrCreateSystem<UnitLifeCycleSystem>();
        world.GetOrCreateSystem<ResourceSystem>();
        world.GetOrCreateSystem<ExecuteActionsSystem>();
        world.GetOrCreateSystem<CleanupSystem>();
        world.GetOrCreateSystem<TimerSystem>();
        world.GetOrCreateSystem<PathFindingSystem>();
        world.GetOrCreateSystem<VisionSystem_Server>();
        world.GetOrCreateSystem<AISystem>();
    }
}
