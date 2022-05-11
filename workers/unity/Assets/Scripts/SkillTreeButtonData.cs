using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(RectTransform))]
public class SkillTreeButtonData : MonoBehaviour
{

    public Image Icon;
    public Image GlowingRing;
    public Image ButtonImageUP;
    public Image ButtonImageDOWN;
    public Image HoverGlow;
    public String InfoText;
    public Image InfoSketch;
    public UILineRenderer InConnectionLine;
    public SkillTreeButtonData IncomingConnection;
    public List<SkillTreeButtonData> OutgoingConnections = new List<SkillTreeButtonData>();
    public SkillTreeButtonData DecisionPartner;
    public Color ColorizeIn;
    public uint Tier;
    public int NodeCost;
    public SkillTreeStateHandler StateHandler;
    public RectTransform rectTransform;
    [SerializeField]
    UnityEngine.Gradient graduate = new UnityEngine.Gradient();
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
        StatChange,
        DecisionNode,
        TierUpgrade
    }
    public Nodetype NodeType;
    public bool initialized = false;
    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        if (StateHandler != null && !initialized)
        {
            rectTransform = gameObject.GetComponent<RectTransform>();


            InConnectionLine.Points[0] = new Vector2(0, 0);
            if (IncomingConnection != null)
            {
                if (IncomingConnection.initialized)
                {
                    InConnectionLine.Points[1] = new Vector2(IncomingConnection.rectTransform.anchoredPosition.x - rectTransform.anchoredPosition.x, IncomingConnection.rectTransform.anchoredPosition.y - rectTransform.anchoredPosition.y);
                    InConnectionLine.gameObject.transform.SetParent(StateHandler.LinePanel, true);
                    InConnectionLine.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                    InConnectionLine.SetAllDirty();

                }
                else return;
            }
            else
            {
                //RectTransform baseRect = StateHandler.gameObject.GetComponent<RectTransform>();
                InConnectionLine.Points[1] = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
                InConnectionLine.gameObject.transform.SetParent(StateHandler.LinePanel, true);
                InConnectionLine.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                InConnectionLine.SetAllDirty();
            }


            if ((int) NodeType <= 3)
            {

                ButtonImageUP.sprite = StateHandler.ButtonUPSet[(int) NodeType];
                ButtonImageUP.gameObject.SetActive(true);
                ButtonImageDOWN.sprite = StateHandler.ButtonDOWNSet[(int) NodeType];
                UpdateButtonstate();
                rectTransform = gameObject.GetComponent<RectTransform>();
                initialized = true;
            }
            else if ((int) NodeType == 4)
            {
                //Decision Node Special Case
                rectTransform = gameObject.GetComponent<RectTransform>();
                initialized = true;
            }
            else if ((int) NodeType == 5)
            {
                ButtonImageUP.sprite = StateHandler.ButtonUPSet[0];
                ButtonImageUP.gameObject.SetActive(true);
                ButtonImageDOWN.sprite = StateHandler.ButtonDOWNSet[0];
                UpdateButtonstate();
                if (State == ButtonState.learned)
                {
                    ColorizeIn = StateHandler.PlayerColor;
                    colorize();
                }
                rectTransform = GetComponent<RectTransform>();
                initialized = true;
            }
        }
    }

    public void UpdateButtonstate()
    {
        if (IncomingConnection != null)
        {
            if (IncomingConnection.State == ButtonState.learned && StateHandler.CurrentTier() >= Tier)
            {
                if (State != ButtonState.learned && State != ButtonState.unlearned && StateHandler.Knowledge - NodeCost >= 0)
                {
                    State = ButtonState.unlearned;
                    ColorizeIn = Color.white;
                    InConnectionLine.color = Color.white;
                    colorize();
                }
                else if (State == ButtonState.unlearned && StateHandler.Knowledge - NodeCost < 0)
                {
                    State = ButtonState.unaccessible;
                    ColorizeIn = new Color(.6f, .6f, .6f, .5f);
                    InConnectionLine.color = new Color(.6f, .6f, .6f, .5f); ;
                    colorize();
                }
            }
            else if (IncomingConnection.State == ButtonState.invisible || IncomingConnection.NodeType == Nodetype.DecisionNode)
            {
                State = ButtonState.invisible;
                ColorizeIn = new Color(0f, 0f, 0f, .0f);
                InConnectionLine.color = new Color(0f, 0f, 0f, .0f);
                colorize();
            }
            else
            {
                State = ButtonState.unaccessible;
                ColorizeIn = new Color(.6f, .6f, .6f, .5f);
                InConnectionLine.color = new Color(.6f, .6f, .6f, .5f); ;
                colorize();
            }
        }
    }

    public void learnNode()
    {
        if (StateHandler != null) {
            if (StateHandler.Knowledge - NodeCost >= 0)
            {
                State = ButtonState.learned;
                ColorizeIn = StateHandler.PlayerColor;
                StateHandler.Knowledge -= NodeCost;
                StateHandler.KnowledgeText.text = StateHandler.Knowledge.ToString();
                colorize();
                if (NodeType == Nodetype.TierUpgrade)
                {
                    StateHandler.IncrementTier();
                }
                var nodes = StateHandler.Nodes;
                StateHandler.Nodes = nodes;
                StateHandler.UpdateAllOtherButtons(this);
            }
        }

    }

    public void colorize()
    {
        GlowingRing.color = ColorizeIn;
        HoverGlow.color = ColorizeIn;
        foreach (SkillTreeButtonData b in OutgoingConnections)
        {
            b.colorize();
        }
        if (IncomingConnection != null)
        {
            GraduateLine(InConnectionLine, ColorizeIn, IncomingConnection.ColorizeIn);
        }
        else
        {
            GraduateLine(InConnectionLine, ColorizeIn, StateHandler.PlayerColor);
        }
    }

    public void GraduateLine(UILineRenderer ren, Color a, Color b)


    {
        var colorkeyarray = new GradientColorKey[2];
        var alphakeyarray = new GradientAlphaKey[2];


        colorkeyarray[0].color = a;
        colorkeyarray[0].time = 0;
        colorkeyarray[1].color = b;
        colorkeyarray[1].time = 1;
        alphakeyarray[0].alpha = a.a;
        alphakeyarray[0].time = 0;
        alphakeyarray[1].alpha = b.a;
        alphakeyarray[1].time = 1;


        graduate.SetKeys(colorkeyarray, alphakeyarray);
        ren.Gradient = graduate;
        ren.SetVerticesDirty();
    }

    public void PopulateTooltip()
    {
        StateHandler.ToolTipPanel.DescriptionText.text = InfoText;
        StateHandler.ToolTipPanel.Cost.text = NodeCost.ToString();
    }
}
