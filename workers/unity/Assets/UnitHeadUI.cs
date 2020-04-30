using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitHeadUI : MonoBehaviour
{
    public float DestroyWaitTime;
    public bool FlagForDestruction;
    public Text FloatHealthText;
    public Animator FloatHealthAnimator;
    public Animator ArmorAnimator;
    public float PlanningBufferTime;
    public GameObject DeathBlowImage;
    public GameObject ArmorPanel;
    public Text ArmorText;
    public HeadUILockedActionDisplay ActionDisplayPrefab;
    [HideInInspector]
    public HeadUILockedActionDisplay ActionDisplayInstance;
}
