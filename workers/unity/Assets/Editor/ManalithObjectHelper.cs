using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ManalithObject))]
public class ManalithObjectHelper : Editor
{

    ManalithObject myTarget;

    private void OnEnable()
    {
        myTarget = (ManalithObject) target;

    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        if (GUILayout.Button("Initialize CompReferences"))
        {
            myTarget.FillSelectionOutlineObjects();
            serializedObject.ApplyModifiedProperties();
            PrefabUtility.SavePrefabAsset(myTarget.gameObject);
        }

    }
}
