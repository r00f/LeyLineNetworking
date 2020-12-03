using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using FMODUnity;

public class MiniMapTile : MonoBehaviour
{
    public Image TileImage;
    public RectTransform TileRect;
    public Color TileInvisibleColor;
    [HideInInspector]
    public Color TileColor;
    public bool EmitSoundEffect;

    [Header("Optional Fields")]
    public MapEffectComponent DeathBlowMapEffect;
    //public RectTransform DeathBlow;
    public Vector2 DeathCrossSize;
    public bool EvenOutlineOffset = true;
    public RectTransform DarknessTile;
    public float DarknessAlphaDefault;
    public Image DarknessTileImage;
    public RectTransform UnitPlayerColorSprite;
    //public RectTransform DeathCrossPrefab;
    public MapEffectComponent BecomeVisibleMapEffect;
    public UILineRenderer UILineRenderer;
}
