using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class ManalithObject : MonoBehaviour
{
    public string Name;
    public List<MeshRenderer> EmissionColorRenderers;
    //public List<GameObject> SelectionOutlineRenderers;
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
}

