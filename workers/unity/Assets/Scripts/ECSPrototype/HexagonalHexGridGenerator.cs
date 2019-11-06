using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using System;
using Unity.Entities;
/// <summary>
/// Generates hexagonal shaped grid of hexagons.
/// </summary>
/// 


namespace LeyLineHybridECS
{
    [ExecuteInEditMode()]
    class HexagonalHexGridGenerator : ICellGridGenerator
    {

        public GameObject HexagonPrefab;
        [SerializeField]
        int Radius;

        public List<Cell> hexagons;

        #if UNITY_EDITOR

        public override List<Cell> GenerateGrid()
        {
            hexagons = new List<Cell>();



            for (int i = 0; i < Radius; i++)
            {
                for (int j = 0; j < (Radius * 2) - i - 1; j++)
                {
                    GameObject hexagon = Instantiate(HexagonPrefab);
                    float w = hexagon.GetComponent<CellDimensions>().Size * 2;
                    float h = hexagon.GetComponent<CellDimensions>().Size * Mathf.Sqrt(3);
                    //GameObject hexagon = PrefabUtility.InstantiatePrefab(HexagonPrefab as GameObject) as GameObject;
                    //Vector2 hexSize = hexagon.GetComponent<CellDimensions>().Value;

                    hexagon.transform.position = transform.position + new Vector3((i * w * 0.75f), 0, (i * h * 0.5f) + (j * h));
                    hexagon.GetComponent<Position3DDataComponent>().Value = new Position3D
                    {
                        Value = hexagon.transform.position
                    };

                    float2 offsetCoord = new float2(i, Radius - j - 1 - (i / 2));
                    hexagon.GetComponent<CoordinateDataComponent>().Value = new GridCoordinates
                    {

                        OffsetCoordinate = offsetCoord,
                        CubeCoordinate = CubeCoord(offsetCoord)
                    };
                     
                    hexagon.GetComponent<MovementCost>().Value = 1;

                    //hexagon.GetComponent<Hexagon>().HexGridType = HexGridType.odd_q;
                    hexagons.Add(hexagon.GetComponent<Cell>());

                    hexagon.transform.parent = CellsParent;

                    if (i == 0) continue;

                    GameObject hexagon2 = Instantiate(HexagonPrefab);
                    //GameObject hexagon2 = PrefabUtility.InstantiatePrefab(HexagonPrefab as GameObject) as GameObject;

                    hexagon2.transform.position = transform.position + new Vector3((-i * w * 0.75f), 0, (i * h * 0.5f) + (j * h));
                    hexagon2.GetComponent<Position3DDataComponent>().Value = new Position3D
                    {
                        Value = hexagon2.transform.position
                    };

                    float2 offsetCoord2 = new float2(-i, Radius - j - 1 - (i / 2));

                    hexagon2.GetComponent<CoordinateDataComponent>().Value = new GridCoordinates
                    {
                        OffsetCoordinate = offsetCoord2,
                        CubeCoordinate = CubeCoord(offsetCoord2)
                    };

                    hexagon2.GetComponent<MovementCost>().Value = 1;

                    //hexagon2.GetComponent<Hexagon>().HexGridType = HexGridType.odd_q;
                    hexagons.Add(hexagon2.GetComponent<Cell>());
                    hexagon2.transform.parent = CellsParent;
                }
            }

                foreach (var h in hexagons)
                {
                    var neighbours = h.GetComponent<Neighbours>().NeighboursList;
                    var offsetCoord = h.GetComponent <CoordinateDataComponent>().Value.OffsetCoordinate;
                    var cellType = h.GetComponent<CellType>();

                    cellType.UpdateTerrain();

                foreach (var direction in _directions)
                    {
                        Cell neighbour = Array.Find(hexagons.ToArray(), c => c.GetComponent<CoordinateDataComponent>().Value.OffsetCoordinate.Equals(CubeToOffsetCoords(CubeCoord(offsetCoord) + direction)));
                        if (neighbour == null) continue;
                        neighbours.Add(neighbour);
                    }
                }

            
            return hexagons;
        }

        protected float3 CubeCoord(float2 offsetCoordinates)
        {

            float3 ret = new float3
            {
                x = offsetCoordinates.x,
                z = offsetCoordinates.y - (offsetCoordinates.x + (Mathf.Abs(offsetCoordinates.x) % 2)) / 2
            };
            ret.y = -ret.x - ret.z;

            return ret;

        }


        /// <summary>
        /// Converts cube coordinates back to offset coordinates.
        /// </summary>
        /// <param name="cubeCoords">Cube coordinates to convert.</param>
        /// <returns>Offset coordinates corresponding to given cube coordinates.</returns>

        protected float2 CubeToOffsetCoords(float3 cubeCoords)
        {
            float2 ret = new float2
            {
                x = cubeCoords.x,
                y = cubeCoords.z + (cubeCoords.x + (Mathf.Abs(cubeCoords.x) % 2)) / 2
            };

            return ret;
        }

        protected static readonly float3[] _directions =  {
        new float3(+1, -1, 0), new float3(+1, 0, -1), new float3(0, +1, -1),
        new float3(-1, +1, 0), new float3(-1, 0, +1), new float3(0, -1, +1)};

    #endif
    }
}


