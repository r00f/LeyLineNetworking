using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FMODUnity;

[RequireComponent(typeof(Button))]
public class ButtonSoundHandler : MonoBehaviour, IPointerEnterHandler
{
    public string OnPointerEnterEventPath;
    public string OnPointerDownEventPath;

    //
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OnPointerEnterEventPath.Length != 0)
            RuntimeManager.PlayOneShot(OnPointerEnterEventPath);
    }

    public void Start()
    {
        var button = GetComponent<Button>();
        button.onClick.AddListener(delegate { OnClick(); });
    }

    public void OnClick()
    {
        if (OnPointerDownEventPath.Length != 0)
            RuntimeManager.PlayOneShot(OnPointerDownEventPath);
    }
}
