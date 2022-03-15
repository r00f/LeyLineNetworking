
using System.Collections.Generic;
using Improbable;
//using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;
//using Improbable.Gdk.StandardTypes;
using Generic;
using Player;
using Cell;
using Unit;
using LeyLineHybridECS;
using Improbable.Gdk.PlayerLifecycle;
using UnityEngine;

public static class LeyLineEntityTemplates {

    private static readonly List<string> AllWorkerAttributes =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient, WorkerUtils.MapSpawn };

    private static readonly List<string> GameLogicAndClient =
    new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient};

    private static readonly List<string> GameLogicAndMapSpawn =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.MapSpawn };

    static Settings settings;

    public static EntityTemplate InitializeMapEventSender(Vector3f position)
    {
        var template = new EntityTemplate();
        template.AddComponent(new Position.Snapshot(new Coordinates {X = position.X, Y= position.Y, Z = position.Z }), WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "InitializeMapEventSender" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new InitMapEvent.Snapshot(), WorkerUtils.UnityGameLogic);
        template.SetReadAccess(GameLogicAndMapSpawn.ToArray());
        return template;
    }

    public static EntityTemplate GameState(Vector3f position, uint worldIndex, Vector2f mapCenter, Dictionary<Vector2i, MapCell> mapData, List<UnitSpawn> unitSpawns)
    {
        settings = Resources.Load<Settings>("Settings");

        var gameState = new GameState.Snapshot
        {
            CurrentState = GameStateEnum.waiting_for_players,
            CalculateWaitTime = .5f,
            CurrentWaitTime = .5f,
            RopeTime = settings.RopeTime,
            CurrentRopeTime = 30f,
            MapCenter = mapCenter,
            MinExecuteStepTime = settings.MinimumExecuteTime,
            InitMapWaitTime = 2f
        };

        var effectStack = new EffectStack.Snapshot
        {
            InterruptEffects = new List<ActionEffect>(),
            AttackEffects = new List<ActionEffect>(),
            MoveEffects = new List<ActionEffect>(),
            SkillshotEffects = new List<ActionEffect>()
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

        var oVisionClusters = new ObstructVisionClusters.Snapshot
        {
            RawClusters = new List<Vector3fList>()
        };

        var map = new MapData.Snapshot
        {
            CoordinateCellDictionary = mapData,
            UnitsSpawnList = unitSpawns
        };

        var template = new EntityTemplate();
        template.AddComponent(new ClientWorkerIds.Snapshot { ClientWorkerIds = new List<string>(), PlayerAttributes = new List<PlayerAttribute>() }, WorkerUtils.MapSpawn);
        template.AddComponent(oVisionClusters, WorkerUtils.MapSpawn);
        template.AddComponent(wIndex, WorkerUtils.MapSpawn);
        template.AddComponent(pos, WorkerUtils.MapSpawn);
        template.AddComponent(map, WorkerUtils.MapSpawn);

        template.AddComponent(new Metadata.Snapshot { EntityType = "GameState" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(gameState, WorkerUtils.UnityGameLogic);
        template.AddComponent(effectStack, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(GameLogicAndClient.ToArray());
        return template;
    }

    public static EntityTemplate Player(EntityId entityId, string workerId, byte[] serializedArguments)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(workerId);

        var energy = new PlayerEnergy.Snapshot
        {
            MaxEnergy = 80,
            Energy = 40,
            BaseIncome = 5,
            Income = 0,
            IncomeAdded = true
        };

        var playerAttributes = new PlayerAttributes.Snapshot
        {
            PlayerAttribute = new PlayerAttribute { StartingUnitNames = new List<string> { "KingCroak", "Leech", "Leech", "Axalotl" } }
        };

        var factionSnapshot = new FactionComponent.Snapshot();

        var playerVision = new Vision.Snapshot
        {
            RequireUpdate = false,
            CellsInVisionrange = new List<Vector2i>()
        };

        var playerPathing = new PlayerPathing.Snapshot
        {
            CellsInRange = new List<MapCell>(),
            CoordinatesInRange = new List<Vector2i>(),
            CachedMapPaths = new Dictionary<MapCell, MapCellList>()
        };

        var playerState = new PlayerState.Snapshot
        {
            UnitTargets = new Dictionary<long, CubeCoordinateList>(),
            EndStepReady = true,
            SelectedAction = new Action
            {
                Targets = new List<ActionTarget>(),
                Effects = new List<ActionEffect>(),
                Index = -3
            }
        };

        var wIndex = new WorldIndex.Snapshot();
        var pos = new Position.Snapshot { Coords = new Coordinates{X = -500, Y= 0, Z = -500 } };
        var template = new EntityTemplate();

        template.AddComponent(pos, WorkerUtils.MapSpawn);
        template.AddComponent(wIndex, WorkerUtils.MapSpawn);
        template.AddComponent(factionSnapshot, WorkerUtils.MapSpawn);
        template.AddComponent(playerAttributes, WorkerUtils.MapSpawn);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.MapSpawn);
        PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.MapSpawn);
        template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.MapSpawn);

        template.AddComponent(playerVision, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(new SimulatedPlayer.Snapshot(), client);
        template.AddComponent(playerState, client);
        template.AddComponent(playerPathing, client);

        template.SetReadAccess(GameLogicAndClient.ToArray());

        return template;
    }

    public static EntityTemplate Unit(string clientWorkerId, string unitName, Vector3f position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, uint startRotation)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(clientWorkerId);

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

        var factionSnapshot = new FactionComponent.Snapshot
        {
            Faction = faction
        };

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var health = new Health.Snapshot
        {
            MaxHealth = Stats.BaseHealth,
            CurrentHealth = Stats.BaseHealth
        };

        var energy = new Energy.Snapshot
        {
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation,
            VisionRange = Stats.VisionRange
        };

        var clientActionRequest = new ClientActionRequest.Snapshot
        {
            ActionId = -3,
            TargetCoordinate = new Vector3f(0, 0, 0)
        };

        var actions = SetActions(Stats);

        var incomingActionEffects = new IncomingActionEffects.Snapshot
        {
            InterruptEffects = new List<IncomingActionEffect>(),
            AttackEffects = new List<IncomingActionEffect>(),
            MoveEffects = new List<IncomingActionEffect>(),
            SkillshotEffects = new List<IncomingActionEffect>()
        };

        var template = new EntityTemplate();

        if(Stats.IsHero)
            template.AddComponent(new Hero.Snapshot(), WorkerUtils.UnityGameLogic);

        template.AddComponent(clientActionRequest, client);
        template.AddComponent(incomingActionEffects, WorkerUtils.UnityGameLogic);
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(health, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(actions, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(GameLogicAndClient.ToArray());
        return template;
    }

    public static EntityTemplate ManalithUnit(string unitName, Position.Component position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, uint startRotation, List<Vector3f> circleCellCoords, List<Vector3f> pathCellCoords, List<ManalithSlot> manalithSlots, Vector3f connectedManalithCoord)
    {
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
            Faction = faction
        };

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var energy = new Energy.Snapshot
        {
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation,
            VisionRange = Stats.VisionRange
        };

        var actions = SetActions(Stats);
        energy.Harvesting = true;

        var owningComponent = new OwningWorker.Snapshot();

        var manalith = new Manalith.Snapshot
        {
            CircleCoordinatesList = circleCellCoords,
            PathCoordinatesList = pathCellCoords,
            ConnectedManalithCoordinate = connectedManalithCoord,
            Manalithslots = manalithSlots
        };

        var template = new EntityTemplate();

        template.AddComponent(owningComponent, WorkerUtils.MapSpawn);
        template.AddComponent(manalith, WorkerUtils.MapSpawn);
        template.AddComponent(factionSnapshot, WorkerUtils.MapSpawn);
        template.AddComponent(pos, WorkerUtils.MapSpawn);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.MapSpawn);
        template.AddComponent(energy, WorkerUtils.MapSpawn);
        template.AddComponent(coord, WorkerUtils.MapSpawn);
        template.AddComponent(movementVariables, WorkerUtils.MapSpawn);
        template.AddComponent(wIndex, WorkerUtils.MapSpawn);
        template.AddComponent(turnTimer, WorkerUtils.MapSpawn);
        template.AddComponent(actions, WorkerUtils.MapSpawn);
        template.SetReadAccess(WorkerUtils.MapSpawn);
        return template;
    }

    public static EntityTemplate ReplicateManalithUnit(string unitName, Vector3f position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, uint startRotation, Manalith.Component m)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(Worlds.ClientWorkerId);

        var turnTimer = new TurnTimer.Snapshot
        {
            Timers = new List<Timer>()
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

        var coord = new CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };

        var factionSnapshot = new FactionComponent.Snapshot
        {
            Faction = faction
        };

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var energy = new Energy.Snapshot
        {
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation,
            VisionRange = Stats.VisionRange
        };

        var actions = SetActions(Stats);
        energy.Harvesting = true;

        var owningComponent = new OwningWorker.Snapshot();

        var manalith = new Manalith.Snapshot
        {
            CircleCoordinatesList = m.CircleCoordinatesList,
            PathCoordinatesList = m.PathCoordinatesList,
            ConnectedManalithCoordinate = m.ConnectedManalithCoordinate,
            Manalithslots = m.Manalithslots
        };

        var clientActionRequest = new ClientActionRequest.Snapshot
        {
            ActionId = -3,
            TargetCoordinate = new Vector3f(0, 0, 0)
        };


        var template = new EntityTemplate();

        template.AddComponent(clientActionRequest, WorkerUtils.UnityClient);
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);
        template.AddComponent(manalith, WorkerUtils.UnityGameLogic);
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(turnTimer, WorkerUtils.UnityGameLogic);
        template.AddComponent(actions, WorkerUtils.UnityGameLogic);
        template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(GameLogicAndClient.ToArray());
        return template;
    }

    public static EntityTemplate ObstructVisionClusters(Vector3f position, uint worldIndex, List<Vector3fList> rawClusters)
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

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var oVisionClusters = new ObstructVisionClusters.Snapshot
        {
            RawClusters = rawClusters
        };

        var template = new EntityTemplate();
        template.AddComponent(oVisionClusters, WorkerUtils.MapSpawn);
        template.AddComponent(wIndex, WorkerUtils.MapSpawn);
        template.AddComponent(pos, WorkerUtils.MapSpawn);
        template.AddComponent(new Metadata.Snapshot { EntityType = "ObstructVisionClusters" }, WorkerUtils.MapSpawn);
        return template;
    }

    public static EntityTemplate NeutralUnit(string workerId, string unitName, Vector3f position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, AIUnitDataSet aiUnitData, uint startRotation, bool isManalithUnit = false)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(workerId);

        var turnTimer = new TurnTimer.Snapshot
        {
            Timers = new List<Timer>()
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

        var coord = new CubeCoordinate.Snapshot
        {
            CubeCoordinate = cubeCoordinate
        };

        var factionSnapshot = new FactionComponent.Snapshot
        {
            Faction = faction
        };

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var energy = new Energy.Snapshot
        {
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation,
            VisionRange = Stats.VisionRange
        };

        var actions = SetActions(Stats);

        var incomingActionEffects = new IncomingActionEffects.Snapshot
        {
            InterruptEffects = new List<IncomingActionEffect>(),
            AttackEffects = new List<IncomingActionEffect>(),
            MoveEffects = new List<IncomingActionEffect>(),
            SkillshotEffects = new List<IncomingActionEffect>()
        };

        var owningComponent = new OwningWorker.Snapshot { WorkerId = workerId };

        var template = new EntityTemplate();

        var aiUnit = new AiUnit.Snapshot
        {
            ActionTypeWeightsList = new List<Vector3f>(),
            MoveActionsPrioList = new List<Vector2int>(),
            AttackActionsPrioList = new List<Vector2int>(),
            UtilityActionsPrioList = new List<Vector2int>(),
            CulledMoveActionsPrioList = new List<Vector2int>(),
            CulledAttackActionsPrioList = new List<Vector2int>(),
            CulledUtilityActionsPrioList = new List<Vector2int>()

        };

        var clientActionRequest = new ClientActionRequest.Snapshot
        {
            ActionId = -3,
            TargetCoordinate = new Vector3f(0, 0, 0)
        };

        foreach (Vector3 v3 in aiUnitData.ActionTypeWeightsList)
        {
            aiUnit.ActionTypeWeightsList.Add(new Vector3f(v3.x, v3.y, v3.z));
        }

        foreach (Vector2 v2 in aiUnitData.MoveActionPrioList)
        {
            aiUnit.MoveActionsPrioList.Add(new Vector2int((int) v2.x, (int) v2.y));
        }
        foreach (Vector2 v2 in aiUnitData.AttackActionPrioList)
        {
            aiUnit.AttackActionsPrioList.Add(new Vector2int((int) v2.x, (int) v2.y));
        }
        foreach (Vector2 v2 in aiUnitData.UtitlityActionPrioList)
        {
            aiUnit.UtilityActionsPrioList.Add(new Vector2int((int) v2.x, (int) v2.y));
        }

        var health = new Health.Snapshot
        {
            MaxHealth = Stats.BaseHealth,
            CurrentHealth = Stats.BaseHealth
        };

        template.AddComponent(clientActionRequest, WorkerUtils.UnityGameLogic);
        template.AddComponent(incomingActionEffects, WorkerUtils.UnityGameLogic);
        template.AddComponent(aiUnit, WorkerUtils.UnityGameLogic);
        template.AddComponent(health, WorkerUtils.UnityGameLogic);

        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);

        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);

        template.AddComponent(turnTimer, WorkerUtils.UnityGameLogic);
        template.AddComponent(actions, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(GameLogicAndClient.ToArray());
        return template;
    }

    static Actions.Snapshot SetActions (UnitDataSet inStats){

        Action myNullableAction = new Action
        {
            Index = -3,
            Targets = new List<ActionTarget>(),
            Effects = new List<ActionEffect>()
        };
        List<Action> myOtherActions = new List<Action>();

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
            ActionsList = myOtherActions,
            NullAction = myNullableAction,
            CurrentSelected = myNullableAction,
            LastSelected = myNullableAction,
            LockedAction = myNullableAction
        };

        return newActions;
        }

    static Action SetAction (ECSAction inAction, int index)
    {
        Action newAction = new Action
        {
            TimeToExecute = inAction.TimeToExecute,
            ActionExecuteStep = (ExecuteStepEnum) (int) inAction.ActionExecuteStep,
            Name = inAction.name,
            Index = index,
            Targets = new List<ActionTarget>(),
            Effects = new List<ActionEffect>(),
            CombinedCost = 0
        };

        for (int i = 0; i <= inAction.Targets.Count - 1; i++)
        {
            ActionTarget newAT = new ActionTarget
            {
                EnergyCost = inAction.Targets[i].energyCost,
                Targettingrange = inAction.Targets[i].targettingRange,
                Mods = new List<TargetMod>()
            };

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
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.aoe
                        };
                        mod.AoeNested.Radius = go1.areaSize;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryPath)
                    {
                        SecondaryPath go1 = t as SecondaryPath;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.path
                        };
                        mod.PathNested.Costpertile = go1.costPerTile;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryLine)
                    {
                        SecondaryLine go1 = t as SecondaryLine;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.line
                        };
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryRing)
                    {
                        SecondaryRing go1 = t as SecondaryRing;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.ring
                        };
                        mod.RingNested.Radius = go1.radius;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryCone)
                    {
                        SecondaryCone go1 = t as SecondaryCone;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.cone
                        };
                        mod.ConeNested.Radius = go1.radius;
                        mod.ConeNested.Extent = go1.extent;
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
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.aoe
                        };
                        mod.AoeNested.Radius = go1.areaSize;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryPath)
                    {
                        SecondaryPath go1 = t as SecondaryPath;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.path
                        };
                        newAT.Mods.Add(mod);

                    }
                    if (t is SecondaryLine)
                    {
                        SecondaryLine go1 = t as SecondaryLine;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.line
                        };
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryRing)
                    {
                        SecondaryRing go1 = t as SecondaryRing;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.ring
                        };
                        mod.RingNested.Radius = go1.radius;
                        newAT.Mods.Add(mod);
                    }
                    if (t is SecondaryCone)
                    {
                        SecondaryCone go1 = t as SecondaryCone;
                        TargetMod mod = new TargetMod
                        {
                            CoordinatePositionPairs = new List<CoordinatePositionPair>(),
                            ModType = ModTypeEnum.cone
                        };
                        mod.ConeNested.Radius = go1.radius;
                        mod.ConeNested.Extent = go1.extent;
                        newAT.Mods.Add(mod);
                    }
                }
            }

            newAction.Targets.Add(newAT);
        }
        for(int i = 0; i <= inAction.Effects.Count - 1; i++)
        {
            ActionEffect AF = new ActionEffect
            {
                TargetCoordinates = new List<Vector3f>(),
                TurnDuration = inAction.Effects[i].TurnDuration,
                UnitDuration = inAction.Effects[i].UnitDuration
            };
            AF.MoveAlongPathNested.CoordinatePositionPairs = new List<CoordinatePositionPair>();

            if (inAction.Effects[i] is ECS_SpawnEffect)
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
                AF.DealDamageNested.UpForce = go.UpForce;
                AF.DealDamageNested.ExplosionForce = go.ExplosionForce;
                AF.DealDamageNested.ExplosionRadius = go.ExplosionRadius;
                AF.DealDamageNested.DamageAmount = go.DamageAmount;
            }
            if (inAction.Effects[i] is ECS_ArmorEffect)
            {
                ECS_ArmorEffect go = inAction.Effects[i] as ECS_ArmorEffect;
                AF.EffectType = EffectTypeEnum.gain_armor;
                AF.GainArmorNested.ArmorAmount = go.ArmorAmount;
            }

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
