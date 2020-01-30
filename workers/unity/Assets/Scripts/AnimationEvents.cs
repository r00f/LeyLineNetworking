using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public bool EventTrigger;
    public int EffectGameObjectIndex;

    public void TriggerEvent()
    {
        EventTrigger = true;
    }

    public void SwitchEffect(int index = 0)
    {
        EffectGameObjectIndex = index;
        //EffectGameObjectIndex = -1;
    }
}
