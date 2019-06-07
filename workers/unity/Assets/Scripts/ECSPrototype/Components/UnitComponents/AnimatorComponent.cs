using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorComponent : MonoBehaviour
{

    [Header("GenericAnimation")]
    public Animator Animator;
    public bool ExecuteTriggerSet;
    public bool InitialValuesSet;

    [Header("Movement")]
    public Transform RotateTransform;
    public bool DestinationReachTriggerSet;
    public Vector3 RotationTarget;
    public Vector3 DestinationPosition;
    
    [Header("GetHit")]
    public uint LastHealth;
    [SerializeField]
    public bool TriggerEnter;
    [SerializeField]
    Collider WeaponCollider;

    [Header("RagdollHandling")]
    public List<Rigidbody> RagdollRigidBodies;
    public List<Transform> Props;
    public bool die;

    public void Update()
    {
        if (die)
        {
            Die();
        }
    }

    public void Die()
    {
        //disable animator
        Animator.enabled = false;

        //move props out of skeleton
        foreach (Transform t in Props)
        {
            t.parent = transform;
        }

        //set all rigidbodies to non kinematic
        foreach (Rigidbody r in RagdollRigidBodies)
        {
            r.isKinematic = false;
        }

        die = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("DamageCollider") && other != WeaponCollider)
        {
            TriggerEnter = true;
        }
    }

}
