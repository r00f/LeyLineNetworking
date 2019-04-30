using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorComponent : MonoBehaviour
{
    [SerializeField]
    public Transform RotateTransform;
    [SerializeField]
    public Animator Animator;
    [SerializeField]
    public bool ExecuteTriggerSet;
    [SerializeField]
    public bool DestinationReachTriggerSet;
    public Vector3 DestinationPosition;
}
