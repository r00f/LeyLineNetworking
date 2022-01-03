using Unity.Entities;

public struct SimPlayerSkipTime : IComponentData
{
    public float StartPlanningWaitTime;
    public float SkipTurnWaitTime;
}
