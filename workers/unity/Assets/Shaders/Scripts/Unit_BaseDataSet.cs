using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeyLineHybridECS;

public class Unit_BaseDataSet : MonoBehaviour
{
    [Range(0, 20)]
    public uint VisionRange;
    public uint BaseHealth;
    public uint EnergyIncome;
    public ECSAction BasicMove;
    public ECSAction BasicAttack;
    public GameObject SelectUnitButtonInstance;
    public bool IsHero;
    public bool UIInitialized;
    public uint UnitTypeId;
    public Sprite UnitTypeSprite;
    public List<ECSAction> Actions;
    public List<ECSAction> SpawnActions;

}
