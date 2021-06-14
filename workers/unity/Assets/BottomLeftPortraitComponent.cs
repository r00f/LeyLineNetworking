using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomLeftPortraitComponent : MonoBehaviour
{
    [Header("Portrait")]
    public SetPortraitClip AnimatedPortrait;
    public Text PortraitNameText;
    public Image PortraitPlayerColorGlow;

    [Header("Unit Info")]
    public GameObject UnitInfoPanel;
    //public GameObject UnitStatsPanel;
    public Text PortraitHealthText;
    public Text PortraitArmorText;
    public Text PortraitRegenText;
    public HealthBar PortraitHealthBar;
    public Text UnitDescription;

    [Header("Manalith Info")]
    public GameObject ManalithInfoPanel;
    public Text ManalithEnergyGainText;
    public List<ManalithInfoHexHelper> InfoPanelHexes = new List<ManalithInfoHexHelper>();

}
