using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillBarParticleComponent : MonoBehaviour
{
    [SerializeField]
    public ParticleSystem LoopPS;
    [SerializeField]
    public ParticleSystem BurstPS;
    [SerializeField]
    public RectTransform Rect;
    [SerializeField]
    public RectTransform ParentRect;
}
