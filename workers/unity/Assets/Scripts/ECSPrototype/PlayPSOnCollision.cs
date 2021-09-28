using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayPSOnCollision : MonoBehaviour
{
    public List<ParticleSystem> ParticleSystems;
    public bool HasCollided;
    Vector3 StartPosition;
    public Rigidbody RigidBody;

    private void Start()
    {
        HasCollided = false;
        StartPosition = transform.position;
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!HasCollided)
        {
            foreach(ParticleSystem p in ParticleSystems)
            {
                p.Play();

            }
            HasCollided = true;
        }
    }

    public void ResetCheese()
    {
        RigidBody.isKinematic = true;
        transform.position = StartPosition;
        HasCollided = false;
        RigidBody.isKinematic = false;
    }
}
