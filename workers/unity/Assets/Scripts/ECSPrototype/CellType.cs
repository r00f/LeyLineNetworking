#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Unity.Mathematics;

namespace LeyLineHybridECS
{

    [ExecuteInEditMode()]
    public class CellType : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        IsTaken isTaken;
        //[SerializeField, HideInInspector]
        //float3 pos3D;
        [SerializeField, HideInInspector]
        CellDimensions cellDimensions;
        [SerializeField, HideInInspector]
        TerrainController terrainController;
        [SerializeField, HideInInspector]
        List<GameObject> objectsOnTile = new List<GameObject>();

        [Header("Generic Cell Settings")]
        public TerrainType thisCellsTerrain;
        [SerializeField]
        [Range(0, 10)]
        float terrainHeightOffset;
        public bool ignoreLeylineStamp;
        public Vector2 DetailPathSpawnDirectionMinMax = new Vector2(0, 360);


        [Header("Manalith Settings")]
        public int ManalithChainIndex;
        [SerializeField]
        GameObject ManalithObject;
        [SerializeField]
        public uint ManalithSpawnDirection;

        // Use this for initialization
        void OnEnable()
        {
            if(!terrainController)
                terrainController = FindObjectOfType<TerrainController>();
        }

        public void UpdateTerrainTexture()
        {
            terrainController.SetHexagonTerrainTexture(cellDimensions.Size, transform.localPosition + transform.parent.localPosition - new Vector3(0, terrainHeightOffset, 0), thisCellsTerrain.textureIndex);
        }

        public void UpdateTerrainHeight()
        {
            terrainController.SetHexagonTerrainHeight(cellDimensions.Size, transform.localPosition + transform.parent.localPosition - new Vector3(0, terrainHeightOffset, 0), transform.position);
        }

        public void ApplyCellOffset()
        {
            //pos3D = new float3(pos3D.x, transform.parent.position.y + terrainHeightOffset + thisCellsTerrain.yOffset, pos3D.z);
            //pos3D.Value.y = transform.parent.position.y + height;
            transform.localPosition = new Vector3(transform.localPosition.x, thisCellsTerrain.yOffset + terrainHeightOffset, transform.localPosition.z);
        }

        //This thing here should be called when a new TerrainType is set
        //Converts all the info stored on that type into what is on the tile and spawns objects
        public void UpdateTerrain()
        {
            if (thisCellsTerrain != null)
            {
                if(thisCellsTerrain.cellTerrainYOffset != 0)
                    terrainHeightOffset = thisCellsTerrain.cellTerrainYOffset;

                transform.localPosition = new Vector3(transform.localPosition.x, thisCellsTerrain.yOffset + terrainHeightOffset, transform.localPosition.z);
                UpdateTerrainHeight();
                UpdateTerrainTexture();

                if (thisCellsTerrain.Walkable)
                {
                    isTaken.Value = false;
                }
                else
                {
                    isTaken.Value = true;
                }

                GetComponent<EditorIsCircleCell>().IsLeylineCircleCell = false;

                if (objectsOnTile.Count > 0)
                {
                    for (int b = objectsOnTile.Count - 1; b >= 0; b--)
                    {
                        DestroyImmediate(objectsOnTile[b]);
                    }
                    objectsOnTile.Clear();
                }

                if (thisCellsTerrain.AssetsToSpawn.Count > 0)
                {
                    if (thisCellsTerrain.TerrainName == "Manalith")
                    {
                        if(ManalithObject)
                        {
                            var manaLithParent = GameObject.Find("Manaliths").transform;
                            var manaLithGroup = manaLithParent.GetComponent<ManalithGroup>();
                            GameObject go = (GameObject) PrefabUtility.InstantiatePrefab(ManalithObject);
                            go.transform.position = transform.position;
                            go.transform.Rotate(new Vector3(0, ManalithSpawnDirection, 0));
                            go.transform.parent = manaLithParent;
                            var initializer = go.GetComponent<ManalithInitializer>();
                            initializer.occupiedCell = GetComponent<Cell>();
                            GetComponent<IsTaken>().Value = true;
                            go.transform.SetSiblingIndex(ManalithChainIndex);
                            manaLithGroup.ManalithInitializers[ManalithChainIndex] = initializer;
                            manaLithGroup.ConnectManalithInitializerScripts();
                            objectsOnTile.Add(go);
                        }
                        else
                        {
                            Debug.LogError("No Manalith Object Assigned to Manalith Cell - Assign Manalith Object to Cell in editor");
                        }
                    }
                }
            }
        }
    }
}

#endif
