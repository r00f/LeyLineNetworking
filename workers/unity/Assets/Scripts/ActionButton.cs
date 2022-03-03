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
    public RectTransform ButtonRect;
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
        if(!Visuals.activeSelf)
        {
            if(SelectHandler.Selected)
                SelectHandler.Selected = false;
            if(TurnStepBauble.enabled)
                TurnStepBauble.enabled = false;
            if(SelectedGlow.enabled)
                SelectedGlow.enabled = false;
            if (Hovered)
                Hovered = false;
        }

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
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;
    }
}
