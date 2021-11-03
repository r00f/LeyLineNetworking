using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHeadUIReferences : MonoBehaviour
{
    public uint IncomingDamage;
    public uint IncomingArmor;
    public float HealthBarYOffset;
    public UnitHeadUI UnitHeadUIPrefab;
    public HealthBar UnitHeadHealthBarPrefab;
    [HideInInspector]
    public UnitHeadUI UnitHeadUIInstance;
    [HideInInspector]
    public HealthBar UnitHeadHealthBarInstance;
    public float HealthTextDelay;
}
