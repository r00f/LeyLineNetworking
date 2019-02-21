using UnityEngine;
using System.Collections;
using UnityEditor;


#if UNITY_EDITOR
namespace LeyLineHybridECS
{
    [CustomEditor(typeof(ManalithInitializer))]
    public class ManaLithHelper : Editor
    {

        ManalithInitializer myTarget;

        private void OnEnable()
        {
            myTarget = (ManalithInitializer)target;

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (GUILayout.Button("Connect Manalith"))
            {
                myTarget.ConnectManaLith();
            }

            serializedObject.ApplyModifiedProperties();
        }

    }



}
#endif
