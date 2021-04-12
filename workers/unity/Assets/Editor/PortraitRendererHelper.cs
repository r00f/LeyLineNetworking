using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    [CustomEditor(typeof(PortraitRenderer)), CanEditMultipleObjects]

    public class PortraitRendererHelper : Editor
    {
#if UNITY_EDITOR
        PortraitRenderer[] myPortraitRenderers;

        private void OnEnable()
        {
            Object[] monoObjects = targets;
            myPortraitRenderers = new PortraitRenderer[monoObjects.Length];
            for (int i = 0; i < monoObjects.Length; i++)
            {
                myPortraitRenderers[i] = monoObjects[i] as PortraitRenderer;
            }

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            /*
            if (GUILayout.Button("Fill Unit List"))
            {
                for (int i = myPortraitRenderers.Length - 1; i >= 0; i--)
                {
                    myPortraitRenderers[i].FillUnitList();
                }
            }
            */

            if (GUILayout.Button("Render Unit Portrait Sequences"))
            {
                for (int i = myPortraitRenderers.Length - 1; i >= 0; i--)
                {
                    myPortraitRenderers[i].RecordUnitPortraitSequence(0);
                }
            }

            if (GUILayout.Button("Render Manalith Portrait Sequences"))
            {
                for (int i = myPortraitRenderers.Length - 1; i >= 0; i--)
                {
                    myPortraitRenderers[i].RecordManalithPortraitSequence(0);
                }
            }

            
            if (GUILayout.Button("Colorize"))
            {
                for (int i = myPortraitRenderers.Length - 1; i >= 0; i--)
                {
                    myPortraitRenderers[i].Colorize();
                }
            }
            

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }

}
