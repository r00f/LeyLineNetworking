using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{

    public class UnitVisionData : MonoBehaviour
    {
        public int VisionRange;
        public List<IsVisible> VisibleToThis;
        public bool RequireUpdate;
    }
}