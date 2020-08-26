using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIReferences : MonoBehaviour
{
    public Canvas Canvas;

    public Animator SlideOutUIAnimator;
    public GameObject UIMainPanel;

    [Header("EscapeMenu")]
    public EscapeMenu EscapeMenu;

    [Header("Startup")]
    public float StartUpWaitTime = 3f;
    public GameObject StartupPanel;
    public GameObject MatchReadyPanel;

    [Header("TopLeftHeroPanel")]
    public Image ManaConnector;
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



    [Header("UnitPortrait")]
    public GameObject InfoEnabledPanel;
    public Image PortraitPlayerColor;
    public SetPortraitClip AnimatedPortrait;

    [Header("UnitStats")]
    public Text UnitStats;

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

    [Header("ReadyPanel")]
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
    [HideInInspector]
    public Color FriendlyColor;
    [HideInInspector]
    public Color EnemyColor;
    [HideInInspector]
    public string CurrentStateString;

    [Header("ParticleSystems")]
    public List<ParticleSystem> CowExhaleParticleSystems;
    public FillBarParticleComponent FriendlyFillBarParticle;
    public FillBarParticleComponent EnemyFillBarParticle;
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

    public enum UIEffectsFired
    {
        planning,
        readyFired,
        interruptFired,
        attackFired,
        moveFired,
        skillshotFired
    }
}
