using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unit;
using Improbable;
using UnityEngine.Animations;
using Generic;
using FMODUnity;
using Unity.Entities;

public class Projectile : MonoBehaviour
{
    //currently only used for axa shield
    public GameObject DestinationObjectPrefab;
    public GameObject DestinationExplosionPrefab;
    public Transform ExplosionSpawnTransform;
    public CollisionDetectionComponent CollisionDetection;

    public uint TravellingCurveCutOff;
    public float TargetYOffset;
    public bool ArriveInstantly;
    public float MovementDelay;
    public float DestroyAfterSeconds;
    public bool DestroyAtDestination;


    [Header ("Transform Projectile")]
    public Transform TransformToMove;
    public Transform TransformToRotate;
    public Transform PhysicsExplosionOrigin;
    public float TravellingSpeed;
    public float MaxHeight;
    public float Acceleration;

    public int DegreesPerSecond;
    public int AxaShieldOrbitCount;
    public List<GameObject> DisableAtDestinationObjects;
    public List<GameObject> DisableBeforeDestructionObjects;

    [Header("Physics Projectile")]
    public ForceMode LaunchForceMode;
    public List<Rigidbody> RigidbodiesToLaunch;
    //public Rigidbody ToungeEnd;
    //public Rigidbody ToungeBase;
    public int LaunchForce;

    [Header("Tounge")]
    public int ContractSpeed;
    public int ContractUpForce;
    //public Joint BaseJoint;
    public List<SpringJoint> SpringJoints;

    [HideInInspector]
    public bool Launched;

    public float ParticleSystemsStopWaitTime;
    public List<ParticleSystem> ParticleSystemsToStop;
    public ParticleSystem ExplosionParticleSystem;
    public StudioEventEmitter ExplosionEventEmitter;

    /*
    [Header("Impact Physics Explosion")]
    public float ExplosionWaitTime;
    public float ExplosionRadius;
    public int ExplosionForce;
    */

    //Internal vars

    [HideInInspector]
    public uint UnitFaction;

    [HideInInspector]
    public Action Action;
    [HideInInspector]
    public bool DestinationReached;
    [HideInInspector]
    public bool EffectTriggered;
    [HideInInspector]
    public bool FlagForDestruction;
    [HideInInspector]
    public long UnitId;
    [HideInInspector]
    public bool QueuedForDestruction;
    [HideInInspector]
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
