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
    public Dictionary<Action, Vector3> GetHitEffects = new Dictionary<Action, Vector3>();
    //public List<GetHitEffect> GetHitEffects;
    public Vector3 HitPosition;
    public uint CurrentArmor;
    public uint CurrentHealth;
    public Vector3f LastStationaryCoordinate;

}
