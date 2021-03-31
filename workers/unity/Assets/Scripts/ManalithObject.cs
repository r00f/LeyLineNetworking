using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class ManalithObject : MonoBehaviour
{
    public string Name;
    public List<MeshRenderer> EmissionColorRenderers;
    public List<GameObject> SelectionOutlineRenderers;
    public List<Light> Lights;
    public List<ParticleSystem> ParticleSystems;
    public List<ParticleSystem> OneShotParticleSystems;
    public StudioEventEmitter GainControlSoundEmitter;

    public void FillSelectionOutlineObjects()
    {
        SelectionOutlineRenderers.Clear();
        foreach(MeshRenderer r in GetComponentsInChildren<MeshRenderer>())
        {
            SelectionOutlineRenderers.Add(r.gameObject);
        }
    }

}

