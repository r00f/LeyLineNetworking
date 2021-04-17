using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainType : ScriptableObject
{
    [Header("Mechanics Settings")]
    public string TerrainName;
    public int additionalCost;
    public bool obstructVision = false;
    public bool Walkable = true;

    [Header("TileColors")]
    public int MapCellIconIndex;
    public int MapCellColorIndex;
    public int textureIndex;

    [Header("Trees")]
    public Vector2 TreeHeightMinMax;
    public Vector2 TreeIndexMinMax;
    public Vector2 NeighbourIndexMinMax;
    public Vector2 NeighbourAmountMinMax;
    [Range(0, 100)]
    public int probabilityToSpawnNeighbourAsset;

    [Header ("Detail Objects")]
    public int RandomDetailRotationIncrement;
    public List<Vector2> DetailObjectIndexRanges;
    public List<Vector2> DetailObjectAmounts;
    public List<int> DetailObjectSpawnProbabilities;
    public List<Vector2> DetailObjectRanges;

    [Header("Height Offset")]
    [Range (-15.0f, 15.0f)]
    public float yOffset;
    [Range(-15.0f, 15.0f)]
    public float cellTerrainYOffset;
    [Range(-2.0f, 2.0f)]
    public float DetailObjectSpawnYOffset;
    public Vector2 DetailObjectSpawnYOffsetMinMax;

    public List<GameObject> AssetsToSpawn = new List<GameObject>();


}
