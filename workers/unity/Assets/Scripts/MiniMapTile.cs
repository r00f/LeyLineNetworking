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
    public MapEffectComponent GetCapturedMapEffect;
    public int AddPingSize;
    public Vector2 DeathCrossSize;
    public bool EvenOutlineOffset = true;
    public RectTransform UnitPlayerColorSprite;
    public MapEffectComponent BecomeVisibleMapEffect;
    public UILineRenderer UILineRenderer;
}
