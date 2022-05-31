using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public bool VoiceTrigger;
    public bool FootStepTrigger;
    public bool EventTrigger;
    public bool EventTriggered;
    public int EffectGameObjectIndex;

    
    public void TriggerEvent()
    {
        EventTrigger = true;
    }

    public void ActionPreview()
    {
        //Dummy Method for action Preview start
    }

    public void SwitchEffect(int index = 0)
    {
        EffectGameObjectIndex = index;
    }

    public void PlayVoiceSFX()
    {
        VoiceTrigger = true;
    }

    public void PlayFootStepSFX()
    {
        FootStepTrigger = true;
    }
}
