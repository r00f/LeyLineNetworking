using Generic;
using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;

public struct UnitVision : IComponentData
{
    public bool RequireUpdate;
    public float InitialWaitTime;
    public uint VisionRange;
    public FixedList512<Vector2i> Vision;
}
