using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu, System.Serializable]
public class Settings : ScriptableObject
{
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
    public Material CellMat;
    public Mesh CellMesh;
    //public Mesh TestMesh;
    //public Material TestMat;
    public List<Texture2D> ParialColorMasks;
    //public GameObject MouseClickPS;
    public GameObject ExplosionDebugSphere;

    [Header("Snapshot Settings")]
    public uint MapCount;
    public uint MapsPerRow;
    public uint MapOffset;

    [Header("Gameplay Settings")]
    public float MinimumExecuteTime;
    public float RopeTime;
    public LayerMask MouseRayCastLayerMask;
    
}
