using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColorMeshes : MonoBehaviour
{
    [SerializeField]
    public List<Renderer> FullColorMeshes = new List<Renderer>();
    [SerializeField]
    public List<Renderer> EmissionColorMeshes = new List<Renderer>();
    [SerializeField]
    public float EmissionIntensity;
    [SerializeField]
    public float EmissionLerpTime;
    [SerializeField, ColorUsage(true, true)]
    public Color EmissionLerpColor;
    [SerializeField]
    public Color color;
    [SerializeField]
    public List<Renderer> detailColorMeshes = new List<Renderer>();
    public List<Texture2D> PartialColorMasks;
}
