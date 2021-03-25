using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSelectedHandler : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public bool Selected;

    public void OnSelect(BaseEventData eventData)
    {
        Selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Selected = false;
    }
}
