using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class ManalithObject : MonoBehaviour
{
    public string Name;
    public List<MeshRenderer> EmissionColorRenderers;
    public List<Light> Lights;
    public List<ParticleSystem> ParticleSystems;
    public List<ParticleSystem> OneShotParticleSystems;
    public StudioEventEmitter GainControlSoundEmitter;

}
