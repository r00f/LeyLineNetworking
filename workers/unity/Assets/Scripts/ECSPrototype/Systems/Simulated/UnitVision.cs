using Generic;
using Unity.Entities;
using Unity.Collections;

public struct UnitVision : IComponentData
{
    public bool RequireUpdate;
    public float InitialWaitTime;
    public uint VisionRange;
    public FixedList512<Vector2i> Vision;
}
