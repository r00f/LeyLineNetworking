using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererComponent : MonoBehaviour
{
    [SerializeField]
    public LineRenderer lineRenderer;
    [SerializeField]
    public Vector3 pathOffset;
    [SerializeField]
    public Vector3 arcOffset;
    [SerializeField]
    public Color pathColor;
    [SerializeField]
    public Color arcColor;


}
