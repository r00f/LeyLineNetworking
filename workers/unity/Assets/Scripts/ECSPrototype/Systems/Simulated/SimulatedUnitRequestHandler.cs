using Unity.Entities;

public struct SimulatedUnitRequestHandler : IComponentData
{
    public bool SelectActionRequestSent;
    public bool LockActionRequestSent;

}
