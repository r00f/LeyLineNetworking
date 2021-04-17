using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ManalithClientData : MonoBehaviour
{
    public long ManalithEntityID = 0;
    public string NodeName;
    public MiniMapTile MiniMapTilePrefab;
    [HideInInspector]
    public MiniMapTile MiniMapTileInstance;
    [HideInInspector]
    public MiniMapTile BigMapTileInstance;
    public TeamColorMeshes ManalithUnitTeamColorMeshes;
}
