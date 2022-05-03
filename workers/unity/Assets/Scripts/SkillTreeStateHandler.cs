using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeStateHandler : MonoBehaviour
{
    uint currentTier = 0;
    public List<SkillTreeButtonData> Nodes = new List<SkillTreeButtonData>();
    public List<Image> ButtonUPSet = new List<Image>();
    public List<Image> ButtonDOWNSet = new List<Image>();
    public Color PlayerColor = Color.white;
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
            }
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
