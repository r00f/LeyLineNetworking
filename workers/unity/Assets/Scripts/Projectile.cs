using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unit;
using Improbable;

public class Projectile : MonoBehaviour
{

    public List<Vector3> TravellingCurve;
    public float TravellingSpeed;
    public float MaxHeight;


    public ParticleSystem BodyParticleSystem;
    public ParticleSystem TrailParticleSystem;
    public ParticleSystem ExplosionParticleSystem;


    [HideInInspector]
    public int CurrentTargetId = 0;
    [HideInInspector]
    public EffectTypeEnum EffectOnDetonation;
    [HideInInspector]
    public HashSet<Vector3f> CoordinatesToTrigger;
    [HideInInspector]
    public bool IsTravelling;
    [HideInInspector]
    public float MovementPercentage;

}
