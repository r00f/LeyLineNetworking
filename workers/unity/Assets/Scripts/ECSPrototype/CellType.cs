using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace LeyLineHybridECS
{

    [ExecuteInEditMode()]
    public class CellType : MonoBehaviour
    {
        public int ManalithChainIndex;
        [SerializeField]
        IsTaken isTaken;
        [SerializeField]
        Position3DDataComponent pos3D;
        [SerializeField]
        CellDimensions cellDimensions;
        [SerializeField]
        TerrainController terrainController;

        [SerializeField]
        List<GameObject> objectsOnTile = new List<GameObject>();

        public TerrainType thisCellsTerrain;

        [SerializeField]
        [Range(0, 10)]
        float terrainHeightOffset;

        #if UNITY_EDITOR

        // Use this for initialization
        void OnEnable()
        {

            if(!terrainController)
                terrainController = FindObjectOfType<TerrainController>();
            /*
            if(!isTaken)
                isTaken = GetComponent<IsTaken>();
            if(!pos3D)
                pos3D = GetComponent<Position3DDataComponent>();
            if(!cellDimensions)
                cellDimensions = GetComponent<CellDimensions>();
           */
            //myHexagon = GetComponent<MyHexagon>();
        }


        //This thing here should be called when a new TerrainType is set
        //Converts all the info stored on that type into what is on the tile and spawns objects
        public void UpdateTerrain()
        {

            if (thisCellsTerrain != null)
            {
                pos3D.Value = new Position3D
                {
                    Value = new Vector3(pos3D.Value.Value.x, transform.parent.position.y + terrainHeightOffset + thisCellsTerrain.yOffset, pos3D.Value.Value.z)
                };
                //pos3D.Value.y = transform.parent.position.y + height;
                transform.localPosition = new Vector3(transform.localPosition.x, thisCellsTerrain.yOffset + terrainHeightOffset, transform.localPosition.z);


                terrainController.SetHexagonTerrainHeight(cellDimensions.Size, transform.position - new Vector3(0, terrainHeightOffset, 0));
                terrainController.SetHexagonTerrainTexture(cellDimensions.Size, transform.position - new Vector3(0, terrainHeightOffset, 0), thisCellsTerrain.textureIndex);
                //terrainController.SetHexagonTerrainDetails(cellDimensions.Size, transform.position, thisCellsTerrain.detailIndex, thisCellsTerrain.detailSpawnPercentage);
                terrainController.UpdateHexagonTrees();

                if (thisCellsTerrain.Walkable)
                {
                    isTaken.Value = false;
                }
                else
                {
                    isTaken.Value = true;
                }

                if (objectsOnTile.Count > 0)
                {
                    for (int b = objectsOnTile.Count - 1; b >= 0; b--)
                    {
                        DestroyImmediate(objectsOnTile[b]);
                    }
                    objectsOnTile.Clear();
                }

                if (thisCellsTerrain.assets_to_Spawn.Count > 0)
                {
                    if (thisCellsTerrain.spawnAllAssets)
                    {
                        for (int i = thisCellsTerrain.assets_to_Spawn.Count; i >= 0; i--)
                        {
                            /*
                            if (Random.Range(0, 100) <= thisCellsTerrain.probabilityToSpawnAsset)
                            {
                                
                                GameObject go = PrefabUtility.InstantiatePrefab(thisCellsTerrain.assets_to_Spawn[i]) as GameObject;
                                go.transform.parent = transform;
                                go.transform.position = transform.position;
                                
                                GameObject go = Instantiate(thisCellsTerrain.assets_to_Spawn[i], transform.position, Quaternion.identity, transform);
                                objectsOnTile.Add(go);

                            }
                            */
                        }
                    }
                    else
                    {
                        if (thisCellsTerrain.terrain_Name == "Manalith")
                        {
                            var manaLithParent = GameObject.Find("Manaliths").transform;
                            var manaLithGroup = manaLithParent.GetComponent<ManalithGroup>();
                            GameObject go = Instantiate(thisCellsTerrain.assets_to_Spawn[0], transform.position, Quaternion.identity, manaLithParent);
                            var initializer = go.GetComponent<ManalithInitializer>();
                            initializer.occupiedCell = GetComponent<Cell>();
                            go.transform.SetSiblingIndex(ManalithChainIndex);
                            manaLithGroup.ManalithInitializers[ManalithChainIndex] = initializer;
                            manaLithGroup.ConnectManalithInitializerScripts();
                            objectsOnTile.Add(go);
                        }
                        /*
                        else
                        {
                            GameObject go = Instantiate(thisCellsTerrain.assets_to_Spawn[0], transform.position, Quaternion.identity, transform);
                            objectsOnTile.Add(go);
                        }

                        
                       int c = Random.Range(0, thisCellsTerrain.assets_to_Spawn.Count);
                       if (Random.Range(0, 100) <= thisCellsTerrain.probabilityToSpawnAsset)
                       {

                           GameObject go = PrefabUtility.InstantiatePrefab(thisCellsTerrain.assets_to_Spawn[c]) as GameObject;
                           go.transform.parent = transform;
                           go.transform.position = transform.position;



                       }
                       */
                    }
                }
            }
        }

#endif
    }


}
