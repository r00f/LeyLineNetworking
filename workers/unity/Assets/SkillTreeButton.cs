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
        if(data.State == SkillTreeButtonData.ButtonState.unlearned)
        {
            data.learnNode();
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        data.ButtonImageDOWN.gameObject.SetActive(false);
        data.ButtonImageUP.gameObject.SetActive(true);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        data.HoverGlow.gameObject.SetActive(true);

        if (!data.StateHandler.ToolTipPanel.gameObject.activeSelf)
        {
            data.StateHandler.ToolTipPanel.gameObject.SetActive(true);
            data.StateHandler.ToolTipPanel.rect.anchoredPosition = new Vector2(data.rectTransform.anchoredPosition.x - (data.StateHandler.ToolTipPanel.rect.sizeDelta.x * .75f), data.rectTransform.anchoredPosition.y);
            data.PopulateTooltip();
        }

    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        data.HoverGlow.gameObject.SetActive(false);
        
    }
}
