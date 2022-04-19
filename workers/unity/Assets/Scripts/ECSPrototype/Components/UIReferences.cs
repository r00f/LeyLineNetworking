using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIReferences : MonoBehaviour
{
    public Canvas Canvas;

    //public Animator SlideOutUIAnimator;

    public bool OpponentReady;
    public bool UIActive;

    public float EnergyLerpSpeed;
    public bool DollyPathCameraActive;

    public Material SelectionOutlineMaterial;
    public DollyCameraComponent dollyCam;

    [Header("SubCanvases")]
    public List<Canvas> Canvases;

    [Header("MenuPanels")]
    public HelpPanel HelpPanelComponent;
    //public GameObject UIMainPanel;
    public GameObject HelpPanel;
    public GameObject SkillTreePanel;

    [Header("MenuButtons")]
    public Button RevealVisionButton;
    public MenuButton MainMenuButton;
    public MenuButton HelpButton;
    public MenuButton SkilltreeButton;

    [Header("EscapeMenu")]
    public EscapeMenu EscapeMenu;

    [Header("Startup")]
    public float StartUpWaitTime = 3f;
    public GameObject StartupPanel;
    public GameObject MatchReadyPanel;

    [Header("TopLeftHeroPanel")]
    public SelectUnitButton SelectHeroButton;
    public Text HeroBaubleEnergyText;
    public Image TopEnergyFill;
    public HealthBar HeroHealthBar;
    public Image HeroPortraitPlayerColor;

    [Header("EnergyBarPanel")]
    public float MaxFillAmount;
    public Image HeroEnergyIncomeFill;
    public Image HeroCurrentEnergyFill;
    public Text CurrentEnergyText;
    public Text MaxEnergyText;
    public Text TotalEnergyIncomeText;

    [Header("InGameUI")]
    public GameObject IngameUIPanel;
    public GameObject HealthBarsPanel;
    public GameObject ManalithInfoPanel;
    public GameObject ActionEffectUIPanel;

    [Header("UnitPortrait")]
    public BottomLeftPortraitComponent BottomLeftPortrait;
    public UnitInfoPanel UnitInfoPanel;
    /*
    public GameObject InfoEnabledPanel;
    public Image PortraitPlayerColor;
    public Image PortraitPlayerColorGlow;
    public SetPortraitClip AnimatedPortrait;
    public Text PortraitNameText;
    
    [Header("UnitStats")]
    public Text UnitStats;

    [Header("UnitPortraitInfoPanel")]
    public Text PortraitHealthText;
    public Text PortraitArmorText;
    public Text PortraitRegenText;
    public HealthBar PortraitHealthBar;
    */
    [Header("Actions")]
    public GameObject ActionPanel;
    public Button CancelActionButton;
    public List<ActionButton> Actions;
    public List<ActionButton> SpawnActions;
    public SwapActionGroupButton SwapActionButton;
    public GameObject ActionButtonGroup;
    public GameObject SpawnButtonGroup;

    [Header("SelectedActionToolTip")]
    public SelectedActionToolTip SAToolTip;

    public List<Sprite> ExecuteStepSprites;

    [Header("UnitGroups")]
    public float GroupBaubleFillUpSpeed;
    public float GroupBaubleFillDownSpeed;
    public float EnergyConnectorPositiveSpeed;
    public float EnergyConnectorNegativeSpeed;
    public List<UnitGroupUI> UnitGroups;
    public SelectUnitButton UnitButtonPrefab;
    public UnitGroupUI UnitGroupPeasantPrefab;
    public UnitGroupUI UnitGroupPrefab;
    public GameObject UnitGroupsParent;
    public Dictionary<uint, UnitGroupUI> ExistingUnitGroups = new Dictionary<uint, UnitGroupUI>();
    public Image EnergyConnectorPlayerColorFill;
    public Image EnergyConnectorNegativeFill;
    public Vector2 ECFillAmounts;

    [Header("GameOver")]
    public GameObject GameOverPanel;
    public GameObject VictoryPanel;
    public GameObject DefeatPanel;
    public GameObject DrawPanel;

    [Header("MapPanel")]
    public MinimapScript MinimapComponent;
    public GameObject MapPanel;
    public MinimapScript BigMapComponent;
    public Text BigMapTurnCounter;

    /*
    [Header("ManaLithUI")]
    public ManalithInfoComponent ManalithIconPrefab;
    public ManalithTooltip ManalithToolTipFab;
    */

    [Header("ReadyPanel")]
    public TurnStatePanel TurnStatePnl;

    [HideInInspector]
    public bool RopeSlamOneTime;
    [HideInInspector]
    public float FriendlyRopeEndFillAmount;
    [HideInInspector]
    public float EnemyRopeEndFillAmount;
    [HideInInspector]
    public float RopeEndsLerpTime;

    public Color FriendlyIncomeColor;
    [HideInInspector]
    public Color FriendlyColor;
    [HideInInspector]
    public Color EnemyColor;
    [HideInInspector]
    public string CurrentStateString;


    [Header("MainTurnDisplay")]
    public MainTurnDisplay TurnDisplay;

    [Header("ParticleSystems")]
    public List<ParticleSystem> CowExhaleParticleSystems;
    public FillBarParticleComponent FriendlyRopeBarParticle;
    public FillBarParticleComponent EnemyRopeBarParticle;
    public ParticleSystem FriendlyReadyBurstPS;
    public ParticleSystem EnemyReadyBurstPS;

    [Header("TurnWheel")]
    public UIEffectsFired CurrentEffectsFiredState;

    [Header("FMODBusPaths")]
    public string MasterBusString;
    public string InGameSFXBusString;
    public string EnvironmentString;
    public string SFXBusString;
    public string UISFXBusString;
    public string UINonMapSFXBusString;
    public string MusicBusString;

    public FMOD.Studio.Bus MasterBus;
    public FMOD.Studio.Bus IngameSFXBus;
    public FMOD.Studio.Bus EnvironmentBus;
    public FMOD.Studio.Bus SFXBus;
    public FMOD.Studio.Bus UISFXBus;
    public FMOD.Studio.Bus UINonMapSFXBus;
    public FMOD.Studio.Bus MusicBus;

    [Header("SoundEventPaths")]
    public string ReadySoundEventPath;
    public string OpponentReadySoundEventPath;
    public string ExecuteStepChangePath;
    public string PlanningSlideInPath;


    public enum UIEffectsFired
    {
        planning,
        enemyReadyFired,
        readyFired,
        interruptFired,
        attackFired,
        moveFired,
        skillshotFired,
        gameOverFired
    }
}
