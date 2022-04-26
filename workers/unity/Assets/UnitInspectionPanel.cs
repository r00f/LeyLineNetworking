using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitInspectionPanel : MonoBehaviour
{
    public Text UnitName;
    public Text UnitDescription;
    public SetPortraitClip Portrait;
    public Image PortraitGlow;
    public List<ActionButton> HoverOnlyButtons = new List<ActionButton>();
    public SelectedActionToolTip ToolTip;
}
