using Improbable;
using LeyLineHybridECS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generic;

public class AnimatorComponent : MonoBehaviour
{

    [Header("GenericAnimation")]
    public ECSAction CurrentLockedAction;
    public Animator Animator;
    public AnimationEvents AnimationEvents;
    public bool ExecuteTriggerSet;
    public bool InitialValuesSet;
    public Transform ProjectileSpawnOrigin;
    public List<GameObject> Visuals;
    public float EnableVisualsDelay;
    public List<GameObject> CharacterEffects;

    [Header("Movement")]
    public Vector3f LastStationaryCoordinate;
    public int RotationSpeed;
    public Transform RotateTransform;
    public bool DestinationReachTriggerSet;
    public Vector3 RotationTarget;
    public Vector3 DestinationPosition;
    
    [Header("GetHit")]
    public uint LastHealth;
    [SerializeField]
    public bool ActionEffectTrigger = false;


    [Header("Death")]
    public bool Dead;
    public List<GameObject> ObjectsToDisable;
    public List<Rigidbody> RagdollRigidBodies;
    public List<Transform> Props;
    public ParticleSystem DeathParticleSystem;
    public int DeathParticlesCount;
    public Transform DeathExplosionPos;
    public int DeathExplosionForce;
    public float DeathExplosionRadius;

}
