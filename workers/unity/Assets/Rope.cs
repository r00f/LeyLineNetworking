using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class Rope : MonoBehaviour
{
    public StudioEventEmitter RopeLoopEmitter;
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
