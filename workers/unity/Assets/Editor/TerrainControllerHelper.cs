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

            if (GUILayout.Button("Flush"))
            {
                myTarget.FlushTerrain();
            }

            if (GUILayout.Button("Update Trees"))
            {
                myTarget.UpdateTerrainDetailObjects();
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
                myTarget.UpdateTerrainHeight();
            }

            if (GUILayout.Button("Set Whole Terrain Texture"))
            {
                myTarget.SetWholeTerrainTexture();
                myTarget.UpdateAllMapTileTextures();
            }

            if (GUILayout.Button("Update all Terrain Tiles"))
            {
                myTarget.UpdateAllMapTiles();
            }

            if (GUILayout.Button("Connect Manaliths"))
            {
                myTarget.ConnectManaliths();
            }


            if (GUILayout.Button("Generate Map"))
            {
                myTarget.GenerateMap();
            }

            if (GUILayout.Button("Generate River Outline"))
            {
                myTarget.GenerateRiverOutline();
            }

            serializedObject.ApplyModifiedProperties();
        }
}
#endif
}
