using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementAnimComponent : MonoBehaviour
{
    public List<Transform> Transforms;
    public Vector3 RandomizeAxis;
    public Vector3 RotationAxis;
    public int DegreesPerSecond;
    public Vector3 TranslationDistance;
    public float TranslationTime;
    public bool ReturnToOrigin;
    public bool Continuous;
}
