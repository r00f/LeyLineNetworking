using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    [SerializeField]
    SphereCollider WeaponCollider;

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
