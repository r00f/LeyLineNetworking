using Improbable;
using Improbable.Gdk.Core;
using Improbable.PlayerLifecycle;
using UnityEngine;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using LeyLineHybridECS;
using System.Collections.Generic;
using Cells;

namespace BlankProject.Editor
{
    internal static class SnapshotGenerator
    {
        public struct Arguments
        {
            public string OutputPath;
        }

        public static void Generate(Arguments arguments)
        {
            Debug.Log("Generating snapshot.");
            var snapshot = CreateSnapshot();

            Debug.Log($"Writing snapshot to: {arguments.OutputPath}");
            snapshot.WriteToFile(arguments.OutputPath);
        }

        private static Snapshot CreateSnapshot()
        {
            var snapshot = new Snapshot();
            AddPlayerSpawner(snapshot);
            AddGameState(snapshot);
            AddCellGrid(snapshot);
            AddManaliths(snapshot);
            return snapshot;
        }

        private static void AddGameState(Snapshot snapshot)
        {
            foreach(EditorWorldIndex wi in Object.FindObjectsOfType<EditorWorldIndex>())
            {
                Vector3f pos = new Vector3f(wi.transform.position.x + 50, wi.transform.position.y, wi.transform.position.z);
                var gameState = LeyLineEntityTemplates.GameState(pos, wi.WorldIndex);
                snapshot.AddEntity(gameState);
            }
        }

        private static void AddManaliths(Snapshot snaphot)
        {
            foreach(ManalithInitializer m in Object.FindObjectsOfType<ManalithInitializer>())
            {
                var circle = new CellAttributeList
                {
                    CellAttributes = new List<CellAttribute>()
                };

                
                foreach (Cell n in m.leyLineCircle)
                {
                    circle.CellAttributes.Add(new CellAttribute
                    {
                        Position = new Vector3f(n.transform.position.x, n.transform.position.y, n.transform.position.z),
                        CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                        IsTaken = n.GetComponent<IsTaken>().Value,
                        MovementCost = n.GetComponent<MovementCost>().Value
                    });
                }
                
                Vector3f pos = new Vector3f(m.transform.position.x, m.transform.position.y, m.transform.position.z);
                uint worldIndex = m.transform.parent.parent.GetComponent<EditorWorldIndex>().WorldIndex;
                var manalith = LeyLineEntityTemplates.Manalith(pos, circle, worldIndex);
                snaphot.AddEntity(manalith);
            }
        }

        private static void AddCellGrid(Snapshot snapshot)
        {
            foreach (Cell c in Object.FindObjectsOfType<Cell>())
            {
                var neighbours = new CellAttributeList
                {
                    CellAttributes = new List<CellAttribute>()
                };

                foreach(Cell n in c.GetComponent<Neighbours>().NeighboursList)
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
                uint worldIndex = c.transform.parent.parent.GetComponent<EditorWorldIndex>().WorldIndex;
                var cell = LeyLineEntityTemplates.Cell(cubeCoord, pos, c.GetComponent<IsTaken>().Value, c.GetComponent<EditorIsCircleCell>().Value, c.GetComponent<UnitToSpawnEditor>().UnitName, c.GetComponent<UnitToSpawnEditor>().IsHeroSpawn, c.GetComponent<UnitToSpawnEditor>().Faction, neighbours, worldIndex, c.GetComponent<CellType>().thisCellsTerrain.obstructVision);
                snapshot.AddEntity(cell);
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

            template.SetReadAccess(UnityClientConnector.WorkerType, UnityGameLogicConnector.WorkerType, AndroidClientWorkerConnector.WorkerType, iOSClientWorkerConnector.WorkerType);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            snapshot.AddEntity(template);
        }
    }
}
