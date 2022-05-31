using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class Rope : MonoBehaviour
{
    [HideInInspector]
    public bool RopeSlamOneTime;
    [HideInInspector]
    public float FriendlyRopeEndFillAmount;
    [HideInInspector]
    public float EnemyRopeEndFillAmount;
    [HideInInspector]
    public float RopeEndsLerpTime;

    public float RopeFillsEndDist;
    public float ReadySwooshFadeOutSpeed;
    public float RopeEndFadeOutSpeed;
    public float ReadyImpulseLerpSpeed;
    public float RopeEndLerpSpeed;
    public Text RopeTimeText;
    public Image FriendlyRope;
    public Image EnemyRope;
    public FillBarParticleComponent FriendlyRopeBarParticle;
    public FillBarParticleComponent EnemyRopeBarParticle;
    public ParticleSystem FriendlyReadyBurstPS;
    public ParticleSystem EnemyReadyBurstPS;

}
