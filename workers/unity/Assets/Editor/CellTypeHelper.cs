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

        protected virtual void OnSceneGUI()
        {
            for (int i = myCellTypes.Length - 1; i >= 0; i--)
            {
                if (myCellTypes[i] == null)
                {
                    return;
                }

                if (myCellTypes[i].DetailPathSpawnDirectionMinMax.x != 0 || myCellTypes[i].DetailPathSpawnDirectionMinMax.y != 360)
                {
                    Handles.color = Color.cyan;
                    Handles.ArrowHandleCap(0, myCellTypes[i].transform.position, Quaternion.Euler(new Vector3(0, myCellTypes[i].DetailPathSpawnDirectionMinMax.x, 0)), 1f, EventType.Repaint);
                    Handles.ArrowHandleCap(0, myCellTypes[i].transform.position, Quaternion.Euler(new Vector3(0, myCellTypes[i].DetailPathSpawnDirectionMinMax.y, 0)), 1f, EventType.Repaint);
                }


                if (myCellTypes[i].ManalithSpawnDirection != 0)
                {
                    Handles.color = Color.yellow;
                    Handles.ArrowHandleCap(0, myCellTypes[i].transform.position, Quaternion.Euler(new Vector3(0, myCellTypes[i].ManalithSpawnDirection, 0)), 1f, EventType.Repaint);
                }
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
