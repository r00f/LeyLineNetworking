using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHeadUIReferences : MonoBehaviour
{
    public float HealthBarYOffset;
    public UnitHeadUI UnitHeadUIPrefab;
    public HealthBar UnitHeadHealthBarPrefab;
    [HideInInspector]
    public UnitHeadUI UnitHeadUIInstance;
    [HideInInspector]
    public HealthBar UnitHeadHealthBarInstance;
}
