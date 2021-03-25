using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FMODUnity;
using System;

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
    [SerializeField]
    ButtonSelectedHandler SelectHandler;
    [Header("Component References")]
    public GameObject Visuals;
    public Image Icon;
    public Image TurnStepBauble;
    public Image SelectedGlow;
    public int ExecuteStepIndex;
    public string ActionName;
    public string ActionDescription;
    public Button Button;
    public uint EnergyCost;
    public int ActionIndex;
    public int UnitId;
    public bool Hovered;

    private void Update()
    {
        if(!SelectHandler.Selected && !Hovered)
        {
            TurnStepBauble.enabled = false;
            SelectedGlow.enabled = false;
        }
        else
        {
            TurnStepBauble.enabled = true;
            SelectedGlow.enabled = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Visuals.activeSelf)
        {
            Hovered = true;
            UIRef.TTRect.anchoredPosition = new Vector2(RectTransform.anchoredPosition.x, UIRef.TTRect.anchoredPosition.y);
            UIRef.TTActionCost.text = "" + EnergyCost;
            UIRef.TTActionDescription.text = ActionDescription;
            UIRef.TTActionName.text = ActionName.Replace(" ", Environment.NewLine);
            UIRef.TTExecuteStepImage.sprite = UIRef.ExecuteStepSprites[ExecuteStepIndex];
            UIRef.TTPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;
        UIRef.TTPanel.SetActive(false);
        //IconRectTransform.anchoredPosition = new Vector2(0, 0);
    }


}
