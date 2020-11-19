using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIReferences : MonoBehaviour
{
    public Canvas Canvas;

    public Animator SlideOutUIAnimator;

    public bool OpponentReady;
    public bool UIActive;

    [Header("MenuPanels")]
    public GameObject UIMainPanel;
    public GameObject HelpPanel;
    public GameObject SkillTreePanel;

    [Header("MenuButtons")]
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
    public Image HeroEnergyIncomeFill;
    public Image HeroCurrentEnergyFill;
    public Text CurrentMaxEnergyText;
    public Text TotalEnergyIncomeText;

    [Header("InGameUI")]
    public GameObject IngameUIPanel;
    public GameObject HealthBarsPanel;
    public GameObject ManalithInfoPanel;
    public GameObject ActionEffectUIPanel;

    [Header("UnitPortrait")]
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

    [Header("Actions")]
    public List<ActionButton> Actions;
    public List<ActionButton> SpawnActions;
    public SwapActionGroupButton SwapActionButton;
    public GameObject ActionButtonGroup;
    public GameObject SpawnButtonGroup;

    [Header("SelectedActionToolTip")]
    public GameObject SAInfoPanel;
    public Text SAActionName;
    public Text SAActionDescription;
    public Text SAEnergyText;
    public Text SACooldownText;
    public Image SAExecuteStepIcon;

    [Header("ToolTip")]
    public GameObject TTPanel;
    public RectTransform TTRect;
    public Text TTActionName;
    public Text TTActionDescription;
    public Text TTActionCost;
    public Image TTExecuteStepImage;

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

    [Header("ManaLithUI")]
    public ManalithInfoComponent ManalithIconPrefab;
    public ManalithTooltip ManalithToolTipFab;

    [Header("ReadyPanel")]
    public float RopeFillsEndDist;
    public float ReadySwooshFadeOutSpeed;
    public float RopeEndFadeOutSpeed;
    public float ReadyImpulseLerpSpeed;
    public float RopeEndLerpSpeed;
    public Text RopeTimeText;
    public Image FriendlyReadyDot;
    public Image EnemyReadyDot;
    public Image FriendlyReadySwoosh;
    public Image EnemyReadySwoosh;
    public Image FriendlyRope;
    public Image EnemyRope;
    public Text TurnStateText;
    public Animator GOButtonAnimator;
    public float CacelGraceTime;
    public GOButton GOButtonScript;
    public StudioEventEmitter RopeLoopEmitter;

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


    [Header("ParticleSystems")]
    public List<ParticleSystem> CowExhaleParticleSystems;
    public FillBarParticleComponent FriendlyReadySwooshParticle;
    public FillBarParticleComponent EnemyReadySwooshParticle;
    public FillBarParticleComponent FriendlyRopeBarParticle;
    public FillBarParticleComponent EnemyRopeBarParticle;
    public ParticleSystem FriendlyReadyBurstPS;
    public ParticleSystem EnemyReadyBurstPS;

    [Header("TurnWheel")]
    public Animator TurnWheelAnimator;
    public float ReadyOutSpeed;
    public float ReadyInSpeed;
    public ReadySlider EnemyReadySlider;
    public ReadySlider FriendlyReadySlider;
    public List<Image> SmallWheelColoredParts;
    public List<Image> BigWheelColoredParts;
    public List<Image> TurnStepFlares;
    public UIEffectsFired CurrentEffectsFiredState;

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
        skillshotFired
    }
}
