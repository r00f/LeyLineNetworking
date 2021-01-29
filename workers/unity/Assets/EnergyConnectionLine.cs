using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class EnergyConnectionLine : MonoBehaviour
{
    public LineRenderer LineRenderer;
    public List<Transform> DefaultPointList;
    public Transform DefaultPointParent;
    public List<Transform> BodyPartLineConnections;
    public float RandomOffset;
    public Vector2 AnimationIntervalMinMax;
    public Vector2 CurveThiccnessMinMax;
    float CurrentAnimIntervalTime;
    AnimationCurve animCurve;
    //public 

    private void OnEnable()
    {
        if (!DefaultPointParent || BodyPartLineConnections.Count == 0)
            return;

        Debug.Log("AYAYA");
        DefaultPointList.Clear();

        DefaultPointList.Add(BodyPartLineConnections[0]);

        for (int i = 0; i < DefaultPointParent.childCount; i++)
        {
            DefaultPointList.Add(DefaultPointParent.GetChild(i).GetComponent<Transform>());
        }

        DefaultPointList.Add(BodyPartLineConnections[1]);

        animCurve = new AnimationCurve();
        LineRenderer.positionCount = DefaultPointList.Count;
        CurrentAnimIntervalTime = AnimationIntervalMinMax.x;

        for (int i = 0; i < DefaultPointList.Count; i++)
        {
            LineRenderer.SetPosition(i, DefaultPointList[i].position);
            animCurve.AddKey(new Keyframe(i / ((float) LineRenderer.positionCount - 1), 0.05f));
        }

        LineRenderer.widthCurve = animCurve;
    }

    void Update()
    {
        if (DefaultPointList.Count == 0 || LineRenderer.positionCount == 0)
            return;

        LineRenderer.SetPosition(0, DefaultPointList[0].position);
        LineRenderer.SetPosition(LineRenderer.positionCount - 1, DefaultPointList[LineRenderer.positionCount - 1].position);

        if(CurrentAnimIntervalTime > 0)
        {
            CurrentAnimIntervalTime -= Time.deltaTime;
        }
        else
        {
            for (int i = 1; i < DefaultPointList.Count - 1; i++)
            {
                animCurve.MoveKey(i, new Keyframe(i / ((float)LineRenderer.positionCount - 1), Random.Range(CurveThiccnessMinMax.x, CurveThiccnessMinMax.y)));
                LineRenderer.SetPosition(i, DefaultPointList[i].position + Random.insideUnitSphere * RandomOffset);
            }

            LineRenderer.widthCurve = animCurve;
            CurrentAnimIntervalTime = Random.Range(AnimationIntervalMinMax.x, AnimationIntervalMinMax.y);
        }
    }
}
