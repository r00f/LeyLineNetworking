using UnityEngine;
using UnityEngine.UI;

public class MainTurnDisplay : MonoBehaviour
{
    [Header("TurnstateDisplay")]
    [SerializeField]
    Image screenGlowImage;
    [SerializeField]
    MovingStepsData turnstateDisplay;
    [SerializeField]
    Text currentTurnstepText;
    [SerializeField]
    Text turnXLargeText;
    [SerializeField]
    float fadeDuration;
    [SerializeField]
    float scrollSpeed;
    [SerializeField]
    float turnXFadeDuration;
    [Header("EnergyGain")]
    [SerializeField]
    Text energyText;
    [SerializeField]
    Text energyNumber;
    [SerializeField]
    Image energyIcon;
    [Header("BountyEnergy")]
    [SerializeField]
    Text bonusEnergyText;
    [SerializeField]
    Text bonusEnergyNumber;
    [SerializeField]
    Image bonusEnergyIcon;
    [HideInInspector]
    public uint EnergyGained = 0;
    [HideInInspector]
    public uint BonusEnergy = 0;
    [HideInInspector]
    public uint CurrentStepID;
    [HideInInspector]
    public Color ColorizeText;
    [HideInInspector]
    public string StateName;
    RectTransform TurnstateDisplayRect;
    public bool StepIsActive = false;
    public float timer;
    float startpos;
    float xposInterval;
    bool bonusgained = false;

    void Start()
    {
        TurnstateDisplayRect = turnstateDisplay.gameObject.GetComponent<RectTransform>();
        startpos = TurnstateDisplayRect.anchoredPosition.x;
        xposInterval = TurnstateDisplayRect.rect.width / 6;
    }

    // Update is called once per frame
    void Update()
    {
        float delta = Time.deltaTime;
        if (CurrentStepID == 0)
        {

            //reset scrolling bar at the end
            if (TurnstateDisplayRect.anchoredPosition.x != startpos)
            {
                if (TurnstateDisplayRect.anchoredPosition.x - delta * scrollSpeed <= startpos - 5 * xposInterval)
                {
                    TurnstateDisplayRect.anchoredPosition = new Vector2(startpos, TurnstateDisplayRect.anchoredPosition.y);

                }
                else if (TurnstateDisplayRect.anchoredPosition.x - delta * scrollSpeed != startpos - 5 * xposInterval)
                {
                    turnstateDisplay.PlanningOutline.color = Color.black;
                    TurnstateDisplayRect.anchoredPosition = new Vector2(TurnstateDisplayRect.anchoredPosition.x - (delta * scrollSpeed), TurnstateDisplayRect.anchoredPosition.y);
                }
            }
            //if scrolling is finished
            else
            {
                if (turnXLargeText.text != StateName)
                {
                    currentTurnstepText.text = "";
                    turnXLargeText.text = StateName;
                    turnXLargeText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                    energyText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                    energyNumber.text = "+ " + EnergyGained.ToString();
                    energyNumber.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                    energyIcon.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                    timer = 0f;
                    if (BonusEnergy > 0)
                    {
                        bonusEnergyIcon.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        bonusEnergyNumber.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        bonusEnergyNumber.text = "+" + BonusEnergy.ToString();
                        bonusEnergyText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        BonusEnergy = 0;
                        bonusgained = true;
                    }
                    else
                    {
                        bonusEnergyNumber.text = "+ 0";
                        bonusEnergyIcon.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0);
                        bonusEnergyNumber.color = new Color(1, 1, 1, 0);
                        bonusEnergyText.color = new Color(1, 1, 1, 0);
                        bonusgained = false;
                    }
                }
                if (timer < 1f)
                {
                    Color Lerpcolor = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);

                    turnXLargeText.color = Lerpcolor;
                    energyText.color = Lerpcolor;
                    energyNumber.color = Lerpcolor;
                    energyIcon.color = Lerpcolor;
                    if (bonusgained)
                    {
                        bonusEnergyText.color = Lerpcolor;
                        bonusEnergyNumber.color = Lerpcolor;
                        bonusEnergyIcon.color = Color.Lerp(new Color(bonusEnergyIcon.color.r, bonusEnergyIcon.color.g, bonusEnergyIcon.color.b, 1), new Color(bonusEnergyIcon.color.r, bonusEnergyIcon.color.g, bonusEnergyIcon.color.b, 0), timer);
                    }
                    turnstateDisplay.uncoloredImages[0].color = Color.Lerp(new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 1), new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 0), timer);
                    turnstateDisplay.PlanningOutline.color = Color.Lerp(Color.black, new Color(0, 0, 0, 0), timer);
                    timer += delta / turnXFadeDuration;
                }
            }
        }
        else if (CurrentStepID > 0 && CurrentStepID < 5)
        {
            if (turnstateDisplay.uncoloredImages[0].color.a < 1)
            {
                turnstateDisplay.PlanningOutline.color = Color.black;
                turnstateDisplay.uncoloredImages[0].color = new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 1);
            }

            // Move scrolling bar to desired position
            if (TurnstateDisplayRect.anchoredPosition.x > (startpos - ((int) CurrentStepID * xposInterval)+.05f))
            {
                if (TurnstateDisplayRect.anchoredPosition.x < (startpos - ((int) CurrentStepID * xposInterval) - (delta * scrollSpeed + .05f)))
                {
                    TurnstateDisplayRect.anchoredPosition = new Vector2 (startpos - (((int) CurrentStepID * xposInterval) - (delta * scrollSpeed + .1f)), TurnstateDisplayRect.anchoredPosition.y);
                }
                else
                {
                    TurnstateDisplayRect.anchoredPosition = new Vector2(TurnstateDisplayRect.anchoredPosition.x - delta * scrollSpeed, TurnstateDisplayRect.anchoredPosition.y);
                }
            }
            else
            {
                if (StepIsActive)
                {
                    if (currentTurnstepText.text != StateName)
                    {
                        timer = 0f;
                        currentTurnstepText.text = StateName;
                        screenGlowImage.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        currentTurnstepText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        turnstateDisplay.coloredImages[(int) CurrentStepID - 1].color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                    }
                }
            }
            if (CurrentStepID == 1)
            {
                if (turnstateDisplay.coloredImages[3].color.a > 0.5)
                {
                    turnstateDisplay.coloredImages[3].color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
            else if (turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color.a > 0.5)
            {
                turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
        else if (CurrentStepID == 5)
        {
            Color Lerpcolor = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0);

            turnXLargeText.color = Lerpcolor;
            energyText.color = Lerpcolor;
            energyNumber.color = Lerpcolor;
            energyIcon.color = Lerpcolor;
            if (bonusgained)
            {
                bonusEnergyText.color = Lerpcolor;
                bonusEnergyNumber.color = Lerpcolor;
                bonusEnergyIcon.color = new Color(bonusEnergyIcon.color.r, bonusEnergyIcon.color.g, bonusEnergyIcon.color.b, 0);
            }
            turnstateDisplay.uncoloredImages[0].color = new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 0);
            turnstateDisplay.PlanningOutline.color = new Color(0, 0, 0, 0);

        }
        if (currentTurnstepText.color.a > 0)
        {
            if (currentTurnstepText.text != StateName /*|| new Color(currentTurnstepText.color.r, currentTurnstepText.color.g, currentTurnstepText.color.b, 1) != new Color(ColorizeText.a, ColorizeText.g, ColorizeText.b, 1)*/)
            {
                screenGlowImage.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0);
                currentTurnstepText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0);
            }
            else
            {
                currentTurnstepText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                screenGlowImage.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                timer += delta / fadeDuration;
            }
        }
    }
}
