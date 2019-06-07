using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    [SerializeField]
    SphereCollider WeaponCollider;

    public void EnableWeaponCollider()
    {
        WeaponCollider.enabled = true;
    }

    public void DisableWeaponCollider()
    {
        WeaponCollider.enabled = false;
    }
}
