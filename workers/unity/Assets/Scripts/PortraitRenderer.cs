using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using System.IO;
using UnityEditor.Recorder.Input;

public class PortraitRenderer : MonoBehaviour
{

    [SerializeField]
    Color color;

    [SerializeField]
    Settings settings;

    [SerializeField]
    int faction;

    [SerializeField]
    List<UnitComponentReferences> unitComponentReferences = new List<UnitComponentReferences>();

    [SerializeField]
    List<ManalithObject> manalithObjects = new List<ManalithObject>();

    [SerializeField]
    Vector2 outputPixelSize;

    [SerializeField]
    RecorderController m_RecorderController;


    public void FillUnitList()
    {

        unitComponentReferences.Clear();
        unitComponentReferences.AddRange(GetComponentsInChildren<UnitComponentReferences>());

        //teamColorMeshes.Clear();
        //teamColorMeshes.AddRange(GetComponentsInChildren<TeamColorMeshes>());
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
        var idleClipLength = unitComponentReferences[index].AnimatorComp.Animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
        controllerSettings.SetRecordModeToTimeInterval(0, idleClipLength);
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();
        unitComponentReferences[index].AnimatorComp.Animator.Play("Idle", 0, 0);
        StartCoroutine(WaitToNextUnitRender(5f, index , go));
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

        // Image sequence
        var imageRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
        imageRecorder.name = "My Image Recorder";
        imageRecorder.Enabled = true;
        imageRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        var mediaOutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Recordings", unitObject.transform.parent.parent.name, unitObject.transform.parent.name, unitObject.name, faction.ToString());

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

}
