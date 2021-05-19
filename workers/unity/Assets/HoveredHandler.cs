using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoveredHandler : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    public bool Hovered;
    public GameObject HoverPanel;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;
    }
}
