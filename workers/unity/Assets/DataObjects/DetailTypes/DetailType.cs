using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class DetailType : ScriptableObject
{
    public int DetailObjectSpawnProbability;
    public int RandomDetailRotationIncrement = 60;
    public Vector2 DetailObjectIndexRange;
    public Vector2 DetailObjectAmount;
    public Vector2 DetailObjectRange;
    public bool UseSpawnAngle;
}
