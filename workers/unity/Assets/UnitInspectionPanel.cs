using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitInspectionPanel : MonoBehaviour
{
    public Text UnitDescription;
    public Image Portrait;
    public List<ActionButton> HoverOnlyButtons = new List<ActionButton>();

    public SelectedActionToolTip ToolTip;
}
