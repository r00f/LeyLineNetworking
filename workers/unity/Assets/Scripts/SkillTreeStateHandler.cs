using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class SkillTreeStateHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    uint currentTier = 0;
    public List<SkillTreeButtonData> Nodes = new List<SkillTreeButtonData>();
    public List<Sprite> ButtonUPSet = new List<Sprite>();
    public List<Sprite> ButtonDOWNSet = new List<Sprite>();
    public Transform LinePanel;
    public Color PlayerColor = Color.red;
    public int Knowledge;
    public TreeNodeTooltipHandler ToolTipPanel;
    public RectTransform rect;
    bool dragmode;
    Vector2 MoseDragStartpos = new Vector2(0,0);
    // Start is called before the first frame update


    public uint CurrentTier()
    {
        return currentTier;
    }

    public void IncrementTier(/*button validation ID*/)
    {
        currentTier++;
        foreach (SkillTreeButtonData button in Nodes)
        {
            if(button.Tier == currentTier)
            {
                button.UpdateButtonstate();
                Debug.Log("Tier upgraded to:" + currentTier);
            }
        }
    }
    void Start()
    {
        foreach(SkillTreeButtonData n in Nodes)
        {
            n.StateHandler = this;
        }
        ToolTipPanel.rect = ToolTipPanel.gameObject.GetComponent<RectTransform>();
        rect = gameObject.GetComponent<RectTransform>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (dragmode)
        {
            Vector2 mouseDiff = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Vector2 desiredPos;
            float delta = Time.deltaTime;
            desiredPos = rect.anchoredPosition + (mouseDiff - MoseDragStartpos);
            Debug.Log(mouseDiff);
            if (inbounds_x(desiredPos.x))
            {
                rect.anchoredPosition = new Vector2(desiredPos.x, rect.anchoredPosition.y);
            }
            if (inbounds_y(desiredPos.y))
            {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, desiredPos.y);
            }
            MoseDragStartpos = mouseDiff;
        }
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragmode = true;
        MoseDragStartpos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        dragmode = false;
    }

    bool inbounds_x(float DesiredPosX)
    {
        bool isInBounds = false;
        if(DesiredPosX > -(rect.sizeDelta.x/2) && DesiredPosX < (rect.sizeDelta.x / 2)) { 
            isInBounds = true;
        }
        return isInBounds;
    }
    bool inbounds_y(float DesiredPosY)
    {
        bool isInBounds = false;
        if (DesiredPosY > -rect.sizeDelta.y && DesiredPosY <= 0)
        {
            isInBounds = true;
        }
        return isInBounds;
    }


}
