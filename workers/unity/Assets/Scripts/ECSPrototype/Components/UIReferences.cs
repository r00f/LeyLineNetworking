using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIReferences : MonoBehaviour
{
    public Canvas Canvas;

    public GameObject UIMainPanel;

    [Header("EscapeMenu")]
    public EscapeMenu EscapeMenu;

    [Header("Startup")]
    public float StartUpWaitTime = 3f;
    public GameObject StartupPanel;
    public GameObject MatchReadyPanel;

    [Header("TopLeftHeroPanel")]
    public SelectUnitButton SelectHeroButton;
    public Text TopEnergyText;
    public Image TopEnergyFill;
    public HealthBar HeroHealthBar;
    public Image HeroPortraitPlayerColor;

    [Header("EnergyBarPanel")]
    public Image LeftEnergyIncomeFill;
    public Image LeftCurrentEnergyFill;
    public Text CurrentEnergyText;
    public Text MaxEnergyText;

    [Header("InGameUI")]
    //public float HealthBarYOffset;
    public GameObject IngameUIPanel;
    public GameObject HealthBarsPanel;
    public GameObject ManalithInfoPanel;
    public GameObject ActionEffectUIPanel;

    [Header("ReadyPanel")]
    public Button ReadyButton;
    public Image RopeBar;

    [Header("UnitPortrait")]
    public GameObject InfoEnabledPanel;
    public Image PortraitPlayerColor;
    public SetPortraitClip AnimatedPortrait;

    [Header("UnitStats")]
    public Text UnitStats;

    [Header("TurnWheel")]
    public float WheelRotationSpeed;
    public RectTransform TurnWheelBig;
    public RectTransform TurnWheelSmol;
    public float ReadyOutSpeed;
    public float ReadyInSpeed;
    public ReadySlider BlueReady;
    public ReadySlider RedReady;
    public List<Image> SmallWheelColoredParts;
    public List<Image> BigWheelColoredParts;
    public List<Image> TurnStepFlares;


    [Header("UnitPortraitInfoPanel")]
    public Text PortraitHealthText;
    public Text PortraitArmorText;
    public Text PortraitRegenText;
    public HealthBar PortraitHealthBar;
    //public GameObject HeroEnergyPanel;

    [Header("ButtonLists")]
    public List<ActionButton> Actions;
    public List<ActionButton> SpawnActions;
    public GameObject SpawnToggleGO;
    public GameObject ActionButtonGroup;
    public GameObject SpawnButtonGroup;
    public GameObject ActionsButton;
    public GameObject SpawnButton;

    //public Toggle ActionsToggle;
    //public Toggle SpawnActionsToggle;
    //public List<Toggle> TurnStateToggles;

    [Header("SelectedActionToolTip")]
    public Text SAActionName;
    public Text SAActionDescription;
    public Text SAEnergyText;
    public Image SAExecuteStepIcon;
    public Image SAExecuteStepBackGround;
    public Image SAEnergyFill;

    [Header("ToolTip")]
    public GameObject TTPanel;
    public RectTransform TTRect;
    public Text TTActionName;
    public Text TTActionDescription;
    public Text TTActionCost;
    public Image TTExecuteStepImage;

    public List<Sprite> ExecuteStepSprites;

    [Header("UnitGroups")]
    public List<UnitGroupUI> UnitGroups;
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
    public GameObject MiniMapCellTilesPanel;
    public GameObject MiniMapUnitTilesPanel;
    public GameObject MiniMapManalithTilesPanel;
    public GameObject MiniMapPlayerTilePanel;

    [Header("ManaLithUI")]
    public ManalithInfoComponent ManalithIconPrefab;
    public ManalithTooltip ManalithToolTipFab;
}
