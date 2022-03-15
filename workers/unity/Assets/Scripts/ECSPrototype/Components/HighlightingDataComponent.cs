using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Improbable;
using Generic;
using Unity.Collections;
using Player;

public struct HighlightingDataComponent : IComponentData
{
    public uint Range;
    public uint RingRadius;
    public uint ConeRadius;
    public uint ConeExtent;
    public uint AoERadius;
    public byte LineAoE;
    public byte PathingRange;
    public byte IsUnitTarget;
    public byte PathLine;
    public bool ShowIngameUI;
    public float InputCooldown;
    public bool CancelState;
    public float CancelTime;
    public Vector3f HoveredCoordinate;
    public Vector3f LastHoveredCoordinate;
    public Vector3 HoveredPosition;
    public uint TargetRestrictionIndex;
    public uint EffectRestrictionIndex;
    public float LineYOffset;
    public int TurnStepIndex;
    public CubeCoordList CurrentUnitTargets;
    //public int SelectActionBuffer;
    public float ResetHighlightsBuffer;
}

public struct CubeCoordList
{
    public FixedList128<Vector3f> CubeCoordinates;
    public int TurnStepIndex;
    public uint DamageAmount;
    public uint ArmorAmount;
    public bool IsUnitTarget;


    public CubeCoordList(FixedList128<Vector3f> cubeCoordinates, int turnStepIndex, uint damageAmount, uint armorAmount, bool isUnitTarget)
    {
        CubeCoordinates = cubeCoordinates;
        TurnStepIndex = turnStepIndex;
        DamageAmount = damageAmount;
        ArmorAmount = armorAmount;
        IsUnitTarget = isUnitTarget;
    }
}


