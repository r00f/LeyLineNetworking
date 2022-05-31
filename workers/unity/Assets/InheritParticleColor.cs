using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class InheritParticleColor : MonoBehaviour
{
    ParticleSystem parentParticleSystem;
    public List<ParticleSystem> ChildParticleSystems;
    [SerializeField]
    bool inheritAlpha = true;

    // Start is called before the first frame update
    void Start()
    {
        parentParticleSystem = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        ParticleSystem.MainModule pm = parentParticleSystem.main;

        foreach (ParticleSystem p in ChildParticleSystems)
        {
            ParticleSystem.MainModule m = p.main;
            float a = p.main.startColor.color.a;

            if (inheritAlpha)
                a = m.startColor.color.a;

            m.startColor = new Color(pm.startColor.color.r, pm.startColor.color.g, pm.startColor.color.b, a);
        }    
    }
}
