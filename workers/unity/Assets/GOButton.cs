﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class GOButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    UIReferences UIRef;
    [Header("AdjustableVariables")]
    [SerializeField]
    float LightCircleDefaultAlpha;
    [SerializeField]
    float LightCircleLerpSpeed;
    [SerializeField]
    float LightFlareLerpSpeed;
    [SerializeField]
    float LightCircleHoverMax;
    [SerializeField]
    float LightFlareHoverMax;


    [Header("ComponentReferences")]
    [SerializeField]
    public Button Button;
    [SerializeField]
    Animator animator;
    [SerializeField]
    public Image LightCircle;
    [SerializeField]
    public Image LightFlare;
    [SerializeField]
    public Image LightInner;

    [HideInInspector]
    public bool PlayerInCancelState;

    bool hovered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        animator.SetBool("Hovered", true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        animator.SetBool("Hovered", false);
    }

    public void Start()
    {
        var button = GetComponent<Button>();
        button.onClick.AddListener(delegate { OnClick(); });
        LightInner.color = new Color(LightInner.color.r, LightInner.color.g, LightInner.color.b, 0);
    }

    private void Update()
    {
        animator.SetBool("Cancel", PlayerInCancelState);

        if (hovered && Button.interactable)
        {
            if (LightCircle.color.a < LightCircleHoverMax)
                LightCircle.color += new Color(0, 0, 0, LightCircleLerpSpeed * Time.deltaTime);
            else
                LightCircle.color -= new Color(0, 0, 0, LightCircleLerpSpeed * Time.deltaTime);

            if (LightFlare.color.a < LightFlareHoverMax)
                LightFlare.color += new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
            else
                LightFlare.color -= new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
        }
        else
        {
            if(LightInner.color.a > 0)
            {
                LightInner.color -= new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
            }
            if (LightCircle.color.a > LightCircleDefaultAlpha)
                LightCircle.color -= new Color(0, 0, 0, LightCircleLerpSpeed * Time.deltaTime);

            if (LightFlare.color.a > 0)
                LightFlare.color -= new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
        }
    }

    public void OnClick()
    {
        //COMPLETELY FLARE UP LIGHTS
        LightFlare.color = new Color(LightFlare.color.r, LightFlare.color.g, LightFlare.color.b, 1);
        LightCircle.color = new Color(LightCircle.color.r, LightCircle.color.g, LightCircle.color.b, 1);
    }

    public void SetLightsToPlayerColor(Color color)
    {
        LightFlare.color = color;
        LightCircle.color = color;
        LightInner.color = color;
    }

}
