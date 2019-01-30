using UnityEngine;
using System.Collections;

namespace LeyLineHybridECS
{
    public class MeshGradientColor : MonoBehaviour
    {

        public MeshColor ManalithColor;
        public MeshColor ConnectedManalithColor;
        public Mesh mesh;
        public Vector2[] uv;
        public Color[] colors;

        void Start()
        {
            mesh = GetComponent<MeshFilter>().mesh;
            uv = mesh.uv;
            colors = new Color[uv.Length];
        }

    }

}
