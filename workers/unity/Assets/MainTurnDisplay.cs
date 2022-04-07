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
        float FadeDuration;
        [SerializeField]
        float TextDisplayDuration;
        [SerializeField]
        float ScrollSpeed;
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
                    //special planning case, display energy gained / turn X text, reset top if at end.
                }
                else if (CurrentStepID > 0 && CurrentStepID < 5)
                {
                Debug.Log(TurnstateDisplayRect.anchoredPosition.x + " < X Pos // Comparison Value > " + (100 - ((int)CurrentStepID * 320)));
                    // Move scrolling bar to desired position
                    if (TurnstateDisplayRect.anchoredPosition.x > (100 - ((int)CurrentStepID * 320)))
                    {
                    Debug.Log("It should move buut...");
                        if (TurnstateDisplayRect.anchoredPosition.x - (delta * ScrollSpeed) < (100 - ((int)CurrentStepID * 320)))
                        {
                            TurnstateDisplayRect.anchoredPosition = new Vector2(100 - ((int)CurrentStepID * 320), TurnstateDisplayRect.anchoredPosition.y);

                        }
                        else {
                            TurnstateDisplayRect.anchoredPosition = new Vector2(TurnstateDisplayRect.anchoredPosition.x - (delta * ScrollSpeed), TurnstateDisplayRect.anchoredPosition.y);
                            Debug.Log("Vibin");
                        }
                    }
                    else if (CurrentTurnStepWaitTime > BaseTurnStepWaitTime)
                    {
                        // display text and enable colored symbol
                    }
                }
            }
        }

    }
