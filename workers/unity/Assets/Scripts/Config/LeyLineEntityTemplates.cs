﻿using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Movement;
using Improbable.Gdk.StandardTypes;
using Improbable.Gdk.Health;
using Improbable.Common;
using Unity.Entities;
using UnityEngine;

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


    public static EntityTemplate Cell(Vector3f position, bool isTaken, string unitName)
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

        var taken = new Cells.IsTaken.Snapshot
        {
            IsTaken = isTaken
        };

        var unitToSpawn = new Cells.UnitToSpawn.Snapshot
        {

            UnitName = unitName
        };

        var template = new EntityTemplate();
        template.AddComponent(pos, gameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Cell" }, gameLogic);
        template.AddComponent(new Persistence.Snapshot(), gameLogic);
        template.AddComponent(unitToSpawn, gameLogic);
        template.AddComponent(taken, gameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;

    }

    public static EntityTemplate Player(string workerId, Vector3f position)
    {
        var client = $"workerId:{workerId}";

        var (spawnPosition, spawnYaw, spawnPitch) = SpawnPoints.GetRandomSpawnPoint();

        var serverResponse = new ServerResponse
        {
            Position = spawnPosition.ToIntAbsolute()
        };

        var rotationUpdate = new RotationUpdate
        {
            Yaw = spawnYaw.ToInt1k(),
            Pitch = spawnPitch.ToInt1k()
        };

        var energy = new Player.EnergyComponent.Snapshot
        {
            MaxEnergy = 10,
            Energy = 10
        };

        var pos = new Position.Snapshot { Coords = spawnPosition.ToSpatialCoordinates() };
        var serverMovement = new ServerMovement.Snapshot { Latest = serverResponse };
        var clientMovement = new ClientMovement.Snapshot { Latest = new ClientRequest() };
        var clientRotation = new ClientRotation.Snapshot { Latest = rotationUpdate };

        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Generic.FactionComponent.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Player.PlayerState.Snapshot(), client);
        template.AddComponent(serverMovement, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientMovement, client);
        template.AddComponent(clientRotation, client);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        //template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
        return template;
    }

    public static EntityTemplate Unit(string unitName, Position.Component position, uint faction)
    {

        var pos = new Position.Snapshot
        {
            Coords = position.Coords
        };

        var health = new HealthComponent.Snapshot
        {
            MaxHealth = 10,
            Health = 10

        };

        var factionSnapshot = new Generic.FactionComponent.Snapshot
        {
            Faction = faction
        };


        var template = new EntityTemplate();
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(health, WorkerUtils.UnityGameLogic);
        //template.AddComponent(new HealthRegenComponent.Snapshot(), WorkerUtils.UnityGameLogic);

        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }
}
