using System.Collections.Generic;
using Improbable;
using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;
using Improbable.Gdk.StandardTypes;
using Improbable.Gdk.Health;
using Generic;
using Player;
using Cells;
using Unit;

public static class LeyLineEntityTemplates {

    
    private static readonly List<string> AllWorkerAttributes =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient };


    public static EntityTemplate GameState(Vector3f position, uint worldIndex)
    {
        var gameState = new Generic.GameState.Snapshot
        {
            CurrentState = Generic.GameStateEnum.planning,
            PlayersOnMapCount = 0
        };

        var wIndex = new Generic.WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var pos = new Position.Snapshot
        {
            Coords = new Coordinates
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z
            }
        };

        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "GameState" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(gameState, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate Manalith(Vector3f position, CellAttributeList circleCells, uint worldIndex)
    {
        var pos = new Position.Snapshot
        {
            Coords = new Coordinates
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z
            }
        };

        var circle = new CircleCells.Snapshot
        {
            CircleAttributeList = circleCells
        };

        var wIndex = new Generic.WorldIndex.Snapshot
        {
            Value = worldIndex
        };


        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Manalith" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(new Generic.FactionComponent.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(circle, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate Cell(Vector3f cubeCoordinate, Vector3f position, bool isTaken, bool isCircleCell, string unitName, bool isSpawn, uint faction, CellAttributeList neighbours, uint worldIndex, bool inObstruction)
    {
        var gameLogic = WorkerUtils.UnityGameLogic;
        //var clientLogic = WorkerUtils.UnityClient;

        //var owningComponent = new OwningWorker.Snapshot { WorkerId = client };

        var pos = new Position.Snapshot
        {
            Coords = new Coordinates
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z
            }
        };

        var coord = new CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };


        var cellAttributes = new CellAttributesComponent.Snapshot
        {
            CellAttributes = new CellAttributes
            {
                Cell = new CellAttribute
                {
                    Position = position,
                    CubeCoordinate = cubeCoordinate,
                    IsTaken = isTaken,
                    MovementCost = 1,
                    ObstructVision = inObstruction
                },
                Neighbours = neighbours
            }

        };

        var unitToSpawn = new UnitToSpawn.Snapshot
        {
            UnitName = unitName,
            IsSpawn = isSpawn,
            Faction = (worldIndex - 1) * 2 + faction
        };

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var template = new EntityTemplate();
        template.AddComponent(pos, gameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Cell" }, gameLogic);
        template.AddComponent(new Persistence.Snapshot(), gameLogic);
        template.AddComponent(unitToSpawn, gameLogic);
        template.AddComponent(coord, gameLogic);
        template.AddComponent(cellAttributes, gameLogic);
        template.AddComponent(wIndex, gameLogic);
        if (isCircleCell)
            template.AddComponent(new IsCircleCell.Snapshot(), gameLogic);
        if (isSpawn)
            template.AddComponent(new IsSpawn.Snapshot(), gameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());

        return template;
    }

    public static EntityTemplate Player(string workerId, Vector3f position)
    {
        var client = $"workerId:{workerId}";

        var energy = new EnergyComponent.Snapshot
        {
            MaxEnergy = 10,
            Energy = 10
        };

        var playerAttributes = new PlayerAttributes.Snapshot
        {
            HeroName = "KingCroak"
        };

        var factionSnapshot = new FactionComponent.Snapshot();

        var playerVision = new Vision.Snapshot
        {
            CellsInVisionrange = new List<Cells.CellAttributes>()
        };

        var wIndex = new WorldIndex.Snapshot();


        var pos = new Position.Snapshot { Coords = position.ToUnityVector().ToSpatialCoordinates() };
        var clientHeartbeat = new PlayerHeartbeatClient.Snapshot();
        var serverHeartbeat = new PlayerHeartbeatServer.Snapshot();
        var owningComponent = new OwningWorker.Snapshot { WorkerId = client };


        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientHeartbeat, client);
        template.AddComponent(serverHeartbeat, WorkerUtils.UnityGameLogic);
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(new PlayerState.Snapshot(), client);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(playerAttributes, WorkerUtils.UnityGameLogic);
        template.AddComponent(playerVision, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
        return template;
    }

    public static EntityTemplate Unit(string workerId, string unitName, Position.Component position, Vector3f cubeCoordinate, Generic.FactionComponent.Component faction, uint worldIndex, Unit_BaseDataSet Stats)
    {
        var client = workerId;

        var pos = new Position.Snapshot
        {
            Coords = position.Coords
        };

        var coord = new Generic.CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };


        var health = new HealthComponent.Snapshot
        {
            MaxHealth = 10,
            Health = 10

        };

        var factionSnapshot = new Generic.FactionComponent.Snapshot
        {
            Faction = faction.Faction,
            TeamColor = faction.TeamColor
        };

        var cellsToMarkSnapshot = new CellsToMark.Snapshot
        {
            CellsInRange = new List<CellAttributes>(),
            CachedPaths = new Dictionary<CellAttribute, CellAttributeList>()
        };

        
        var clientPathSnapshot = new ClientPath.Snapshot
        {
            Path = new CellAttributeList(new List<CellAttribute>()),
        };

        var serverPathSnapshot = new ServerPath.Snapshot
        {
            Path = new CellAttributeList(new List<CellAttribute>()),
        };

        var movementVariables = new MovementVariables.Snapshot
        {
            MovementRange = 4,
            TravelTime = 1.7f,
        };

        var wIndex = new Generic.WorldIndex.Snapshot
        {
            Value = worldIndex

        };

        var unitVision = new Vision.Snapshot
        {

            CellsInVisionrange = new List<Cells.CellAttributes>(),
            RequireUpdate = true,
            VisionRange = Stats.VisionRange

        };

        var clientHeartbeat = new PlayerHeartbeatClient.Snapshot();
        var serverHeartbeat = new PlayerHeartbeatServer.Snapshot();
        var owningComponent = new OwningWorker.Snapshot { WorkerId = client };

        var template = new EntityTemplate();
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientHeartbeat, client);
        template.AddComponent(serverHeartbeat, WorkerUtils.UnityGameLogic);
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);
        template.AddComponent(health, WorkerUtils.UnityGameLogic);
        template.AddComponent(cellsToMarkSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(serverPathSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientPathSnapshot, client);
        template.AddComponent(unitVision, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }
}
