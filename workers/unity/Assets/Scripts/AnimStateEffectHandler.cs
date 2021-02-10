using System.Collections.Generic;
using UnityEngine;

public class AnimStateEffectHandler : StateMachineBehaviour
{
    public List<Vector2> EffectOnTimestamps;
    public List<Vector2> CurrentEffectOnTimestamps;

    public List<Vector2> EffectOffTimestamps;
    public List<Vector2> CurrentEffectOffTimestamps;

    public bool IsActiveState;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IsActiveState = true;

        for (int i = 0; i < EffectOnTimestamps.Count; i++)
            CurrentEffectOnTimestamps[i] = EffectOnTimestamps[i];

        for (int i = 0; i < EffectOffTimestamps.Count; i++)
            CurrentEffectOffTimestamps[i] = EffectOffTimestamps[i];
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        for (int i = 0; i < CurrentEffectOnTimestamps.Count; i++)
        {
            if (CurrentEffectOnTimestamps[i].x >= 0f)
            {
                CurrentEffectOnTimestamps[i] -= new Vector2(Time.deltaTime, 0);
            }
        }

        for (int i = 0; i < CurrentEffectOffTimestamps.Count; i++)
        {
            if (CurrentEffectOffTimestamps[i].x >= 0f)
            {
                CurrentEffectOffTimestamps[i] -= new Vector2(Time.deltaTime, 0);
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IsActiveState = false;
    }
}
