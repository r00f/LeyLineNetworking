using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManalithTooltip : MonoBehaviour
{
    public Text ManalithName;
    public Text ManalithBaseEnergyGain;
    public List<ManalithUISlot> ManalithUISlots;
    public Image NodeImage;
    public RectTransform RectTrans;
    public long ActiveManalithID = 0;
}
