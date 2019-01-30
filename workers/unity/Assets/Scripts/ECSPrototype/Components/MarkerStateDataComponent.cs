using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Entities;

namespace LeyLineHybridECS
{
    [Serializable]
    public struct MarkerState : IComponentData
    {
        public enum State
        {
            Neutral = 0,
            Hovered = 1,
            Clicked = 2,
            Reachable = 3
        }

        public State CurrentState;

        public int IsSet;

        //public List<GameObject> MarkerObjects;
        /*
        public GameObject ClickedMarker;
        public GameObject HoveredMarker;
        public GameObject ReachableMarker;
        */
    }

    public class MarkerStateDataComponent : ComponentDataWrapper<MarkerState> { }
}