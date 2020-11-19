using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class MiniMapTile : MonoBehaviour
{
    public Image TileImage;
    public RectTransform TileRect;
    public Color TileInvisibleColor;
    [HideInInspector]
    public Color TileColor;
    public float SmallTileScale;
    public float BigTileScale;

    [Header("Optional Fields")]
    public RectTransform UnitPlayerColorSprite;
    public RectTransform DeathCrossPrefab;
    public ParticleSystem UnitBecomeVisiblePingPS;
    public UILineRenderer UILineRenderer;
}
