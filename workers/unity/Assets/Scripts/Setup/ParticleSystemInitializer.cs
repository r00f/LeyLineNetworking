using UnityEngine;
using System.Collections;

namespace LeyLineHybridECS
{
    public class ParticleSystemInitializer : MonoBehaviour
    {
        Mesh mesh;
        ParticleSystem pathPs;

        void Start()
        {
            pathPs = GetComponentInChildren<ParticleSystem>();
            mesh = GetComponent<MeshFilter>().mesh;
            if(pathPs)
            {
                ParticleSystem pPs = pathPs;
                var pathShapeModule = pPs.shape;
                pathShapeModule.shapeType = ParticleSystemShapeType.Mesh;
                pathShapeModule.mesh = mesh;
            }

        }
    }
}
