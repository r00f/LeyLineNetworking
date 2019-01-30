using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using Unity.Entities;
using System;


namespace LeyLineHybridECS
{
    [Serializable]
    public struct GridCoordinates : IComponentData
    {
        public float2 OffsetCoordinate;
        public float3 CubeCoordinate;
    }

    public class CoordinateDataComponent : ComponentDataWrapper<GridCoordinates> { }

}


