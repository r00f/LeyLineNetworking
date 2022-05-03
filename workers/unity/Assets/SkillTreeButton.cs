using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(SkillTreeButtonData))]
public class SkillTreeButton : Button
{
    SkillTreeButtonData data;
    // Start is called before the first frame update
    protected override void Start()
    {
        data = GetComponent<SkillTreeButtonData>();
    }

    // Update is called once per frame
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        data.ButtonImageDOWN.gameObject.SetActive(true);
        data.ButtonImageUP.gameObject.SetActive(false);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        data.ButtonImageDOWN.gameObject.SetActive(false);
        data.ButtonImageUP.gameObject.SetActive(true);
    }
}
