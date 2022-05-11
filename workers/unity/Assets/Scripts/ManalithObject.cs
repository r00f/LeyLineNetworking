using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class ManalithObject : MonoBehaviour
{
    [HideInInspector]
    public float ChargedPSParticleLifeTimeModifier = 1000;
    public float ChargedPSParticleSpeed = 5;
    public int CurrentTargetIndex;
    public float CurrentTravelTime;
    public Vector3[] ChargePSTravelCurve;
    public bool MoveChargedParticlesTowardsHero;
    public string Name;
    public List<MeshRenderer> EmissionColorRenderers;
    public ParticleSystem ChargedPS;
    public List<MeshRenderer> DetailColorRenderers;
    public List<Light> Lights;
    public List<ParticleSystem> ParticleSystems;
    public List<ParticleSystem> OneShotParticleSystems;
    public StudioEventEmitter GainControlSoundEmitter;
    public float PortraitAnimLength;
    public MiniMapTile MiniMapTilePrefab;
    [HideInInspector]
    public MiniMapTile MiniMapTileInstance;
    [HideInInspector]
    public MiniMapTile BigMapTileInstance;
    public ParticleSystem.Particle[] ChargedPSParticles;
    /*
    public void FillSelectionOutlineObjects()
    {
        SelectionOutlineRenderers.Clear();
        foreach(MeshRenderer r in GetComponentsInChildren<MeshRenderer>())
        {
            SelectionOutlineRenderers.Add(r.gameObject);
        }
    }
    */
    public void InitializeChargedParticlesIfNeeded()
    {
        if (ChargedPSParticles == null || ChargedPSParticles.Length < ChargedPS.main.maxParticles)
            ChargedPSParticles = new ParticleSystem.Particle[ChargedPS.main.maxParticles];
    }

}

