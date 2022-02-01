using System.Collections.Generic;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    public int CurrentMouseClickIndex;
    public List<SFXComponent> MouseClickSFXComponents;

    public float RangeLineYOffset;
    public List<HexEdgePositionPair> Edges = new List<HexEdgePositionPair>();
    public List<LineRenderer> RangeOutlineRenderers = new List<LineRenderer>();
    public List<HexOutlineShape> Shapes = new List<HexOutlineShape>();
}

public struct HexOutlineShape
{
    public List<HexEdgePositionPair> Edges;
    public HashSet<Vector3> Positions;
}

public struct HexEdgePositionPair
{
    public Vector3 A;
    public Vector3 B;
}

