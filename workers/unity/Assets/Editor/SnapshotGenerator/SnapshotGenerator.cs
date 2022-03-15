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
            AddManaliths(snapshot);
            AddObstructVisionClusters(snapshot);
            AddInitializeMapEventSenders(snapshot);
            return snapshot;
        }

        private static void AddGameState(Snapshot snapshot)
        {
            var columnCount = settings.MapCount / settings.MapsPerRow;
            //Vector2f mapGridCenterOffset = new Vector2f(-400f, -400f);

            for (uint i = 0; i < columnCount; i++)
            {
                for (uint y = 0; y < settings.MapsPerRow; y++)
                {
                    foreach (EditorWorldIndex wi in Object.FindObjectsOfType<EditorWorldIndex>())
                    {
                        Vector3f pos = Vector3fext.FromUnityVector(new Vector3(settings.MapOffset * y, 0, settings.MapOffset * i) + new Vector3(settings.MapGridCenterOffset.X, 0f, settings.MapGridCenterOffset.Y));
                        Vector2f mapCenter = new Vector2f(wi.centerCellTransform.position.x + settings.MapOffset * y - wi.transform.position.x + settings.MapGridCenterOffset.X, wi.centerCellTransform.position.z + settings.MapOffset * i - wi.transform.position.z + settings.MapGridCenterOffset.Y);

                        Dictionary<Vector2i, MapCell> map = new Dictionary<Vector2i, MapCell>();
                        List<UnitSpawn> unitSpawnList = new List<UnitSpawn>();
                        foreach (LeyLineHybridECS.Cell c in Object.FindObjectsOfType<LeyLineHybridECS.Cell>())
                        {
                            Vector3f cubeCoord = new Vector3f(c.GetComponent<CoordinateDataComponent>().CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().CubeCoordinate.z);
                            Vector2i axialCoord = CellGridMethods.CubeToAxial(cubeCoord);
                            Vector3f p = new Vector3f(c.transform.position.x + settings.MapOffset * y - wi.transform.position.x + settings.MapGridCenterOffset.X, c.transform.position.y, c.transform.position.z + settings.MapOffset * i - wi.transform.position.z + settings.MapGridCenterOffset.Y);

                            MapCell mapCell = new MapCell
                            {
                                AxialCoordinate = axialCoord,
                                Position = p,
                                IsTaken = c.GetComponent<IsTaken>().Value,
                                MovementCost = (uint) c.GetComponent<MovementCost>().Value,
                                MapCellColorIndex = (uint) c.GetComponent<CellType>().thisCellsTerrain.MapCellColorIndex
                            };

                            if (c.GetComponent<UnitToSpawnEditor>().IsUnitSpawn)
                            {
                                unitSpawnList.Add(new UnitSpawn
                                {
                                    AxialCoordinate = axialCoord,
                                    UnitName = c.GetComponent<UnitToSpawnEditor>().UnitName,
                                    StartingUnitIndex = c.GetComponent<UnitToSpawnEditor>().StartUnitIndex,
                                    StartRotation = c.GetComponent<UnitToSpawnEditor>().StartRotation,
                                    Faction = c.GetComponent<UnitToSpawnEditor>().Faction
                                });
                            }

                            map.Add(axialCoord, mapCell);
                        }

                        var gameState = LeyLineEntityTemplates.GameState(pos, (i * settings.MapsPerRow) + y + 1, mapCenter, map, unitSpawnList);
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

                foreach(Vector3fList c in BuildRawClusters(wi.WorldIndex))
                {
                    obstructVision.Add(c);
                }

                var oVisionCluster = LeyLineEntityTemplates.ObstructVisionClusters(pos, wi.WorldIndex, obstructVision);
                snapshot.AddEntity(oVisionCluster);
            }
        }

        private static void AddManaliths(Snapshot snapshot)
        {
            foreach (ManalithInitializer m in Object.FindObjectsOfType<ManalithInitializer>())
            {
                List<ManalithSlot> slots = new List<ManalithSlot>();
                List<Vector3f> circleCellCoords = new List<Vector3f>();

                foreach (LeyLineHybridECS.Cell n in m.leyLineCircle)
                {
                    if (n.GetComponent<IsTaken>().Value != true)
                    {
                        slots.Add(new ManalithSlot
                        {
                            AxialCoordinate = CellGridMethods.CubeToAxial(new Vector3f(n.GetComponent<CoordinateDataComponent>().CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().CubeCoordinate.z)),
                            IsTaken = n.GetComponent<IsTaken>().Value
                        });
                    }
                    circleCellCoords.Add(new Vector3f(n.GetComponent<CoordinateDataComponent>().CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().CubeCoordinate.z));
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

                var coordComp = m.GetComponent<ManalithInitializer>().occupiedCell.GetComponent<CoordinateDataComponent>().CubeCoordinate;

                var coord = new Vector3f(coordComp.x, coordComp.y, coordComp.z);

                var connectedManalithCoord = new Vector3f();

                if (m.connectedManaLith)
                    connectedManalithCoord = Vector3fext.ToVector3f(m.connectedManaLith.occupiedCell.GetComponent<CoordinateDataComponent>().CubeCoordinate);

                var pathCoordList = new List<Vector3f>();

                foreach (float3 f3 in m.leyLinePathCoords)
                {
                    pathCoordList.Add(Vector3fext.ToVector3f(f3));
                }

                var manalith = LeyLineEntityTemplates.ManalithUnit(m.name, pos, coord, 0, worldIndex, stats, (uint) m.transform.eulerAngles.y, circleCellCoords, pathCoordList, slots, connectedManalithCoord);
                snapshot.AddEntity(manalith);
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

        private static List<Vector3fList> BuildRawClusters(uint worldIndex)
        {
            List<Vector3fList> RawClusters = new List<Vector3fList>();

            List<Vector3f> obstructed = new List<Vector3f>();

            foreach (LeyLineHybridECS.Cell c in Object.FindObjectsOfType<LeyLineHybridECS.Cell>())
            {
                if (c.GetComponent<CellType>().thisCellsTerrain.obstructVision)
                {
                    obstructed.Add(new Vector3f(c.GetComponent<CoordinateDataComponent>().CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().CubeCoordinate.z));
                }
            }
            List<RawCluster> raw = new List<RawCluster>();

            while (obstructed.Count > 0)
            {
                Vector3f c = obstructed[0];
                RawCluster go = new RawCluster(c);
                obstructed.Remove(c);
                BuildCluster(c, go, obstructed, out obstructed);
                raw.Add(go);
            }

            for (int i = raw.Count - 1; i >= 0; i--)
            {
                if (raw[i].cluster.Coordinates.Count > 0)
                {
                    RawClusters.Add(raw[i].cluster);
                }
            }

            return RawClusters;
        }

        private static void BuildCluster(Vector3f cell, RawCluster cluster, List<Vector3f> obstructed, out List<Vector3f> newObstructed)
        {
            List<Vector3f> neighbours = new List<Vector3f>();

            for (uint i = 0; i < 6; i++)
                neighbours.Add(CellGridMethods.CubeNeighbour(cell, i));

            for (int i = neighbours.Count - 1; i >= 0; i--)
            {
                bool contains = false;
                {
                    foreach (Vector3f c in obstructed)
                    {
                        if (Vector3fext.ToUnityVector(c) == Vector3fext.ToUnityVector(neighbours[i]))
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

                foreach (Vector3f c in cluster.cluster.Coordinates)
                {
                    if (Vector3fext.ToUnityVector(c) == Vector3fext.ToUnityVector(neighbours[i])) contains = true;
                }

                if (!contains)
                {
                    bool isSet = false;
                    Vector3f toRemove = new Vector3f();
                    foreach (Vector3f c in obstructed)
                    {
                        if (Vector3fext.ToUnityVector(c) == Vector3fext.ToUnityVector(neighbours[i]))
                        {
                            toRemove = c;
                            isSet = true;
                        }
                    }

                    if (isSet)
                    {
                        obstructed.Remove(toRemove);
                        cluster.cluster.Coordinates.Add(toRemove);
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
        public Vector3fList cluster;

        public RawCluster(Vector3fList inCluster)
        {
            cluster = inCluster;
        }
        public RawCluster(Vector3f inStart)
        {
            cluster = new Vector3fList
            {
                Coordinates = new List<Vector3f>
                {
                    inStart
                }
            };
        }
    }
}
