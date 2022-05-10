using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeStateHandler : MonoBehaviour
{
    uint currentTier = 0;
    public List<SkillTreeButtonData> Nodes = new List<SkillTreeButtonData>();
    public List<Sprite> ButtonUPSet = new List<Sprite>();
    public List<Sprite> ButtonDOWNSet = new List<Sprite>();
    public Transform LinePanel;
    public Color PlayerColor = Color.red;
    public int Knowledge;
    public TreeNodeTooltipHandler ToolTipPanel;
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
                button.UpdateButtonstate(currentTier);
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

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
