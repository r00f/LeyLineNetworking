using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UnitComponentReferences))]
public class UnitCompReferencesHelper : Editor
{

    UnitComponentReferences myTarget;

    private void OnEnable()
    {
        myTarget = (UnitComponentReferences) target;

    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        if (GUILayout.Button("Initialize CompReferences"))
        {
            myTarget.InitializeComponentReferences();
            serializedObject.ApplyModifiedProperties();
            PrefabUtility.SavePrefabAsset(myTarget.gameObject);
        }

    }
}
