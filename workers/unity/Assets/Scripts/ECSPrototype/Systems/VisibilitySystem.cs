using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace LeyLineHybridECS
{
    public class VisibilitySystem : ComponentSystem
    {
        struct PlayerData
        {
            public readonly int Length;
            public ComponentArray<PlayerVisionData> Vision;
            public readonly ComponentDataArray<Faction> Faction;
        }
        struct CellData
        {
            public readonly int Length;
            public ComponentDataArray<IsVisible> IsVisibleData;
            //public ComponentArray<CellType> CellTypeData;
            //public ComponentArray<Cell> Cell;
        }

        struct UnitData
        {
            public readonly int Length;
            public ComponentArray<UnitVisionData> UnitVisionData;
            public ComponentArray<OccupiedCell> UnitOccupiedCell;
            public readonly ComponentDataArray<Faction> Faction;
        }
        [Inject] private PlayerData m_pData;

        [Inject] private CellData m_cData;

        [Inject] private UnitData m_uData;

        [Inject] private CellGridSystem m_CGS;

        private List<List<IsVisible>> FixClusters = new List<List<IsVisible>>();
        private bool init = true;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            
        }

        protected override void OnUpdate()
        {
            if (init) BuildFixClusters();

            if(GameStateSystem.CurrentState == GameStateSystem.State.Moving)
            {
                for (int i = m_uData.Length - 1; i >= 0; i--)
                {
                    var u_Vision = m_uData.UnitVisionData[i];
                    var u_OccupiedCell = m_uData.UnitOccupiedCell[i];
                    var u_Faction = m_uData.Faction[i];

                    if (u_Vision.RequireUpdate == true)
                    {
                        UpdateUnitVision(u_Vision, u_OccupiedCell, u_Faction);
                        u_Vision.RequireUpdate = false;
                    }

                }

                for (int i = m_pData.Length - 1; i >= 0; i--)
                {
                    var p_Vision = m_pData.Vision[i];
                    var p_Faction = m_pData.Faction[i];

                    if (p_Vision.RequireUpdate == true)
                    {
                        UpdatePlayerVision(p_Faction, p_Vision);
                        p_Vision.RequireUpdate = false;
                    }
                }

            }


            for (int i = 0; i < m_cData.Length; i++)
            {
                var c_IsVisible = m_cData.IsVisibleData[i];

                if (c_IsVisible.RequireUpdate == 1)
                {
                    /*
                    GameObject go = m_cData.IsVisibleData[i].GO;
                    bool isVisible = m_cData.IsVisibleData[i].Value;

                    if (c_IsVisible.LerpSpeed == 0f)
                    {
                        go.SetActive(isVisible);
                        c_IsVisible.RequireUpdate = false;
                    }
                    else
                    {
                        MeshRenderer meshRenderer = m_cData.IsVisibleData[i].MeshRenderer;
                        if (!isVisible)
                        {
                            if (meshRenderer.material.color.a > 0)
                            {
                                meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a - c_IsVisible.LerpSpeed * Time.deltaTime);
                            }
                            else
                            {
                                go.SetActive(isVisible);
                                c_IsVisible.RequireUpdate = false;
                            }

                        }
                        else
                        {
                            go.SetActive(isVisible);
                            if (meshRenderer.material.color.a < 1)
                            {
                                meshRenderer.material.color = new Color(1, 1, 1, meshRenderer.material.color.a + c_IsVisible.LerpSpeed * Time.deltaTime);
                            }
                            else
                            {
                                c_IsVisible.RequireUpdate = false;
                            }

                        }
                    }
                    */
                }
            }
        }



        private void UpdatePlayerVision (Faction p_Faction, PlayerVisionData p_Vision)
        {

            foreach(IsVisible c in p_Vision.Vision)
            {
                IsVisible IV = c;
                IV.Value = 0;
                IV.RequireUpdate = 1;
            }

            p_Vision.Vision.Clear();

            for(int i = m_uData.Length - 1; i >= 0; i--)
            {
                var u_FactionData = m_uData.Faction[i];
                var u_VisionData = m_uData.UnitVisionData[i];
                if(u_FactionData.Value == p_Faction.Value)
                {
                    foreach (IsVisible c in u_VisionData.VisibleToThis)
                    {
                        p_Vision.Vision.Add(c);
                        IsVisible IV = c;
                        IV.Value = 1;
                        IV.RequireUpdate = 1;
                    }
                }
            }

        }

        private void UpdateUnitVision (UnitVisionData UVD, OccupiedCell OCell, Faction Fact)
        {
            //Debug.Log("HALLO");
            List<IsVisible> sight = new List<IsVisible>();
            /*
            foreach (Cell c in m_CGS.GetRadius(OCell.Cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate, UVD.VisionRange))
            {
                sight.Add(c.GetComponent<IsVisible>());
            }
            */

            List<IsVisible> Obstructive = new List<IsVisible>();
            List<List<IsVisible>> RelevantClusters = new List<List<IsVisible>>();
            foreach (IsVisible c in sight)
            {
                /*
                if (c.GetComponent<CellType>().thisCellsTerrain.obstructVision)
                {
                    Obstructive.Add(c);
                }
                */
            }

            RelevantClusters.Clear();
            //RelevantClusters = FixClusters;

            while (Obstructive.Count > 0)
            {
                int i = Obstructive.Count-1;
                if (i >= 0)
                {
                    List<IsVisible> Cluster = new List<IsVisible>();
                    foreach(List<IsVisible> lc in FixClusters)
                    {
                        if (lc.Contains(Obstructive[i]))
                        {
                            Cluster = lc;
                        }
                    }
                    if (Cluster.Count > 0)
                    {
                        foreach (IsVisible c in Cluster)
                        {
                            if (sight.Contains(c)) sight.Remove(c);
                            if (Obstructive.Contains(c)) Obstructive.Remove(c);
                        }
                        RelevantClusters.Add(Cluster);
                    }
                }
               
            }

            //Debug.Log(RelevantClusters.Count);

            for (int i = RelevantClusters.Count - 1; i >= 0; i--)
            {
                ObstructVisionCluster OVC = new ObstructVisionCluster(RelevantClusters[i], OCell.Cell.GetComponent<IsVisible>());
                Cluster_DetermineAngles(OVC);
                //sight = Cluster_UseAngles(OVC, sight);
            }


            UVD.VisibleToThis = sight;

            for (int i = m_pData.Length-1; i>=0; i--)
            {
                var Factiondata = m_pData.Faction[i];
                var Visiondata = m_pData.Vision[i];
                if(Factiondata.Value == Fact.Value)
                {
                    Visiondata.RequireUpdate = true;
                }
            }
        }

        private void Cluster_DetermineAngles(ObstructVisionCluster inCluster)
        {
            foreach (IsVisible c in inCluster.cluster)
            {
                /*
                Angle angle = new Angle(c, m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>()));

                if (!inCluster.RelevantAngles.Contains(angle))
                {
                    inCluster.RelevantAngles.Add(angle);
                }
                */
            }
            //print("Relevant angles Count: " + inCluster.RelevantAngles.Count());

        }

        /*
        private List<IsVisible> Cluster_UseAngles(ObstructVisionCluster inCluster, List<IsVisible> watching)
        {
            int count = inCluster.RelevantAngles.Count;
            Angle largest;
            Angle smallest;
            List<IsVisible> Cone = new List<IsVisible>();
            //if count = 1 (solve the broblem of only one angle by making it into 2 angles based on distance to watcher
            inCluster.RelevantAngles.Sort((x, y) => x.angle_float.CompareTo(y.angle_float));
            largest = inCluster.RelevantAngles[count - 1];
            smallest = inCluster.RelevantAngles[0];
            //Debug.Log("Largest: " + largest.angle_float + " , " + "Smallest: " + smallest.angle_float);



            #region logic for checking if something is behind an obstructed cell
            bool specialcase = false;

            for (int i = count - 1; i >= 0; i--)
            {
                if (i == 0)
                    continue;
                else
                {
                    if (Mathf.Abs(inCluster.RelevantAngles[i].angle_float - inCluster.RelevantAngles[i - 1].angle_float) > 60)
                    {
                        specialcase = true;
                    }
                }
            }

            if (!specialcase)
            {
                foreach (IsVisible c in watching)
                {

                    float Angle = m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>());
                    if ((largest.angle_float >= Angle) && (Angle >= smallest.angle_float))
                    {
                        Cone.Add(c);
                    }

                }

                for (int i = count - 1; i >= 0; i--)
                {
                    if (i == 0)
                        continue;
                    else
                    {
                        float3 watcherCubeCoordinate = inCluster.watcher.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;
                        float3 currentCubeCoordinate = inCluster.RelevantAngles[i].cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;
                        float3 nextCubeCoordinate = inCluster.RelevantAngles[i - 1].cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                        foreach (IsVisible c in Cone)
                        {
                            float Angle = m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>());
                            float3 coneCellCubeCoordinate = c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;


                            if (inCluster.RelevantAngles[i].angle_float >= Angle && Angle >= inCluster.RelevantAngles[i - 1].angle_float)
                            {
                                if (m_CGS.GetDistance(watcherCubeCoordinate, currentCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                                {
                                    if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, currentCubeCoordinate))
                                    {
                                        if (watching.Contains(c)) watching.Remove(c);
                                    }
                                }
                                else
                                {
                                    if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                                    {
                                        if (watching.Contains(c)) watching.Remove(c);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                return watching;
            }
            #region SpecialCase
            else
            {
                Debug.Log("SPECIAL CASE!! DOES IT WORK?");
                List<Angle> Positives = new List<Angle>();
                List<Angle> Negatives = new List<Angle>();
                for (int i = count - 1; i >= 0; i--)
                {
                    if (inCluster.RelevantAngles[i].angle_float >= 0)
                    {
                        Positives.Add(inCluster.RelevantAngles[i]);
                    }
                    else {
                        Negatives.Add(inCluster.RelevantAngles[i]);
                    }

                }
                Debug.Log(Positives[0].angle_float + " ; " + Positives[Positives.Count - 1].angle_float);
                foreach (IsVisible c in watching)
                {
                    float Angle = m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>());
                    if ((Positives[Positives.Count - 1].angle_float <= Angle && Angle >= 0) || (Angle <= Negatives[0].angle_float && Angle <= 0))
                    {
                        Cone.Add(c);
                    }
                }
                Debug.Log("clustercount" + Cone.Count);

                float3 watcherCubeCoordinate = inCluster.watcher.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;
                float3 largestCubeCoordinate = largest.cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;
                float3 smallestCubeCoordinate = smallest.cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                foreach (IsVisible c in Cone)
                {
                    float Angle = m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>());
                    float3 coneCellCubeCoordinate = c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                    if (largest.angle_float <= Angle || Angle <= smallest.angle_float)
                    {

                        if (m_CGS.GetDistance(watcherCubeCoordinate, largestCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, smallestCubeCoordinate))
                        {
                            if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, largestCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                        else
                        {
                            if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, smallestCubeCoordinate))
                            {
                                if (watching.Contains(c)) watching.Remove(c);
                            }
                        }
                    }
                }
                for (int i = 0; i < Positives.Count - 1; i++)
                {
                    float3 currentCubeCoordinate = inCluster.RelevantAngles[i].cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;
                    float3 nextCubeCoordinate = inCluster.RelevantAngles[i + 1].cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                    foreach (IsVisible c in Cone)
                    {
                        float Angle = m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>());
                        float3 coneCellCubeCoordinate = c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                        if (inCluster.RelevantAngles[i].angle_float >= Angle && Angle >= inCluster.RelevantAngles[i + 1].angle_float)
                        {
                            if (m_CGS.GetDistance(watcherCubeCoordinate, currentCubeCoordinate) <= m_CGS.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                            {
                                if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, currentCubeCoordinate))
                                {
                                    if (watching.Contains(c)) watching.Remove(c);
                                }
                            }
                            else
                            {
                                if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                                {
                                    if (watching.Contains(c)) watching.Remove(c);
                                }
                            }
                        }
                    }
                }
                for (int i = Negatives.Count; i > 0; i--)
                {
                    float3 currentCubeCoordinate = inCluster.RelevantAngles[i].cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;
                    float3 nextCubeCoordinate = inCluster.RelevantAngles[i - 1].cell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                    foreach (IsVisible c in Cone)
                    {
                        float Angle = m_CGS.GetAngles(inCluster.watcher.GetComponent<Cell>(), c.GetComponent<Cell>());
                        float3 coneCellCubeCoordinate = c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                        if (inCluster.RelevantAngles[i].angle_float >= Angle && Angle >= inCluster.RelevantAngles[i - 1].angle_float)
                        {
                            if (m_CGS.GetDistance(watcherCubeCoordinate, currentCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                            {
                                if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, currentCubeCoordinate))
                                {
                                    if (watching.Contains(c)) watching.Remove(c);
                                }
                            }
                            else
                            {
                                if (m_CGS.GetDistance(watcherCubeCoordinate, coneCellCubeCoordinate) >= m_CGS.GetDistance(watcherCubeCoordinate, nextCubeCoordinate))
                                {
                                    if (watching.Contains(c)) watching.Remove(c);
                                }
                            }
                        }
                    }
                }
                return watching;
            }
            
#endregion
    }
    */

        private void BuildFixClusters()
        {
            init = false;
            List<IsVisible> obstructed = new List<IsVisible>();

            for (int i = m_cData.Length - 1; i >= 0; i--)
            {
                //var CellTypeData = m_cData.CellTypeData[i];
                var isVisible = m_cData.IsVisibleData[i];
                //var Cell = m_cData.Cell[i];
                /*
                if (CellTypeData.thisCellsTerrain.obstructVision)
                {
                    obstructed.Add(isVisible);
                }
                */
            }

            List<RawCluster> raw = new List<RawCluster>();
            while (obstructed.Count > 0)
            {
                IsVisible c = obstructed[0];
                RawCluster go = new RawCluster(c);
                obstructed.Remove(c);
                //BuildCluster(c, go, obstructed, out obstructed);
                raw.Add(go);

            }

            for (int i = raw.Count - 1; i >= 0; i--)
            {
                if (raw[i].cluster.Count > 0)
                {
                    FixClusters.Add(raw[i].cluster);
                }
            }
        }
        /*
        private void BuildCluster(IsVisible cell, RawCluster cluster, List<IsVisible> obstructed, out List<IsVisible> newObstructed)
        {
            //List<Cell> neighbours = cell.GetComponent<Neighbours>().NeighboursList;
            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                if (neighbours[i] == null || !obstructed.Contains(neighbours[i].GetComponent<IsVisible>())) neighbours.Remove(neighbours[i]);
            }

            for (int i = neighbours.Count - 1; i >= 0; i--)
            {

                if (!cluster.cluster.Contains(neighbours[i].GetComponent<IsVisible>()))
                {
                    obstructed.Remove(neighbours[i].GetComponent<IsVisible>());
                    cluster.cluster.Add(neighbours[i].GetComponent<IsVisible>());
                    //Debug.Log("added " + i + " to" + cluster);
                    BuildCluster(neighbours[i].GetComponent<IsVisible>(), cluster, obstructed, out obstructed);
                }
            }
            newObstructed = obstructed;
        }
        */
    }
    public struct ObstructVisionCluster
    {
        public List<IsVisible> cluster;
        public List<Angle> RelevantAngles;
        public IsVisible watcher;
        public ObstructVisionCluster(List<IsVisible> inCluster, IsVisible inWatcher)

        {
            watcher = inWatcher;
            cluster = inCluster;
            RelevantAngles = new List<Angle>();
        }

        public ObstructVisionCluster(IsVisible start, IsVisible inWatcher)
        {
            watcher = inWatcher;
            cluster = new List<IsVisible>();
            cluster.Add(start);
            RelevantAngles = new List<Angle>();
        }
    }
    public struct RawCluster
    {
        public List<IsVisible> cluster;
        public RawCluster(List<IsVisible> inCluster)
        {
            cluster = inCluster;
        }
        public RawCluster(IsVisible inStart)
        {
            cluster = new List<IsVisible>();
            cluster.Add(inStart);
        }
    }
    public struct Angle
    {
        public IsVisible cell;
        public float angle_float;
        public Angle(IsVisible inCell, float inAngle)
        {
            cell = inCell;
            angle_float = inAngle;
        }
    }
}
