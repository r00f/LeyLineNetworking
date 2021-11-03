using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Core;
using Generic;
using Unit;
using Cell;
using Improbable;
using Player;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class AISystem : JobComponentSystem
{
    EntityQuery m_AiUnitData;
    EntityQuery m_PlayerUnitData;
    CommandSystem m_CommandSystem;
    PathFindingSystem m_PathFindingSystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_AiUnitData = GetEntityQuery(
            ComponentType.ReadWrite<AiUnit.Component>(),
            ComponentType.ReadWrite<Actions.Component>(),
            ComponentType.ReadOnly<Vision.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<CellsToMark.Component>()
        );

        var PlayerUnitDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(Manalith.Component),
                typeof(AiUnit.Component)
            },
            All = new ComponentType[]
            {
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>()
            }
        };

        m_PlayerUnitData = GetEntityQuery(PlayerUnitDesc);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_CommandSystem = World.GetExistingSystem<CommandSystem>();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

        if (cleanUpStateEvents.Count > 0)
        {
            UpdateAIUnits();
        }

        return inputDeps;
    }

    public void UpdateAIUnits()
    {
        Entities.ForEach((Entity e, ref AiUnit.Component aiUnit, ref Vision.Component vision, ref Actions.Component actions, ref SpatialEntityId AIid, ref CellsToMark.Component unitCellsToMark, ref CubeCoordinate.Component unitCoord, in WorldIndexShared unitWorldIndex) =>
        {
            SetAggroedUnit(vision, ref aiUnit, actions, AIid, unitCoord);

            aiUnit.CulledMoveActionsPrioList = CullPrioList(aiUnit.CulledMoveActionsPrioList, aiUnit.MoveActionsPrioList, actions, unitCoord.CubeCoordinate, aiUnit.AggroedUnitCoordinate, 1);
            aiUnit.CulledAttackActionsPrioList = CullPrioList(aiUnit.CulledAttackActionsPrioList, aiUnit.AttackActionsPrioList, actions, unitCoord.CubeCoordinate, aiUnit.AggroedUnitCoordinate);
            aiUnit.CulledUtilityActionsPrioList = CullPrioList(aiUnit.CulledUtilityActionsPrioList, aiUnit.UtilityActionsPrioList, actions, unitCoord.CubeCoordinate, unitCoord.CubeCoordinate);

            SetAIUnitState(ref aiUnit);

            if(aiUnit.CurrentState != AiUnitStateEnum.idle && aiUnit.CurrentState != AiUnitStateEnum.returning)
            {
                SetChosenActionType(ref aiUnit);
                ChooseAIAction(ref aiUnit, ref actions, AIid, unitCellsToMark, unitCoord, unitWorldIndex);
            }
        })
        .WithoutBurst()
        .Run();
    }

    public List<Vector2int> CullPrioList(List<Vector2int> culledActionPrioList, List<Vector2int> actionPrioList, Actions.Component actions, Vector3f unitCoord, Vector3f targetCoord, int inExtraRange = 0)
    {
        culledActionPrioList.Clear();

        foreach (Vector2int v2 in actionPrioList)
        {
            var extrarange = 0;
            if (actions.ActionsList[v2.X].Targets[0].Mods.Count != 0 && actions.ActionsList[v2.X].Targets[0].Mods[0].ModType == ModTypeEnum.aoe)
            {
                extrarange = actions.ActionsList[v2.X].Targets[0].Mods[0].AoeNested.Radius;
            }

            extrarange += inExtraRange;

            //Check if aggroed unit is in range of this attack
            if (CellGridMethods.GetDistance(unitCoord, targetCoord) <= actions.ActionsList[v2.X].Targets[0].Targettingrange + extrarange)
            {
                culledActionPrioList.Add(v2);
            }
        }

        return culledActionPrioList;

    }

    public void SetAggroedUnit(Vision.Component vision, ref AiUnit.Component aiUnit, Actions.Component actions, SpatialEntityId AIid, CubeCoordinate.Component unitCoord)
    {
        var extrarange = 0;

        //check if last action in actionsList (action with the highest range) is an AoE
        if (actions.ActionsList[actions.ActionsList.Count - 1].Targets[0].Mods.Count != 0 && actions.ActionsList[actions.ActionsList.Count - 1].Targets[0].Mods[0].ModType == ModTypeEnum.aoe)
        {
            extrarange = actions.ActionsList[actions.ActionsList.Count - 1].Targets[0].Mods[0].AoeNested.Radius;
        }

        //check if last action in actionsList (action with the highest range) can reach Current aggroed unit and randomize aggroed unit if not
        if (aiUnit.CurrentState == AiUnitStateEnum.idle || CellGridMethods.GetDistance(unitCoord.CubeCoordinate, aiUnit.AggroedUnitCoordinate) > actions.ActionsList[actions.ActionsList.Count - 1].Targets[0].Targettingrange + extrarange)
        {
            RandomizeUnitAggression(vision, ref aiUnit, actions, AIid);
        }
        else
        {
            RefreshUnitAggression(vision, ref aiUnit, actions, AIid);
        }
    }

    public void RandomizeUnitAggression(Vision.Component aiVision, ref AiUnit.Component aiUnit, Actions.Component act, SpatialEntityId aiId)
    {
        aiUnit.AggroedUnitCoordinate = new Vector3f(999, 999, 999);
        aiUnit.AggroedUnitId = 0;
        aiUnit.AnyUnitInVisionRange = false;
        aiUnit.AggroedUnitFaction = 0;

        var aiU = aiUnit;

        Entities.WithNone<AiUnit.Component>().WithAll<Health.Component>().ForEach((in CubeCoordinate.Component coord, in SpatialEntityId id, in FactionComponent.Component faction) =>
        {
            if (aiVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
            {
                //faction.Faction
                aiU.AggroedUnitFaction = faction.Faction;
                aiU.AggroedUnitCoordinate = coord.CubeCoordinate;
                aiU.AggroedUnitId = id.EntityId.Id;
                aiU.AnyUnitInVisionRange = true;
            }
        })
        .WithoutBurst()
        .Run();

        aiUnit = aiU;
    }

    public void RefreshUnitAggression(Vision.Component aiVision, ref AiUnit.Component aiUnit, Actions.Component act, SpatialEntityId aiId)
    {
        bool aggroedUnitExists = false;
        var aiU = aiUnit;

        Entities.WithNone<AiUnit.Component>().WithAll<Health.Component>().ForEach((in CubeCoordinate.Component coord, in SpatialEntityId id) =>
        {
            if (aiU.AggroedUnitId == id.EntityId.Id && aiVision.CellsInVisionrange.ContainsKey(coord.CubeCoordinate))
            {
                aggroedUnitExists = true;
                aiU.AggroedUnitCoordinate = coord.CubeCoordinate;
            }
        })
        .WithoutBurst()
        .Run();

        aiUnit = aiU;

        if (aggroedUnitExists)
            return;
        else
            RandomizeUnitAggression(aiVision, ref aiUnit, act, aiId);
    }

    public void SetAIUnitState(ref AiUnit.Component aiUnit)
    {
        switch (aiUnit.CurrentState)
        {
            case AiUnitStateEnum.idle:
                if (aiUnit.AnyUnitInVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.aggroed;
                break;
            case AiUnitStateEnum.aggroed:
                if (aiUnit.CulledAttackActionsPrioList.Count != 0)
                    aiUnit.CurrentState = AiUnitStateEnum.inAttackRange;
                else if (aiUnit.AnyUnitInVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.chasing;
                else
                    aiUnit.CurrentState = AiUnitStateEnum.idle;
                break;
            case AiUnitStateEnum.chasing:
                if (aiUnit.CulledAttackActionsPrioList.Count != 0)
                    aiUnit.CurrentState = AiUnitStateEnum.inAttackRange;
                else if (!aiUnit.AnyUnitInVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.idle;
                break;
            case AiUnitStateEnum.inAttackRange:
                if (aiUnit.AnyUnitInVisionRange)
                    aiUnit.CurrentState = AiUnitStateEnum.chasing;
                else
                    aiUnit.CurrentState = AiUnitStateEnum.idle;
                break;
            case AiUnitStateEnum.returning:
                break;
        }

    }

    public void SetChosenActionType(ref AiUnit.Component aiUnit)
    {
        //Check if unit is in InAttackRange and discard attack action type if not
        Vector3 ActionTypeProbabilities = Vector3fext.ToUnityVector(aiUnit.ActionTypeWeightsList[(int)aiUnit.CurrentState]);

        //int numberOfOptions = 0;
        bool[] considerTypes = new bool[3];
        int numberofOptions = 0;

        if (aiUnit.CulledMoveActionsPrioList.Count > 0)
        {
            considerTypes[0] = true;
            numberofOptions++;
        }

        if (aiUnit.CulledAttackActionsPrioList.Count > 0)
        {
            considerTypes[1] = true;
            numberofOptions++;
        }

        if (aiUnit.CulledUtilityActionsPrioList.Count > 0)
        {
            considerTypes[2] = true;
            numberofOptions++;
        }

        if(numberofOptions == 0)
        {
            return;
        }
        else if(numberofOptions == 1)
        {
            if(considerTypes[0])
                aiUnit.ChosenActionType = ActionTypeEnum.move;
            else if(considerTypes[1])
                aiUnit.ChosenActionType = ActionTypeEnum.attack;
            else if (considerTypes[2])
                aiUnit.ChosenActionType = ActionTypeEnum.utility;
        }
        else if (numberofOptions == 2)
        {
            var randomMax = 0;

            if (considerTypes[0])
                randomMax += (int) ActionTypeProbabilities.x;
            if (considerTypes[1])
                randomMax += (int) ActionTypeProbabilities.y;
            if (considerTypes[2])
                randomMax += (int) ActionTypeProbabilities.z;

            var r = Random.Range(0, randomMax);

            if(considerTypes[0] && considerTypes[1])
            {
                //Debug.Log("ConsiderMoveAttack");
                //if the roll is 0-x / x-y 
                if (r >= 0 && r <= ActionTypeProbabilities.x)
                {
                    aiUnit.ChosenActionType = ActionTypeEnum.move;
                }
                else if (r >= ActionTypeProbabilities.x && r <= randomMax)
                {
                    aiUnit.ChosenActionType = ActionTypeEnum.attack;
                }
            }
            else if(considerTypes[0] && considerTypes[2])
            {
                //Debug.Log("ConsiderMoveUtility");
                //if the roll is 0-x / x-y 
                if (r >= 0 && r <= ActionTypeProbabilities.x)
                {
                    aiUnit.ChosenActionType = ActionTypeEnum.move;
                }
                else if (r >= ActionTypeProbabilities.x && r <= randomMax)
                {
                    aiUnit.ChosenActionType = ActionTypeEnum.utility;
                }
            }
            else if(considerTypes[1] && considerTypes[2])
            {
                //Debug.Log("ConsiderAttackUtility");
                //if the roll is 0-x / x-y 
                if (r >= 0 && r <= ActionTypeProbabilities.y)
                {
                    aiUnit.ChosenActionType = ActionTypeEnum.attack;
                }
                else if (r >= ActionTypeProbabilities.y && r <= randomMax)
                {
                    aiUnit.ChosenActionType = ActionTypeEnum.utility;
                }
            }
        }
        else if (numberofOptions == 3)
        {
            var r = Random.Range(1, ActionTypeProbabilities.magnitude);

            //if the roll is 0-x / x-y / y-z
            if (r >= 0 && r <= ActionTypeProbabilities.x)
            {
                aiUnit.ChosenActionType = ActionTypeEnum.move;
            }
            else if (r >= ActionTypeProbabilities.x && r <= ActionTypeProbabilities.x + ActionTypeProbabilities.y)
            {
                aiUnit.ChosenActionType = ActionTypeEnum.attack;
            }
            else if (r >= ActionTypeProbabilities.x + ActionTypeProbabilities.y && r <= ActionTypeProbabilities.magnitude)
            {
                aiUnit.ChosenActionType = ActionTypeEnum.utility;
            }
        }
    }

    public void ChooseAIAction(ref AiUnit.Component aiUnit, ref Actions.Component actions, SpatialEntityId AIid, CellsToMark.Component unitCellsToMark, CubeCoordinate.Component unitCoord, WorldIndexShared unitWorldIndex)
    {
        unitCellsToMark.CellsInRange.Clear();
        unitCellsToMark.CachedPaths.Clear();

        switch(aiUnit.ChosenActionType)
        {
            case ActionTypeEnum.move:
                //Debug.Log("SetMoveAction");
                actions.CurrentSelected = SelectActionFromPrioGroup(aiUnit.CulledMoveActionsPrioList, actions);
                unitCellsToMark.CellsInRange = m_PathFindingSystem.GetRadius(unitCoord.CubeCoordinate, (uint) actions.CurrentSelected.Targets[0].Targettingrange, unitWorldIndex);
                unitCellsToMark.CachedPaths = m_PathFindingSystem.GetAllPathsInRadius((uint) actions.ActionsList[0].Targets[0].Targettingrange, unitCellsToMark.CellsInRange, unitCellsToMark.CellsInRange[0].Cell);
                var request = new Actions.SetTargetCommand.Request
                (
                    AIid.EntityId,
                    new SetTargetRequest(ClosestPathTarget(aiUnit.AggroedUnitCoordinate, unitCellsToMark.CachedPaths))
                );
                m_CommandSystem.SendCommand(request);
                break;

            case ActionTypeEnum.attack:
                //Debug.Log("SetAttackAction");

                actions.CurrentSelected = SelectActionFromPrioGroup(aiUnit.CulledAttackActionsPrioList, actions);

                var extrarange = 0;

                if (actions.CurrentSelected.Targets[0].Mods.Count != 0 && actions.CurrentSelected.Targets[0].Mods[0].ModType == ModTypeEnum.aoe)
                {
                    extrarange = actions.CurrentSelected.Targets[0].Mods[0].AoeNested.Radius;
                }

                if (extrarange != 0)
                {
                    for (uint j = 0; j < 6; j++)
                    {
                        if (CellGridMethods.GetDistance(CellGridMethods.CubeNeighbour(aiUnit.AggroedUnitCoordinate, j), unitCoord.CubeCoordinate) <= actions.CurrentSelected.Targets[0].Targettingrange)
                        {
                            var attackRequest = new Actions.SetTargetCommand.Request
                            (
                                AIid.EntityId,
                                new SetTargetRequest(CellGridMethods.CubeNeighbour(aiUnit.AggroedUnitCoordinate, j))
                            );
                            m_CommandSystem.SendCommand(attackRequest);
                        }
                    }
                }
                else
                {
                    var attackRequest = new Actions.SetTargetCommand.Request
                    (
                        AIid.EntityId,
                        new SetTargetRequest(aiUnit.AggroedUnitCoordinate)
                    );
                    m_CommandSystem.SendCommand(attackRequest);
                }

                break;

            case ActionTypeEnum.utility:

                //Debug.Log("SetUtilityAction");
                actions.CurrentSelected = SelectActionFromPrioGroup(aiUnit.CulledUtilityActionsPrioList, actions);
                var utilityRequest = new Actions.SetTargetCommand.Request
                (
                    AIid.EntityId,
                    new SetTargetRequest(unitCoord.CubeCoordinate)
                );
                m_CommandSystem.SendCommand(utilityRequest);
                break;
        }
    }

    public Action SelectActionFromPrioGroup(List<Vector2int> culledPrioList, Actions.Component actions)
    {
        var action = new Action();
        var randomMax = 0;

        foreach (Vector2int v2 in culledPrioList)
        {
            randomMax += v2.Y;
        }

        var r = Random.Range(0, randomMax);

        var lastProbability = 0;

        for(int i = 0; i < culledPrioList.Count; i++)
        {
            var currentProbability = culledPrioList[i].Y;

            if (r >= lastProbability && r <= currentProbability + lastProbability)
            {
                return actions.ActionsList[culledPrioList[i].X];
            }

            lastProbability += currentProbability;
        }

        return action;
    }

    public Vector3f ClosestPathTarget(Vector3f targetCoord, Dictionary<CellAttribute, CellAttributeList> inDict)
    {
        return inDict.Keys.OrderBy(i => CellGridMethods.GetDistance(targetCoord, i.CubeCoordinate)).ToList()[0].CubeCoordinate;
    }

}
