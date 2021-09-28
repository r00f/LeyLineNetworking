using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsExplosion : MonoBehaviour
{

    public bool ExplodeTrigger;
    public bool ResetTrigger;
    Rigidbody[] SceneRigidbodies;
    List<Transform> RigidbodyTransforms = new List<Transform>();
    List<Vector3> RigidbodyStartPositions = new List<Vector3>();
    List<Quaternion> RigidbodyStartRotations = new List<Quaternion>();
    public int ResetCount;
    public float explosionForce;
    public float explosionRadius;
    public int upForce;
    public List<Animator> AnimatorsToDisable;
    public float minTimeScale;
    public float slowdownSpeed;
    public float waitTime;
    float currentWaitTime;


    public void SetExplodeTriggerTrue()
    {
        ExplodeTrigger = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentWaitTime = waitTime;
        SceneRigidbodies = FindObjectsOfType<Rigidbody>();

        foreach(Rigidbody r in SceneRigidbodies)
        {
            r.isKinematic = true;
            RigidbodyTransforms.Add(r.transform);
            RigidbodyStartPositions.Add(r.transform.position);
            RigidbodyStartRotations.Add(r.transform.rotation);
        }

        foreach(Animator a in AnimatorsToDisable)
        {
            a.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButtonDown("Fire1"))
            ExplodeTrigger = true;
        if (Input.GetButtonDown("Fire2"))
            ResetTrigger = true;

        if(ExplodeTrigger)
        {
            Explode();

            if (Time.timeScale > minTimeScale)
            {
                Time.timeScale /= slowdownSpeed;
                Time.fixedDeltaTime = 0.02F * Time.timeScale;
            }
            else
            {
                if (currentWaitTime <= 0)
                {
                    ExplodeTrigger = false;
                    Time.timeScale = 1;
                    Time.fixedDeltaTime = 0.02F;
                    currentWaitTime = waitTime;
                }
                else
                    currentWaitTime -= Time.deltaTime/Time.timeScale;
            }
        }

        if(ResetTrigger)
        {
            ExplodeTrigger = false;
            Reset();
            ResetTrigger = false;
        }
    }

    public void Reset()
    {
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02F;
        currentWaitTime = waitTime;

        foreach (Animator a in AnimatorsToDisable)
        {
            a.enabled = true;
        }

        for (int q = 0; q < ResetCount; q ++ )
        {
            for (int i = 0; i < RigidbodyTransforms.Count; i++)
            {
                SceneRigidbodies[i].velocity = Vector3.zero;
                SceneRigidbodies[i].isKinematic = true;
                RigidbodyTransforms[i].position = RigidbodyStartPositions[i];
                RigidbodyTransforms[i].rotation = RigidbodyStartRotations[i];
            }
        }
    }

    public void Explode()
    {
        foreach (Animator a in AnimatorsToDisable)
        {
            a.enabled = false;
        }
        foreach (Rigidbody r in SceneRigidbodies)
        {
            r.isKinematic = false;
            r.AddForce(Vector3.up * upForce, ForceMode.Impulse);
            r.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }
    }
}
