using UnityEngine;
using System.Collections.Generic;

public class GarbageCollectorComponent : MonoBehaviour
{
    public float MaxSinkTime;
    public float CurrentSinkTime;
    public float SinkSpeed;
    public List<GameObject> GarbageObjects;
    public List<Rigidbody> GarbageRigidbodies;

}
