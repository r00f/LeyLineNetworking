using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitHeadUI : MonoBehaviour
{
    [HideInInspector]
    public bool FlagForDestruction;
    public float DestroyWaitTime;

    public Text FloatHealthText;
    public Animator FloatHealthAnimator;
    public Animator ArmorAnimator;
    public Animator DeathCrossAnimator;
    public float PlanningBufferTime;
    public GameObject DeathBlowImage;
    public GameObject ArmorPanel;
    public Text ArmorText;
    public HeadUILockedActionDisplay ActionDisplay;
    public HealthBar HealthBar;
    public Text EnergyGainText;

}
