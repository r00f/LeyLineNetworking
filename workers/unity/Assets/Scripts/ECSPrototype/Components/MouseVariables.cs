using UnityEngine;
using System.Collections;
using Unity.Entities;
using System;

[Serializable]
public struct MouseVariables : IComponentData
{
    public float Distance;
    public float yOffset;
}
