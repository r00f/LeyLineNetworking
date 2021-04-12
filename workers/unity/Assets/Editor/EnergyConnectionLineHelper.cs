using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    [CustomEditor(typeof(EnergyConnectionLine)), CanEditMultipleObjects]

    public class EnergyConnectionLineHelper : Editor
    {
#if UNITY_EDITOR
        EnergyConnectionLine[] myCellTypes;

        private void OnEnable()
        {
            Object[] monoObjects = targets;
            myCellTypes = new EnergyConnectionLine[monoObjects.Length];
            for (int i = 0; i < monoObjects.Length; i++)
            {
                myCellTypes[i] = monoObjects[i] as EnergyConnectionLine;
            }

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (GUILayout.Button("SetUpTransforms"))
            {
                for (int i = myCellTypes.Length - 1; i >= 0; i--)
                {
                    //myCellTypes[i].SetUpTransforms();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }

}
