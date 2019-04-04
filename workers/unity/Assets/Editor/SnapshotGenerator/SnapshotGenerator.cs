using Improbable;
using Improbable.Gdk.Core;
using Improbable.PlayerLifecycle;
using UnityEngine;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using LeyLineHybridECS;
using System.Collections.Generic;

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
            return snapshot;
        }

        private static void AddGameState(Snapshot snapshot)
        {
            snapshot.AddEntity(LeyLineEntityTemplates.GameState());
        }

        private static void AddCellGrid(Snapshot snapshot)
        {
            foreach (Cell c in Object.FindObjectsOfType<Cell>())
            {
                var terrain = c.GetComponent<CellType>().thisCellsTerrain;
                var neighbours = new List<Cells.CellAttribute>();
                foreach(Cell n in c.GetComponent<Neighbours>().NeighboursList)
                {
                    neighbours.Add(new Cells.CellAttribute
                    {
                        Position = new Vector3f(n.transform.position.x, n.transform.position.y, n.transform.position.z),
                        CubeCoordinate = new Vector3f(n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, n.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z),
                        IsTaken = n.GetComponent<IsTaken>().Value,
                        MovementCost = n.GetComponent<MovementCost>().Value,
                        ObstructVision = n.GetComponent<CellType>().thisCellsTerrain.obstructVision
                     
                    });
                }
                var cell = LeyLineEntityTemplates.Cell(new Vector3f(c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.x, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.y, c.GetComponent<CoordinateDataComponent>().Value.CubeCoordinate.z), 
                    new Vector3f(c.GetComponent<Position3DDataComponent>().Value.Value.x, c.GetComponent<Position3DDataComponent>().Value.Value.y, c.GetComponent<Position3DDataComponent>().Value.Value.z), c.GetComponent<IsTaken>().Value, c.GetComponent<UnitToSpawn>().UnitName, c.GetComponent<UnitToSpawn>().IsHeroSpawn, c.GetComponent<UnitToSpawn>().Faction,
                    neighbours, terrain.obstructVision);
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
                    X = 50,
                    Y = 2,
                    Z = 31

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
