using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Unit_BaseDataSet : MonoBehaviour
{
    [Range (0,20)]
    public uint VisionRange;
    public uint BaseHealth;
    public uint EnergyUpkeep;
    public uint EnergyIncome;
    public uint SpawnCost;
    public uint MovementRange;
    public bool IsHero;
}
