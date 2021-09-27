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
            /*
            if (GUILayout.Button("Connect Manalith"))
            {
                myTarget.InitManalithCircle();
            }
            */

            if (GUILayout.Button("Generate Meshes"))
            {
                myTarget.GenerateMeshes();
            }

            /*
            if (GUILayout.Button("Fill Circle Coords"))
            {
                myTarget.FillCircleCoordinatesList();
            }
            if (GUILayout.Button("Fill Path Coords"))
            {
                myTarget.FillPathCoordinatesList();
            }
            */
            serializedObject.ApplyModifiedProperties();
        }

    }



}
#endif
