using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCollisionHandler : MonoBehaviour
{
    public ParticleAttractor ParticleAttractor;

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("UnitTriggerEnter");

        if (ParticleAttractor)
        {
            if (other.transform.GetComponent<ParticleSystem>())
            {
                //Debug.Log("PSFoundOnTrigger");
                ParticleAttractor.AttractedPS = other.transform.GetComponent<ParticleSystem>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (ParticleAttractor && ParticleAttractor.AttractedPS)
        {
            if (other.transform.GetComponent<ParticleSystem>())
            {
                //Debug.Log("RemovePSOnExit");
                ParticleAttractor.AttractedPS = null;
            }
        }
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("UnitCollisionEnter");
        if (ParticleAttractor)
        {
            if (collision.transform.GetComponent<ParticleSystem>())
            {
                Debug.Log("PSFoundOnCollision");
                ParticleAttractor.AttractedPS = collision.transform.GetComponent<ParticleSystem>();
            }
        }
    }
    */
}
