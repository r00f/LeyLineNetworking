using UnityEngine;
using System.Collections;

namespace LeyLineHybridECS
{
    public class MeshColor : MonoBehaviour
    {
        [ColorUsage(true, true)]
        public Color Color;
        [ColorUsage(true, true)]
        public Color LerpColor;
        public MeshRenderer MeshRenderer;
        public ParticleSystem ParticleSystem;
        public float EmissionMultiplier;
    }

}

