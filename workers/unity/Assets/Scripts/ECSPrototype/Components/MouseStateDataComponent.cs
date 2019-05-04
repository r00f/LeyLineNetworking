﻿using UnityEngine;
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

        public float Distance;
        public State CurrentState;
        public float yOffset;
        public byte ClickEvent;
    }

    public class MouseStateDataComponent : ComponentDataWrapper<MouseState> { }
}

