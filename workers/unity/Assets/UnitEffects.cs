using Generic;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

public class UnitEffects : MonoBehaviour
{
    public GameObject BloodParticleSystem;
    public GameObject BodyPartBloodParticleSystem;
    public GameObject DefenseParticleSystem;

    [Header("GetHit")]
    public Vector3 HitPosition;
    public Action Action;
    public uint CurrentArmor;
    public uint CurrentHealth;
    public Vector3f LastStationaryCoordinate;
    [SerializeField]
    public bool ActionEffectTrigger = false;

}
