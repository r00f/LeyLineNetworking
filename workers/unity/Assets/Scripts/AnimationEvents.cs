using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public bool EventTrigger;

    public void TriggerEvent(int eventIndex = 0)
    {
        EventTrigger = true;
    }
}
