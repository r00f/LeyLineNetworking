using System.IO;
using Cell;
using Unit;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using LeyLineHybridECS;
using UnityEditor;
using Unity.Entities;
using UnityEngine;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Mathematics;

namespace BlankProject.Editor
{
    internal static class SnapshotGenerator
    {
        private static Settings settings;

        private static readonly string DefaultSnapshotPath = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "..",
                "snapshots",
                "default.snapshot"));

        [MenuItem("SpatialOS/Generate snapshot")]
        public static void Generate()
        {
            settings = Resources.Load<Settings>("Settings");
            Debug.Log("Generating snapshot.");
            var snapshot = CreateSnapshot();

            Debug.Log($"Writing snapshot to: {DefaultSnapshotPath}");
            snapshot.WriteToFile(DefaultSnapshotPath);
        }

        private static Snapshot CreateSnapshot()
        {
            var snapshot = new Snapshot();
            AddGameState(snapshot);
            AddPlayerSpawner(snapshot);
            AddCellGrid(snapshot);
            AddManaliths(snapshot);
            AddObstructVisionClusters(snapshot);
            AddInitializeMapEventSenders(snapshot);
            return snapshot;
        }

        private static void AddGameState(Snapshot snapshot)
        {
            var columnCount = settings.MapCount / settings.MapsPerRow;

            for (uint i = 0; i < columnCount; i++)
            {
                for (uint y = 0; y < settings.MapsPerRow; y++)
                {
                    foreach (EditorWorldIndex wi in Object.FindObjectsOfType<EditorWorldIndex>())
                    {
                        Vector3f pos = new Vector3f(settings.MapOffset * y, 0, settings.MapOffset * i);
                        Vector2f mapCenter = new Vector2f(wi.centerCellTransform.position.x + settings.MapOffset * y - wi.transform.position.x, wi.centerCellTransform.position.z + settings.MapOffset * i - wi.transform.position.z);

                        Dictionary<Vector2i, MapCell> map = new Dictionary<Vector2i, MapCell>();
                        foreach (LeyLineHybridECS.Cell c in Object.FindObjectsOfType<LeyLineHybridECS.Cell>())
                        {
                            Vector3f cubeCoord = new Vector3f(c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z);
                            Vector2i axialCoord = CellGridMethods.CubeToAxial(cubeCoord);
                            Vector3f p = new Vector3f(c.transform.position.x + settings.MapOffset * y - wi.transform.position.x, c.transform.position.y, c.transform.position.z + settings.MapOffset * i - wi.transform.position.z);

                            MapCell mapCell = new MapCell
                            {
                                AxialCoordinate = axialCoord,
                                Position = p,
                                IsTaken = c.GetComponent<IsTaken>().Value,
                                MovementCost = (uint)c.GetComponent<MovementCost>().Value
                            };

                            map.Add(axialCoord, mapCell);
                        }
                        var gameState = LeyLineEntityTemplates.GameState(pos, (i * settings.MapsPerRow) + y + 1, mapCenter, map);
                        snapshot.AddEntity(gameState);
                    }
                }
            }
            
        }

        private static void AddInitializeMapEventSenders(Snapshot snapshot)
        {
            foreach(UnityGameLogicConnector gameLogic in Object.FindObjectsOfType<UnityGameLogicConnector>())
            {
                Vector3f p = new Vector3f(gameLogic.transform.position.x, gameLogic.transform.position.y, gameLogic.transform.position.z);
                snapshot.AddEntity(LeyLineEntityTemplates.InitializeMapEventSender(p));
            }
        }

        private static void AddObstructVisionClusters(Snapshot snapshot)
        {
            foreach (EditorWorldIndex wi in Object.FindObjectsOfType<EditorWorldIndex>())
            {
                Vector3f pos = new Vector3f(wi.transform.position.x, 0 , wi.transform.position.z);

                var obstructVision = new List<Vector3fList>();

                foreach(CellAttributesList c in BuildRawClusters(wi.WorldIndex))
                {
                    var currentClusterList = new List<Vector3f>();
                    foreach(CellAttributes cA in c.CellAttributes)
                    {
                        currentClusterList.Add(cA.Cell.CubeCoordinate);
                    }
                    obstructVision.Add(new Vector3fList(currentClusterList));
                }

                var oVisionCluster = LeyLineEntityTemplates.ObstructVisionClusters(pos, wi.WorldIndex, obstructVision);
                snapshot.AddEntity(oVisionCluster);
            }
        }

        private static void AddManaliths(Snapshot snapshot)
        {
            foreach (ManalithInitializer m in Object.FindObjectsOfType<ManalithInitializer>())
            {
                var circle = new CellAttributeList
                {
                    CellAttributes = new List<CellAttribute>()
                };

                foreach (LeyLineHybridECS.Cell n in m.leyLineCircle)
                {
                    circle.CellAttributes.Add(new CellAttribute
                    {
                        Position = new Vector3f(n.transform.position.x, n.transform.position.y, n.transform.position.z),
                        CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                        IsTaken = n.GetComponent<IsTaken>().Value,
                        MovementCost = n.GetComponent<MovementCost>().Value
                    });
                }

                Position.Component pos = new Position.Component
                {
                    Coords = new Coordinates
                    {
                        X = m.transform.position.x,
                        Y = m.transform.position.y,
                        Z = m.transform.position.z
                    }
                };

                uint worldIndex = 0;

                var stats = m.GetComponent<UnitDataSet>();
                var AIstats = m.GetComponent<AIUnitDataSet>();

                var coordComp = m.GetComponent<ManalithInitializer>().occupiedCell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                var coord = new Vector3f(coordComp.x, coordComp.y, coordComp.z);

                var connectedManalithCoord = new Vector3f();

                if (m.connectedManaLith)
                    connectedManalithCoord = Vector3fext.ToVector3f(m.connectedManaLith.occupiedCell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate);

                var pathCoordList = new List<Vector3f>();

                foreach (float3 f3 in m.leyLinePathCoords)
                {
                    pathCoordList.Add(Vector3fext.ToVector3f(f3));
                }

                var manalith = LeyLineEntityTemplates.ManalithUnit(m.name, pos, coord, 0, worldIndex, stats, (uint) m.transform.eulerAngles.y, circle, pathCoordList, connectedManalithCoord);
                snapshot.AddEntity(manalith);
            }
        }

        private static void AddCellGrid(Snapshot snapshot)
        {
            foreach (LeyLineHybridECS.Cell c in Object.FindObjectsOfType<LeyLineHybridECS.Cell>())
            {
                var neighbours = new CellAttributeList
                {
                    CellAttributes = new List<CellAttribute>()
                };

                foreach (LeyLineHybridECS.Cell n in c.GetComponent<Neighbours>().NeighboursList)
                {
                    neighbours.CellAttributes.Add(new CellAttribute
                    {
                        Position = new Vector3f(n.transform.position.x, n.transform.position.y, n.transform.position.z),
                        CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                        IsTaken = n.GetComponent<IsTaken>().Value,
                        MovementCost = n.GetComponent<MovementCost>().Value,
                        ObstructVision = n.GetComponent<CellType>().thisCellsTerrain.obstructVision
                    });
                }

                Vector3f pos = new Vector3f(c.transform.position.x, c.transform.position.y, c.transform.position.z);
                Vector3f cubeCoord = new Vector3f(c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z);
                uint worldIndex = 0;
                int mapCellColor = c.GetComponent<CellType>().thisCellsTerrain.MapCellColorIndex;
                var cell = LeyLineEntityTemplates.ArcheTypeCell(cubeCoord, pos, c.GetComponent<IsTaken>().Value, c.GetComponent<EditorIsCircleCell>().IsLeylineCircleCell, c.GetComponent<UnitToSpawnEditor>().UnitName, c.GetComponent<UnitToSpawnEditor>().IsUnitSpawn, c.GetComponent<UnitToSpawnEditor>().Faction, neighbours, worldIndex, c.GetComponent<CellType>().thisCellsTerrain.obstructVision, mapCellColor, c.GetComponent<UnitToSpawnEditor>().StartUnitIndex, c.GetComponent<UnitToSpawnEditor>().StartRotation);
                snapshot.AddEntity(cell);
            }
        }

        private static void AddPlayerSpawner(Snapshot snapshot)
        {
            Position.Snapshot pos = new Position.Snapshot
            {
                Coords = new Coordinates
                {
                    X = 0,
                    Y = 0,
                    Z = 0
                }
            };

            var template = new EntityTemplate();
            template.AddComponent(pos, WorkerUtils.MapSpawn);
            template.AddComponent(new Metadata.Snapshot { EntityType = "PlayerCreator" }, WorkerUtils.MapSpawn);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.MapSpawn);
            template.AddComponent(new PlayerCreator.Snapshot(), WorkerUtils.MapSpawn);

            template.SetReadAccess(WorkerUtils.UnityClient, WorkerUtils.UnityGameLogic, WorkerUtils.MapSpawn);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.MapSpawn);

            snapshot.AddEntity(template);
        }

        private static List<CellAttributesList> BuildRawClusters(uint worldIndex)
        {
            List<CellAttributesList> RawClusters = new List<CellAttributesList>();

            List<CellAttributes> obstructed = new List<CellAttributes>();

            foreach (LeyLineHybridECS.Cell c in Object.FindObjectsOfType<LeyLineHybridECS.Cell>())
            {
                if (c.GetComponent<CellType>().thisCellsTerrain.obstructVision)
                {
                    var cell = new CellAttribute
                    {
                        Position = new Vector3f(c.transform.position.x, c.transform.position.y, c.transform.position.z),
                        CubeCoordinate = new Vector3f(c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                        IsTaken = c.GetComponent<IsTaken>().Value,
                        MovementCost = 1,
                        ObstructVision = c.GetComponent<CellType>().thisCellsTerrain.obstructVision
                    };


                    var neighbours = new CellAttributeList
                    {
                        CellAttributes = new List<CellAttribute>()
                    };

                    foreach (LeyLineHybridECS.Cell n in c.GetComponent<Neighbours>().NeighboursList)
                    {
                        neighbours.CellAttributes.Add(new CellAttribute
                        {
                            Position = new Vector3f(n.transform.position.x, n.transform.position.y, n.transform.position.z),
                            CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                            IsTaken = n.GetComponent<IsTaken>().Value,
                            MovementCost = n.GetComponent<MovementCost>().Value,
                            ObstructVision = n.GetComponent<CellType>().thisCellsTerrain.obstructVision
                        });
                    }

                    obstructed.Add(new CellAttributes
                    {
                        Cell = cell,
                        CellMapColorIndex = 0,
                        Neighbours = neighbours
                    });

                }
            }
            List<RawCluster> raw = new List<RawCluster>();

            while (obstructed.Count > 0)
            {
                CellAttributes c = obstructed[0];
                RawCluster go = new RawCluster(c);
                obstructed.Remove(c);
                BuildCluster(c, go, obstructed, out obstructed);
                raw.Add(go);
                //Debug.Log("Cluster:" + go.cluster.Count);
            }

            for (int i = raw.Count - 1; i >= 0; i--)
            {
                if (raw[i].cluster.CellAttributes.Count > 0)
                {
                    RawClusters.Add(raw[i].cluster);
                }
            }

            return RawClusters;
        }

        private static void BuildCluster(CellAttributes cell, RawCluster cluster, List<CellAttributes> obstructed, out List<CellAttributes> newObstructed)
        {
            List<CellAttribute> neighbours = cell.Neighbours.CellAttributes;
            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                bool contains = false;
                {
                    foreach (CellAttributes c in obstructed)
                    {
                        if (Vector3fext.ToUnityVector(c.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate))
                        {
                            contains = true;
                        }
                    }
                }
                if (!contains) neighbours.Remove(neighbours[i]);
            }

            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                bool contains = false;

                foreach (CellAttributes c in cluster.cluster.CellAttributes)
                {
                    if (Vector3fext.ToUnityVector(c.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate)) contains = true;
                }

                if (!contains)
                {
                    bool isSet = false;
                    CellAttributes toRemove = new CellAttributes();
                    foreach (CellAttributes c in obstructed)
                    {
                        if (Vector3fext.ToUnityVector(c.Cell.CubeCoordinate) == Vector3fext.ToUnityVector(neighbours[i].CubeCoordinate))
                        {
                            toRemove = c;
                            isSet = true;
                        }
                    }

                    if (isSet)
                    {
                        obstructed.Remove(toRemove);
                        cluster.cluster.CellAttributes.Add(toRemove);
                        //Debug.Log("added " + i + " to" + cluster);
                        BuildCluster(toRemove, cluster, obstructed, out obstructed);
                    }
                }
            }
            newObstructed = obstructed;
        }
    }

    public struct RawCluster
    {
        public CellAttributesList cluster;

        public RawCluster(CellAttributesList inCluster)
        {
            cluster = inCluster;
        }
        public RawCluster(CellAttributes inStart)
        {
            cluster = new CellAttributesList
            {
                CellAttributes = new List<CellAttributes>()
            };
            cluster.CellAttributes.Add(inStart);
        }
    }
}
