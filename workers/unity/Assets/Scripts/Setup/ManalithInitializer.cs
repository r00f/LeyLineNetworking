
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif


namespace LeyLineHybridECS
{
    [ExecuteInEditMode]
    public class ManalithInitializer : MonoBehaviour
    {
        TerrainController terrainController;
        DijkstraPathfinding pathFinder;
        HexagonalHexGridGenerator gridGenerator;

        [SerializeField]
        public List<bool> TakenNeighbours = new List<bool>();

        [SerializeField]
        public Cell occupiedCell;

        [SerializeField]
        public LineRenderer leyLinePathRenderer;

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

        [SerializeField]
        [Range(0, 256)]
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

        public GameObject iconprefab;
        public float iconHightoffset;
        //public Vector3 planePos;

        #if UNITY_EDITOR
        public void GeneratePathAndFinalize()
        {
            UpdateLeyLinePath();
            FillCircleCoordinatesList();
            FillPathCoordinatesList();
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(leyLinePathRenderer.gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(leyLinePathRenderer.gameObject);
        }

        public void UpdateLeyLineCircle()
        {
            if(!terrainController)
                terrainController = FindObjectOfType<TerrainController>();

            foreach (Cell c in leyLineCircle)
            {
                if(c.GetComponent<EditorIsCircleCell>())
                    c.GetComponent<EditorIsCircleCell>().IsLeylineCircleCell = false;
            }

            if (TakenNeighbours.Count == 6)
            {
                SetTakenNeighbours();
            }

            switch (circleSize)
            {
                case CircleSize.Three:

                    var coord = Vector3fext.ToVector3f(occupiedCell.GetComponent<CoordinateDataComponent>().CubeCoordinate);
                    var circleCoords = CellGridMethods.CircleDegreeToCoords(coord, occupiedCell.GetComponent<CellType>().ManalithSpawnDirection);

                    //Debug.Log(Vector3fext.ToUnityVector(circleCoords.Key) + " " + Vector3fext.ToUnityVector(circleCoords.Value));

                    //factor in direction to determine circle center
                    circleCenter = occupiedCell.transform.localPosition + occupiedCell.transform.parent.localPosition + transform.forward * (occupiedCell.GetComponent<CellDimensions>().Value.x / 2);

                    leyLineCircle.Clear();

                    leyLineCircle.Add(occupiedCell);

                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (Vector3fext.ToFloat3(circleCoords.Key).Equals(occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<CoordinateDataComponent>().CubeCoordinate) || Vector3fext.ToFloat3(circleCoords.Value).Equals(occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<CoordinateDataComponent>().CubeCoordinate))
                            leyLineCircle.Add(occupiedCell.GetComponent<Neighbours>().NeighboursList[i]);
                    }
                    foreach (Cell c in leyLineCircle)
                    {
                        c.GetComponent<EditorIsCircleCell>().IsLeylineCircleCell = true;
                    }
                    break;

                case CircleSize.Seven:
                    circleCenter = transform.localPosition;
                    leyLineCircle.Add(occupiedCell);

                    if (leyLineCircle.Count == 1)
                        leyLineCircle.AddRange(occupiedCell.GetComponent<Neighbours>().NeighboursList);

                    for (int i = leyLineCircle.Count - 1; i >= 7; i--)
                    {
                        leyLineCircle.RemoveAt(i);
                    }
                    foreach (Cell c in leyLineCircle)
                    {
                        c.GetComponent<EditorIsCircleCell>().IsLeylineCircleCell = true;
                    }
                    break;
            }

            UpdateCircleRenderer();
            AddManalithNodeStampPositions();

        }

        public void SetTakenNeighbours()
        {
            switch (occupiedCell.GetComponent<CellType>().ManalithSpawnDirection)
            {
                case 30:
                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (i < 2)
                        {
                            if (TakenNeighbours[i + 4])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                        else
                        {
                            if (TakenNeighbours[i - 2])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                    }
                    break;
                case 90:
                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (i == 0)
                        {
                            if (TakenNeighbours[i + 5])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                        else
                        {
                            if (TakenNeighbours[i - 1])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                    }
                    break;
                case 150:
                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (TakenNeighbours[i])
                            occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                    }
                    break;
                case 210:
                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (i < 5)
                        {
                            if (TakenNeighbours[i + 1])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                        else
                        {
                            if (TakenNeighbours[i - 5])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                    }
                    break;
                case 270:
                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (i < 4)
                        {
                            if (TakenNeighbours[i + 2])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                        else
                        {
                            if (TakenNeighbours[i - 4])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                    }
                    break;
                case 330:
                    for (int i = 0; i < occupiedCell.GetComponent<Neighbours>().NeighboursList.Count; i++)
                    {
                        if (i < 3)
                        {
                            if (TakenNeighbours[i + 3])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                        else
                        {
                            if (TakenNeighbours[i - 3])
                                occupiedCell.GetComponent<Neighbours>().NeighboursList[i].GetComponent<IsTaken>().Value = true;
                        }
                    }
                    break;
            }
        }

        public void UpdateLeyLinePath()
        {
            leyLinePath.Clear();
            pathFinder = new DijkstraPathfinding();
            gridGenerator = FindObjectOfType<HexagonalHexGridGenerator>();

            if (connectedManaLith != null && connectedManaLith != this)
            {
                leyLinePathRenderer.GetComponentInParent<MeshGradientColor>().ConnectedManalithColor = connectedManaLith.GetComponent<MeshColor>();
                leyLinePathRenderer.gameObject.SetActive(true);
                leyLinePath.AddRange(FindPath(gridGenerator.hexagons.ToArray(), connectedManaLith.occupiedCell));
                leyLinePath.Add(occupiedCell);
                leyLinePath.Reverse();
                RemovePathOverLap();
                UpdatePathRenderer();
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
                for(int i = leyLinePath.Count - 1; i > leyLinePath.Count/2; i--)
                {
                    if(leyLinePath[i - 1].GetComponent<EditorIsCircleCell>().IsLeylineCircleCell)
                    {
                        leyLinePath.RemoveAt(i);
                    }
                }

                for (int i = 0; i < leyLinePath.Count/2; i++)
                {
                    if (leyLinePath[i + 1].GetComponent<EditorIsCircleCell>().IsLeylineCircleCell)
                    {
                        leyLinePath.RemoveAt(i);
                    }
                }
            }
        }

        #region LineRendererUpdates
        void UpdatePathRenderer()
        {
            leyLinePathRenderer.positionCount = leyLinePath.Count;
            for (int i = 0; i < leyLinePathRenderer.positionCount; i++)
            {
                Vector3 pos = leyLinePath[i].transform.position + offset;
                leyLinePathRenderer.SetPosition(i, pos);
            }

            for (int i = 0; i < leyLinePath.Count - 1; i++)
            {
                if(!leyLinePath[i].GetComponent<CellType>().ignoreLeylineStamp || !leyLinePath[i + 1].GetComponent<CellType>().ignoreLeylineStamp)
                {
                    Vector3 currentPos = leyLinePath[i].transform.localPosition + leyLinePath[i].transform.parent.localPosition + offset;
                    Vector3 nextPos = leyLinePath[i + 1].transform.localPosition + leyLinePath[i + 1].transform.parent.localPosition + offset;
                    Vector3 direction = nextPos - currentPos;

                    if (!leyLinePath[i].GetComponent<CellType>().ignoreLeylineStamp)
                    {
                        leyLinePath[i].GetComponent<EditorIsCircleCell>().IsLeylinePathCell = true;

                        if (leyLinePath[i + 1].GetComponent<CellType>().ignoreLeylineStamp)
                            direction /= 2;
                    }

                    if(leyLinePath[i].GetComponent<CellType>().ignoreLeylineStamp && !leyLinePath[i + 1].GetComponent<CellType>().ignoreLeylineStamp)
                    {
                        direction /= 2;
                        currentPos += direction;
                    }

                    for (int s = 1; s <= lineSegments + 1; s++)
                    {
                        Vector3 segmentPos = currentPos + (direction * s / lineSegments);

                        if (!terrainController.leyLineCrackPositions.Contains(segmentPos))
                        {
                            terrainController.leyLineCrackPositions.Add(segmentPos - new Vector3(0, 0.1f, 0));
                        }
                    }
                }
                EditorUtility.SetDirty(terrainController);
            }
        }

        void UpdateCircleRenderer()
        {
            CalculateCircle(Vector3.Distance(circleCenter, leyLineCircle[1].transform.localPosition + leyLineCircle[1].transform.parent.localPosition));
        }

        void AddManalithNodeStampPositions()
        {
            foreach(Cell c in leyLineCircle)
            {
                terrainController.ManalithNodeStampPositions.Add(c.transform.localPosition + c.transform.parent.localPosition + offset);
            }
        }

        public void CalculateCircle(float radius)
        {
            float deltaTheta = (float)(2.0 * Mathf.PI) / circleSegments;
            float theta = 0f;

            for (int i = 0; i < circleSegments; i++)
            {
                float x = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta);
                Vector3 pos = new Vector3(x, 0, z) + circleCenter;
                if (!terrainController.leyLineCircleCrackPositions.Contains(pos))
                    terrainController.leyLineCircleCrackPositions.Add(pos + offset);
                theta += deltaTheta;
            }
        }

        #endregion

        public void FillPathCoordinatesList()
        {
            leyLinePathCoords.Clear();
            foreach(Cell c in leyLinePath)
            {
                leyLinePathCoords.Add(c.transform.GetComponent<CoordinateDataComponent>().CubeCoordinate);
            }
        }

        public void FillCircleCoordinatesList()
        {
            leyLineCircleCoords.Clear();
            foreach (Cell c in leyLineCircle)
            {
                leyLineCircleCoords.Add(c.transform.GetComponent<CoordinateDataComponent>().CubeCoordinate);
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


