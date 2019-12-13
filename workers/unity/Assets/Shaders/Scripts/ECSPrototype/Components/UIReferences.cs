﻿using System.Collections;
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

    [Header("TopLeftHeroPanel")]
    public Image TopEnergyIncomeFill;
    public Image TopCurrentEnergyFill;
    public Image TopHealthFill;
    public Image TopArmorFill;

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
    public Image BlueColor;
    public Image RedColor;
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
    public Image ArmorFill;
    //public GameObject HeroEnergyPanel;

    [Header("ButtonLists")]
    public List<ActionButton> Actions;
    public List<ActionButton> SpawnActions;
    public GameObject SpawnToggleGO;
    public Toggle ActionsToggle;
    public Toggle SpawnActionsToggle;
    public List<Toggle> TurnStateToggles;

    [Header("ToolTip")]
    public GameObject TTPanel;
    public RectTransform TTRect;
    public Text TTActionName;
    public Text TTActionDescription;
    public Text TTActionCost;
    public Image TTExecuteStepImage;
    public List<Sprite> ExecuteStepSprites;

    [Header("UnitGroups")]
    public SelectUnitButton UnitButtonPrefab;
    public UnitGroupUI UnitGroupPrefab;
    public GameObject UnitGroupsParent;
    public Dictionary<uint, UnitGroupUI> ExistingUnitGroups = new Dictionary<uint, UnitGroupUI>();

    [Header("GameOver")]
    public GameObject GameOverPanel;
    public GameObject VictoryPanel;
    public GameObject DefeatPanel;
    public GameObject DrawPanel;

    [Header("MapPanel")]
    public MinimapScript MinimapComponent;


}