using UnityEngine;
using System.Collections;

namespace LeyLineHybridECS
{
    public class MeshGradientColor : MonoBehaviour
    {

        public MeshColor ManalithColor;
        public MeshColor ConnectedManalithColor;
        public MeshFilter PathMeshFilter;
        [HideInInspector]
        public Mesh mesh;
        public Vector2[] uv;
        public Gradient Gradient;
        public Gradient MapGradient;
        //[ColorUsage(true, true)]
        public Color[] colors;
        public float EmissionMultiplier;

        void Start()
        {
            mesh = PathMeshFilter.mesh;
            uv = mesh.uv;
            colors = new Color[uv.Length];
        }

    }

}
