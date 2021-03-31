using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR
namespace LeyLineHybridECS
{
    [CustomEditor(typeof(ManalithGroup))]
    public class ManalithGroupHelper : Editor
    {
        ManalithGroup myTarget;
        private void OnEnable()
        {
            myTarget = (ManalithGroup)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (GUILayout.Button("Connect Manaliths"))
            {
                myTarget.ConnectManaliths();
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
}
#endif
