using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnStatePanel : MonoBehaviour
{
    public Animator ExecuteStepPanelAnimator;
    public List<HoveredHandler> TurnStepHoveredHandlers = new List<HoveredHandler>();
    public float RopeFillsEndDist;
    public float ReadySwooshFadeOutSpeed;
    public float RopeEndFadeOutSpeed;
    public float ReadyImpulseLerpSpeed;
    public float RopeEndLerpSpeed;
    public Text RopeTimeText;
    public Image FriendlyReadyDot;
    public Image EnemyReadyDot;
    public Image FriendlyRope;
    public Image EnemyRope;
    public Text TurnStateText;
    public Animator GOButtonAnimator;
    public float CacelGraceTime;
    public float SlidersOpenMultiplier;
    public GOButton GOButtonScript;
    public StudioEventEmitter RopeLoopEmitter;
}
