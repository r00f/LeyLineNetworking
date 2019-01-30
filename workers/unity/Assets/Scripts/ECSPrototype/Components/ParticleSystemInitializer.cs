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
            pathPs = GetComponent<ParticleSystem>();
            mesh = GetComponent<MeshFilter>().mesh;

            ParticleSystem pPs = pathPs;
            var pathShapeModule = pPs.shape;
            pathShapeModule.shapeType = ParticleSystemShapeType.Mesh;
            pathShapeModule.mesh = mesh;
        }
    }
}
