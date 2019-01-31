using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;


#if UNITY_EDITOR
namespace LeyLineHybridECS
{

    public class TerrainController : MonoBehaviour
    {
        [SerializeField]
        Terrain terrain;

        [SerializeField]
        HexagonalHexGridGenerator hexGridGenerator;

        [SerializeField]
        float hexXrangeMultiplier;

        [SerializeField]
        float height;

        [SerializeField]
        int textureIndex;

        float[,] terrainHeights;

        List<TreeInstance> treeList = new List<TreeInstance>();

        public List<Vector3> leyLineCrackPositions = new List<Vector3>();

        [SerializeField]
        Texture2D leyLineCrackBrush;

        [SerializeField]
        public Vector3 leyLineCrackWorldPos;

        [SerializeField]
        public float leyLineCrackSize;

        [SerializeField]
        float[,] strength;

        public void GetTerrainHeight()
        {
            //print(terrain.terrainData.GetHeight((int)getTerrainHeightCoordinates.x, (int)getTerrainHeightCoordinates.y));
            print(terrain.terrainData.alphamapWidth);
            print(terrain.terrainData.heightmapWidth);
        }

#if UNITY_EDITOR

        public void UpdateLeyLineCracks()
        {
            //UpdateTerrainHeight();
            foreach(Vector3 v in leyLineCrackPositions)
            {
                PaintHeightAtPosition(leyLineCrackSize, v);
            }

        }

        public void PaintHeightAtPosition(float size, Vector3 position)
        {
            
            //paint height with crackbrush at terrain position

            //convert length of array to raise to world space units
            int totalXrange = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * size);
            int totalYrange = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * size);

            terrainHeights = new float[totalXrange, totalYrange];
            //print(terrainHeights.Length);
            strength = new float[totalXrange, totalYrange];
            //print(totalXrange);

            int xOffset = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * position.x) - totalXrange / 2;
            int yOffset = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * position.z) - totalYrange / 2;

            //generates a square
            for (int x = 0; x < totalXrange; x++)
            {

                for (int y = 0; y < totalYrange; y++)
                {

                    strength[x, y] = 1 - leyLineCrackBrush.GetPixelBilinear(x / (float)totalXrange, y / (float)totalYrange).a;

                    
                    //print(strength[x, y]);
                    //since I could not find MeshResolution -> Terrain Height access from code I hardcoded it
                    //this converts height from worldspace to terrainspace
                    if(strength[x,y] == 0)
                        terrainHeights[x, y] = strength[x, y] + (position.y / 600);
                    else
                    {
                        terrainHeights[x, y] = terrain.terrainData.GetHeight(xOffset + y, yOffset + x) / 600;
                    }
                }
            }

            //convert offset where to raise to world coordinates



            terrain.terrainData.SetHeights(xOffset, yOffset, terrainHeights);


        }

        public void UpdateTerrainHeight()
        {
            foreach (Cell c in hexGridGenerator.hexagons)
            {
                SetHexagonTerrainHeight(c.GetComponent<CellDimensions>().Value, c.transform.position);
            }
        }

        public void UpdateHexagonTrees()
        {
            treeList = new List<TreeInstance>();

            foreach (Cell c in hexGridGenerator.hexagons)
            {
                if (c.GetComponent<CellType>().thisCellsTerrain.spawnTree)
                {
                    TreeInstance treeInstance = new TreeInstance()
                    {
                        prototypeIndex = 0,
                        color = Color.black,
                        heightScale = UnityEngine.Random.Range(.1f, .2f),
                        widthScale = UnityEngine.Random.Range(.1f, .2f),
                    };
                    SpawnHexagonTree(c.transform.position, treeInstance);
                }
            }
            terrain.terrainData.treeInstances = treeList.ToArray();
            terrain.Flush();
        }

        public void SpawnHexagonTree(Vector3 hexPos, TreeInstance treeInstance)
        {

            float xCenter = 1 / terrain.terrainData.size.x * hexPos.x;
            float zCenter = 1 / terrain.terrainData.size.z * hexPos.z;
            float yCenter = hexPos.y / 600;

            treeInstance.position = new Vector3(xCenter, yCenter, zCenter);

            if (!treeList.Contains(treeInstance))
            {
                treeList.Add(treeInstance);
            }
        }

#endif

        public void SetSquareTerrainHeight()
        {
            /*
            //convert length of array to raise to world space units
            int totalXrange = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * terrainHeightsSize);
            int totalYrange = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * terrainHeightsSize);

            terrainHeights = new float[totalXrange, totalYrange];
            print(terrainHeights.Length);

            //generates a square
            for (int x = 0; x < totalXrange; x++)
            {

                for (int y = 0; y < totalYrange; y++)
                {
                    //since I could not find MeshResolution -> Terrain Height access from code I hardcoded it
                    //this converts height from worldspace to terrainspace
                    terrainHeights[x, y] = height / 600;
                }
            }

            //convert offset where to raise to world coordinates

            int xOffset = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * terrainOffsetCoordinates.x) - totalXrange / 2;
            int yOffset = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * terrainOffsetCoordinates.y) - totalYrange / 2;

            terrain.terrainData.SetHeights(xOffset, yOffset, terrainHeights);
            */
        }

        public void SetHexagonTerrainHeight(float2 hexSize, Vector3 hexPos)
        {
            //convert length of array to raise to world space units
            int totalXrange = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * hexSize.x);
            int totalYrange = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * hexSize.y);

            terrainHeights = new float[totalXrange, totalYrange];

            int xRange;

            int xCenter = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * hexPos.x) - totalXrange / 2;
            int yCenter = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * hexPos.z) - totalYrange / 2;

            //sets size of xRange, yRange to height

            for (int y = 0; y < totalYrange; y++)
            {
                if (y < (int)(totalYrange / hexXrangeMultiplier))
                {
                    xRange = (int)((y + 1) * hexXrangeMultiplier);
                }
                else if (y > (int)(totalYrange - totalYrange / hexXrangeMultiplier))
                {
                    xRange = (int)(hexXrangeMultiplier * (totalYrange - y));
                }
                else
                {
                    xRange = totalYrange;
                }

                for (int x = 0; x < totalXrange; x++)
                {
                    //since I could not find MeshResolution -> Terrain Height access from code I hardcoded it
                    //this converts height from worldspace to terrainspace
                    if (x >= (totalYrange / 2) - xRange / 2 && x <= (totalYrange / 2) + xRange / 2)
                        //if the point to raise is not a part of the hex, set it to be the position it had before
                        terrainHeights[x, y] = hexPos.y / 600;
                    else
                    {
                        Vector3 offsetPos;
                        if (x > totalXrange / 2)
                        {
                            if (y > totalYrange / 2)
                            {
                                //top
                                offsetPos = new Vector3(hexSize.x / 2, 0, hexSize.y / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(-hexSize.x / 2, 0, hexSize.y / 2);

                            }
                            //right

                            //print("right checkpos = " + checkPos.x);

                        }
                        else
                        {
                            if (y > totalYrange / 2)
                            {
                                //top
                                offsetPos = new Vector3(hexSize.x / 2, 0, -hexSize.y / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(-hexSize.x / 2, 0, -hexSize.y / 2);

                            }

                        }

                        terrainHeights[x, y] = terrain.SampleHeight(hexPos + offsetPos) / 600;
                    }

                }
            }

            terrain.terrainData.SetHeights(xCenter, yCenter, terrainHeights);

        }
        
        public void SetHexagonTerrainTexture(float hexSize, Vector3 hexPos, int textureLayerIndex)
        {
            //convert length of array to raise to world space units
            int totalXrange = (int)(terrain.terrainData.alphamapWidth / terrain.terrainData.size.x * hexSize);
            int totalYrange = (int)(terrain.terrainData.alphamapHeight / terrain.terrainData.size.z * hexSize);

            float[,,] alphaMap = new float[totalXrange, totalYrange, terrain.terrainData.alphamapLayers];

            int xRange;

            int xCenter = (int)(terrain.terrainData.alphamapWidth / terrain.terrainData.size.x * hexPos.x) - totalXrange / 2;
            int yCenter = (int)(terrain.terrainData.alphamapHeight / terrain.terrainData.size.z * hexPos.z) - totalYrange / 2;

            //sets size of xRange, yRange to height

            for (int y = 0; y < totalYrange; y++)
            {
                if (y < (int)(totalYrange / hexXrangeMultiplier))
                {
                    xRange = (int)((y + 1) * hexXrangeMultiplier);
                }
                else if (y > (int)(totalYrange - totalYrange / hexXrangeMultiplier))
                {
                    xRange = (int)(hexXrangeMultiplier * (totalYrange - y));
                }
                else
                {
                    xRange = totalYrange;
                }

                for (int x = 0; x < totalXrange; x++)
                {
                    //since I could not find MeshResolution -> Terrain Height access from code I hardcoded it
                    //this converts height from worldspace to terrainspace
                    if (x >= (totalYrange / 2) - xRange / 2 && x <= (totalYrange / 2) + xRange / 2)
                    {
                        alphaMap[x, y, textureLayerIndex] = 1;


                    }
                    //if the point to raise is not a part of the hex, set it to be the position it had before
                    else
                    {

                        Vector3 offsetPos;
                        if (x > totalXrange / 2)
                        {
                            if (y > totalYrange / 2)
                            {
                                //top
                                offsetPos = new Vector3(hexSize / 2, 0, hexSize / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(-hexSize / 2, 0, hexSize / 2);

                            }
                            //right

                            //print("right checkpos = " + checkPos.x);

                        }
                        else
                        {
                            if (y > totalYrange / 2)
                            {
                                //top
                                offsetPos = new Vector3(hexSize / 2, 0, -hexSize / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(-hexSize / 2, 0, -hexSize / 2);

                            }

                        }

                        alphaMap[x, y, GetMainTextureAtWorldPos(hexPos + offsetPos)] = 1;
                    }

                }
            }

            terrain.terrainData.SetAlphamaps(xCenter, yCenter, alphaMap);
        }

        public void SetHexagonTerrainDetails(float hexSize, Vector3 hexPos, int detailLayerIndex, float spawnPercentage)
        {
            //convert length of array to raise to world space units
            int totalXrange = (int)(terrain.terrainData.detailWidth / terrain.terrainData.size.x * hexSize);
            int totalYrange = (int)(terrain.terrainData.detailHeight / terrain.terrainData.size.z * hexSize);

            int[,] detailMap = new int[totalXrange, totalYrange];

            int xRange;

            int xCenter = (int)(terrain.terrainData.detailWidth / terrain.terrainData.size.x * hexPos.x) - totalXrange / 2;
            int yCenter = (int)(terrain.terrainData.detailHeight / terrain.terrainData.size.z * hexPos.z) - totalYrange / 2;

            //sets size of xRange, yRange to height

            for (int y = 0; y < totalYrange; y++)
            {
                if (y < (int)(totalYrange / hexXrangeMultiplier))
                {
                    xRange = (int)((y + 1) * hexXrangeMultiplier);
                }
                else if (y > (int)(totalYrange - totalYrange / hexXrangeMultiplier))
                {
                    xRange = (int)(hexXrangeMultiplier * (totalYrange - y));
                }
                else
                {
                    xRange = totalYrange;
                }

                for (int x = 0; x < totalXrange; x++)
                {
                    //since I could not find MeshResolution -> Terrain Height access from code I hardcoded it
                    //this converts height from worldspace to terrainspace
                    if (x >= (totalYrange / 2) - xRange / 2 && x <= (totalYrange / 2) + xRange / 2)
                    {
                        float random = UnityEngine.Random.Range(0f, 1f);

                        if (random < spawnPercentage)
                        {
                            detailMap[x, y] = 1;
                        }
                        else
                        {
                            detailMap[x, y] = 0;
                        }

                    }
                    //if the point to spawn detail is not a part of the hex, don't spawn any detail
                    else
                    {
                        detailMap[x, y] = 0;
                    }

                }
            }
            terrain.terrainData.SetDetailLayer(xCenter, yCenter, detailLayerIndex, detailMap);
        }





        public void RemoveHexagonTree(TreeInstance treeInstance)
        {
            if (treeList.Contains(treeInstance))
            {
                treeList.Remove(treeInstance);
            }
            terrain.terrainData.treeInstances = treeList.ToArray();
            terrain.Flush();
        }

        public void SetSlopeTexture()
        {

            float[,,] map = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, terrain.terrainData.alphamapLayers];

            // For each point on the alphamap...
            for (int y = 0; y < terrain.terrainData.alphamapHeight; y++)
            {
                for (int x = 0; x < terrain.terrainData.alphamapWidth; x++)
                {
                    // Get the normalized terrain coordinate that
                    // corresponds to the the point.
                    float normX = x * 1.0f / (terrain.terrainData.alphamapWidth - 1);
                    float normY = y * 1.0f / (terrain.terrainData.alphamapHeight - 1);

                    // Get the steepness value at the normalized coordinate.
                    var angle = terrain.terrainData.GetSteepness(normX, normY);

                    // Steepness is given as an angle, 0..90 degrees. Divide
                    // by 90 to get an alpha blending value in the range 0..1.
                    var frac = angle / 90.0;
                    //slope texture
                    map[y, x, 3] = (float)frac;
                    //non slope texture
                    map[y, x, GetMainTextureAtTerrainPos(new Vector2(x, y))] = (float)(1 - frac);
                }
            }
            terrain.terrainData.SetAlphamaps(0, 0, map);
        }

        #region Sample Texture at World Pos

        private float[] GetTextureMixAtWorldPos(Vector3 WorldPos)
        {
            // returns an array containing the relative mix of textures
            // on the main terrain at this world position.

            // The number of values in the array will equal the number
            // of textures added to the terrain.

            // calculate which splat map cell the worldPos falls within (ignoring y)
            int mapX = (int)((WorldPos.x / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth);
            int mapZ = (int)((WorldPos.z / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight);

            // get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
            float[,,] splatmapData = terrain.terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            // extract the 3D array data to a 1D array:
            float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

            for (int n = 0; n < cellMix.Length; n++)
            {
                cellMix[n] = splatmapData[0, 0, n];
            }
            return cellMix;
        }

        private int GetMainTextureAtWorldPos(Vector3 WorldPos)
        {
            // returns the zero-based index of the most dominant texture
            // on the main terrain at this world position.
            float[] mix = GetTextureMixAtWorldPos(WorldPos);

            float maxMix = 0;
            int maxIndex = 0;

            // loop through each mix value and find the maximum
            for (int n = 0; n < mix.Length; n++)
            {
                if (mix[n] > maxMix)
                {
                    maxIndex = n;
                    maxMix = mix[n];
                }
            }
            return maxIndex;
        }

        private float[] GetTextureMixAtTerrainPos(Vector2 terrainPos)
        {
            // returns an array containing the relative mix of textures
            // on the main terrain at this world position.

            // The number of values in the array will equal the number
            // of textures added to the terrain.

            // calculate which splat map cell the worldPos falls within (ignoring y)
            int mapX = (int)terrainPos.x;
            int mapZ = (int)terrainPos.y;

            // get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
            float[,,] splatmapData = terrain.terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            // extract the 3D array data to a 1D array:
            float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

            for (int n = 0; n < cellMix.Length; n++)
            {
                cellMix[n] = splatmapData[0, 0, n];
            }
            return cellMix;
        }

        private int GetMainTextureAtTerrainPos(Vector2 terrainPos)
        {
            // returns the zero-based index of the most dominant texture
            // on the main terrain at this world position.
            float[] mix = GetTextureMixAtTerrainPos(terrainPos);

            float maxMix = 0;
            int maxIndex = 0;

            // loop through each mix value and find the maximum
            for (int n = 0; n < mix.Length; n++)
            {
                if (mix[n] > maxMix)
                {
                    maxIndex = n;
                    maxMix = mix[n];
                }
            }
            return maxIndex;
        }

        #endregion

        public void SetWholeTerrainHeight()
        {
            int xRes = terrain.terrainData.heightmapWidth;
            int yRes = terrain.terrainData.heightmapHeight;

            terrainHeights = new float[xRes, yRes];

            //sets the whole Terrain to height in world coordinates.

            for (int x = 0; x < xRes; x++)
            {
                for (int y = 0; y < yRes; y++)
                {
                    //since I could not find MeshResolution > Terrain Height access from code I hardcoded it
                    terrainHeights[x, y] = height / 600;
                }
            }

            terrain.terrainData.SetHeights(0, 0, terrainHeights);
        }

        public void SetWholeTerrainTexture()
        {
            int xRes = terrain.terrainData.alphamapWidth;
            int yRes = terrain.terrainData.alphamapHeight;

            float[,,] alphaMap = new float[xRes, yRes, terrain.terrainData.alphamapLayers];

            //sets the whole Terrain to height in world coordinates.

            for (int x = 0; x < xRes; x++)
            {
                for (int y = 0; y < yRes; y++)
                {
                    alphaMap[x, y, textureIndex] = 1;
                }
            }

            terrain.terrainData.SetAlphamaps(0, 0, alphaMap);
        }


    }
}
#endif