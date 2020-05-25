using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAttractor : MonoBehaviour
{
    public ParticleSystem AttractedPS;

    public float AttractionLerpSpeed;
    public float AttractionForce;
    public float AttractionRange;

    public bool UseNormalizedDistance;

    void Update()
    {
        if(AttractedPS)
        {
            ParticleSystem.Particle[] emittedParticles = new ParticleSystem.Particle[AttractedPS.particleCount];
            AttractedPS.GetParticles(emittedParticles);

            for (int i = 0; i < emittedParticles.Length; i++)
            {
                Vector3 dist = transform.position - emittedParticles[i].position;

                if(dist.magnitude < AttractionRange)
                {
                    if(UseNormalizedDistance)
                        emittedParticles[i].velocity = Vector3.Lerp(emittedParticles[i].velocity, dist.normalized * AttractionForce, AttractionLerpSpeed);
                    else
                        emittedParticles[i].velocity = Vector3.Lerp(emittedParticles[i].velocity, dist * AttractionForce, AttractionLerpSpeed);
                }
            }

            AttractedPS.SetParticles(emittedParticles);

        }
    }

    /*
    public void SetParticleSystem(ParticleSystem particleSystem)
    {
        Debug.Log("SetParticleSystem");
        AttractedPS = particleSystem;
    }
    */
}
