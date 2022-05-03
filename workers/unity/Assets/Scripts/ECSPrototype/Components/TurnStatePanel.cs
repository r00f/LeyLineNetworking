using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnStatePanel : MonoBehaviour
{
    public Animator ExecuteStepPanelAnimator;
    public List<HoveredHandler> TurnStepHoveredHandlers = new List<HoveredHandler>();
    public Rope RopeComponent;
    public Image FriendlyReadyDot;
    public Image EnemyReadyDot;
    public Text TurnStateText;
    public Animator GOButtonAnimator;
    public float CacelGraceTime;
    public float SlidersOpenMultiplier;
    public GOButton GOButtonScript;
}
