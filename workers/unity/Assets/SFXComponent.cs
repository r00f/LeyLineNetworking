using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(ParticleSystem), typeof(StudioEventEmitter))]
public class SFXComponent : MonoBehaviour
{
    public ParticleSystem PS;
    public StudioEventEmitter SoundEmitter;
}
