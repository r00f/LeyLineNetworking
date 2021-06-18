using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
[System.Serializable]
public class ChampionUnitsDataObject : ScriptableObject
{
    public string Name;
    public UnitDataSet ChampionUnitFab;
    public List<UnitDataSet> UnitFabs;
}
