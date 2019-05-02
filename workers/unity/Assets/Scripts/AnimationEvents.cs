using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    [SerializeField]
    SphereCollider WeaponCollider;

    [SerializeField]
    List<Rigidbody> RagdollRigidBodies;

    [SerializeField]
    Animator anim;

    [SerializeField]
    bool die;

    public void Start()
    {
        /*
        foreach (Rigidbody r in RagdollRigidBodies)
        {
            r.isKinematic = true;
        }
        */
    }

    public void Update()
    {
        if(die)
        {
            Die();
        }
    }

    public void Die()
    {
        anim.enabled = false;
        foreach (Rigidbody r in RagdollRigidBodies)
        {
            r.isKinematic = false;
        }
        die = false;
    }

    public void EnableWeaponCollider()
    {
        Debug.Log("EnableWeaponCollider");
        WeaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        Debug.Log("DisableWeaponCollider");
        WeaponCollider.enabled = false;
    }




}
