using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FMODUnity;

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
    [SerializeField]
    public StudioEventEmitter CancelStateEmitter;

    [HideInInspector]
    public bool PlayerInCancelState;
    [HideInInspector]
    public bool PlayerReady;

    bool hovered;
    public bool RotatingBack;


    //int cancelCloseToOpenHash;
    //int cancelOpenHash;
    //int rotateBackHash;
    //int rotateToGoHash;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        //animator.SetBool("Hovered", true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        //animator.SetBool("Hovered", false);
    }

    public void Start()
    {
        var button = GetComponent<Button>();
        /*
        rotateToGoHash = Animator.StringToHash("Base Layer.RotateBack -> Base Layer.GO");
        cancelCloseToOpenHash = Animator.StringToHash("Base Layer.Cancel -> Base Layer.CancelOpen");
        cancelOpenHash = Animator.StringToHash("Base Layer.CancelOpen");
        rotateBackHash = Animator.StringToHash("Base Layer.RotateBack");
        */
        button.onClick.AddListener(delegate { OnClick(); });
        //LightInner.color = new Color(LightInner.color.r, LightInner.color.g, LightInner.color.b, 0);
    }

    private void Update()
    {
        //animator.SetBool("Ready", PlayerReady);
        //if(!PlayerReady)
            //animator.SetBool("Cancel", PlayerInCancelState);

        /*
        if ((animator.GetCurrentAnimatorStateInfo(0).fullPathHash == rotateBackHash || animator.GetCurrentAnimatorStateInfo(0).fullPathHash == cancelOpenHash || animator.GetAnimatorTransitionInfo(0).fullPathHash == cancelCloseToOpenHash) && animator.GetAnimatorTransitionInfo(0).fullPathHash != rotateToGoHash)
        {
            RotatingBack = true;
        }
        else
            RotatingBack = false;

        if (PlayerInCancelState && !PlayerReady)
        {
            if (!CancelStateEmitter.IsPlaying())
                CancelStateEmitter.Play();
        }
        else
        {
            if (CancelStateEmitter.IsPlaying())
                CancelStateEmitter.Stop();
        }

        */

        if (hovered && Button.interactable)
        {
            if (LightCircle.color.a < LightCircleHoverMax)
                LightCircle.color += new Color(0, 0, 0, LightCircleLerpSpeed * Time.deltaTime);
            else
                LightCircle.color -= new Color(0, 0, 0, LightCircleLerpSpeed * Time.deltaTime);

            /*
            if (LightFlare.color.a < LightFlareHoverMax)
                LightFlare.color += new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
            else
                LightFlare.color -= new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
                */
        }
        else
        {
            /*
            if(LightInner.color.a > 0)
            {
                LightInner.color -= new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
            }
            */
            if (LightCircle.color.a > LightCircleDefaultAlpha)
                LightCircle.color -= new Color(0, 0, 0, LightCircleLerpSpeed * Time.deltaTime);
            /*
            if (LightFlare.color.a > 0)
                LightFlare.color -= new Color(0, 0, 0, LightFlareLerpSpeed * Time.deltaTime);
                */
        }
    }

    public void OnClick()
    {
        //COMPLETELY FLARE UP LIGHTS
        //LightFlare.color = new Color(LightFlare.color.r, LightFlare.color.g, LightFlare.color.b, 1);
        LightCircle.color = new Color(LightCircle.color.r, LightCircle.color.g, LightCircle.color.b, 1);
    }

    public void SetLightsToPlayerColor(Color color)
    {
        //LightFlare.color = color;
        LightCircle.color = color;
        //LightInner.color = color;
    }

}
