using UnityEngine;
using System.Collections;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{
    [Serializable]
    public struct MouseState : IComponentData
    {

        public enum State
        {
            Neutral = 0,
            Hovered = 1,
            Clicked = 2
        }

        public State CurrentState;
    }

    public class MouseStateDataComponent : ComponentDataWrapper<MouseState> { }
}

