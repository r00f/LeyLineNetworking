﻿using System.Collections.Generic;
using Improbable;
using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;
using Improbable.Gdk.Movement;
using Improbable.Gdk.StandardTypes;
using Improbable.Gdk.Health;
using Improbable.Common;
using Unity.Entities;
using UnityEngine;
using Player;
using Generic;

public static class LeyLineEntityTemplates {

    
    private static readonly List<string> AllWorkerAttributes =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient };


    public static EntityTemplate GameState()
    {
        var gameState = new Generic.GameState.Snapshot
        {
            CurrentState = Generic.GameStateEnum.spawning
        };

        var template = new EntityTemplate();
        template.AddComponent(new Position.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "GameState" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(gameState, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }


    public static EntityTemplate Cell(Vector3f cubeCoordinate, Vector3f position, bool isTaken, string unitName, bool isSpawn, uint faction, List<Cells.CellAttribute> neighbours, bool inObstruction)
    {
        
        var gameLogic = WorkerUtils.UnityGameLogic;
        //var clientLogic = WorkerUtils.UnityClient;

        var pos = new Position.Snapshot
        {
            Coords = new Coordinates
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z
            }
        };

        var coord = new Generic.CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };


        var cellAttributes = new Cells.CellAttributesComponent.Snapshot
        {
            CellAttributes = new Cells.CellAttributes
            {
                Cell = new Cells.CellAttribute
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

        var unitToSpawn = new Cells.UnitToSpawn.Snapshot
        {
            UnitName = unitName,
            IsSpawn = isSpawn,
            Faction = faction
        };

        

        var template = new EntityTemplate();
        template.AddComponent(pos, gameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Cell" }, gameLogic);
        template.AddComponent(new Persistence.Snapshot(), gameLogic);
        template.AddComponent(unitToSpawn, gameLogic);
        template.AddComponent(coord, gameLogic);
        template.AddComponent(cellAttributes, gameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate Player(string workerId, Vector3f position)
    {
        var client = $"workerId:{workerId}";

        var(spawnPosition, spawnYaw, spawnPitch) = SpawnPoints.GetRandomSpawnPoint();

        var serverResponse = new ServerResponse
        {
            Position = spawnPosition.ToIntAbsolute()
        };

        var rotationUpdate = new RotationUpdate
        {
            Yaw = spawnYaw.ToInt1k(),
            Pitch = spawnPitch.ToInt1k()
        };

        var energy = new EnergyComponent.Snapshot
        {
            MaxEnergy = 10,
            Energy = 10
        };

        var playerAttributes = new PlayerAttributes.Snapshot
        {
            HeroName = "KingCroak"
        };

        var playerVision = new Vision.Snapshot
        {
            CellsInVisionrange = new List<Cells.CellAttributes>()
        };

        var pos = new Position.Snapshot { Coords = spawnPosition.ToSpatialCoordinates() };
     
        var serverMovement = new ServerMovement.Snapshot { Latest = serverResponse };
        var clientMovement = new ClientMovement.Snapshot { Latest = new ClientRequest() };
        var clientRotation = new ClientRotation.Snapshot { Latest = rotationUpdate };

        var clientHeartbeat = new PlayerHeartbeatClient.Snapshot();
        var serverHeartbeat = new PlayerHeartbeatServer.Snapshot();
        var owningComponent = new OwningWorker.Snapshot { WorkerId = client };


        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientHeartbeat, client);
        template.AddComponent(serverHeartbeat, WorkerUtils.UnityGameLogic);
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);
        template.AddComponent(new FactionComponent.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(new PlayerState.Snapshot(), client);
        template.AddComponent(playerAttributes, WorkerUtils.UnityGameLogic);
        template.AddComponent(serverMovement, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientMovement, client);
        template.AddComponent(clientRotation, client);
        template.AddComponent(playerVision, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        //template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
        return template;
    }

    public static EntityTemplate Unit(string workerId, string unitName, Position.Component position, Vector3f cubeCoordinate, FactionComponent.Component faction, Unit_BaseDataSet Stats)
    {
        var client = workerId;

        var pos = new Position.Snapshot
        {
            Coords = position.Coords
        };

        var coord = new CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };


        var health = new HealthComponent.Snapshot
        {
            MaxHealth = 10,
            Health = 10

        };

        var factionSnapshot = new FactionComponent.Snapshot
        {
            Faction = faction.Faction,
            TeamColor = faction.TeamColor
        };

        var cellsToMarkSnapshot = new Unit.CellsToMark.Snapshot
        {
            CellsInRange = new List<Cells.CellAttributes>(),
            CachedPaths = new Dictionary<Cells.CellAttribute, Unit.CellAttributeList>()
        };

        var currentPathSnapshot = new Unit.CurrentPath.Snapshot
        {
            Path = new Unit.CellAttributeList(new List<Cells.CellAttribute>()),
        };

        var clientHeartbeat = new PlayerHeartbeatClient.Snapshot();
        var serverHeartbeat = new PlayerHeartbeatServer.Snapshot();
        var owningComponent = new OwningWorker.Snapshot { WorkerId = client };
        var unitVision = new Vision.Snapshot
        {

            CellsInVisionrange = new List<Cells.CellAttributes>(),
            RequireUpdate = true,
            VisionRange = Stats.VisionRange
            
        };
        
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
        template.AddComponent(currentPathSnapshot, client);
        template.AddComponent(unitVision, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }
}
