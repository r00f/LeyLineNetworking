using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{
    public class PlayerVisionData : MonoBehaviour
    {
        public List<IsVisible> Vision;
        public bool RequireUpdate;
    }
}