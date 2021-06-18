using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
[System.Serializable]
public class MapUnitDataObject : ScriptableObject
{
    public string Name;
    public List<UnitDataSet> ManalithUnits;
    public List<UnitDataSet> NeutralUnits;
}
