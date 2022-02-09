using Improbable;
using LeyLineHybridECS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generic;
using FMODUnity;

public class AnimatorComponent : MonoBehaviour
{

    public List<AnimStateEffectHandler> AnimStateEffectHandlers;
    public Transform WeaponTransform;
    public bool IsMoving;

    [Header("GenericAnimation")]
    [HideInInspector]
    public ECSAction CurrentLockedAction;

    public ECSAction CurrentPreviewAction;
    public Vector3 CurrentPreviewTarget;
    public int CurrentPreviewIndex;
    public bool ResumePreviewAnimation;

    public Animator Animator;
    public AnimationEvents AnimationEvents;
    [HideInInspector]
    public bool ExecuteTriggerSet;
    [HideInInspector]
    public bool InitialValuesSet;
    public Transform ProjectileSpawnOrigin;
    public List<GameObject> Visuals;
    public float EnableVisualsDelay;
    public List<GameObject> CharacterEffects;

    [Header("SoundFX")]
    public StudioEventEmitter VoiceEmitter;
    public StudioEventEmitter FootStempEmitter;

    [Header("Movement")]
    public int RotationSpeed;
    public Transform RotateTransform;
    [HideInInspector]
    public bool DestinationReachTriggerSet;
    [HideInInspector]
    public Vector3 RotationTarget;
    [HideInInspector]
    public Vector2 DestinationPosition;

    [Header("Death")]
    public bool Dead;
    [HideInInspector]
    public bool DeathEventTrigger;
    public List<GameObject> DisableOnDeathObjects;
    public List<Rigidbody> RagdollRigidBodies;
    public List<Transform> Props;

    public float DismemberPercentage;
    public List<Joint> DismemberJoints;

    [HideInInspector]
    public ParticleSystem DeathParticleSystem;
    [HideInInspector]
    public int DeathParticlesCount;
    [HideInInspector]
    public Transform DeathExplosionPos;
    [HideInInspector]
    public int DeathExplosionForce;
    [HideInInspector]
    public float DeathExplosionRadius;

}
