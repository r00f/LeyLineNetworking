using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.UI;

public class SetPortraitClip : MonoBehaviour
{
    public AnimatorOverrideController AnimatorOverrideController;
    public Animator Animator;
    public Image GenericImage;
    //public Image PlayerColorImage;
   
    public void Start()
    {
        Animator.runtimeAnimatorController = AnimatorOverrideController;
    }
}
