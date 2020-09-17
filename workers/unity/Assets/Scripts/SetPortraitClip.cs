using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations;

public class SetPortraitClip : MonoBehaviour
{
    [SerializeField]
    public AnimationClip portraitAnimationClip;

    public bool OverrideAnimSet;

    protected AnimatorOverrideController animatorOverrideController;
    protected Animator animator;
   

    public void Start()
    {
        animator = GetComponent<Animator>();


        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = animatorOverrideController;

    }

    public void Update()
    {
        //Debug.Log(animatorOverrideController.animationClips[0].name);

        if (animatorOverrideController.animationClips[0].name != portraitAnimationClip.name)
        {
            //Debug.Log(animatorOverrideController.animationClips[0].name);
            //List<KeyValuePair<AnimationClip, AnimationClip>> anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            //anims.Add(new KeyValuePair<AnimationClip,AnimationClip>(animatorOverrideController.animationClips[0], portraitAnimationClip));

            animatorOverrideController["KingCroakPortrait"] = portraitAnimationClip;
            OverrideAnimSet = true;
            //Debug.Log(portraitAnimationClip.name);
            //var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            //anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animatorOverrideController.clips[0].originalClip, portraitAnimationClip));
            //animatorOverrideController.ApplyOverrides(anims);

            //Debug.Log(animatorOverrideController.clips[0].originalClip.name + " ," + animatorOverrideController.clips[0].overrideClip.name);
            //Debug.Log(animatorOverrideController.animationClips[0].name);
        }
    }
}
