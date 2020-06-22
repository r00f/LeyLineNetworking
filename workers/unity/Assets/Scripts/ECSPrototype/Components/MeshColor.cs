using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    public class MeshColor : MonoBehaviour
    {
        [ColorUsage(true, true)]
        public Color Color;
        [ColorUsage(true, true)]
        public Color LerpColor;

        public Color MapColor;
        public Color MapLerpColor;
        public MeshRenderer MeshRenderer;
        //public List<MeshRenderer> EmissionColorRenderers;
        //public List<Light> Lights;
        public ParticleSystem CirclePs;
        public ManalithObject ManaLithObject;
        public float EmissionMultiplier;
    }

}

