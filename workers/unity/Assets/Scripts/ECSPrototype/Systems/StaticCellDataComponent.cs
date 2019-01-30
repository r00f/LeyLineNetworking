using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System;

[Serializable]
public struct StaticCellData : IComponentData
{
    //public ComponentDataArray<StaticCellData> Neighbours;
    //public fixed StaticCellData Neighbours;
    public bool IsCircleCell;
    public float2 CellDimensions;
    public float2 OffsetCoordinate;
    public float3 CubeCoordinate;
    public float3 Position3D;
}

public class StaticCellDataComponent : ComponentDataWrapper<StaticCellData> { }
