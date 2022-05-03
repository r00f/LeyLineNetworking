using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class SkillTreeButtonData : MonoBehaviour
{

    public Image Icon;
    public Image GlowingRing;
    public Image ButtonImageUP;
    public Image ButtonImageDOWN;
    public Button myButton;
    public String InfoText;
    public Image InfoSketch;
    public UILineRenderer InConnectionLine;
    public SkillTreeButtonData IncomingConnection;
    public List<SkillTreeButtonData> OutgoingConnections = new  List<SkillTreeButtonData>();
    public SkillTreeButtonData DecisionPartner;
    public uint Tier;
    public uint NodeCost;
    SkillTreeStateHandler StateHandler;
    public RectTransform rectTransform;
    public enum ButtonState
    {
        invisible,
        unaccessible,
        unlearned,
        learned
    }
    public ButtonState State;

    public enum Nodetype
    {
        UnlockUnit,
        UpgradeUnitType,
        UlockAction,
        GenericUpgrade,
        DecisionNode,
        TierUpgrade
    }
    public Nodetype NodeType;
    public bool initialized = false;
    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        if(StateHandler != null && !initialized)
        {
            rectTransform = gameObject.GetComponent<RectTransform>();

            InConnectionLine.Points[1] = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
            if(IncomingConnection!=null)
            {
                if (IncomingConnection.initialized)
                {
                    InConnectionLine.Points[0] = new Vector2(IncomingConnection.rectTransform.anchoredPosition.x, IncomingConnection.rectTransform.anchoredPosition.y);
                }
                else
                {
                    return;
                }
            }
            else
            {
                RectTransform baseRect = StateHandler.gameObject.GetComponent<RectTransform>();
                InConnectionLine.Points[0] = new Vector2(baseRect.anchoredPosition.x, baseRect.anchoredPosition.y);
            }


            if ((int) NodeType <= 3)
            {
                ButtonImageUP.sprite = StateHandler.ButtonUPSet[(int) NodeType].sprite;
                ButtonImageUP.gameObject.SetActive(true);
                ButtonImageDOWN.sprite = StateHandler.ButtonDOWNSet[(int) NodeType].sprite;
                UpdateButtonstate(StateHandler.CurrentTier());
                initialized = true;
            }
            else if ((int) NodeType == 4)
            {
                //Decision Node Special Case
                initialized = true;
            }
            else if ((int)NodeType == 5)
            {
                ButtonImageUP.sprite = StateHandler.ButtonUPSet[0].sprite;
                ButtonImageUP.gameObject.SetActive(true);
                ButtonImageDOWN.sprite = StateHandler.ButtonDOWNSet[0].sprite;
                UpdateButtonstate(StateHandler.CurrentTier());
                initialized = true;
            }
        }
    }

    public void UpdateButtonstate (uint currentTier)
    {
        if (IncomingConnection != null)
        {
            if (IncomingConnection.State == ButtonState.learned && currentTier >= Tier)
            {
                if (State != ButtonState.learned || State != ButtonState.unlearned)
                {
                    State = ButtonState.unlearned;
                }
            }
            else if (IncomingConnection.State == ButtonState.invisible || IncomingConnection.NodeType == Nodetype.DecisionNode)
            {
                State = ButtonState.invisible;
            }
            else
            {
                State = ButtonState.unaccessible;
            }
        }
    }

    public void learnNode()
    {

        State = ButtonState.learned;

        if (StateHandler){
            if (NodeType == Nodetype.TierUpgrade)
            {
                StateHandler.IncrementTier();
            }
            foreach (SkillTreeButtonData b in OutgoingConnections)
            {
                b.UpdateButtonstate(StateHandler.CurrentTier());
            }
        }

    }
}
