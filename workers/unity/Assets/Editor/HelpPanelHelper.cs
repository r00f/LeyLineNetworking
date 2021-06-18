using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HelpPanel))]
public class HelpPanelHelper : Editor
{

    HelpPanel myTarget;

    private void OnEnable()
    {
        myTarget = (HelpPanel) target;

    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        if (GUILayout.Button("Find All Components"))
        {
            myTarget.FindComponents();
            serializedObject.ApplyModifiedProperties();
            //PrefabUtility.SavePrefabAsset(myTarget.gameObject);
        }

    }
}
