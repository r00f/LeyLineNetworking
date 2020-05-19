using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FMODUnity;

public class ActionButton : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    [SerializeField]
    UIReferences UIRef;
    [SerializeField]
    RectTransform RectTransform;
    [SerializeField]
    RectTransform IconRectTransform;
    [SerializeField]
    Vector2 Offset;
    [Header("Component References")]
    public GameObject Visuals;
    public Image Icon;
    public Image TurnStepBauble;
    public int ExecuteStepIndex;
    public string ActionName;
    public string ActionDescription;
    public Button Button;
    public uint EnergyCost;
    public int ActionIndex;
    public int UnitId;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Visuals.activeSelf)
        {
            UIRef.TTRect.anchoredPosition = new Vector2(RectTransform.anchoredPosition.x, UIRef.TTRect.anchoredPosition.y);
            UIRef.TTActionCost.text = "" + EnergyCost;
            UIRef.TTActionDescription.text = ActionDescription;
            UIRef.TTActionName.text = ActionName;
            UIRef.TTExecuteStepImage.sprite = UIRef.ExecuteStepSprites[ExecuteStepIndex];
            UIRef.TTPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIRef.TTPanel.SetActive(false);
        //IconRectTransform.anchoredPosition = new Vector2(0, 0);
    }


}
