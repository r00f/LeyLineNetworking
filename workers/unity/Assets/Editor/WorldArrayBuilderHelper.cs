using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldArrayBuilder))]
public class WorldArrayBuilderHelper : Editor
{
    WorldArrayBuilder myTarget;

    private void OnEnable()
    {
        //myTarget = (WorldArrayBuilder) target;
        myTarget = (WorldArrayBuilder) target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        if (GUILayout.Button("Build World Array"))
        {
            myTarget.BuildMapArray();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
