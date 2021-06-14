using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LeyLineHybridECS;

public class UnitDataSet : MonoBehaviour
{

    public string UnitName;
    [TextArea]
    public string UnitDescription;
    [Range(0, 20)]
    public uint VisionRange;
    public uint BaseHealth;
    //public uint EnergyBaseIncome;
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
