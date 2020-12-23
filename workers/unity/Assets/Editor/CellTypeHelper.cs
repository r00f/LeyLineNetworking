using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    [CustomEditor(typeof(CellType)), CanEditMultipleObjects]

    public class CellTypeHelper : Editor
    {
#if UNITY_EDITOR
        CellType[] myCellTypes;

        private void OnEnable()
        {
            Object[] monoObjects = targets;
            myCellTypes = new CellType[monoObjects.Length];
            for (int i = 0; i < monoObjects.Length; i++)
            {
                myCellTypes[i] = monoObjects[i] as CellType;
            }

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (GUILayout.Button("UpdateTerrain"))
            {
                for (int i = myCellTypes.Length - 1; i >= 0; i--)
                {
                    myCellTypes[i].UpdateTerrain();
                }
            }




            if (GUILayout.Button("Apply Height Offset"))
            {
                for (int i = myCellTypes.Length - 1; i >= 0; i--)
                {
                    myCellTypes[i].ApplyCellOffset();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }

}
