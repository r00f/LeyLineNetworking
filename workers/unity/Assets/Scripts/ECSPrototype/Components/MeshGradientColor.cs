using UnityEngine;
using System.Collections;

namespace LeyLineHybridECS
{
    public class MeshGradientColor : MonoBehaviour
    {
        public LineRenderer LeylineRenderer;
        public MeshColor ConnectedManalithColor;
        public Gradient Gradient;
        public Gradient MapGradient;
        public float EmissionMultiplier;

        [HideInInspector]
        public GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        [HideInInspector]
        public GradientColorKey[] colorKeys = new GradientColorKey[4];
    }

}
