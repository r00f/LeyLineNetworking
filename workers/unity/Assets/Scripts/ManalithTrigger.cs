using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManalithTrigger : MonoBehaviour
{
    [SerializeField]
    ParticleSystem CirclePS;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);

        if(other.GetComponent<UnitCollisionHandler>())
        {
            Debug.Log("ManalithATTRACTTTTT");
            other.GetComponent<UnitCollisionHandler>().ParticleAttractor.AttractedPS = CirclePS;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ManalithOnCollisionEnter");

        if (collision.transform.GetComponent<UnitCollisionHandler>())
        {
            Debug.Log("ManalithATTRACTTTTT");
            collision.transform.GetComponent<UnitCollisionHandler>().ParticleAttractor.AttractedPS = CirclePS;
        }
    }
}
