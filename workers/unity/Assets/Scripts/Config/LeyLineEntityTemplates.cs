
using System.Collections.Generic;
using Improbable;
using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;
using Improbable.Gdk.StandardTypes;
using Generic;
using Player;
using Cell;
using Unit;
using LeyLineHybridECS;
using static LeyLineHybridECS.ECSAction;

public static class LeyLineEntityTemplates {

    private static readonly List<string> AllWorkerAttributes =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient };

    public static EntityTemplate GameState(Vector3f position, uint worldIndex)
    {
        var gameState = new GameState.Snapshot
        {
            CurrentState = GameStateEnum.waiting_for_players,
            PlayersOnMapCount = 0,
            CalculateWaitTime = 5f,
            CurrentWaitTime = 0f,
            PlanningTime = 60f,
            CurrentPlanningTime = 60f,
            RopeDisplayTime = 30f
        };

        var wIndex = new WorldIndex.Snapshot
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

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };


        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Manalith" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(new FactionComponent.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(circle, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate Cell(Vector3f cubeCoordinate, Vector3f position, bool isTaken, bool isCircleCell, string unitName, bool isSpawn, uint faction, CellAttributeList neighbours, uint worldIndex, bool inObstruction, int mapColorIndex)
    {
        var gameLogic = WorkerUtils.UnityGameLogic;

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
                CellMapColorIndex = mapColorIndex,
                Cell = new CellAttribute
                {
                    Position = position,
                    CubeCoordinate = cubeCoordinate,
                    IsTaken = isTaken,
                    MovementCost = 1,
                    ObstructVision = inObstruction,
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

    public static EntityTemplate Player(string workerId, byte[] serializedArguments)
    {
        var client = $"workerId:{workerId}";

        var energy = new PlayerEnergy.Snapshot
        {
            MaxEnergy = 40,
            Energy = 20,
            BaseIncome = 3
        };

        var playerAttributes = new PlayerAttributes.Snapshot
        {
            HeroName = "KingCroak"
        };

        var factionSnapshot = new FactionComponent.Snapshot();

        var playerVision = new Vision.Snapshot
        {
            CellsInVisionrange = new List<Vector3f>(),
            Lastvisible = new List<Vector3f>(),
            Positives = new List<Vector3f>(),
            Negatives = new List<Vector3f>()
            
        };

        var playerState = new PlayerState.Snapshot
        {
            CellsInRange = new List<CellAttribute>(),
            CachedPaths = new Dictionary<CellAttribute, CellAttributeList>(),
            UnitTargets = new Dictionary<long, CubeCoordinateList>(),
            EndStepReady = true

        };

        var wIndex = new WorldIndex.Snapshot();

        var pos = new Position.Snapshot { Coords = new Coordinates() };
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
        template.AddComponent(playerState, client);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(playerAttributes, WorkerUtils.UnityGameLogic);
        template.AddComponent(playerVision, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
        return template;
    }

    public static EntityTemplate Unit(string workerId, string unitName, Position.Component position, Vector3f cubeCoordinate, FactionComponent.Component faction, uint worldIndex, Unit_BaseDataSet Stats)
    {
        var client = workerId;

        var turnTimer = new TurnTimer.Snapshot
        {
            Timers = new List<Timer>()
        };

        var pos = new Position.Snapshot
        {
            Coords = position.Coords
        };

        var coord = new CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };

        var factionSnapshot = new FactionComponent.Snapshot
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

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var unitVision = new Vision.Snapshot
        {
            CellsInVisionrange = new List<Vector3f>(),
            Lastvisible = new List<Vector3f>(),
            Positives = new List<Vector3f>(),
            Negatives = new List<Vector3f>(),
            RequireUpdate = true,
            VisionRange = Stats.VisionRange
        };

        var health = new Health.Snapshot
        {
            MaxHealth = Stats.BaseHealth,
            CurrentHealth = Stats.BaseHealth
        };

        var energy = new Energy.Snapshot
        {
            //SpawnCost = Stats.SpawnCost,
            //EnergyUpkeep = Stats.EnergyUpkeep,
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new MovementVariables.Snapshot
        {
            //MovementRange = Stats.MovementRange,
            TravelTime = 1.7f
        };


        var actions = SetActions(Stats);

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
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(cellsToMarkSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(clientPathSnapshot, client);
        template.AddComponent(unitVision, WorkerUtils.UnityGameLogic);
        template.AddComponent(turnTimer, WorkerUtils.UnityGameLogic);
        template.AddComponent(actions, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    static Actions.Snapshot SetActions (Unit_BaseDataSet inStats){

        Action myBasicAttack = new Action();
        myBasicAttack.Targets = new List<ActionTarget>();
        myBasicAttack.Effects = new List<ActionEffect>();
        Action myBasicMove = new Action();
        myBasicMove.Targets = new List<ActionTarget>();
        myBasicMove.Effects = new List<ActionEffect>();
        Action myNullableAction = new Action();
        myNullableAction.Index = -3;
        myNullableAction.Targets = new List<ActionTarget>();
        myNullableAction.Effects = new List<ActionEffect>();
        List<Action> myOtherActions = new List<Action>();


        if (inStats.BasicMove != null)
        {
            myBasicMove = SetAction(inStats.BasicMove, -2);
        }
        else
        {
            myBasicMove = myNullableAction;
        }

        if (inStats.BasicAttack != null)
        {
            myBasicAttack = SetAction(inStats.BasicAttack, -1);
        }
        else
        {
            myBasicAttack = myNullableAction;
        }

        for (int i = 0; i < inStats.Actions.Count; i++)
        {
            myOtherActions.Add(SetAction(inStats.Actions[i], i));
        }

        for (int i = 0; i < inStats.SpawnActions.Count; i++)
        {
            myOtherActions.Add(SetAction(inStats.SpawnActions[i], i + inStats.Actions.Count));
        }

        var newActions = new Actions.Snapshot
        {
            BasicMove = myBasicMove,
            BasicAttack = myBasicAttack,
            OtherActions = myOtherActions,
            NullAction = myNullableAction,
            CurrentSelected = myNullableAction,
            LastSelected = myNullableAction,
            LockedAction = myNullableAction
        };

        return newActions;
        }

    static Action SetAction (ECSAction inAction, int index)
    {
        Action newAction = new Action();
        newAction.TimeToExecute = inAction.TimeToExecute;
        newAction.ActionExecuteStep = (ExecuteStepEnum)(int)inAction.ActionExecuteStep;
        newAction.Name = inAction.name;
        newAction.Index = index;
        newAction.Targets = new List<ActionTarget>();
        newAction.Effects = new List<ActionEffect>();
        newAction.CombinedCost = 0;
        for (int i = 0; i <= inAction.Targets.Count - 1; i++)
        {
            ActionTarget newAT = new ActionTarget();
            newAT.EnergyCost = inAction.Targets[i].energyCost;
            newAT.Targettingrange = inAction.Targets[i].targettingRange;
            newAT.Mods = new List<TargetMod>();

            if (inAction.Targets[i] is ECSATarget_Tile)
            {
                ECSATarget_Tile go = inAction.Targets[i] as ECSATarget_Tile;
                newAT.TargetType = TargetTypeEnum.cell;
                newAT.CellTargetNested.RequireEmpty = go.requireEmpty;
                newAT.CellTargetNested.RequireVisible = false;
                
                switch (inAction.Targets[i].HighlighterToUse)
                {
                    case ECSActionTarget.HighlightDef.Path:
                        newAT.Higlighter = UseHighlighterEnum.pathing;
                        break;
                    case ECSActionTarget.HighlightDef.Radius:
                        newAT.Higlighter = UseHighlighterEnum.no_pathing;
                        break;
                    case ECSActionTarget.HighlightDef.Path_Visible:
                        newAT.Higlighter = UseHighlighterEnum.pathing_visible;
                        break;
                    case ECSActionTarget.HighlightDef.Radius_Visible:
                        newAT.Higlighter = UseHighlighterEnum.no_pathing_visible;
                        break;
                }
                foreach (ECSActionSecondaryTargets t in go.SecondaryTargets)
                {
                    if (t is SecondaryArea)
                    {
                        SecondaryArea go1 = t as SecondaryArea;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.aoe;
                        mod.AoeNested.Radius = go1.areaSize;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryPath)
                    {
                        SecondaryPath go1 = t as SecondaryPath;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.path;
                        mod.PathNested.Costpertile = go1.costPerTile;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryLine)
                    {
                        SecondaryLine go1 = t as SecondaryLine;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.line;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryRing)
                    {
                        SecondaryRing go1 = t as SecondaryRing;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.ring;
                        mod.RingNested.Radius = go1.radius;
                        newAT.Mods.Add(mod);
                    }
                }
            }
            if (inAction.Targets[i] is ECSATarget_Unit)
            {
                ECSATarget_Unit go = inAction.Targets[i] as ECSATarget_Unit;
                newAT.TargetType = TargetTypeEnum.unit;
                switch (go.Restrictions)
                {
                    case ECSATarget_Unit.UnitRestrictions.Any:
                        newAT.UnitTargetNested.UnitReq = UnitRequisitesEnum.any;
                        break;
                    case ECSATarget_Unit.UnitRestrictions.Enemy:
                        newAT.UnitTargetNested.UnitReq = UnitRequisitesEnum.enemy;
                        break;
                    case ECSATarget_Unit.UnitRestrictions.Friendly:
                        newAT.UnitTargetNested.UnitReq = UnitRequisitesEnum.friendly;
                        break;
                    case ECSATarget_Unit.UnitRestrictions.Self:
                        newAT.UnitTargetNested.UnitReq = UnitRequisitesEnum.self;
                        break;
                    case ECSATarget_Unit.UnitRestrictions.FriendlyOther:
                        newAT.UnitTargetNested.UnitReq = UnitRequisitesEnum.friendly_other;
                        break;
                    case ECSATarget_Unit.UnitRestrictions.Other:
                        newAT.UnitTargetNested.UnitReq = UnitRequisitesEnum.other;
                        break;
                }
                switch (inAction.Targets[i].HighlighterToUse)
                {
                    case ECSActionTarget.HighlightDef.Path:
                        newAT.Higlighter = UseHighlighterEnum.pathing;
                        break;
                    case ECSActionTarget.HighlightDef.Radius:
                        newAT.Higlighter = UseHighlighterEnum.no_pathing;
                        break;
                    case ECSActionTarget.HighlightDef.Path_Visible:
                        newAT.Higlighter = UseHighlighterEnum.pathing_visible;
                        break;
                    case ECSActionTarget.HighlightDef.Radius_Visible:
                        newAT.Higlighter = UseHighlighterEnum.no_pathing_visible;
                        break;
                }
                foreach (ECSActionSecondaryTargets t in go.SecondaryTargets)
                {
                    if (t is SecondaryArea)
                    {
                        SecondaryArea go1 = t as SecondaryArea;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.aoe;
                        mod.AoeNested.Radius = go1.areaSize;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryPath)
                    {
                        SecondaryPath go1 = t as SecondaryPath;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.path;
                        newAT.Mods.Add(mod);

                    }
                    if (t is SecondaryLine)
                    {
                        SecondaryLine go1 = t as SecondaryLine;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.line;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryRing)
                    {
                        SecondaryRing go1 = t as SecondaryRing;
                        TargetMod mod = new TargetMod();
                        mod.CoordinatePositionPairs = new List<CoordinatePositionPair>();
                        mod.ModType = ModTypeEnum.ring;
                        mod.RingNested.Radius = go1.radius;
                        newAT.Mods.Add(mod);
                    }
                }
            }

            newAction.Targets.Add(newAT);
        }
        for(int i = 0; i <= inAction.Effects.Count - 1; i++)
        {
            ActionEffect AF = new ActionEffect();
            if(inAction.Effects[i] is ECS_SpawnEffect)
            {
                ECS_SpawnEffect go = inAction.Effects[i] as ECS_SpawnEffect;
                AF.EffectType = EffectTypeEnum.spawn_unit;
                AF.SpawnUnitNested.UnitName = go.UnitNameToSpawn;
            }
            if (inAction.Effects[i] is ECS_MoveAlongPathEffect)
            {
                ECS_MoveAlongPathEffect go = inAction.Effects[i] as ECS_MoveAlongPathEffect;
                AF.EffectType = EffectTypeEnum.move_along_path;
                AF.MoveAlongPathNested.TimePerCell = go.TimePerCell;
            }
            if (inAction.Effects[i] is ECS_DealDamageEffect)
            {
                ECS_DealDamageEffect go = inAction.Effects[i] as ECS_DealDamageEffect;
                AF.EffectType = EffectTypeEnum.deal_damage;
                AF.DealDamageNested.DamageAmount = go.damageAmount;
            }
            if (inAction.Effects[i] is ECS_ArmorEffect)
            {
                ECS_ArmorEffect go = inAction.Effects[i] as ECS_ArmorEffect;
                AF.EffectType = EffectTypeEnum.gain_armor;
                AF.GainArmorNested.ArmorAmount = go.ArmorAmount;
            }
            AF.TurnDuration = inAction.Effects[i].TurnDuration;

            switch (inAction.Effects[i].ApplyToTargets)
            {
                case ECSActionEffect.ApplyTo.All:
                    AF.ApplyToTarget = ApplyToTargetsEnum.both;
                    break;
                case ECSActionEffect.ApplyTo.AllPrimary:
                    AF.ApplyToTarget = ApplyToTargetsEnum.primary;
                    break;
                case ECSActionEffect.ApplyTo.AllSecondary:
                    AF.ApplyToTarget = ApplyToTargetsEnum.secondary;
                    break;
            }
            AF.TargetSpecification = inAction.Effects[i].specificTargetIdentifier;
            switch (inAction.Effects[i].ApplyToRestrictions)
            {
                case ECSActionEffect.applyRestrictions.Any:
                    AF.ApplyToRestrictions = ApplyToRestrictionsEnum.any;
                    break;
                case ECSActionEffect.applyRestrictions.Enemy:
                    AF.ApplyToRestrictions = ApplyToRestrictionsEnum.enemy;
                    break;
                case ECSActionEffect.applyRestrictions.Friendly:
                    AF.ApplyToRestrictions = ApplyToRestrictionsEnum.friendly;
                    break;
                case ECSActionEffect.applyRestrictions.FriendlyOther:
                    AF.ApplyToRestrictions = ApplyToRestrictionsEnum.friendly_other;
                    break;
                case ECSActionEffect.applyRestrictions.Other:
                    AF.ApplyToRestrictions = ApplyToRestrictionsEnum.other;
                    break;
                case ECSActionEffect.applyRestrictions.Self:
                    AF.ApplyToRestrictions = ApplyToRestrictionsEnum.self;
                    break;
            }
            newAction.Effects.Add(AF);
        }
      return newAction;
    }

}
