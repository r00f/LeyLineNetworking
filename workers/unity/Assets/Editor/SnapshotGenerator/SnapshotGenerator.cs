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

        private static string DefaultSnapshotPath = Path.GetFullPath(
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
            //AddEffectStack(snapshot);
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
                        Vector3f pos = new Vector3f(wi.transform.position.x + settings.MapOffset * y, wi.transform.position.y, wi.transform.position.z + settings.MapOffset * i);
                        Vector2f mapCenter = new Vector2f(wi.centerCellTransform.position.x + settings.MapOffset * y, wi.centerCellTransform.position.z + settings.MapOffset * i);
                        var gameState = LeyLineEntityTemplates.GameState(pos, (i * settings.MapsPerRow) + y + 1, mapCenter);
                        snapshot.AddEntity(gameState);
                    }
                }
            }
        }

        private static void AddManaliths(Snapshot snapshot)
        {
            var columnCount = settings.MapCount / settings.MapsPerRow;

            for (uint i = 0; i < columnCount; i++)
            {
                for (uint y = 0; y < settings.MapsPerRow; y++)
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
                                Position = new Vector3f(n.transform.position.x + settings.MapOffset * y, n.transform.position.y, n.transform.position.z + settings.MapOffset * i),
                                CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                                IsTaken = n.GetComponent<IsTaken>().Value,
                                MovementCost = n.GetComponent<MovementCost>().Value
                            });
                        }

                        Position.Component pos = new Position.Component
                        {
                            Coords = new Coordinates
                            {
                                X = m.transform.position.x + settings.MapOffset * y,
                                Y = m.transform.position.y,
                                Z = m.transform.position.z + settings.MapOffset * i
                            }
                        };

                        uint worldIndex = (i * settings.MapsPerRow) + y + 1;

                        var stats = m.GetComponent<UnitDataSet>();
                        var AIstats = m.GetComponent<AIUnitDataSet>();

                        var coordComp = m.GetComponent<ManalithInitializer>().occupiedCell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate;

                        var coord = new Vector3f(coordComp.x, coordComp.y, coordComp.z);

                        //Debug.Log(Regex.Replace(stats.UnitName, @"\s+", ""));
                        //Debug.Log("AddManalithUnitToSnapshot");

                        var connectedManalithCoord = new Vector3f();

                        if (m.connectedManaLith)
                            connectedManalithCoord = Vector3fext.ToVector3f(m.connectedManaLith.occupiedCell.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate);

                        var pathCoordList = new List<Vector3f>();

                        foreach (float3 f3 in m.leyLinePathCoords)
                        {
                            pathCoordList.Add(Vector3fext.ToVector3f(f3));
                        }

                        var manalith = LeyLineEntityTemplates.ManalithUnit(m.name, pos, coord, 0, worldIndex, stats, AIstats, (uint) m.transform.eulerAngles.y, circle, pathCoordList, connectedManalithCoord);
                        snapshot.AddEntity(manalith);
                    }
                }
            }
        }

        private static void AddCellGrid(Snapshot snapshot)
        {
            var columnCount = settings.MapCount / settings.MapsPerRow;

            for (uint i = 0; i < columnCount; i++)
            {
                for (uint y = 0; y < settings.MapsPerRow; y++)
                {
                    /*
                    foreach (EditorWorldIndex wi in Object.FindObjectsOfType<EditorWorldIndex>())
                    {
                        Vector3f pos = new Vector3f(wi.transform.position.x + settings.MapOffset * y, wi.transform.position.y, wi.transform.position.z + settings.MapOffset * i);
                        Vector2f mapCenter = new Vector2f(wi.centerCellTransform.position.x + settings.MapOffset * y, wi.centerCellTransform.position.z + settings.MapOffset * i);
                        var gameState = LeyLineEntityTemplates.GameState(pos, , mapCenter);
                        snapshot.AddEntity(gameState);
                    }
                    */


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
                                Position = new Vector3f(n.transform.position.x + settings.MapOffset * y, n.transform.position.y, n.transform.position.z + settings.MapOffset * i),
                                CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                                IsTaken = n.GetComponent<IsTaken>().Value,
                                MovementCost = n.GetComponent<MovementCost>().Value,
                                ObstructVision = n.GetComponent<CellType>().thisCellsTerrain.obstructVision
                            });
                        }
                        Vector3f pos = new Vector3f(c.transform.position.x + settings.MapOffset * y, c.transform.position.y, c.transform.position.z + settings.MapOffset * i);
                        Vector3f cubeCoord = new Vector3f(c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z);
                        uint worldIndex = (i * settings.MapsPerRow) + y + 1;
                        int mapCellColor = c.GetComponent<CellType>().thisCellsTerrain.MapCellColorIndex;
                        var cell = LeyLineEntityTemplates.Cell(cubeCoord, pos, c.GetComponent<IsTaken>().Value, c.GetComponent<EditorIsCircleCell>().IsLeylineCircleCell, c.GetComponent<UnitToSpawnEditor>().UnitName, c.GetComponent<UnitToSpawnEditor>().IsUnitSpawn, c.GetComponent<UnitToSpawnEditor>().IsManalithUnit, c.GetComponent<UnitToSpawnEditor>().Faction, neighbours, worldIndex, c.GetComponent<CellType>().thisCellsTerrain.obstructVision, mapCellColor, c.GetComponent<UnitToSpawnEditor>().StartUnitIndex, c.GetComponent<UnitToSpawnEditor>().StartRotation);
                        snapshot.AddEntity(cell);
                    }
                }
            }
        }

        private static void AddPlayerSpawner(Snapshot snapshot)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

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
            template.AddComponent(pos, serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "PlayerCreator" }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new PlayerCreator.Snapshot(), serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            snapshot.AddEntity(template);
        }
        /*
        private static void AddEffectStack(Snapshot snapshot)
        {
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            Position.Snapshot pos = new Position.Snapshot
            {
                Coords = new Coordinates
                {
                    X = 0,
                    Y = 0,
                    Z = 0
                }
            };


            var gameStateStacks = new List<GameStateEffectStack>();



            foreach (EditorWorldIndex wi in Object.FindObjectsOfType<EditorWorldIndex>())
            {
                gameStateStacks.Add(new GameStateEffectStack
                {
                    WorldIndex = wi.WorldIndex,
                    InterruptEffects = new List<ActionEffect>(),
                    AttackEffects = new List<ActionEffect>(),
                    MoveEffects = new List<ActionEffect>(),
                    SkillshotEffects = new List<ActionEffect>()
                }
                );
            }

            gameStateStacks.Sort((x, y) => x.WorldIndex.CompareTo(y.WorldIndex));

            EffectStack.Snapshot effectStack = new EffectStack.Snapshot
            {
                GameStateEffectStacks = gameStateStacks
            };

            var template = new EntityTemplate();
            template.AddComponent(pos, serverAttribute);
            template.AddComponent(new Metadata.Snapshot { EntityType = "EffectStack" }, serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(effectStack, serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            snapshot.AddEntity(template);
        }
        */
    }
}
