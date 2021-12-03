using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[ExecuteInEditMode]
public class WorldArrayBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    public EditorWorldIndex MapPrefab;
    public uint MapCount;
    public uint MapsPerRow;
    public uint MapPositionOffset;

    public void BuildMapArray()
    {
        var childs = transform.childCount;
        var columnCount = MapCount / MapsPerRow;

        for (int i = childs - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (uint i = 0; i < columnCount; i++)
        {
            for(uint y = 0; y < MapsPerRow; y++)
            {
                EditorWorldIndex map = (EditorWorldIndex) PrefabUtility.InstantiatePrefab(MapPrefab, transform);
                map.transform.position = new Vector3(MapPositionOffset * y, 0, MapPositionOffset * i);
                map.WorldIndex = (i * MapsPerRow) + y + 1;
            }
        }
    }
#endif
}
