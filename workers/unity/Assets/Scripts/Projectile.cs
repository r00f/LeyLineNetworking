using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unit;
using Improbable;

public class Projectile : MonoBehaviour
{

    public List<Vector3> travellingCurve;
    public float travellingSpeed;
    public float maxheight;
    [HideInInspector]
    public int currentTargetId = 0;
    public EffectTypeEnum effectonDetonation { get; set; }
    public HashSet<Vector3f> CoordinatesToTrigger { get; set; }
    public bool isTravelling { get; set; }
}
