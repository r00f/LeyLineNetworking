using Generic;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

public class UnitEffects : MonoBehaviour
{
    public bool DisplayDeathSkull;
    public GameObject BloodParticleSystem;
    public GameObject BodyPartBloodParticleSystem;
    public GameObject DefenseParticleSystem;
    public GameObject ShieldBrakeParticleSystem;
    public ParticleSystem SecondWindParticleSystemInstance;
    public AxaShield AxaShield;

    [Header("GetHit")]
    public Vector3 AttackPosition;
    public Vector3 HitPosition;
    public Action Action;
    public uint CurrentArmor;
    public uint CurrentHealth;
    public Vector3f LastStationaryCoordinate;
    [SerializeField]
    public bool ActionEffectTrigger = false;

}
