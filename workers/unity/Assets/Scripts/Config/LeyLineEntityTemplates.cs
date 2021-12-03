
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
using static LeyLineHybridECS.ECSAction;
using Improbable.Gdk.PlayerLifecycle;
using UnityEngine;

public static class LeyLineEntityTemplates {

    private static readonly List<string> AllWorkerAttributes =
        new List<string> { WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient };

    static Settings settings;

    public static EntityTemplate GameState(Vector3f position, uint worldIndex, Vector2f mapCenter)
    {
        settings = Resources.Load<Settings>("Settings");

        var gameState = new GameState.Snapshot
        {
            CurrentState = GameStateEnum.waiting_for_players,
            PlayersOnMapCount = 0,
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

        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "GameState" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
        template.AddComponent(gameState, WorkerUtils.UnityGameLogic);
        template.AddComponent(effectStack, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate Cell(Vector3f cubeCoordinate, Vector3f position, bool isTaken, bool isCircleCell, string unitName, bool isSpawn, bool isManalithUnitSpawn, uint faction, CellAttributeList neighbours, uint worldIndex, bool inObstruction, int mapColorIndex, uint startingUnitIndex, uint startRotation = 0)
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
                    ObstructVision = inObstruction
                },
                Neighbours = neighbours
            }

        };

        var unitToSpawn = new UnitToSpawn.Snapshot();

        if (faction == 0)
        {
            unitToSpawn = new UnitToSpawn.Snapshot
            {
                UnitName = unitName,
                Faction = 0,
                StartRotation = startRotation,
                ManalithUnit = isManalithUnitSpawn
            };
        }
        else
        {
            unitToSpawn = new UnitToSpawn.Snapshot
            {
                UnitName = unitName,
                Faction = faction,
                StartRotation = startRotation,
                ManalithUnit = isManalithUnitSpawn,
                StartingUnitIndex = startingUnitIndex
               
            };
        }

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
        if (isTaken)
            template.AddComponent(new StaticTaken.Snapshot(), gameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());

        return template;
    }

    public static EntityTemplate Player(EntityId entityId, string workerId, byte[] serializedArguments)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(workerId);

        var energy = new PlayerEnergy.Snapshot
        {
            MaxEnergy = 80,
            Energy = 40,
            BaseIncome = 0,
            Income = 0,
            IncomeAdded = true
        };

        var playerAttributes = new PlayerAttributes.Snapshot
        {
            HeroName = "KingCroak",
            StartingUnitNames = new List<string> {"Leech", "Leech", "Axalotl"}
        };

        var factionSnapshot = new FactionComponent.Snapshot();

        var playerVision = new Vision.Snapshot
        {
            RequireUpdate = false,
            CellsInVisionrange = new Dictionary<Vector3f, uint>(),
            Lastvisible = new List<Vector3f>(),
            Positives = new List<Vector3f>(),
            Negatives = new List<Vector3f>()
        };

        var playerPathing = new PlayerPathing.Snapshot
        {
            CellsInRange = new List<CellAttribute>(),
            CachedPaths = new Dictionary<CellAttribute, CellAttributeList>(),
            CoordinatesInRange = new List<Vector3f>()
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
        var pos = new Position.Snapshot { Coords = new Coordinates{X = -200, Y= 0, Z = 0 } };
        var template = new EntityTemplate();
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.UnityGameLogic);
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(new SimulatedPlayer.Snapshot(), client);
        template.AddComponent(playerState, client);
        template.AddComponent(playerPathing, client);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(playerAttributes, WorkerUtils.UnityGameLogic);
        template.AddComponent(playerVision, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);
        PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.UnityGameLogic);
        return template;
    }

    public static EntityTemplate Unit(/*string workerId,*/ string unitName, Position.Component position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, uint startRotation)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(WorkerUtils.UnityClient);
        /*
        var turnTimer = new TurnTimer.Snapshot
        {
            Timers = new List<Timer>()
        };
        */

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

        var cellsToMarkSnapshot = new CellsToMark.Snapshot
        {
            CellsInRange = new List<CellAttributes>(),
            CachedPaths = new Dictionary<CellAttribute, CellAttributeList>()
        };

        /*
        var clientPathSnapshot = new ClientPath.Snapshot
        {
            Path = new CellAttributeList(new List<CellAttribute>()),
        };
        */

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var unitVision = new Vision.Snapshot
        {
            CellsInVisionrange = new Dictionary<Vector3f, uint>(),
            Lastvisible = new List<Vector3f>(),
            Positives = new List<Vector3f>(),
            Negatives = new List<Vector3f>(),
            RequireUpdate = false,
            InitialWaitTime = .1f,
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


        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation
        };

        var actions = SetActions(Stats);

        var incomingActionEffects = new IncomingActionEffects.Snapshot
        {
            InterruptEffects = new List<IncomingActionEffect>(),
            AttackEffects = new List<IncomingActionEffect>(),
            MoveEffects = new List<IncomingActionEffect>(),
            SkillshotEffects = new List<IncomingActionEffect>()
        };

        //var clientHeartbeat = new PlayerHeartbeatClient.Snapshot();
        //var serverHeartbeat = new PlayerHeartbeatServer.Snapshot();
        //var owningComponent = new OwningWorker.Snapshot {WorkerId = workerId};

        var template = new EntityTemplate();

        if(Stats.IsHero)
            template.AddComponent(new Hero.Snapshot(), WorkerUtils.UnityGameLogic);

        template.AddComponent(incomingActionEffects, WorkerUtils.UnityGameLogic);
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        //template.AddComponent(clientHeartbeat, client);
       // template.AddComponent(serverHeartbeat, WorkerUtils.UnityGameLogic);
        //template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);
        template.AddComponent(health, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(cellsToMarkSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        //template.AddComponent(clientPathSnapshot, client);
        template.AddComponent(unitVision, WorkerUtils.UnityGameLogic);
        //template.AddComponent(turnTimer, WorkerUtils.UnityGameLogic);
        template.AddComponent(actions, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate ManalithUnit(string unitName, Position.Component position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, AIUnitDataSet aiUnitData, uint startRotation, CellAttributeList circleCells, List<Vector3f> pathCellCoords, Vector3f connectedManalithCoord)
    {
        //var workerId = 
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

        var cellsToMarkSnapshot = new CellsToMark.Snapshot
        {
            CellsInRange = new List<CellAttributes>(),
            CachedPaths = new Dictionary<CellAttribute, CellAttributeList>()
        };

        var wIndex = new WorldIndex.Snapshot
        {
            Value = worldIndex
        };

        var unitVision = new Vision.Snapshot
        {
            CellsInVisionrange = new Dictionary<Vector3f, uint>(),
            Lastvisible = new List<Vector3f>(),
            Positives = new List<Vector3f>(),
            Negatives = new List<Vector3f>(),
            RequireUpdate = true,
            InitialWaitTime = .1f,
            VisionRange = Stats.VisionRange
        };

        var energy = new Energy.Snapshot
        {
            //SpawnCost = Stats.SpawnCost,
            //EnergyUpkeep = Stats.EnergyUpkeep,
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation
        };

        var actions = SetActions(Stats);
        energy.Harvesting = true;

        List<ManalithSlot> slots = new List<ManalithSlot>();

        foreach (CellAttribute n in circleCells.CellAttributes)
        {
            if (n.IsTaken != true)
            {
                ManalithSlot go = new ManalithSlot
                {
                    CorrespondingCell = n
                };
                slots.Add(go);
            }
        }

        var owningComponent = new OwningWorker.Snapshot();

        var circleCellCoords = new List<Vector3f>();

        foreach (CellAttribute c in circleCells.CellAttributes)
        {
            circleCellCoords.Add(c.CubeCoordinate);
        }

        var manalith = new Manalith.Snapshot
        {
            CircleCoordinatesList = circleCellCoords,
            PathCoordinatesList = pathCellCoords,
            ConnectedManalithCoordinate = connectedManalithCoord,
            Manalithslots = slots
        };


        var template = new EntityTemplate();
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);
        template.AddComponent(manalith, WorkerUtils.UnityGameLogic);
        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(energy, WorkerUtils.UnityGameLogic);
        template.AddComponent(cellsToMarkSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(coord, WorkerUtils.UnityGameLogic);
        template.AddComponent(movementVariables, WorkerUtils.UnityGameLogic);
        template.AddComponent(wIndex, WorkerUtils.UnityGameLogic);
        template.AddComponent(unitVision, WorkerUtils.UnityGameLogic);
        template.AddComponent(turnTimer, WorkerUtils.UnityGameLogic);
        template.AddComponent(actions, WorkerUtils.UnityGameLogic);
        template.SetReadAccess(AllWorkerAttributes.ToArray());
        return template;
    }

    public static EntityTemplate NeutralUnit(string workerId, string unitName, Position.Component position, Vector3f cubeCoordinate, uint faction, uint worldIndex, UnitDataSet Stats, AIUnitDataSet aiUnitData, uint startRotation, bool isManalithUnit = false)
    {
        var client = EntityTemplate.GetWorkerAccessAttribute(workerId);

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
            CellsInVisionrange = new Dictionary<Vector3f, uint>(),
            Lastvisible = new List<Vector3f>(),
            Positives = new List<Vector3f>(),
            Negatives = new List<Vector3f>(),
            RequireUpdate = true,
            InitialWaitTime = .1f,
            VisionRange = Stats.VisionRange
        };

        var energy = new Energy.Snapshot
        {
            //SpawnCost = Stats.SpawnCost,
            //EnergyUpkeep = Stats.EnergyUpkeep,
            EnergyIncome = Stats.EnergyIncome
        };

        var movementVariables = new StartRotation.Snapshot
        {
            Value = startRotation
        };

        var actions = SetActions(Stats);

        var incomingActionEffects = new IncomingActionEffects.Snapshot
        {
            InterruptEffects = new List<IncomingActionEffect>(),
            AttackEffects = new List<IncomingActionEffect>(),
            MoveEffects = new List<IncomingActionEffect>(),
            SkillshotEffects = new List<IncomingActionEffect>()
        };

        //var clientHeartbeat = new PlayerHeartbeatClient.Snapshot();
        //var serverHeartbeat = new PlayerHeartbeatServer.Snapshot();
        var owningComponent = new OwningWorker.Snapshot { WorkerId = workerId };

        var template = new EntityTemplate();

        if (Stats.IsHero)
            template.AddComponent(new Hero.Snapshot(), WorkerUtils.UnityGameLogic);

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

        template.AddComponent(incomingActionEffects, WorkerUtils.UnityGameLogic);
        template.AddComponent(aiUnit, WorkerUtils.UnityGameLogic);
        template.AddComponent(health, WorkerUtils.UnityGameLogic);

        template.AddComponent(factionSnapshot, WorkerUtils.UnityGameLogic);
        template.AddComponent(pos, WorkerUtils.UnityGameLogic);
        template.AddComponent(new Metadata.Snapshot { EntityType = unitName }, WorkerUtils.UnityGameLogic);
        template.AddComponent(owningComponent, WorkerUtils.UnityGameLogic);

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
