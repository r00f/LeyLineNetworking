using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NeighbouredDetailType : DetailType
{
    public Vector2 TreeHeightMinMax;
    public Vector2 NeighbourIndexMinMax;
    public Vector2 NeighbourAmountMinMax;
    [Range(0, 100)]
    public int probabilityToSpawnNeighbourAsset;
}
