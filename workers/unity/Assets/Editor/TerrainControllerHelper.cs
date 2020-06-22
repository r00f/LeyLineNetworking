using UnityEngine;
using System.Collections;
using UnityEditor;

namespace LeyLineHybridECS
{
#if UNITY_EDITOR
[CustomEditor(typeof(TerrainController))]
public class TerrainControllerHelper : Editor
{

    TerrainController myTarget;

    private void OnEnable()
    {
        myTarget = (TerrainController)target;

    }

    public override void OnInspectorGUI()
    {
            base.OnInspectorGUI();
            serializedObject.Update();


            if (GUILayout.Button("Update Trees"))
            {
                myTarget.UpdateHexagonTrees();
            }

            if (GUILayout.Button("Set Slope Texture"))
            {
                myTarget.SetSlopeTexture();
            }
            if (GUILayout.Button("Smooth Terrain"))
            {
                myTarget.SmoothTerrainHeights();
            }

            if (GUILayout.Button("PaintTerrainHeight"))
            {
                myTarget.PaintHeightAtPosition(myTarget.leyLineCrackSize, myTarget.leyLineCrackWorldPos);
            }


            if (GUILayout.Button("Set Whole Terrain Height"))
            {
                myTarget.SetWholeTerrainHeight();
            }

            if (GUILayout.Button("Set Whole Terrain Texture"))
            {
                myTarget.SetWholeTerrainTexture();
            }

            if (GUILayout.Button("Update all Terrain Tiles"))
            {
                myTarget.UpdateAllMapTiles();
            }

            serializedObject.ApplyModifiedProperties();
        }

}
#endif
}