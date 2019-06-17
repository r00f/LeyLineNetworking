using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public bool EventTrigger;

    public void TriggerEvent()
    {
        EventTrigger = true;
    }
}
