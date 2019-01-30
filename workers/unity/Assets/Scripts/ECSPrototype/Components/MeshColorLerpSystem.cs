using UnityEngine;
using System.Collections;
using Unity.Entities;

namespace LeyLineHybridECS
{
    public class MeshColorLerpSystem : ComponentSystem
    {

        public struct CircleData
        {
            public readonly int Length;
            public ComponentArray<MeshColor> MeshColorData;
        }

        [Inject] private CircleData m_CircleData;

        public struct LineData
        {
            public readonly int Length;
            public ComponentArray<MeshGradientColor> MeshGradientColorData;
        }

        [Inject] private LineData m_LineData;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_CircleData.Length; i++)
            {
                var meshColor = m_CircleData.MeshColorData[i];

                if (meshColor.LerpColor != meshColor.Color)
                    meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

                if (meshColor.MeshRenderer.material.color != meshColor.LerpColor)
                    meshColor.MeshRenderer.material.color = meshColor.LerpColor;

                ParticleSystem pPs = meshColor.ParticleSystem;
                var mainModule = pPs.main;

                if (mainModule.startColor.color != meshColor.LerpColor)
                    mainModule.startColor = meshColor.LerpColor;
            }

            for (int i = 0; i < m_LineData.Length; i++)
            {
                var meshGradientColor = m_LineData.MeshGradientColorData[i];


                if(meshGradientColor.ManalithColor.LerpColor != meshGradientColor.ManalithColor.Color || meshGradientColor.ConnectedManalithColor.LerpColor != meshGradientColor.ConnectedManalithColor.Color)
                {
                    // Instead if vertex.y we use uv.x
                    for (int li = 0; li < meshGradientColor.uv.Length; li++)
                    {
                        meshGradientColor.colors[li] = Color.Lerp(meshGradientColor.ManalithColor.LerpColor, meshGradientColor.ConnectedManalithColor.LerpColor, meshGradientColor.uv[li].x / meshGradientColor.uv[meshGradientColor.uv.Length - 1].x);
                    }

                    meshGradientColor.mesh.colors = meshGradientColor.colors;
                }
            }
        }
    }


        /*
        [SerializeField]
        Color color1;
        [SerializeField]
        Color color2;

        Mesh mesh;

        [SerializeField]
        Vector2[] uv;
        ParticleSystem pathPs;


        void Start()
        {
            pathPs = GetComponent<ParticleSystem>();
            mesh = GetComponent<MeshFilter>().mesh;
            uv = mesh.uv;

            ParticleSystem pPs = pathPs;
            var pathShapeModule = pPs.shape;
            pathShapeModule.shapeType = ParticleSystemShapeType.Mesh;
            pathShapeModule.mesh = mesh;
        }

        // Update is called once per frame
        void Update()
        {
            var colors = new Color[uv.Length];

            // Instead if vertex.y we use uv.x
            for (int i = 0; i < uv.Length; i++)
                colors[i] = Color.Lerp(color1, color2, uv[i].x / uv[uv.Length - 1].x);

            mesh.colors = colors;

        }
    }
    */
}