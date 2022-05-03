using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwapActionGroupButton : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{

    public bool ButtonInverted;
    public bool Hovered;
    [SerializeField]
    ButtonSelectedHandler SelectHandler;
    [SerializeField]
    Image IconImage;
    [SerializeField]
    Image GlowImage;
    [SerializeField]
    GameObject SpawnActions;
    [SerializeField]
    GameObject NormalActions;
    [SerializeField]
    public Button Button;
    [SerializeField]
    Image ButtonBgImage;
    [SerializeField]
    Image ButtonHoveredGlow;

    [Header("Sprites")]
    public Sprite ActionsIconSprite;
    public Sprite ActionsGlowSprite;
    public Sprite SpawnIconSprite;
    public Sprite SpawnGlowSprite;


    public void Start()
    {
        Button.onClick.AddListener(delegate { InvertButton(); });
    }

    private void Update()
    {

        if (!Hovered)
        {
            ButtonHoveredGlow.enabled = false;

            //GlowImage.enabled = false;
        }
        else
        {
            ButtonHoveredGlow.enabled = true;

            //GlowImage.enabled = true;

        }
    }

    /*
    public void OnPointerDown(PointerEventData eventData)
    {
        InvertButton();
    }
    */

    public void InvertButton()
    {
        if (!ButtonInverted)
        {
            gameObject.name = "Normal Actions";
            NormalActions.SetActive(false);
            SpawnActions.SetActive(true);
            IconImage.sprite = ActionsIconSprite;
            GlowImage.sprite = ActionsGlowSprite;
            GlowImage.enabled = true;
            ButtonInverted = true;
        }
        else
        {
            gameObject.name = "Spawn Actions";
            NormalActions.SetActive(true);
            SpawnActions.SetActive(false);
            IconImage.sprite = SpawnIconSprite;
            GlowImage.sprite = SpawnGlowSprite;
            GlowImage.enabled = false;
            ButtonInverted = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered = true;

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hovered = false;

    }

}
