using UnityEngine;
using System.Collections;
using Unity.Entities;

namespace LeyLineHybridECS
{
    public class AnimatorSystem : ComponentSystem
    {

        public struct Data
        {
            public AnimatorState AnimatorState;
            public IsIdle IsIdle;
            public PathLists PathLists;
        }

        protected override void OnUpdate()
        {
            foreach(var entity in GetEntities<Data>())
            {
                var animatorState = entity.AnimatorState;
                var isIdle = entity.IsIdle;
                var pathLists = entity.PathLists;

                if (isIdle.Value == true)
                {
                    animatorState.CurrentState = 0;
                }
                /*
                //if the GameState is executing and the unit has a Path Planned
                if (GameStateSystem.CurrentState == GameStateSystem.State.Moving && pathLists.CurrentPath.Count != 0)
                {
                    animatorState.CurrentState = AnimatorState.State.Moving;
                }

                if (animatorState.CurrentState == AnimatorState.State.Moving)
                {
                    animatorState.Animator.SetBool("Moving", true);
                }
                else
                {
                    animatorState.Animator.SetBool("Moving", false);
                }
                */
            }
        }
    }
}

