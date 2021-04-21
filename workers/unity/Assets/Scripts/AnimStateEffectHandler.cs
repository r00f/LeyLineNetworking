using System.Collections.Generic;
using UnityEngine;

public class AnimStateEffectHandler : StateMachineBehaviour
{
    public List<Vector2> EffectOnTimestamps;
    public List<Vector2> EffectOffTimestamps;

    [HideInInspector]
    public List<Vector2> CurrentEffectOnTimestamps;
    [HideInInspector]
    public List<Vector2> CurrentEffectOffTimestamps;

    public bool IsActiveState;


    /*
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        IsActiveState = true;
        CurrentEffectOnTimestamps.AddRange(EffectOnTimestamps);
        CurrentEffectOffTimestamps.AddRange(EffectOffTimestamps);
    }
    */
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IsActiveState = true;
        CurrentEffectOnTimestamps.AddRange(EffectOnTimestamps);
        CurrentEffectOffTimestamps.AddRange(EffectOffTimestamps);
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
