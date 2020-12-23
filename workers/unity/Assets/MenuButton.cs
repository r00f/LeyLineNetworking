using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    public bool Hovered;
    [SerializeField]
    ButtonSelectedHandler SelectHandler;
    [SerializeField]
    Image IconImage;
    [SerializeField]
    Image GlowImage;

    public Button Button;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;
    }

    private void Update()
    {
        if (!Hovered)
        {
            GlowImage.enabled = false;
        }
        else
        {
            GlowImage.enabled = true;
        }
    }
    
}
