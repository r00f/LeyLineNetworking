using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    public class AnimatorState : MonoBehaviour
    {
        public Animator Animator;

        public enum State
        {
            Idle = 0,
            Moving = 1,
            BasicAttack = 2,
        }

        public State CurrentState;
    }
}

