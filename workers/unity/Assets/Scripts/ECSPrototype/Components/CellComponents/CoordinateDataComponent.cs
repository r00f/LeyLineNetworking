using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using Unity.Entities;
using System;


namespace LeyLineHybridECS
{
    [Serializable]
    public class CoordinateDataComponent : MonoBehaviour
    {
        //[HideInInspector]
        public float2 OffsetCoordinate;
        //[HideInInspector]
        public float3 CubeCoordinate;
    }

    //public class CoordinateDataComponent : ComponentDataProxy<GridCoordinates> { }

}


