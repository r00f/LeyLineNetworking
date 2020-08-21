using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillBarParticleComponent : MonoBehaviour
{
    [SerializeField]
    public List<ParticleSystem> ParticleSystems;
    [SerializeField]
    public RectTransform Rect;
    [SerializeField]
    public RectTransform ParentRect;
}
