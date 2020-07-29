using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainType : ScriptableObject
{
    public int MapCellIconIndex;
    public int MapCellColorIndex;
    //public bool spawnTree;
    //public int treeIndex;
    //public float detailSpawnPercentage;
    //public int detailIndex;
    public int RandomTreeRotationIncrement;
    //public bool RandomTreeRotation;
    public int textureIndex;
    public Vector2 TreeHeightMinMax;
    public List<Vector2> DetailObjectIndexRanges;
    public List<Vector2> DetailObjectAmounts;
    public List<int> DetailObjectSpawnProbabilities;
    public Vector2 TreeIndexMinMax;
    public Vector2 GrassAmountMinMax;
    public Vector2 NeighbourIndexMinMax;
    public Vector2 NeighbourAmountMinMax;
    public string terrain_Name;
    public int additionalCost;
    [Range (-15.0f, 15.0f)]
    public float yOffset;
    public bool Walkable = true;
    public bool spawnAllAssets = false;
    [Range(0, 100)]
    public int probabilityToSpawnNeighbourAsset;
    [Range(0, 100)]
    public int probabilityToSpawnAsset;
    public bool obstructVision = false;
    public List<GameObject> assets_to_Spawn = new List<GameObject>();
}
