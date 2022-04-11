using UnityEngine;
using UnityEngine.UI;



public class MainTurnDisplay : MonoBehaviour
    {
        [SerializeField]
        MovingStepsData TurnstateDisplay;
        [SerializeField]
        Text CurrentTurnstepText;
        [SerializeField]
        Text TurnXLargeText;
        [SerializeField]
        Text EnergyText;
        [SerializeField]
        Text EnergyNumber;
        [SerializeField]
        Image EnergyIcon;
        [SerializeField]
        float FadeDuration;
        [SerializeField]
        float TextDisplayDuration;
        [SerializeField]
        float ScrollSpeed;
        [HideInInspector]
        public uint EnergyGained = 0;
        [HideInInspector]
        public uint CurrentStepID;
        [HideInInspector]
        public float BaseTurnStepWaitTime;
        [HideInInspector]
        public float CurrentTurnStepWaitTime;
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
            TurnstateDisplayRect = TurnstateDisplay.gameObject.GetComponent<RectTransform>();
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
                    if (TurnstateDisplayRect.anchoredPosition.x - delta * ScrollSpeed <= 100 - 5 * 320)
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(100, TurnstateDisplayRect.anchoredPosition.y);

                    }
                    else if (TurnstateDisplayRect.anchoredPosition.x - delta * ScrollSpeed != 100 - 5 * 320)
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(TurnstateDisplayRect.anchoredPosition.x - (delta * ScrollSpeed), TurnstateDisplayRect.anchoredPosition.y);
                    }
                }
                //if scrolling is finished
                else
                {
                    if(TurnXLargeText.text != StateName)
                    {
                        TurnXLargeText.text = StateName;
                        TurnXLargeText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        EnergyText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        EnergyNumber.text = EnergyGained.ToString();
                        EnergyNumber.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                        EnergyIcon.color = Color.white;
                        timer = 0f;
                    }
                    if (timer < 1f)
                    {
                        TurnXLargeText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        EnergyText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        EnergyNumber.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        EnergyIcon.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), timer);
                        timer += delta / 2*FadeDuration;
                    }
                }
            }
            else if (CurrentStepID > 0 && CurrentStepID < 5)

            {
                // Move scrolling bar to desired position
                if (TurnstateDisplayRect.anchoredPosition.x > 100 - (int) CurrentStepID * 320)
                {
                    if (TurnstateDisplayRect.anchoredPosition.x - (delta * ScrollSpeed) <= 100 - (int) CurrentStepID * 320)
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(100 - (int) CurrentStepID * 320, TurnstateDisplayRect.anchoredPosition.y);

                    }
                    else
                    {
                        TurnstateDisplayRect.anchoredPosition = new Vector2(TurnstateDisplayRect.anchoredPosition.x - delta * ScrollSpeed, TurnstateDisplayRect.anchoredPosition.y);
                    }
                }
                else if (StepIsActive)
                {
                    if (CurrentTurnstepText.text != StateName)
                    {
                        timer = 0f;
                        CurrentTurnstepText.text = StateName;
                        CurrentTurnstepText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1);
                    }
                    if (CurrentTurnstepText.color.a > 0)
                    {
                        CurrentTurnstepText.color = Color.Lerp(new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 1), new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0), timer);
                        timer += delta / FadeDuration;
                    }
                    else
                    {
                        StepIsActive = false;
                    }
                }
                else
                {
                    CurrentTurnstepText.color = new Color(ColorizeText.r, ColorizeText.g, ColorizeText.b, 0);
                }
            }
            }
        }
}
