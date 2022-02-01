using Generic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu, System.Serializable]
public class Settings : ScriptableObject
{
    public int CellHighlighterLayerOverride;
    public uint ForcePlayerFaction;
    public List<Color> FactionColors;
    public List<Color> FactionIncomeColors;
    public List<Color> FactionMapColors;
    public List<Color> MapCellColors;
    public List<Color> TurnStepColors;
    public List<Color> TurnStepBgColors;
    public List<Color> TurnStepLineColors;

    public GameObject TempleValleyClientMap;
    public Color UINeutralColor;
    public Material ShadowMarkerMat;
    public Material TargetCellMat;
    public Mesh ShadowMarkerMesh;
    public Mesh HighlightCellMesh;
    public MiniMapTile MapCellTile;
    public List<Texture2D> ParialColorMasks;
    public GameObject ExplosionDebugSphere;

    [Header("Snapshot Settings")]
    public uint MapCount;
    public uint MapsPerRow;
    public uint MapOffset;
    public Vector2f MapGridCenterOffset;

    [Header("Gameplay Settings")]
    public float MinimumExecuteTime;
    public float RopeTime;
    public LayerMask MouseRayCastLayerMask;
    
}
