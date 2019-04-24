using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField]
    RectTransform IconRectTransform;
    [SerializeField]
    Vector2 Offset;
    [Header("Component References")]
    public GameObject Visuals;
    public Image Icon;
    public string ActionName;
    public Button Button;
    public int ActionIndex;
    public int UnitId;


    public void OnPointerDown(PointerEventData eventData)
    {
        IconRectTransform.anchoredPosition = Offset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IconRectTransform.anchoredPosition = new Vector2(0, 0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IconRectTransform.anchoredPosition = new Vector2(0, 0);
    }
}
