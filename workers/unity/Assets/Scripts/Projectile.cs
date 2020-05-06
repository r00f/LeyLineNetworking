﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unit;
using Improbable;
using UnityEngine.Animations;
using Generic;
using FMODUnity;

public class Projectile : MonoBehaviour
{
    /*
    public uint ArmorAmount;
    public uint HealthAmount;
    */

    public Action Action;
    public GameObject DestinationObjectPrefab;

    public bool DestroyAtDestination;
    public bool DestinationReached;
    public bool EffectTriggered;
    public bool FlagForDestruction;
    public bool QueuedForDestruction;
    public bool ArriveInstantly;

    public long UnitId;

    public Transform TransformToMove;
    public Transform TransformToRotate;
    public Transform PhysicsExplosionOrigin;
    public float TravellingSpeed;
    public float MaxHeight;
    public float MovementDelay;
    public float Acceleration;
    public float TargetYOffset;
    public int DegreesPerSecond;
    public int AxaShieldOrbitCount;
    public List<GameObject> DisableAtDestinationObjects;
    public List<GameObject> DisableBeforeDestructionObjects;

    [Header("Tounge")]
    public Rigidbody ToungeEnd;
    public Rigidbody ToungeBase;
    public int LaunchForce;
    public int ContractSpeed;
    public int ContractUpForce;
    public bool Launched;
    public Joint BaseJoint;
    public List<SpringJoint> SpringJoints;
    public float DestroyAfterSeconds;

    /*
    [Header("Impact Physics Explosion")]
    public float ExplosionWaitTime;
    public float ExplosionRadius;
    public int ExplosionForce;
    */

    public List<ParticleSystem> ParticleSystemsToStop;
    public ParticleSystem ExplosionParticleSystem;
    public StudioEventEmitter ExplosionEventEmitter;
    public bool ParentExplosionToWorld;


    public Transform SpawnTransform;
    [HideInInspector]
    public List<Vector3> TravellingCurve;
    [HideInInspector]
    public int CurrentTargetId = 0;
    [HideInInspector]
    public bool IsTravelling;
    [HideInInspector]
    public float MovementPercentage;

}
