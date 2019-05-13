using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public struct HighlightingDataComponent : IComponentData
{
    public uint Range;
    public uint AoERadius;
    public byte PathingRange;
    public byte IsUnitTarget;
    public byte PathLine;

}


