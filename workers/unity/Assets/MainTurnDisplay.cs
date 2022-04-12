using UnityEngine;
using UnityEngine.UI;



public class MainTurnDisplay : MonoBehaviour
{
    [SerializeField]
    MovingStepsData turnstateDisplay;
    [SerializeField]
    Text currentTurnstepText;
    [SerializeField]
    Text turnXLargeText;
    [SerializeField]
    Text energyText;
    [SerializeField]
    Text energyNumber;
    [SerializeField]
    Image energyIcon;
    [SerializeField]
    float fadeDuration;
    [SerializeField]
    float scrollSpeed;
    [SerializeField]
    float turnXFadeDuration;
    [HideInInspector]
    public uint EnergyGained = 0;
    [HideInInspector]
    public uint CurrentStepID;
    [HideInInspector]
    public Color ColorizeText;
    [HideInInspector]
    public string StateName;
    [HideInInspector]
    public bool InAnimation = false;
    RectTransform TurnstateDisplayRect;
    public bool StepIsActive = false;
    float timer;

    void Start()
    {
        TurnstateDisplayRect = turnstateDisplay.gameObject.GetComponent<RectTransform>();
    }


    // Update is called once per frame
    void Update()
    {
        if (InAnimation)
        {
            float delta = Time.deltaTime;
            if (CurrentStepID == 0)
            {
                //reset scrolling bar at the end
                if (TurnstateDisplayRect.anchoredPosition.x != 100)
                {
                    if (TurnstateDisplayRect.anchoredPosition.x - delta * scrollSpeed <= 100 - 5 * 320)
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(100, TurnstateDisplayRect.anchoredPosition.y);

                    }
                    else if (TurnstateDisplayRect.anchoredPosition.x - delta * scrollSpeed != 100 - 5 * 320)
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(TurnstateDisplayRect.anchoredPosition.x - (delta * scrollSpeed), TurnstateDisplayRect.anchoredPosition.y);
                    }
                }
                //if scrolling is finished
                else
                {
                    if (turnXLargeText.text != StateName)
                    {
                        turnXLargeText.text = StateName;
                        turnXLargeText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        energyText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        energyNumber.text = "+ " + EnergyGained.ToString();
                        energyNumber.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        energyIcon.color = Color.white;
                        timer = 0f;
                    }
                    if (timer < 1f)
                    {
                        turnXLargeText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        energyText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        energyNumber.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        energyIcon.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), timer);
                        turnstateDisplay.uncoloredImages[0].color = Color.Lerp(new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 1), new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 0), timer);
                        timer += delta / turnXFadeDuration;
                    }
                }
            }
            else if (CurrentStepID > 0 && CurrentStepID < 5)
            {
                if (turnstateDisplay.uncoloredImages[0].color.a == 0) turnstateDisplay.uncoloredImages[0].color = new Color(turnstateDisplay.uncoloredImages[0].color.r, turnstateDisplay.uncoloredImages[0].color.g, turnstateDisplay.uncoloredImages[0].color.b, 1);
                // Move scrolling bar to desired position
                if (TurnstateDisplayRect.anchoredPosition.x > 100 - (int) CurrentStepID * 320)
                {
                    if (TurnstateDisplayRect.anchoredPosition.x - (delta * scrollSpeed) <= 100 - (int) CurrentStepID * 320)
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(100 - (int) CurrentStepID * 320, TurnstateDisplayRect.anchoredPosition.y);
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
                            currentTurnstepText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                            turnstateDisplay.coloredImages[(int)CurrentStepID - 1].color = new Color(turnstateDisplay.coloredImages[(int) CurrentStepID - 1].color.r, turnstateDisplay.coloredImages[(int) CurrentStepID - 1].color.g, turnstateDisplay.coloredImages[(int) CurrentStepID - 1].color.b, 1);

                        }
                    }
                }
                if(CurrentStepID == 1)
                {
                    if (turnstateDisplay.coloredImages[3].color.a != 0)
                    {
                        turnstateDisplay.coloredImages[3].color = new Color(turnstateDisplay.coloredImages[3].color.r, turnstateDisplay.coloredImages[3].color.g, turnstateDisplay.coloredImages[3].color.b, 0);
                    }
                }
                else if(turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color.a != 0)
                {
                    turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color = new Color(turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color.r, turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color.g, turnstateDisplay.coloredImages[(int) CurrentStepID - 2].color.b, 0);
                }
            }
            if (currentTurnstepText.color.a > 0)
            {
                if (currentTurnstepText.text != StateName /*|| new Color(currentTurnstepText.color.r, currentTurnstepText.color.g, currentTurnstepText.color.b, 1) != new Color(ColorizeText.a, ColorizeText.g, ColorizeText.b, 1)*/)
                {
                    currentTurnstepText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0);
                }
                else
                {
                    currentTurnstepText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                    timer += delta / fadeDuration;
                }
            }
        }
    }
}
