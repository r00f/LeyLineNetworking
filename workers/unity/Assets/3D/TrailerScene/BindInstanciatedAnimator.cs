using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using System.Linq;
using UnityEngine.Playables;

public class BindInstanciatedAnimator : MonoBehaviour
{
    public PlayableDirector PlayableDirector; 
    public TimelineAsset TimeLineAsset;
    HeroIdentifier hero;
    TrackAsset AnimationTrack;
    Animator AnimatorToBind;
    GameObject egg;

    // Start is called before the first frame update
    void Start()
    {
        AnimationTrack = TimeLineAsset.GetOutputTracks().FirstOrDefault(t => t.name == "KCAnimTrack");
    }

    // Update is called once per frame
    void Update()
    {
        if(FindObjectOfType<HeroIdentifier>() && !hero)
        {
            hero = FindObjectOfType<HeroIdentifier>();
            AnimatorToBind = hero.transform.GetComponentInChildren<Animator>();
            egg = hero.GetComponent<AnimatorComponent>().CharacterEffects[0].gameObject;
            egg.SetActive(true);
        }

        if(AnimatorToBind && !PlayableDirector.GetGenericBinding(AnimationTrack))
        {
            PlayableDirector.SetGenericBinding(AnimationTrack, AnimatorToBind);
        }
    }
}
