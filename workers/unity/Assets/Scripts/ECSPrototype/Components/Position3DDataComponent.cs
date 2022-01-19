using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{
    [Serializable]
    public class Position3DDataComponent : MonoBehaviour
    {
        public float3 Value;
    }

    //public class Position3DDataComponent : ComponentDataProxy<Position3D> { }
}


