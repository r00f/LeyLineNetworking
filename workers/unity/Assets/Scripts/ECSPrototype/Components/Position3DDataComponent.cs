using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{
    [Serializable]
    public struct Position3D : IComponentData
    {
        public float3 Value;
    }

    public class Position3DDataComponent : ComponentDataProxy<Position3D> { }
}


