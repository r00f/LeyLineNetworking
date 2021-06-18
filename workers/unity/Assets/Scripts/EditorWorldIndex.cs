using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorWorldIndex : MonoBehaviour
{
    public uint WorldIndex;
    public Transform centerCellTransform;
    public List<MapUnitDataObject> Maps = new List<MapUnitDataObject>();
    public List<ChampionUnitsDataObject> Champions = new List<ChampionUnitsDataObject>();

}
