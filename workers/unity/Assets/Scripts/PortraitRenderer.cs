
#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using System.IO;
using UnityEditor.Recorder.Input;
using System;
using UnityEngine.Rendering.HighDefinition;

public class PortraitRenderer : MonoBehaviour
{
    [SerializeField]
    Color color;

    [SerializeField]
    Settings settings;

    [SerializeField]
    int faction;

    [SerializeField]
    int animActionIndex;

    [SerializeField]
    float clipLengthMultiplier;

    [SerializeField]
    List<UnitComponentReferences> unitComponentReferences = new List<UnitComponentReferences>();

    [SerializeField]
    List<ManalithObject> manalithObjects = new List<ManalithObject>();

    [SerializeField]
    Vector2 outputPixelSize;

    [SerializeField]
    RecorderController m_RecorderController;

    [SerializeField]
    int currentRenderingUnitIndex;

    [SerializeField]
    float windUpWaitTime;

    [SerializeField]
    Transform projectileTarget;

    Projectile projectile;

    [SerializeField]
    RecordMode recordmode;

    [SerializeField]
    string cameraBackgroundHexColor;

    public Camera CurrentRenderingCamera;
    public Color HexColor;

    enum RecordMode
    {
        ImageSequence,
        GIF
    }

    private void Start()
    {
        /*
        foreach(UnitComponentReferences cRef in unitComponentReferences)
        {
            cRef.gameObject.SetActive(true);

            if (cRef.AnimatorComp.Animator)
                cRef.AnimatorComp.AnimStateEffectHandlers.AddRange(cRef.AnimatorComp.Animator.GetBehaviours<AnimStateEffectHandler>());

            cRef.gameObject.SetActive(false);

        }
        */
    }

    private void Update()
    {
        if(Camera.current)
            CurrentRenderingCamera = Camera.current;

        if (CurrentRenderingCamera && ColorUtility.TryParseHtmlString("#" + cameraBackgroundHexColor, out HexColor))
        {
            CurrentRenderingCamera.GetComponent<HDAdditionalCameraData>().backgroundColorHDR = HexColor;
            //CurrentRenderingCamera.backgroundColor = HexColor;
        }


        if (unitComponentReferences.Count > currentRenderingUnitIndex)
        {
            if (unitComponentReferences[currentRenderingUnitIndex].BaseDataSetComp.Actions[animActionIndex].ProjectileFab && unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.AnimationEvents.EventTrigger && !unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.AnimationEvents.EventTriggered)
            {
                LaunchProjectile(unitComponentReferences[currentRenderingUnitIndex].BaseDataSetComp.Actions[animActionIndex].ProjectileFab, unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.ProjectileSpawnOrigin, projectileTarget.position);
                unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.AnimationEvents.EventTriggered = true;
            }

            if (projectile)
            {
                HandleProjectile();
            }

            foreach (AnimStateEffectHandler a in unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.AnimStateEffectHandlers)
            {
                if (a.IsActiveState)
                {
                    for (int i = 0; i < a.CurrentEffectOnTimestamps.Count; i++)
                    {
                        if (a.CurrentEffectOnTimestamps[i].x <= 0)
                        {
                            unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.CharacterEffects[(int) a.CurrentEffectOnTimestamps[i].y].gameObject.SetActive(true);
                            a.CurrentEffectOnTimestamps.Remove(a.CurrentEffectOnTimestamps[i]);
                        }
                    }

                    for (int i = 0; i < a.CurrentEffectOffTimestamps.Count; i++)
                    {
                        if (a.CurrentEffectOffTimestamps[i].x <= 0)
                        {
                            unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.CharacterEffects[(int) a.CurrentEffectOffTimestamps[i].y].gameObject.SetActive(false);
                            a.CurrentEffectOffTimestamps.Remove(a.CurrentEffectOffTimestamps[i]);
                        }
                    }
                }
            }
        }
    }

    public void FillUnitList()
    {

        unitComponentReferences.Clear();
        unitComponentReferences.AddRange(GetComponentsInChildren<UnitComponentReferences>());
    }

    IEnumerator WaitToNextUnitRender(float waitTime, int index, GameObject go)
    {
        yield return new WaitForSeconds(waitTime);

        go.SetActive(false);

        if (unitComponentReferences.Count - 1 > index)
        {
            RecordUnitPortraitSequence(index + 1);
        }
    }

    IEnumerator WaitToNextManalithRender(float waitTime, int index, GameObject go)
    {
        yield return new WaitForSeconds(waitTime);

        go.SetActive(false);

        if (manalithObjects.Count - 1 > index)
        {
            RecordManalithPortraitSequence(index + 1);
        }
    }

    IEnumerator WaitAndRenderUnitClip(int index, GameObject go, RecorderControllerSettings controllerSettings)
    {
        yield return new WaitForSeconds(2f);

        currentRenderingUnitIndex = index;

        if (animActionIndex != 0)
        {
            unitComponentReferences[index].AnimatorComp.Animator.SetBool("Executed", false);
            unitComponentReferences[index].AnimatorComp.Animator.SetBool("HasWindup", unitComponentReferences[currentRenderingUnitIndex].BaseDataSetComp.Actions[animActionIndex].HasWindup);
            unitComponentReferences[index].AnimatorComp.AnimStateEffectHandlers.Clear();
            unitComponentReferences[index].AnimatorComp.AnimStateEffectHandlers.AddRange(unitComponentReferences[index].AnimatorComp.Animator.GetBehaviours<AnimStateEffectHandler>()); unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.AnimationEvents.EventTriggered = true;
            unitComponentReferences[currentRenderingUnitIndex].AnimatorComp.AnimationEvents.EventTriggered = false;
            unitComponentReferences[index].AnimatorComp.AnimationEvents.EventTrigger = false;
        }

        float executeWaitTime = 0f;

        if (unitComponentReferences[currentRenderingUnitIndex].BaseDataSetComp.Actions[animActionIndex].HasWindup)
        {
            executeWaitTime = windUpWaitTime;
        }


        

        float clipLength = unitComponentReferences[index].AnimatorComp.Animator.GetCurrentAnimatorClipInfo(0)[0].clip.length * clipLengthMultiplier + executeWaitTime;



        controllerSettings.SetRecordModeToTimeInterval(0, clipLength);
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();


        yield return new WaitForSeconds(executeWaitTime);

        unitComponentReferences[index].AnimatorComp.Animator.SetInteger("ActionIndexInt", animActionIndex);
        unitComponentReferences[index].AnimatorComp.Animator.SetTrigger("Execute");


        yield return new WaitForSeconds(executeWaitTime);

        unitComponentReferences[index].AnimatorComp.Animator.SetBool("Executed", true);
        unitComponentReferences[index].AnimatorComp.Animator.Play("Idle", 0, 0);
        StartCoroutine(WaitToNextUnitRender(5f, index, go));
    }

    IEnumerator WaitAndRenderManalithClip(int index, GameObject go, RecorderControllerSettings controllerSettings)
    {
        yield return new WaitForSeconds(2f);
        var idleClipLength = manalithObjects[index].PortraitAnimLength;
        controllerSettings.SetRecordModeToTimeInterval(0, idleClipLength);
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();
        StartCoroutine(WaitToNextManalithRender(5f, index, go));
    }

    public void RecordUnitPortraitSequence(int index)
    {
        var unitObject = unitComponentReferences[index].gameObject;

        unitObject.SetActive(true);
        ColorizeUnit(unitComponentReferences[index].TeamColorMeshesComp);

        unitComponentReferences[index].AnimatorComp.Animator.Play("Idle");

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        switch (recordmode)
        {
            case RecordMode.ImageSequence:

                // Image sequence
                var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
                imageRecorder.name = "My Image Recorder";
                imageRecorder.Enabled = true;
                imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                var mediaOutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Recordings", "Image Sequence", unitObject.transform.parent.parent.name, unitObject.transform.parent.name, unitObject.name, faction.ToString());

                imageRecorder.OutputFile = Path.Combine(mediaOutputFolder, unitObject.name + "_") + DefaultWildcard.Frame;

                imageRecorder.imageInputSettings = new RenderTextureSamplerSettings
                {
                    OutputWidth = (int) outputPixelSize.x,
                    OutputHeight = (int) outputPixelSize.y,
                    CameraTag = "MainCamera",
                    SuperSampling = SuperSamplingCount.X1,
                    RenderWidth = (int) outputPixelSize.x,
                    RenderHeight = (int) outputPixelSize.y
                };

                controllerSettings.AddRecorderSettings(imageRecorder);
                break;

            case RecordMode.GIF:

                // Image sequence
                var gifRecorder = ScriptableObject.CreateInstance<GIFRecorderSettings>();
                gifRecorder.name = "My GIF Recorder";
                //gifRecorder.FrameRate = 60;
                gifRecorder.Enabled = true;

                var gifMediaOutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Recordings", "GIF", unitObject.transform.parent.parent.name, unitObject.transform.parent.name, unitObject.name, faction.ToString());

                gifRecorder.OutputFile = Path.Combine(gifMediaOutputFolder, unitObject.name + "_") + DefaultWildcard.Frame;

                gifRecorder.imageInputSettings = new RenderTextureSamplerSettings
                {
                    OutputWidth = (int) outputPixelSize.x,
                    OutputHeight = (int) outputPixelSize.y,
                    CameraTag = "MainCamera",
                    SuperSampling = SuperSamplingCount.X1,
                    RenderWidth = (int) outputPixelSize.x,
                    RenderHeight = (int) outputPixelSize.y,
                    FlipFinalOutput = true
                };

                controllerSettings.AddRecorderSettings(gifRecorder);

                break;
        }

        StartCoroutine(WaitAndRenderUnitClip(index, unitObject, controllerSettings));
    }

    public void RecordManalithPortraitSequence(int index)
    {
        var manalithObject = manalithObjects[index].gameObject;

        manalithObject.SetActive(true);
        ColorizeManalith(manalithObjects[index]);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        // Image sequence
        var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
        imageRecorder.name = "My Image Recorder";
        imageRecorder.Enabled = true;
        imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        var mediaOutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Recordings", manalithObject.transform.parent.name, manalithObject.name, faction.ToString());
        imageRecorder.OutputFile = Path.Combine(mediaOutputFolder, manalithObject.name + "_") + DefaultWildcard.Frame;

        imageRecorder.imageInputSettings = new RenderTextureSamplerSettings
        {
            OutputWidth = (int) outputPixelSize.x,
            OutputHeight = (int) outputPixelSize.y,
            CameraTag = "MainCamera",
            SuperSampling = SuperSamplingCount.X1,
            RenderWidth = (int) outputPixelSize.x,
            RenderHeight = (int) outputPixelSize.y
        };

        controllerSettings.AddRecorderSettings(imageRecorder);

        StartCoroutine(WaitAndRenderManalithClip(index, manalithObject, controllerSettings));
    }

    public void Colorize()
    {
        color = settings.FactionColors[faction];

        foreach (TeamColorMeshes teamColorMeshes in GetComponentsInChildren<TeamColorMeshes>())
        {
            ColorizeUnit(teamColorMeshes);
        }

        foreach (ManalithObject m in GetComponentsInChildren<ManalithObject>())
        {
            ColorizeManalith(m);
        }
    }

    public void ColorizeManalith(ManalithObject m)
    {
        color = settings.FactionColors[faction];

        foreach (MeshRenderer r in m.EmissionColorRenderers)
        {
            r.sharedMaterial.SetColor("_EmissiveColor", color * r.sharedMaterial.GetFloat("_EmissiveIntensity"));
        }

        foreach (Light l in m.Lights)
        {
            l.color = color;
        }

        foreach (ParticleSystem p in m.ParticleSystems)
        {
            var mainModule = p.main;
            mainModule.startColor = color;
        }

        for (int i = 0; i < m.DetailColorRenderers.Count; i++)
        {
            Renderer r = m.DetailColorRenderers[i];

            r.sharedMaterial.SetColor("_BaseColor1", color);
        }

    }

    public void ColorizeUnit(TeamColorMeshes teamColorMeshes)
    {
        color = settings.FactionColors[faction];

        teamColorMeshes.color = color;

        for (int m = 0; m < teamColorMeshes.detailColorMeshes.Count; m++)
        {
            Renderer r = teamColorMeshes.detailColorMeshes[m];

            //set layerMask 
            if (m < teamColorMeshes.PartialColorMasks.Count)
                r.sharedMaterial.SetTexture("_LayerMaskMap", teamColorMeshes.PartialColorMasks[m]);
            else
                Debug.LogError("Not Enough PartialColorMasks set, set them on the unit TeamColorMeshes component!");

            //set layer1 color to factionColor
            r.sharedMaterial.SetColor("_BaseColor1", teamColorMeshes.color);
        }

        foreach(LineRenderer l in teamColorMeshes.LineRenderers)
        {
            l.startColor = teamColorMeshes.color;
            l.endColor = teamColorMeshes.color;

        }

        foreach (TrailRenderer tr in teamColorMeshes.TrailRenderers)
        {
            tr.startColor = teamColorMeshes.color;
            tr.endColor = teamColorMeshes.color;
        }

        foreach (Renderer r in teamColorMeshes.EmissionColorMeshes)
        {
            if (r.sharedMaterials[r.sharedMaterials.Length - 1].HasProperty("_EmissiveColor"))
                r.sharedMaterials[r.sharedMaterials.Length - 1].SetColor("_EmissiveColor", teamColorMeshes.color * r.sharedMaterials[r.sharedMaterials.Length - 1].GetFloat("_EmissiveIntensity"));

            if (r.sharedMaterials[r.sharedMaterials.Length - 1].HasProperty("_EmissionColor"))
                r.sharedMaterials[r.sharedMaterials.Length - 1].SetColor("_EmissionColor", teamColorMeshes.color * r.sharedMaterials[r.sharedMaterials.Length - 1].GetFloat("_EmissiveIntensity"));
        }

        foreach (Light l in teamColorMeshes.Lights)
        {
            l.color = teamColorMeshes.color;
        }

        foreach (ParticleSystemRenderer p in teamColorMeshes.EmissiveTrailParticleSystems)
        {
            p.trailMaterial.SetColor("_EmissiveColor", teamColorMeshes.color * p.trailMaterial.GetFloat("_EmissiveIntensity"));
        }

        foreach (LineRenderer l in teamColorMeshes.LineRenderers)
        {
            l.sharedMaterial.SetColor("_EmissiveColor", teamColorMeshes.color * l.sharedMaterial.GetFloat("_EmissiveIntensity"));
        }

        foreach (ParticleSystem p in teamColorMeshes.ParticleSystems)
        {
            ParticleSystem.MainModule main = p.main;
            main.startColor = new Color(teamColorMeshes.color.r, teamColorMeshes.color.g, teamColorMeshes.color.b, main.startColor.color.a);
        }

        foreach (Renderer r in teamColorMeshes.FullColorMeshes)
        {
            if (r.sharedMaterial.HasProperty("_UnlitColor"))
                r.sharedMaterial.SetColor("_UnlitColor", new Color(teamColorMeshes.color.r, teamColorMeshes.color.g, teamColorMeshes.color.b, r.sharedMaterial.GetColor("_UnlitColor").a));
            else if (r.sharedMaterial.HasProperty("_BaseColor"))
                r.sharedMaterial.SetColor("_BaseColor", new Color(teamColorMeshes.color.r, teamColorMeshes.color.g, teamColorMeshes.color.b, r.sharedMaterial.GetColor("_BaseColor").a));

            if (r is SpriteRenderer)
            {
                r.sharedMaterial.color = teamColorMeshes.color;
            }

            if (r is TrailRenderer)
            {
                TrailRenderer tr = r as TrailRenderer;
                float alpha = 1.0f;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(teamColorMeshes.color, 0.0f), new GradientColorKey(teamColorMeshes.color, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
                );
                tr.colorGradient = gradient;
            }
        }
    }

    public void LaunchProjectile(Projectile projectileFab, Transform spawnTransform, Vector3 targetPos)
    {
        Projectile p = Instantiate(projectileFab, spawnTransform.position, spawnTransform.rotation, spawnTransform.root);

        p.SpawnTransform = spawnTransform;

        //save targetPosition / targetYOffset on units?
        Vector3 offSetTarget = new Vector3(targetPos.x, targetPos.y, targetPos.z);

        List<Vector3> travellingPoints = new List<Vector3>();

        //THIS USES SINUS CALC FOR STRAIGHT LINES -- CHANGE METHOD TO HANDLE STRAIGHT LINES WHITOUT CALCULATING SINUS STUFF
        travellingPoints.AddRange(CalculateSinusPath(spawnTransform.position, offSetTarget, projectileFab.MaxHeight));

        Vector3 distance = offSetTarget - spawnTransform.position;

        foreach (SpringJoint s in projectileFab.SpringJoints)
        {
            s.maxDistance = distance.magnitude / projectileFab.SpringJoints.Count;
        }

        p.TravellingCurve = travellingPoints;
        p.IsTravelling = true;

        projectile = p;
    }

    public Vector3[] CalculateSinusPath(Vector3 origin, Vector3 target, float zenitHeight)
    {
        Vector3 distance = target - origin;
        int numberOfPositions = 8 + (2 * Mathf.RoundToInt(distance.magnitude) / 2);

        Vector3[] sinusPath = new Vector3[numberOfPositions + 1];
        float heightDifference = origin.y - target.y;
        float[] ypositions = CalculateSinusPoints(numberOfPositions);
        float xstep = 1.0f / numberOfPositions;

        for (int i = 0; i < ypositions.Length; i++)
        {
            float sinYpos = ypositions[i] * zenitHeight - heightDifference * (xstep * i);
            sinusPath[i] = origin + new Vector3(distance.x * (xstep * i), sinYpos, distance.z * (xstep * i));
        }

        sinusPath[numberOfPositions] = target;

        return sinusPath;
    }

    public float[] CalculateSinusPoints(int numberOfPositions)
    {
        float[] yPosArray = new float[numberOfPositions];
        float xStep = 1.0f / numberOfPositions;
        int i = 0;

        for (float x = 0.0f; x <= 1.0f; x += xStep)
        {
            if (i < yPosArray.Length)
            {
                yPosArray[i] = (float) Math.Sin(x * Math.PI);
                i++;
            }
        }

        return yPosArray;
    }

    public void HandleProjectile()
    {
        if (projectile.CollisionDetection)
        {
            if (projectile.CollisionDetection.HasCollided)
            {
                //Debug.Log("Projectile Collision Detected");
                projectile.DestinationReached = true;
                projectile.FlagForDestruction = true;
            }
        }

        if (projectile.DestroyAfterSeconds != 0)
        {
            if (projectile.DestroyAfterSeconds > 0.05f)
            {
                projectile.DestroyAfterSeconds -= Time.deltaTime;
            }
            else
            {
                projectile.FlagForDestruction = true;
            }
        }

        if (!projectile.Launched && projectile.TravellingCurve.Count != 0)
        {
            foreach (Rigidbody r in projectile.RigidbodiesToLaunch)
            {
                projectile.MovementDelay *= Vector3.Distance(projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset, r.position);
                Vector3 direction = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset - r.position;
                r.AddForce(direction * projectile.LaunchForce, projectile.LaunchForceMode);
            }
            projectile.Launched = true;
        }

        if (projectile.DegreesPerSecond != 0 && !projectile.DestinationReached)
            projectile.transform.RotateAround(projectile.transform.position, projectile.transform.forward, projectile.DegreesPerSecond * Time.deltaTime);

        if (projectile.MovementDelay > 0)
        {
            projectile.MovementDelay -= Time.deltaTime;
        }
        else if (projectile.IsTravelling)
        {
            if (projectile.ArriveInstantly)
            {
                if (projectile.RigidbodiesToLaunch.Count == 0)
                    projectile.transform.position = projectile.TravellingCurve[projectile.TravellingCurve.Count - 1] + Vector3.up * projectile.TargetYOffset;

                projectile.DestinationReached = true;
            }
            else if (projectile.CurrentTargetId < projectile.TravellingCurve.Count - 1 - projectile.TravellingCurveCutOff)
            {
                float dist = Vector3.Distance(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1]);
                if (projectile.MovementPercentage >= 1f)
                {
                    projectile.MovementPercentage = 0;
                    projectile.CurrentTargetId++;
                }
                else
                {
                    projectile.MovementPercentage += Time.deltaTime * projectile.TravellingSpeed / dist;
                    if (projectile.RigidbodiesToLaunch.Count == 0)
                    {
                        Vector3 pos = Vector3.Lerp(projectile.TravellingCurve[projectile.CurrentTargetId], projectile.TravellingCurve[projectile.CurrentTargetId + 1], projectile.MovementPercentage);
                        projectile.transform.position = pos;
                    }
                }
            }
            else
            {
                projectile.DestinationReached = true;
            }

            if (projectile.DestinationReached)
            {
                //tounge contraction
                if (projectile.SpringJoints.Count != 0)
                {
                    foreach (Rigidbody r in projectile.RigidbodiesToLaunch)
                    {
                        r.AddForce(Vector3.up * projectile.ContractUpForce, ForceMode.Acceleration);

                    }
                    foreach (SpringJoint s in projectile.SpringJoints)
                    {
                        s.maxDistance = Mathf.Lerp(s.maxDistance, 0, Time.deltaTime * projectile.ContractSpeed);
                    }
                }

                if (projectile.ParticleSystemsStopWaitTime > 0)
                {
                    projectile.ParticleSystemsStopWaitTime -= Time.deltaTime;

                }
                else
                {
                    foreach (ParticleSystem p in projectile.ParticleSystemsToStop)
                    {
                        p.Stop();
                    }
                }

                if (!projectile.EffectTriggered)
                {
                    /*
                    logger.HandleLog(LogType.Warning,
                    new LogEvent("TriggerProjectileEvent")
                    .WithField("InAction", projectile.UnitId));
                    */

                    //m_ActionEffectSystem.TriggerActionEffect(projectile.UnitFaction, projectile.Action, projectile.UnitId, projectile.PhysicsExplosionOrigin, gameStates[0], projectile.AxaShieldOrbitCount);

                    if (projectile.DestinationExplosionPrefab && projectile.ExplosionSpawnTransform)
                    {
                        Instantiate(projectile.DestinationExplosionPrefab, projectile.ExplosionSpawnTransform.position, Quaternion.identity, projectile.transform.parent);
                    }

                    if (projectile.ExplosionEventEmitter)
                    {
                        projectile.ExplosionEventEmitter.Play();
                    }

                    if (projectile.ExplosionParticleSystem)
                    {
                        projectile.ExplosionParticleSystem.Play();
                    }

                    foreach (GameObject go in projectile.DisableAtDestinationObjects)
                    {
                        go.SetActive(false);
                    }

                    projectile.EffectTriggered = true;
                }

                if (projectile.DestroyAtDestination)
                {
                    if (!projectile.FlagForDestruction)
                    {
                        projectile.FlagForDestruction = true;
                    }
                }

                //Explode(projectile);
                projectile.IsTravelling = false;
            }
        }

        if (projectile.FlagForDestruction && !projectile.QueuedForDestruction)
        {
            foreach (GameObject go in projectile.DisableBeforeDestructionObjects)
            {
                go.SetActive(false);
            }
            if (projectile)
                GameObject.Destroy(projectile.gameObject, 0.5f);

            projectile.QueuedForDestruction = true;
        }

    }

}
#endif
