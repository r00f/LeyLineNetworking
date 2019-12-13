using UnityEngine;
using System.Collections;
using System;
using Unity.Entities;

[Serializable]
public struct MovementData : IComponentData
{
    public int Range;
    public float Speed;
    public int PathIndex;
}

public class MovementDataComponent : ComponentDataProxy<MovementData> { }

