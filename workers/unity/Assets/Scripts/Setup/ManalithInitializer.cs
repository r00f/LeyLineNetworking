﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using UnityEditor;
using Unity.Mathematics;

namespace LeyLineHybridECS
{
    [ExecuteInEditMode]
    public class ManalithInitializer : MonoBehaviour
    {
        TerrainController terrainController;
        Camera projectorCam;
        DijkstraPathfinding pathFinder;
        HexagonalHexGridGenerator gridGenerator;
        Transform manaLithParent;
        public bool meshGenerated;

        [SerializeField, HideInInspector]
        public Cell occupiedCell;

        [SerializeField]
        LineRenderer leyLinePathRenderer;
        //[SerializeField]
        //LineRenderer leyLineCircleRenderer;
        Mesh leyLinePathMesh;
        Mesh leyLineCircleMesh;

        [SerializeField]
        public MeshFilter leyLinePathMeshFilter;
        [SerializeField]
        MeshFilter leyLineCircleMeshFilter;

        /*
        [SerializeField]
        MeshRenderer leyLineCircleMeshRenderer;

        [SerializeField]
        ParticleSystem circlePs;
        [SerializeField]
        ParticleSystem pathPs;
        */

        [SerializeField]
        List<Cell> leyLinePath = new List<Cell>();

        public List<Cell> leyLineCircle = new List<Cell>();


        [SerializeField]
        public List<float3> leyLinePathCoords = new List<float3>();

        public List<float3> leyLineCircleCoords = new List<float3>();

        [HideInInspector]
        public List<LineRenderer> connectedLeyLineRenderers = new List<LineRenderer>();

        [SerializeField]
        public ManalithInitializer connectedManaLith;

        //[SerializeField]
        //List<Cell> otherManaliths = new List<Cell>();

        [SerializeField]
        [Range(3, 256)]
        int circleSegments = 256;

        [SerializeField]
        int lineSegments = 10;

        [SerializeField]
        Vector3 offset;

        Vector3 circleCenter;

        public enum CircleSize
        {
            Three,
            Seven
        }

        [SerializeField]
        public CircleSize circleSize;

        
        public uint baseEnergyGain;

        
        public GameObject iconprefab;
        public float iconHightoffset;
        //public Vector3 planePos;

        #if UNITY_EDITOR

        void OnDestroy()
        {
            /*
            foreach (Cell c in leyLineCircle)
            {
                c.GetComponent<IsCircleCell>().Value = false;
            }
            */
        }

        void OnEnable()
        {
            //ConnectManaLith();
        }

        public void GenerateMeshes()
        {
            //manaLithParent = GameObject.Find("Manaliths").transform;
            //transform.parent = manaLithParent;

            if(leyLinePathMeshFilter.sharedMesh != null)
            {
                //create unique name for the asset
                string assetName = "leylinepath" + Resources.FindObjectsOfTypeAll(typeof(Mesh)).Count();

                if(leyLinePathMeshFilter.sharedMesh.name != assetName)
                {
                    Mesh lineMesh = Instantiate(leyLinePathMeshFilter.sharedMesh);
                    AssetDatabase.CreateAsset(lineMesh, "Assets/Resources/ManalithMeshes/" + assetName + ".asset");
                    AssetDatabase.SaveAssets();
                }

                var mesh = Resources.Load("ManalithMeshes/" + assetName);
                leyLinePathMeshFilter.sharedMesh = (Mesh)mesh;
            }

            /*
            if(leyLineCircleMeshFilter.sharedMesh != null)
            {
                //create unique name for the asset
                string assetName = "leylinecircle" + circleSize;

                //if this circlesize does not exist

                if(Resources.Load("ManalithMeshes/" + assetName) == null)
                {
                    Mesh circleMesh = Instantiate(leyLineCircleMeshFilter.sharedMesh);
                    AssetDatabase.CreateAsset(circleMesh, "Assets/Resources/ManalithMeshes/" + assetName + ".asset");
                    AssetDatabase.SaveAssets();
                }

                var mesh = Resources.Load("ManalithMeshes/" + assetName);
                leyLineCircleMeshFilter.sharedMesh = (Mesh)mesh;
            }
            */

        }


        public void ConnectManaLith()
        {
            terrainController = FindObjectOfType<TerrainController>();
            terrainController.leyLineCrackPositions.Clear();

            //otherManaliths.Clear();
            /*
            foreach (ManalithInitializer m in FindObjectsOfType<ManalithInitializer>())
            {
                if (m != this && !otherManaliths.Contains(m.transform.GetComponentInParent<Cell>()))
                    otherManaliths.Add(m.transform.GetComponentInParent<Cell>());
            }
            otherManaliths = otherManaliths.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
            */

            //if this manalith has no connected Manalith, destroy MeshGradientColor component on pressing connect
            if(!connectedManaLith && transform.GetComponent<MeshGradientColor>())
            {
                DestroyImmediate(transform.GetComponent<MeshGradientColor>());
            }
            projectorCam = GameObject.FindGameObjectWithTag("Projector").GetComponent<Camera>();
            gridGenerator = FindObjectOfType<HexagonalHexGridGenerator>();
            pathFinder = new DijkstraPathfinding();

            //leyLineCircleMesh = new Mesh();
            leyLinePathMesh = new Mesh();


            UpdateLeyLineCircle();


            terrainController.UpdateLeyLineCracks();

            FillCircleCoordinatesList();
            FillPathCoordinatesList();


            //UpdateLeyLinePath();
            //RemovePathOverLap();
        }

        public void UpdateLeyLineCircle()
        {

            foreach (Cell c in leyLineCircle)
            {
                if(c.GetComponent<EditorIsCircleCell>())
                    c.GetComponent<EditorIsCircleCell>().Value = false;
            }

            switch (circleSize)
            {
                case CircleSize.Three:
                    circleCenter = transform.position + new Vector3(occupiedCell.GetComponent<CellDimensions>().Value.x / 2, 0, 0);
                    leyLineCircle.Add(occupiedCell);
                    if(leyLineCircle.Count == 1)
                        leyLineCircle.AddRange(occupiedCell.GetComponent<Neighbours>().NeighboursList);
                    for (int i = leyLineCircle.Count - 1; i >= 3; i--)
                    {
                        leyLineCircle.RemoveAt(i);
                    }
                    foreach (Cell c in leyLineCircle)
                    {
                        c.GetComponent<EditorIsCircleCell>().Value = true;
                        //GetComponent<UnitsOnManalith>().UnitsOnCells.Add(c.GetComponent<UnitOnCell>());
                    }
                    UpdateLeyLinePath();
                    UpdateCircleRenderer();
                    return;

                case CircleSize.Seven:
                    circleCenter = transform.position;
                    leyLineCircle.Add(occupiedCell);
                    if (leyLineCircle.Count == 1)
                        leyLineCircle.AddRange(occupiedCell.GetComponent<Neighbours>().NeighboursList);
                    for (int i = leyLineCircle.Count - 1; i >= 7; i--)
                    {
                        leyLineCircle.RemoveAt(i);
                    }
                    foreach (Cell c in leyLineCircle)
                    {
                        c.GetComponent<EditorIsCircleCell>().Value = true;
                        //GetComponent<UnitsOnManalith>().UnitsOnCells.Add(c.GetComponent<UnitOnCell>());
                    }
                    UpdateLeyLinePath();
                    UpdateCircleRenderer();
                    return;
            }

        }

        public void UpdateLeyLinePath()
        {
            leyLinePath.Clear();

            if (connectedManaLith != null && connectedManaLith != this)
            {
                leyLinePathRenderer.GetComponentInParent<MeshGradientColor>().ManalithColor = GetComponent<MeshColor>();
                leyLinePathRenderer.GetComponentInParent<MeshGradientColor>().ConnectedManalithColor = connectedManaLith.GetComponent<MeshColor>();
                leyLinePathRenderer.gameObject.SetActive(true);
                leyLinePath.AddRange(FindPath(gridGenerator.hexagons.ToArray(), connectedManaLith.occupiedCell));
                leyLinePath.Add(occupiedCell);
                leyLinePath.Reverse();
                RemovePathOverLap();
                //connectedManaLith.GetComponentInChildren<ManalithInitializer>().connectedLeyLineRenderers.Add(leyLinePathRenderer);
            }
            else
            {
                leyLinePathRenderer.gameObject.SetActive(false);
            }

        }

        public void RemovePathOverLap()
        {
            if (leyLinePath.Count > 2)
            {
                if (leyLinePath[leyLinePath.Count - 2].GetComponent<EditorIsCircleCell>().Value)
                {
                    leyLinePath.RemoveAt(leyLinePath.Count - 1);
                }
                if (leyLinePath[1].GetComponent<EditorIsCircleCell>().Value)
                {
                    leyLinePath.RemoveAt(0);
                }
            }

            UpdatePathRenderer();
        }

        #region LineRendererUpdates

        void UpdatePathRenderer()
        {
            leyLinePathRenderer.positionCount = leyLinePath.Count;
            for (int i = 0; i < leyLinePathRenderer.positionCount; i++)
            {
                Vector3 pos = leyLinePath[i].transform.position + offset - transform.position;
                leyLinePathRenderer.SetPosition(i, pos);
            }

            for (int i = 0; i < leyLinePath.Count - 1; i++)
            {
                Vector3 currentPos = leyLinePath[i].transform.position + offset;
                Vector3 nextPos = leyLinePath[i + 1].transform.position + offset;

                Vector3 direction = nextPos - currentPos;

                for (int s = 1; s <= lineSegments + 1; s++)
                {
                    Vector3 segmentPos = currentPos + (direction * s / lineSegments);

                    if (!terrainController.leyLineCrackPositions.Contains(segmentPos))
                        terrainController.leyLineCrackPositions.Add(segmentPos - new Vector3(0, 0.1f, 0));
                }
            }

            leyLinePathRenderer.BakeMesh(leyLinePathMesh, projectorCam);


            //ParticleSystem pPs = pathPs;
            //var pathShapeModule = pPs.shape;
            //pathShapeModule.shapeType = ParticleSystemShapeType.Mesh;
            //pathShapeModule.mesh = leyLinePathMesh;

            leyLinePathMeshFilter.mesh = leyLinePathMesh;
            leyLinePathRenderer.enabled = false;
        }

        void UpdateCircleRenderer()
        {

            CalculateCircle(Vector3.Distance(circleCenter, leyLineCircle[1].transform.position));
            //leyLineCircleRenderer.BakeMesh(leyLineCircleMesh, projectorCam);


            //ParticleSystem cPs = circlePs;
            //var circleShapeModule = cPs.shape;
            //circleShapeModule.shapeType = ParticleSystemShapeType.Mesh;
            //circleShapeModule.mesh = leyLineCircleMesh;

            //leyLineCircleMeshFilter.mesh = leyLineCircleMesh;
            //leyLineCircleRenderer.enabled = false;

        }

        public void CalculateCircle(float radius)
        {
            //leyLineCircleRenderer.positionCount = circleSegments;

            float deltaTheta = (float)(2.0 * Mathf.PI) / circleSegments;
            float theta = 0f;

            for (int i = 0; i < circleSegments; i++)
            {
                float x = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta);
                Vector3 pos = new Vector3(x, 0, z) + circleCenter;
                if (!terrainController.leyLineCrackPositions.Contains(pos))
                    terrainController.leyLineCrackPositions.Add(pos + offset - new Vector3 (0, 0.1f, 0));
                //leyLineCircleRenderer.SetPosition(i, pos + offset - transform.position);
                theta += deltaTheta;
            }
        }

        #endregion

        public void FillPathCoordinatesList()
        {
            leyLinePathCoords.Clear();
            foreach(Cell c in leyLinePath)
            {
                leyLinePathCoords.Add(c.transform.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate);
            }
        }

        public void FillCircleCoordinatesList()
        {
            leyLineCircleCoords.Clear();
            foreach (Cell c in leyLineCircle)
            {
                leyLineCircleCoords.Add(c.transform.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate);
            }
        }

        #region PathFinding

        public List<Cell> FindPath(Cell[] cells, Cell destination)
        {
            return pathFinder.FindPath(GetGraphEdges(cells), occupiedCell, destination);
        }

        /// <summary>
        /// Method returns graph representation of cell grid for pathfinding.
        /// </summary>
        protected virtual Dictionary<Cell, Dictionary<Cell, int>> GetGraphEdges(Cell[] cells)
        {
            Dictionary<Cell, Dictionary<Cell, int>> ret = new Dictionary<Cell, Dictionary<Cell, int>>();
            foreach (var cell in cells)
            {
                ret[cell] = new Dictionary<Cell, int>();
                foreach (var neighbour in cell.GetComponent<Neighbours>().NeighboursList)
                {
                    ret[cell][neighbour] = cell.GetComponent<MovementCost>().Value;
                }
            }
            return ret;
        }

        #endregion

    #endif
    }
}
