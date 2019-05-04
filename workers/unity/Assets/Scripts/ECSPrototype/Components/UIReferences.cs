using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIReferences : MonoBehaviour
{
    public Canvas Canvas;

    [Header("Startup")]
    public float StartUpWaitTime = 3f;
    public GameObject StartupPanel;
    public GameObject MatchReadyPanel;

    [Header("EnergyBarPanel")]
    public Image EnergyIncomeFill;
    public Image CurrentEnergyFill;
    public Text HeroEnergyText;

    [Header("HealthBars")]
    public float HealthBarYOffset;
    public GameObject HealthbarsPanel;

    [Header("ReadyPanel")]
    public Button ReadyButton;
    public GameObject RedReady;
    public GameObject BlueReady;
    public Image RopeBar;

    [Header("UnitPortrait")]
    public GameObject InfoEnabledPanel;
    public Image PortraitPlayerColor;
    public SetPortraitClip AnimatedPortrait;

    [Header("UnitStats")]
    public Text UnitStats;

    [Header("BottomBars")]
    public Text HealthText;
    public Image HealthFill;
    //public GameObject HeroEnergyPanel;

    [Header("ButtonLists")]
    public List<ActionButton> Actions;
    public List<ActionButton> SpawnActions;
    public GameObject SpawnToggle;
}
