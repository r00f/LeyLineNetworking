using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsVisibleReferences : MonoBehaviour
{
    public Color InGameTileColor;
    public Color MapTileColor;
    public Collider Collider;
    public MeshRenderer MeshRenderer;
    public List<GameObject> GameObjects;
    public MiniMapTile MiniMapTilePrefab;
    [HideInInspector]
    public MiniMapTile MiniMapTileInstance;
    [HideInInspector]
    public MiniMapTile BigMapTileInstance;
}
