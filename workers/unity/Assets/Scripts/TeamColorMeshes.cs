using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamColorMeshes : MonoBehaviour
{

    [SerializeField]
    public List<Light> Lights = new List<Light>();
    [SerializeField]
    public List<ParticleSystem> ParticleSystems = new List<ParticleSystem>();

    [SerializeField]
    public List<ParticleSystemRenderer> EmissiveTrailParticleSystems = new List<ParticleSystemRenderer>();

    [SerializeField]
    public List<LineRenderer> LineRenderers = new List<LineRenderer>();

    [SerializeField]
    public List<Renderer> FullColorMeshes = new List<Renderer>();
    [SerializeField]
    public List<Renderer> HarvestingEmissionColorMeshes = new List<Renderer>();
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
