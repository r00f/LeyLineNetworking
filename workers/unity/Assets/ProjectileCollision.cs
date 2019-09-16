using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    [SerializeField]
    Projectile projectile;

    private void OnTriggerEnter(Collider other)
    {
        if (projectile.DestinationReached && other.transform == transform.root)
        {
            Debug.Log("Entering own Trigger after reaching dest");
            projectile.FlagForDestruction = true;
        }
    }
}
