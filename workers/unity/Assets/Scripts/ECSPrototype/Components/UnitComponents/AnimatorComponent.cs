using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorComponent : MonoBehaviour
{
    [SerializeField]
    public Transform RotateTransform;
    [SerializeField]
    public Animator Animator;
    [SerializeField]
    public bool ExecuteTriggerSet;
    [SerializeField]
    public bool DestinationReachTriggerSet;
    public Vector3 DestinationPosition;
    public uint LastHealth;
    public bool TriggerEnter;
    [SerializeField]
    Collider WeaponCollider;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("DamageCollider") && other != WeaponCollider)
        {
            Debug.Log("GameObject with name: " + other.name + " enters the trigger");
            TriggerEnter = true;
        }
    }

}
