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
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IsActiveState = true;
        CurrentEffectOnTimestamps.AddRange(EffectOnTimestamps);
        CurrentEffectOffTimestamps.AddRange(EffectOffTimestamps);
    }
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {


        if(animator.speed != 0)
        {
            //Debug.Log(stateInfo.length * stateInfo.normalizedTime);
            for (int i = 0; i < CurrentEffectOnTimestamps.Count; i++)
            {
                if (CurrentEffectOnTimestamps[i].x <= stateInfo.length * stateInfo.normalizedTime)
                {
                    CurrentEffectOnTimestamps[i] = new Vector2(0, CurrentEffectOnTimestamps[i].y);
                }
            }

            for (int i = 0; i < CurrentEffectOffTimestamps.Count; i++)
            {
                if (CurrentEffectOffTimestamps[i].x <= stateInfo.length * stateInfo.normalizedTime)
                {
                    CurrentEffectOffTimestamps[i] = new Vector2(0, CurrentEffectOffTimestamps[i].y);
                }
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IsActiveState = false;
    }
}
