using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeyLineHybridECS;

public class Unit_BaseDataSet : MonoBehaviour
{

    public string UnitName;
    [Range(0, 20)]
    public uint VisionRange;
    public uint BaseHealth;
    public uint EnergyBaseIncome;
    public uint EnergyIncome;
    [HideInInspector]
    public SelectUnitButton SelectUnitButtonInstance;
    public bool IsHero;
    [HideInInspector]
    public bool UIInitialized;
    public uint UnitTypeId;
    public Sprite UnitGroupSprite;
    public Sprite UnitSprite;
    public List<ECSAction> Actions;
    public List<ECSAction> SpawnActions;

}
