using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Improbable;
using Generic;

public struct HighlightingDataComponent : IComponentData
{
    public uint Range;
    public uint RingRadius;
    public uint AoERadius;
    public byte LineAoE;
    public byte PathingRange;
    public byte IsUnitTarget;
    public byte PathLine;
    public float InputCooldown;
    public Vector3f HoveredCoordinate;
    public Vector3f LastHoveredCoordinate;
    public Vector3 HoveredPosition;
    public uint TargetRestrictionIndex;
    public uint EffectRestrictionIndex;
    public float LineYOffset;
}


