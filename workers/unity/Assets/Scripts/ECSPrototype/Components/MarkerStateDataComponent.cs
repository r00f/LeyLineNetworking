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

        public uint NumberOfTargets;

        public int TurnStepIndex;

        public byte TargetTypeSet;

        public byte IsUnit;

        public enum TargetType
        {
            Neutral = 0,
            AttackTarget = 1,
            DefenseTarget = 2,
            HealTarget = 3
        }

        public TargetType CurrentTargetType;
    }
}