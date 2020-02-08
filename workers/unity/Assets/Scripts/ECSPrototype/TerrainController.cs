﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;


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

        [SerializeField]
        int firVariantCount;

        [SerializeField]
        Vector2 treeHeightMinMax;

        [SerializeField]
        Vector2 grassHeightMinMax;

        [SerializeField]
        float grassCircleRange;

        [SerializeField]
        int randomGrasRotationMax;

        float[,] terrainHeights;

        List<TreeInstance> treeList = new List<TreeInstance>();

        public List<Vector3> leyLineCrackPositions = new List<Vector3>();

        [SerializeField]
        List<Texture2D> leyLineCrackBrushes;

        [SerializeField]
        public Vector3 leyLineCrackWorldPos;

        [SerializeField]
        public float leyLineCrackSize;

        [SerializeField]
        float[,] strength;


        #if UNITY_EDITOR

        public void GetTerrainHeight()
        {
            //print(terrain.terrainData.GetHeight((int)getTerrainHeightCoordinates.x, (int)getTerrainHeightCoordinates.y));
            print(terrain.terrainData.alphamapWidth);
            print(terrain.terrainData.heightmapWidth);
        }

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



            int r = UnityEngine.Random.Range(0, leyLineCrackBrushes.Count);
            Texture2D textureToUse = leyLineCrackBrushes[r];

            //generates a square
            for (int x = 0; x < totalXrange; x++)
            {
                for (int y = 0; y < totalYrange; y++)
                {
                    strength[x, y] = 1 - textureToUse.GetPixelBilinear(x / (float)totalXrange, y / (float)totalYrange).a;

                    if(strength[x,y] == 0)
                        terrainHeights[x, y] = strength[x, y] + (position.y / 600);
                    else
                    {
                        terrainHeights[x, y] = terrain.terrainData.GetHeight(xOffset + y, yOffset + x) / 600;
                    }
                }
            }
            terrain.terrainData.SetHeights(xOffset, yOffset, terrainHeights);
        }

        public void UpdateTerrainHeight()
        {
            foreach (Cell c in hexGridGenerator.hexagons)
            {
                SetHexagonTerrainHeight(c.GetComponent<CellDimensions>().Size, c.transform.position);
            }
        }

        public void UpdateHexagonTrees()
        {
            treeList = new List<TreeInstance>();

            foreach (Cell c in hexGridGenerator.hexagons)
            {
                TerrainType terrainType = c.GetComponent<CellType>().thisCellsTerrain;

                if (terrainType.TreeIndexMinMax.x != 0)
                {
                    if (terrainType.GrassAmountMinMax.x == 0)
                    {
                        TreeInstance treeInstance = new TreeInstance()
                        {
                            prototypeIndex = UnityEngine.Random.Range((int)terrainType.TreeIndexMinMax.x, (int)terrainType.TreeIndexMinMax.y + 1) - 1,
                            color = Color.black,
                            heightScale = UnityEngine.Random.Range(treeHeightMinMax.x, treeHeightMinMax.y),
                            widthScale = UnityEngine.Random.Range(treeHeightMinMax.x, treeHeightMinMax.y),
                            rotation = UnityEngine.Random.Range(0f, 360f)
                        };
                        SpawnHexagonTree(c.transform.position - transform.parent.position, treeInstance);
                    }
                    else
                    {
                        int grassAmount = UnityEngine.Random.Range((int)terrainType.GrassAmountMinMax.x, (int)terrainType.GrassAmountMinMax.y + 1);

                        for (int i = 0; i < grassAmount; i++)
                        {
                            TreeInstance treeInstance = new TreeInstance()
                            {
                                prototypeIndex = UnityEngine.Random.Range((int)terrainType.TreeIndexMinMax.x, (int)terrainType.TreeIndexMinMax.y + 1) - 1,
                                color = Color.black,
                                heightScale = UnityEngine.Random.Range(grassHeightMinMax.x, grassHeightMinMax.y),
                                widthScale = UnityEngine.Random.Range(grassHeightMinMax.x, grassHeightMinMax.y),
                                rotation = UnityEngine.Random.Range(0f, randomGrasRotationMax)
                            };

                            //can spawn at same place
                            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * grassCircleRange;

                            Vector3 pos = c.transform.position - transform.parent.position + new Vector3(randomOffset.x, 0, randomOffset.y);

                            SpawnHexagonTree(pos, treeInstance);
                        }
                    }
                }
            }

            //add grass to treeList before setting terrain.treeInstances

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

        public Vector2 WorldToHeightMapPos(float size, Vector3 hexPos)
        {
            Vector2 terrainPos = new Vector2();
            Vector2 rectSize = new Vector2();

            float w = size * 2;
            float h = size * Mathf.Sqrt(3);

            //convert length of array to raise to world space unit
            rectSize.x = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * w);
            rectSize.y = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * h);


            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            terrainPos.x = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * hexPos.x) - width / 2;
            terrainPos.y = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * hexPos.z) - height / 2;

            return terrainPos;
        }

        public Vector2 WorldToDetailMapMapPos(float size, Vector3 hexPos)
        {
            Vector2 detailPos = new Vector2();
            Vector2 rectSize = new Vector2();

            float w = size * 2;
            float h = size * Mathf.Sqrt(3);

            //convert length of array to raise to world space units
            rectSize.x = (int)(terrain.terrainData.detailWidth / terrain.terrainData.size.x * w);
            rectSize.y = (int)(terrain.terrainData.detailHeight / terrain.terrainData.size.z * h);


            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            detailPos.x = (int)(terrain.terrainData.detailWidth / terrain.terrainData.size.x * hexPos.x) - width / 2;
            detailPos.y = (int)(terrain.terrainData.detailHeight / terrain.terrainData.size.z * hexPos.z) - height / 2;

            return detailPos;
        }

        public Vector2 WorldToAlphaMapMapPos(float size, Vector3 hexPos)
        {
            Vector2 alphaMapPos = new Vector2();
            Vector2 rectSize = new Vector2();

            float w = size * 2;
            float h = size * Mathf.Sqrt(3);

            //convert length of array to raise to world space units
            rectSize.x = (int)(terrain.terrainData.alphamapWidth / terrain.terrainData.size.x * w);
            rectSize.y = (int)(terrain.terrainData.alphamapHeight / terrain.terrainData.size.z * h);


            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            alphaMapPos.x = (int)(terrain.terrainData.alphamapHeight / terrain.terrainData.size.x * hexPos.x) - width / 2;
            alphaMapPos.y = (int)(terrain.terrainData.alphamapWidth / terrain.terrainData.size.z * hexPos.z) - height / 2;

            return alphaMapPos;
        }

        public float[,] HeightPixelArray(float size, Vector3 hexPos)
        {
            Vector2 rectSize = new Vector2();
            float w = size * 2;
            float h = size * Mathf.Sqrt(3);
            //convert length of array to raise to world space units
            rectSize.x = (int)(terrain.terrainData.heightmapWidth / terrain.terrainData.size.x * w);
            rectSize.y = (int)(terrain.terrainData.heightmapHeight / terrain.terrainData.size.z * h + 1);

            float [,] pixelArray = new float[(int)rectSize.y, (int)rectSize.x];

            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            int xRange;

            for (int x = 0; x < width; x++)
            {
                
                if (x < (int)(width / hexXrangeMultiplier))
                {
                    xRange = (int)((x + 2) * hexXrangeMultiplier);
                }
                else if (x > (int)(width - width / hexXrangeMultiplier))
                {
                    xRange = (int)(hexXrangeMultiplier * (width - x));
                }
                else
                {
                    xRange = width;
                }
                

                for (int y = 0; y < height; y++)
                {
                    if (y >= (height / 2) - xRange / 2 && y <= (height / 2) + xRange / 2)
                    //if the point to raise is not a part of the hex, set it to be the position it had before
                    pixelArray[y, x] = hexPos.y / 600;
                    
                    else
                    {

                        Vector3 offsetPos;
                        if (x < width / 2)
                        {
                            if (y < height / 2)
                            {
                                //top
                                offsetPos = new Vector3(-w / 2, 0, -h / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(-w / 2, 0, h / 2);
                            }

                        }
                        else
                        {
                            if (y < height / 2)
                            {
                                //top
                                offsetPos = new Vector3(w / 2, 0, -h / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(w / 2, 0, h / 2);
                            }
                        }
                        pixelArray[y, x] = terrain.SampleHeight(hexPos + offsetPos) / 600;
                    }
                    
                }
            }
                return pixelArray;
        }

        public int[,] DetailPixelArray(float size, Vector3 hexPos, float spawnPercentage)
        {
            Vector2 rectSize = new Vector2();
            float w = size * 2;
            float h = size * Mathf.Sqrt(3);
            //convert length of array to raise to world space units
            rectSize.x = (int)(terrain.terrainData.detailWidth / terrain.terrainData.size.x * w);
            rectSize.y = (int)(terrain.terrainData.detailHeight / terrain.terrainData.size.z * h);

            int[,] pixelArray = new int[(int)rectSize.y, (int)rectSize.x];

            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            int xRange;

            for (int x = 0; x < width; x++)
            {
                if (x < (int)(width / hexXrangeMultiplier))
                {
                    xRange = (int)((x + 1) * hexXrangeMultiplier);
                }
                else if (x > (int)(width - width / hexXrangeMultiplier))
                {
                    xRange = (int)(hexXrangeMultiplier * (width - x));
                }
                else
                {
                    xRange = width;
                }

                for (int y = 0; y < height; y++)
                {
                    if (y >= (height / 2) - xRange / 2 && y <= (height / 2) + xRange / 2)
                    {
                        float random = UnityEngine.Random.Range(0f, 1f);

                        if (random < spawnPercentage)
                        {
                            pixelArray[y, x] = 1;
                        }
                        else
                        {
                            pixelArray[y, x] = 0;
                        }
                    }
                    else
                    {
                        pixelArray[y, x] = 0;
                    }
                }
            }
            return pixelArray;
        }

        public float[,,] AlphaMapPixelArray(float size, Vector3 hexPos, int textureLayerIndex)
        {
            Vector2 rectSize = new Vector2();
            float w = size * 2;
            float h = size * Mathf.Sqrt(3) + .1f;
            //convert length of array to raise to world space units
            rectSize.x = (int)(terrain.terrainData.alphamapWidth / terrain.terrainData.size.x * w);
            rectSize.y = (int)(terrain.terrainData.alphamapHeight / terrain.terrainData.size.z * h);

            int width = (int)rectSize.x;
            int height = (int)rectSize.y;

            float[,,] alphaMap = new float[height, width, terrain.terrainData.alphamapLayers];

            int xRange;

            for (int x = 0; x < width; x++)
            {
                if (x < (int)(width / hexXrangeMultiplier))
                {
                    xRange = (int)((x + 1) * hexXrangeMultiplier);
                }
                else if (x > (int)(width - width / hexXrangeMultiplier))
                {
                    xRange = (int)(hexXrangeMultiplier * (width - x));
                }
                else
                {
                    xRange = width;
                }

                for (int y = 0; y < height; y++)
                {
                    if (y >= (height / 2) - xRange / 2 && y <= (height / 2) + xRange / 2)
                        //if the point to raise is not a part of the hex, set it to be the position it had before
                        alphaMap[y, x, textureLayerIndex] = 1;
                    else
                    {

                        Vector3 offsetPos;
                        if (x < width / 2)
                        {
                            if (y < height / 2)
                            {
                                //top
                                offsetPos = new Vector3(-w / 2, 0, -h / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(-w / 2, 0, h / 2);
                            }

                        }
                        else
                        {
                            if (y < height / 2)
                            {
                                //top
                                offsetPos = new Vector3(w / 2, 0, -h / 2);
                            }
                            else
                            {
                                offsetPos = new Vector3(w / 2, 0, h / 2);
                            }
                        }

                        alphaMap[y, x, GetMainTextureAtWorldPos(hexPos + offsetPos)] = 1;
                    }

                }
            }

            return alphaMap;
        }

        public void SetHexagonTerrainHeight(float size, Vector3 hexPos)
        {
            Vector2 terrainPos = WorldToHeightMapPos(size, hexPos);
            terrainHeights = HeightPixelArray(size, hexPos);
            terrain.terrainData.SetHeights((int)terrainPos.x, (int)terrainPos.y, terrainHeights);
        }

        public void SetHexagonTerrainDetails(float size, Vector3 hexPos, int detailLayerIndex, float spawnPercentage)
        {
            Vector2 detailPos = WorldToDetailMapMapPos(size, hexPos);
            int[,] detailMap = DetailPixelArray(size, hexPos, spawnPercentage);
            terrain.terrainData.SetDetailLayer((int)detailPos.x, (int)detailPos.y, detailLayerIndex, detailMap);
        }

        public void SetHexagonTerrainTexture(float size, Vector3 hexPos, int textureLayerIndex)
        {
            Vector2 alphaPos = WorldToAlphaMapMapPos(size, hexPos);
            float[,,] alphaMap = AlphaMapPixelArray(size, hexPos, textureLayerIndex);
            terrain.terrainData.SetAlphamaps((int)alphaPos.x, (int)alphaPos.y, alphaMap);
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

        public void SmoothTerrainHeights()
        {

            float[,] smoothHeights = smooth(terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight), new Vector2(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight), 1);
            terrain.terrainData.SetHeights(0, 0, smoothHeights);

        }

        private float[,] smooth(float[,] heightMap, Vector2 arraySize, int iterations)
        {
            int Tw = (int)arraySize.x;
            int Th = (int)arraySize.y;
            int xNeighbours;
            int yNeighbours;
            int xShift;
            int yShift;
            int xIndex;
            int yIndex;
            int Tx;
            int Ty;
            // Start iterations...
            for (int iter = 0; iter < iterations; iter++)
            {

                for (Ty = 0; Ty < Th; Ty++)
                {
                    // y...
                    if (Ty == 0)
                    {
                        yNeighbours = 2;
                        yShift = 0;
                        yIndex = 0;
                    }
                    else if (Ty == Th - 1)
                    {
                        yNeighbours = 2;
                        yShift = -1;
                        yIndex = 1;
                    }
                    else
                    {
                        yNeighbours = 3;
                        yShift = -1;
                        yIndex = 1;
                    }
                    for (Tx = 0; Tx < Tw; Tx++)
                    {
                        // x...
                        if (Tx == 0)
                        {
                            xNeighbours = 2;
                            xShift = 0;
                            xIndex = 0;
                        }
                        else if (Tx == Tw - 1)
                        {
                            xNeighbours = 2;
                            xShift = -1;
                            xIndex = 1;
                        }
                        else
                        {
                            xNeighbours = 3;
                            xShift = -1;
                            xIndex = 1;
                        }
                        int Ny;
                        int Nx;
                        float hCumulative = 0.0f;
                        int nNeighbours = 0;
                        for (Ny = 0; Ny < yNeighbours; Ny++)
                        {
                            for (Nx = 0; Nx < xNeighbours; Nx++)
                            {
                                if (Nx == xIndex || Ny == yIndex)
                                {
                                    float heightAtPoint = heightMap[Tx + Nx + xShift, Ty + Ny + yShift]; // Get height at point
                                    hCumulative += heightAtPoint;
                                    nNeighbours++;
                                }
                            }
                        }
                        float hAverage = hCumulative / nNeighbours;
                        heightMap[Tx + xIndex + xShift, Ty + yIndex + yShift] = hAverage;
                    }
                }
            }
            return heightMap;
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

        #endif
    }
}