using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Improbable;

public struct HighlightingDataComponent : IComponentData
{
    public uint Range;
    public uint RingRadius;
    public uint AoERadius;
    public byte LineAoE;
    public byte PathingRange;
    public byte IsUnitTarget;
    public byte PathLine;
    public Vector3f HoveredCoordinate;
    public Vector3f LastHoveredCoordinate;
    public Vector3 HoveredPosition;


}


