using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetectionComponent : MonoBehaviour
{
    public bool HasCollided;

    void OnCollisionEnter(Collision collision)
    {
        HasCollided = true;
    }

}
