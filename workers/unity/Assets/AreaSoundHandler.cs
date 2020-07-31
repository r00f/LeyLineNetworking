using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class AreaSoundHandler : MonoBehaviour
{
    [SerializeField]
    StudioEventEmitter riverSoundEmitter;

    [SerializeField]
    List<StudioEventEmitter> areaEmitters;
    [SerializeField]
    List<string> areaTags;

    [SerializeField]
    int collisionCount;

    private void Update()
    {
        if(collisionCount == 0)
        {
            foreach(StudioEventEmitter e in areaEmitters)
            {
                e.enabled = false;
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        collisionCount++;

        for (int i = 0; i < areaEmitters.Count; i++)
        {
            if (other.CompareTag(areaTags[i]))
            {
                Debug.Log("Player entered area tagged with: " + areaTags[i] + ".");
                areaEmitters[i].enabled = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        collisionCount--;
    }
    

}
