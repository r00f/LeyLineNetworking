using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ManalithClientData : MonoBehaviour
{
    public MeshRenderer ManalithHoveredMesh;
    public ManalithInfoComponent IngameIconRef { get; set; }
    public long ManalithEntityID = 0;
    public Transform UIElementTransform;
    public Vector3 WorldPos;
    public Image TooltipBackgroundImage;
    public string NodeName;
    public MiniMapTile MiniMapTilePrefab;
    [HideInInspector]
    public MiniMapTile MiniMapTileInstance;
    [HideInInspector]
    public MiniMapTile BigMapTileInstance;
    public TeamColorMeshes ManalithUnitTeamColorMeshes;
}
