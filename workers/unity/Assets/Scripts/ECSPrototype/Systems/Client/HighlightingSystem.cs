using Cell;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using Unit;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class HighlightingSystem : ComponentSystem
{

    private EndSimulationEntityCommandBufferSystem entityCommandBufferSystem;

    Settings settings;
    PathFindingSystem m_PathFindingSystem;
    EntityQuery m_ActiveUnitData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    EntityQuery m_MarkerStateData;
    EntityQuery m_PlayerStateData;
    EntityQuery m_GameStateData;
    EntityQuery m_HoveredData;

    ILogDispatcher m_LogDispatcher;

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        settings = Resources.Load<Settings>("Settings");


        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<MarkerState>()
            );

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<GameState.Component>()
            );

        m_ActiveUnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Unit_BaseDataSet>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadWrite<CellsToMark.Component>(),
            ComponentType.ReadWrite<ClientPath.Component>(),
            ComponentType.ReadWrite<LineRendererComponent>()
            );

        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<LineRendererComponent>(),
            ComponentType.ReadOnly<Transform>()
            );

        m_MarkerStateData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<MarkerState>()
            );

        m_PlayerStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<PlayerState.Component>(),
            ComponentType.ReadWrite<PlayerPathing.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<PlayerEnergy.Component>(),
            ComponentType.ReadWrite<HighlightingDataComponent>()
            );


        m_HoveredData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<HoveredState>()
            );

    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_LogDispatcher = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
    }

    protected override void OnUpdate()
    {
        if (m_PlayerStateData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() == 0)
            return;

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        var gameStateData = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
        var gameState = gameStateData[0];

        #region playerStateData
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerPathings = m_PlayerStateData.ToComponentDataArray<PlayerPathing.Component>(Allocator.TempJob);
        var playerWorldIndexes = m_PlayerStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerStateData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
        var playerFactions = m_PlayerStateData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

        var playerPathing = playerPathings[0];
        var playerState = playerStates[0];
        var playerWorldIndex = playerWorldIndexes[0].Value;
        var playerHighlightingData = playerHighlightingDatas[0];
        var playerFaction = playerFactions[0];
        #endregion

        var activeUnitLineRenderers = m_ActiveUnitData.ToComponentArray<LineRendererComponent>();

        if (gameState.CurrentState == GameStateEnum.planning)
        {
            if (Vector3fext.ToUnityVector(playerHighlightingData.HoveredCoordinate) != Vector3fext.ToUnityVector(playerHighlightingData.LastHoveredCoordinate)) //&& playerHighlightingData.InputCooldown <= 0)
            {
                if (playerState.CurrentState == PlayerStateEnum.waiting_for_target && playerHighlightingData.TargetRestrictionIndex != 2)
                {
                    //Debug.Log("HighlightingSys PlayerHighAoE: " + playerHighlightingData.AoERadius);

                    HashSet<Vector3f> currentActionTargetHash = new HashSet<Vector3f>();

                    if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
                        currentActionTargetHash = new HashSet<Vector3f>(playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates);
                    currentActionTargetHash.Add(playerHighlightingData.LastHoveredCoordinate);

                    playerState = FillUnitTargetsList(playerState.SelectedAction, playerHighlightingData, playerState, playerPathing, playerFaction.Faction);
                    playerPathing = UpdateSelectedUnit(playerState, playerPathing, playerHighlightingData);
                    SetNumberOfTargets(playerState, currentActionTargetHash);
                    playerStates[0] = playerState;
                    playerPathings[0] = playerPathing;
                    m_PlayerStateData.CopyFromComponentDataArray(playerPathings);
                    m_PlayerStateData.CopyFromComponentDataArray(playerStates);
                }
                else
                {
                    if (playerHighlightingData.SelectActionBuffer > 0)
                    {
                        playerHighlightingData.SelectActionBuffer--;
                    }
                    else
                        ResetHighlights(ref playerState, playerHighlightingData);
                }

                playerHighlightingData.LastHoveredCoordinate = playerHighlightingData.HoveredCoordinate;

            }
        }
        else
        {

            Entities.With(m_MarkerStateData).ForEach((Entity e, ref MarkerState markerState) =>
            {
                if (markerState.NumberOfTargets > 0 || markerState.CurrentState == MarkerState.State.Hovered)
                {
                    markerState.NumberOfTargets = 0;
                    commandBuffer.AddComponent(e, new RequireMarkerUpdate());
                }
            });

            Entities.With(m_ActiveUnitData).ForEach((LineRendererComponent lineRenderer) =>
            {
                lineRenderer.lineRenderer.positionCount = 0;
            });
        }

        playerHighlightingDatas[0] = playerHighlightingData;
        m_PlayerStateData.CopyFromComponentDataArray(playerHighlightingDatas);


     

        //Debug.Log(m_HoveredData.CalculateEntityCount());
        // When clicked hovered marker isnt removed <= Bug! Wrong Check in MouseState.

        gameStateData.Dispose();
        #region playerStateData
        playerPathings.Dispose();
        playerStates.Dispose();
        playerWorldIndexes.Dispose();
        playerHighlightingDatas.Dispose();
        playerFactions.Dispose();
        #endregion

    }

    public PlayerState.Component FillUnitTargetsList(Unit.Action inAction, HighlightingDataComponent inHinghlightningData, PlayerState.Component playerState, PlayerPathing.Component playerPathing, uint inFaction)
    {
        //need to call validateTarget with hoveredCoord before doing anything
        if(!m_PathFindingSystem.ValidateTarget(playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate, inAction, playerState.SelectedUnitId, inFaction, playerPathing.CachedPaths))
        {
            playerState.TargetValid = false;
            //if hovered cell / unit is not valid
            return playerState;
        }
        else
        {
            playerState.TargetValid = true;
        }

        if (inHinghlightningData.AoERadius > 0)
        {
            List<Vector3f> area = new List<Vector3f>();
            
            area = CellGridMethods.CircleDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.AoERadius);
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(area, (int)inAction.ActionExecuteStep, (int)inAction.Effects[0].DealDamageNested.DamageAmount);

            if (cubeCoordList.CubeCoordinates.Count != 1)
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
        }
        else if (inHinghlightningData.RingRadius > 0)
        {
            List<Vector3f> ring = new List<Vector3f>();
            ring = CellGridMethods.RingDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.RingRadius);
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(ring, (int)inAction.ActionExecuteStep, (int)inAction.Effects[0].DealDamageNested.DamageAmount);

            if (cubeCoordList.CubeCoordinates.Count != 1)
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
        }
        else if (inHinghlightningData.LineAoE == 1)
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>(), (int)inAction.ActionExecuteStep, (int)inAction.Effects[0].DealDamageNested.DamageAmount);
            cubeCoordList.CubeCoordinates = CellGridMethods.LineDraw(playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate);

            if (cubeCoordList.CubeCoordinates.Count != 1)
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
        }
        else
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>(), (int)inAction.ActionExecuteStep, (int)inAction.Effects[0].DealDamageNested.DamageAmount);
            cubeCoordList.CubeCoordinates.Add(inHinghlightningData.HoveredCoordinate);

            HashSet<long> unitIds = new HashSet<long>(playerState.UnitTargets.Keys);

            if (unitIds.Contains(playerState.SelectedUnitId))
            {
                playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Clear();
            }

            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
        }

        Entities.With(m_UnitData).ForEach((Entity e, ref CubeCoordinate.Component unitCoord, ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction) =>
        {
            if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
            {
                if (playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Contains(unitCoord.CubeCoordinate))
                {
                    //if target is not valid, remove it from UnitTargets Dict
                    if (!m_PathFindingSystem.ValidateUnitTarget(e, (UnitRequisitesEnum)inHinghlightningData.EffectRestrictionIndex, playerState.SelectedUnitId, unitFaction.Faction))
                    {
                        playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Remove(unitCoord.CubeCoordinate);
                    }
                }
            }
        });

        playerState.TargetDictChange = true;
        return playerState;
    }

    public HighlightingDataComponent GatherHighlightingInformation(Entity e, int actionID, HighlightingDataComponent playerHighlightingData)
    {
        var h = playerHighlightingData;
        ECSActionTarget target = null;
        ECSActionEffect effect = null;

        //var id = EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
        var actions = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);

        if (target == null)
        {
            if (actionID < 0)
            {
                switch (actionID)
                {
                    case -3:
                        break;
                }
            }
            else
            {
                if (actionID < actions.Actions.Count)
                {
                    target = actions.Actions[actionID].Targets[0];
                    effect = actions.Actions[actionID].Effects[0];
                }
                else
                {
                    target = actions.SpawnActions[actionID - actions.Actions.Count].Targets[0];
                    effect = actions.SpawnActions[actionID - actions.Actions.Count].Effects[0];
                }
            }
        }

        h.Range = (uint)target.targettingRange;

        switch (target.HighlighterToUse)
        {
            case ECSActionTarget.HighlightDef.Radius:
                h.PathingRange = 0;
                break;
            case ECSActionTarget.HighlightDef.Path:
                h.PathingRange = 1;
                break;
        }

        if (target is ECSATarget_Unit)
        {
            ECSATarget_Unit unitTarget = target as ECSATarget_Unit;
            h.IsUnitTarget = 1;
            h.TargetRestrictionIndex = (uint)unitTarget.Restrictions;
        }
        else if (target is ECSATarget_Tile)
        {
            h.IsUnitTarget = 0;
            h.TargetRestrictionIndex = 999;
        }

        h.EffectRestrictionIndex = (uint)effect.ApplyToRestrictions;
        HandleMods(target.SecondaryTargets, ref h);
        return h;
    }

    public void SetNumberOfTargets(PlayerState.Component playerState, HashSet<Vector3f> lastHoveredCoords)
    {
        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        Entities.With(m_MarkerStateData).ForEach((Entity e, ref MarkerState markerState, ref CubeCoordinate.Component coord) =>
        {
            uint nOfTargets = 0;
            int turnStepIndex = 0;

            foreach (long l in playerState.UnitTargets.Keys)
            {
                HashSet<Vector3f> targetCoordsHash = new HashSet<Vector3f>(playerState.UnitTargets[l].CubeCoordinates);
                int tI = playerState.UnitTargets[l].TurnStepIndex;

                if (targetCoordsHash.Contains(coord.CubeCoordinate))
                {
                    turnStepIndex = tI;
                    nOfTargets += 1;
                    commandBuffer.AddComponent(e, new RequireMarkerUpdate());
                }
            }

            markerState.TurnStepIndex = turnStepIndex;
            markerState.NumberOfTargets = nOfTargets;

            if(lastHoveredCoords.Contains(coord.CubeCoordinate))
            {
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }

        });
    }

    public PlayerPathing.Component UpdateSelectedUnit(PlayerState.Component inPlayerState, PlayerPathing.Component inPlayerPathing, HighlightingDataComponent playerHighlightingData)
    {
        /*
        m_LogDispatcher.HandleLog(LogType.Warning,
        new LogEvent("InPlayerState")
        .WithField("InPlayerStateCachedPathsCount = ", inPlayerState.CachedPaths.Count));
        */
        var playerPathing = inPlayerPathing;
        var playerState = inPlayerState;

        Entities.With(m_ActiveUnitData).ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId iD, ref CubeCoordinate.Component occCoord, ref WorldIndex.Component worldIndex, ref MouseState mouseState, ref FactionComponent.Component faction)=>
        {
            if (iD.EntityId.Id == playerState.SelectedUnitId)
            {
                Actions.Component actions = EntityManager.GetComponentData<Actions.Component>(e);
                if (mouseState.CurrentState == MouseState.State.Clicked)
                {
                    mouseState.CurrentState = MouseState.State.Neutral;
                }

                if (playerPathing.CellsInRange.Count == 0)
                {
                    if (playerHighlightingData.PathingRange == 1)
                    {
                        uint range = playerHighlightingData.Range;

                        if (playerHighlightingData.PathLine == 1)
                        {
                           if (CheckPlayerEnergy(faction.Faction, 0) < playerHighlightingData.Range)
                           {
                                range = (uint)CheckPlayerEnergy(faction.Faction, 0);
                           }
                        }

                        List<CellAttributes> go = m_PathFindingSystem.GetRadius(occCoord.CubeCoordinate, range, worldIndex.Value);
                        playerPathing.CachedPaths = m_PathFindingSystem.GetAllPathsInRadius(range, go, occCoord.CubeCoordinate);

                        /*
                        m_LogDispatcher.HandleLog(LogType.Warning,
                        new LogEvent("OccupiedCoordinate")
                        .WithField("OccCoord", Vector3fext.ToUnityVector(occCoord.CubeCoordinate)));
                        */
                        foreach (CellAttribute key in playerPathing.CachedPaths.Keys)
                        {
                            playerPathing.CellsInRange.Add(key);
                            playerPathing.CoordinatesInRange.Add(key.CubeCoordinate);
                        }

                        playerPathing.CoordinatesInRange = playerPathing.CoordinatesInRange;
                        playerPathing.CachedPaths = playerPathing.CachedPaths;
                        playerPathing.CellsInRange = playerPathing.CellsInRange;
                        /*
                        m_LogDispatcher.HandleLog(LogType.Warning,
                        new LogEvent("CellsinRange")
                        .WithField("CellsInRangeCount", playerPathing.CellsInRange.Count));
                        */
                    }
                    else
                    {
                        List<CellAttributes> go = m_PathFindingSystem.GetRadius(occCoord.CubeCoordinate, playerHighlightingData.Range, worldIndex.Value);
                        foreach (CellAttributes c in go)
                        {
                            playerPathing.CellsInRange.Add(c.Cell);
                            playerPathing.CoordinatesInRange.Add(c.Cell.CubeCoordinate);
                        }
                        playerPathing.CoordinatesInRange = playerPathing.CoordinatesInRange;
                        playerPathing.CellsInRange = playerPathing.CellsInRange;
                    }
                }

                else
                {
                    if (playerState.TargetValid)
                    {
                        if (playerHighlightingData.IsUnitTarget == 1)
                        {
                            Entities.With(m_UnitData).ForEach((LineRendererComponent hoveredLineRendererComp, ref CubeCoordinate.Component unitCoord) =>
                            {
                                if (Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(playerHighlightingData.HoveredCoordinate))
                                {
                                    playerHighlightingData.LineYOffset = hoveredLineRendererComp.arcOffset.y;
                                }
                            });
                        }
                        else
                            playerHighlightingData.LineYOffset = 0;

                        if (playerHighlightingData.PathLine == 1)
                        {
                            CellAttributeList Path = new CellAttributeList();
                            Path = m_PathFindingSystem.FindPath(playerHighlightingData.HoveredCoordinate, playerPathing.CachedPaths);

                            /*
                            m_LogDispatcher.HandleLog(LogType.Warning,
                            new LogEvent("PathLineUpdate")
                            .WithField("Path[0] = ", Vector3fext.ToUnityVector(Path.CellAttributes[0].CubeCoordinate)));
                            */
                            /*
                            m_LogDispatcher.HandleLog(LogType.Warning,
                            new LogEvent("playerStateLocalVar")
                            .WithField("playerStateLocalVarCachedPathsCount when updating line = ", playerState.CachedPaths.Count));
                            */
                            if (Path.CellAttributes.Count > 0)
                            {
                                UpdatePathLineRenderer(Path, lineRendererComp);
                            }
                        }
                        else if (playerHighlightingData.LineAoE == 0)
                        {
                            //use Arc Line
                            if (playerHighlightingData.HoveredPosition != Vector3.zero)
                            {
                                //playerHighlightingData.tur
                                //Debug.Log("UpdateArcLineRenderer");
                                
                                UpdateArcLineRenderer(playerHighlightingData.LineYOffset, playerHighlightingData.HoveredPosition, lineRendererComp, (int)actions.CurrentSelected.ActionExecuteStep);
                            }
                            else
                                lineRendererComp.lineRenderer.positionCount = 0;
                        }
                        else
                        {
                            lineRendererComp.lineRenderer.positionCount = 0;
                        }
                    }
                    else
                    {
                        if (playerState.UnitTargets.ContainsKey(iD.EntityId.Id))
                        {
                            playerState.UnitTargets[iD.EntityId.Id].CubeCoordinates.Clear();
                            playerState.TargetDictChange = true;
                        }
                        lineRendererComp.lineRenderer.positionCount = 0;
                    }
                }
            }
        });

        return playerPathing;
    }

    void HandleMods(List<ECSActionSecondaryTargets> inMods, ref HighlightingDataComponent highLightingData)
    {
        highLightingData.LineAoE = 0;
        highLightingData.PathLine = 0;
        highLightingData.AoERadius = 0;
        highLightingData.RingRadius = 0;

        foreach (ECSActionSecondaryTargets t in inMods)
        {
            if (t is SecondaryPath)
            {
                highLightingData.PathLine = 1;
            }
            else if (t is SecondaryArea)
            {
                SecondaryArea secAoE = t as SecondaryArea;
                highLightingData.AoERadius = (uint)secAoE.areaSize;
            }
            else if (t is SecondaryLine)
            {
                highLightingData.LineAoE = 1;
            }
            else if (t is SecondaryRing)
            {
                SecondaryRing secRing = t as SecondaryRing;
                highLightingData.RingRadius = secRing.radius;
            }
        }
    }

    void UpdatePathLineRenderer(CellAttributeList inPath, LineRendererComponent inLineRendererComp)
    {

        Color turnStepColor = settings.TurnStepLineColors[2];
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(turnStepColor, 0f),new GradientColorKey(turnStepColor, .9f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, .9f), new GradientAlphaKey(0f, 1f)}
        );
        inLineRendererComp.lineRenderer.colorGradient = gradient;
        inLineRendererComp.lineRenderer.widthCurve = inLineRendererComp.PathLineWidthCurve;

        inLineRendererComp.lineRenderer.positionCount = inPath.CellAttributes.Count + 1;
        inLineRendererComp.lineRenderer.SetPosition(0, inLineRendererComp.transform.position + inLineRendererComp.pathOffset);

        for (int pi = 1; pi <= inPath.CellAttributes.Count; pi++)
        {
             inLineRendererComp.lineRenderer.SetPosition(pi, Vector3fext.ToUnityVector(inPath.CellAttributes[pi - 1].Position) + inLineRendererComp.pathOffset);
        }
    }

    void UpdateArcLineRenderer(float targetYOffset, Vector3 inTarget, LineRendererComponent inLineRendererComp, int turnStepIndex)
    {
        Color turnStepColor = settings.TurnStepLineColors[turnStepIndex];

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.black, 0.1f), new GradientColorKey(turnStepColor, .3f)},
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0.0f), new GradientAlphaKey(0f, 0.1f), new GradientAlphaKey(1f, .3f)}
        );
        inLineRendererComp.lineRenderer.colorGradient = gradient;



        Vector3 arcOrigin = inLineRendererComp.transform.position + inLineRendererComp.arcOffset;
        Vector3 arcTarget = new Vector3(inTarget.x, inTarget.y + targetYOffset, inTarget.z);

        Vector3[] arcPath = CalculateSinusPath(arcOrigin, arcTarget, 1);
        inLineRendererComp.lineRenderer.positionCount = arcPath.Length;

        Keyframe[] keys = inLineRendererComp.ArcLineWidthCurve.keys;
        keys[1].time = 1 - (1 / ((float)arcPath.Length - 6f));
        keys[2].time = 1 - (1 / ((float)arcPath.Length - 5f));
        inLineRendererComp.ArcLineWidthCurve.keys = keys;
        inLineRendererComp.lineRenderer.widthCurve = inLineRendererComp.ArcLineWidthCurve;

        inLineRendererComp.lineRenderer.SetPositions(arcPath);
    }

    public Vector3[] CalculateSinusPath(Vector3 origin, Vector3 target, float zenitHeight)
    {
        Vector3 distance = target - origin;
        int numberOfPositions = 8 + (2 * Mathf.RoundToInt(distance.magnitude) / 2);

        Vector3[] sinusPath = new Vector3[numberOfPositions + 1];
        float heightDifference = origin.y - target.y;
        float[] ypositions = CalculateSinusPoints(numberOfPositions);
        float xstep = 1.0f / numberOfPositions;

        for (int i = 0; i < ypositions.Length; i++)
        {
            float sinYpos = ypositions[i] * zenitHeight - heightDifference * (xstep * i);
            sinusPath[i] = origin + new Vector3(distance.x * (xstep * i), sinYpos, distance.z * (xstep * i));
        }

        sinusPath[numberOfPositions] = target;

        return sinusPath;
    }

    public float[] CalculateSinusPoints(int numberOfPositions)
    {
        float[] yPosArray = new float[numberOfPositions];
        float xStep = 1.0f / numberOfPositions;
        int i = 0;

        for (float x = 0.0f; x <= 1.0f; x += xStep)
        {
            if (i < yPosArray.Length)
            {
                yPosArray[i] = (float)Math.Sin(x * Math.PI);
                i++;
            }
        }

        return yPosArray;
    }

    public void ResetHighlights(ref PlayerState.Component playerState, HighlightingDataComponent playerHigh)
    {
        var p = playerState;

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        HashSet<long> unitIdHash = new HashSet<long>(playerState.UnitTargets.Keys);

        Entities.With(m_ActiveUnitData).ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId unitId, ref Actions.Component actions) =>
        {
            //actions.CurrentSelected condition prevents line from being cleared instantly if invalid target is selected / rightclick action deselect is used
            if (actions.LockedAction.Index == -3 && actions.CurrentSelected.Index == -3)
            {
                if (unitIdHash.Contains(unitId.EntityId.Id) && playerHigh.TargetRestrictionIndex != 2)
                {
                    ResetUnitHighLights(e, p, unitId.EntityId.Id);
                    p.UnitTargets.Remove(unitId.EntityId.Id);
                    p.TargetDictChange = true;
                }
            }
        });

        playerState = p;

        Entities.With(m_MarkerStateData).ForEach((Entity e, ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
        {
            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
            if (markerState.CurrentState != (MarkerState.State)(int)mouseState.CurrentState)
            {
                markerState.CurrentState = (MarkerState.State)(int)mouseState.CurrentState;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
                //markerState.IsSet = 0;
            }
            if(markerState.CurrentState == MarkerState.State.Reachable)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
        });
    }

    public void ResetHighlights()
    {
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerStateData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

        var playerHigh = playerHighlightingDatas[0];
        var playerState = playerStates[0];

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        HashSet<long> unitIdHash = new HashSet<long>(playerState.UnitTargets.Keys);

        Entities.With(m_ActiveUnitData).ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId unitId, ref Actions.Component actions) =>
        {
            if(actions.CurrentSelected.Index != -3)
            {
                actions.CurrentSelected = actions.NullAction;
            }
            //actions.CurrentSelected condition prevents line from being cleared instantly if invalid target is selected / rightclick action deselect is used
            if (actions.LockedAction.Index == -3)
            {
                if (unitIdHash.Contains(unitId.EntityId.Id) && playerHigh.TargetRestrictionIndex != 2)
                {
                    ResetUnitHighLights(e, playerState, unitId.EntityId.Id);
                    playerState.UnitTargets.Remove(unitId.EntityId.Id);
                    playerState.TargetDictChange = true;
                }
            }
        });

        Entities.With(m_MarkerStateData).ForEach((Entity e, ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
        {
            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
            if (markerState.CurrentState != (MarkerState.State)(int)mouseState.CurrentState)
            {
                markerState.CurrentState = (MarkerState.State)(int)mouseState.CurrentState;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
                //markerState.IsSet = 0;
            }
            if (markerState.CurrentState == MarkerState.State.Reachable)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
        });


        playerState.UnitTargets = playerState.UnitTargets;
        playerStates[0] = playerState;
        playerHighlightingDatas[0] = playerHigh;

        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        m_PlayerStateData.CopyFromComponentDataArray(playerHighlightingDatas);

        playerHighlightingDatas.Dispose();
        playerStates.Dispose();
    }


    public void ResetUnitHighLights(Entity e, PlayerState.Component playerState, long unitId)
    {
        var lineRendererComp = EntityManager.GetComponentObject<LineRendererComponent>(e);
        lineRendererComp.lineRenderer.positionCount = 0;

        if (playerState.UnitTargets.ContainsKey(unitId) && playerState.UnitTargets[unitId].CubeCoordinates.Count != 0)
        {
            ResetMarkerNumberOfTargets(playerState.UnitTargets[unitId].CubeCoordinates);

        }
    }

    public void ResetMarkerNumberOfTargets(List<Vector3f> cubeCoords)
    {
        HashSet<Vector3f> coordHash = new HashSet<Vector3f>(cubeCoords);
        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        Entities.With(m_MarkerStateData).ForEach((Entity e, ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
        {
            if (coordHash.Contains(coord.CubeCoordinate) && markerState.NumberOfTargets > 0)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                markerState.NumberOfTargets = 0;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
        });
    }

    public void HighlightReachable(ref PlayerState.Component playerState, ref PlayerPathing.Component playerPathing)
    {
        /*
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerState = playerStates[0];
        var playerPathings = m_PlayerStateData.ToComponentDataArray<PlayerPathing.Component>(Allocator.TempJob);
        */

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();


        //remove selected unit target from player UnitTargets Dict 
        if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
        {
            playerState.UnitTargets.Remove(playerState.SelectedUnitId);
            playerState.TargetDictChange = true;
            //playerStates[0] = playerState;
        }

        //m_PlayerStateData.CopyFromComponentDataArray(playerStates);

        HashSet<Vector3f> playerCellsInRangeHash = new HashSet<Vector3f>(playerPathing.CoordinatesInRange);

        Entities.With(m_MarkerStateData).ForEach((Entity e, ref MarkerState marker, ref CubeCoordinate.Component coord) =>
        {
            if (marker.CurrentState == MarkerState.State.Hovered || marker.CurrentState == MarkerState.State.Clicked)
            {
                marker.CurrentState = MarkerState.State.Neutral;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
            
            if (playerCellsInRangeHash.Contains(coord.CubeCoordinate))
            {
                marker.CurrentState = MarkerState.State.Reachable;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }

        });


    }

    public void ClearPlayerState()
    {
        /*
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerState = playerStates[0];
        playerState.CellsInRange.Clear();
        playerState.CachedPaths.Clear();
        playerState.CellsInRange = playerState.CellsInRange;
        playerState.CachedPaths = playerState.CachedPaths;
        playerStates[0] = playerState;

        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        playerStates.Dispose();
        */
        /*
        var unitIdData = m_ActiveUnitData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
        var lineRendererData = m_ActiveUnitData.ToComponentArray<LineRendererComponent>();

        for (int i = 0; i < unitIdData.Length; i++)
        {
            var iD = unitIdData[i].EntityId.Id;
            var lineRendererComp = lineRendererData[i];

            if (iD == playerState.SelectedUnitId)
            {
                lineRendererComp.lineRenderer.positionCount = 0;
            }
        }
        */
        //m_PlayerStateData.PlayerState[0] = playerstate;
    }

    public void SetSelfTarget(long entityID, int executeStep)
    {
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerState = playerStates[0];

        playerState.UnitTargets.Remove(entityID);

        Entities.With(m_ActiveUnitData).ForEach((LineRendererComponent lineRendererComp, ref SpatialEntityId iD, ref CubeCoordinate.Component coord) =>
        {
            if (iD.EntityId.Id == entityID)
            {
                lineRendererComp.lineRenderer.positionCount = 0;
                CubeCoordinateList list = new CubeCoordinateList(new List<Vector3f> { coord.CubeCoordinate }, executeStep, 0);
                playerState.UnitTargets.Add(iD.EntityId.Id, list);

            }
        });

        //playerStates[0].CellsInRange.Clear();
        //playerStates[0].UnitTargets = playerStates[0].UnitTargets;
        //playerStates[0].TargetDictChange = true;
        playerState.TargetDictChange = true;
        playerStates[0] = playerState;
        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        SetNumberOfTargets(playerStates[0], new HashSet<Vector3f>());
        playerStates.Dispose();


    }

    public int CheckPlayerEnergy(uint playerFaction, uint energyCost = 0)
    {
        int leftOverEnergy = 0;
        Entities.With(m_PlayerStateData).ForEach((ref PlayerEnergy.Component energyComp, ref FactionComponent.Component faction) =>
        {
            if (playerFaction == faction.Faction)
            {
                leftOverEnergy = (int)energyComp.Energy - (int)energyCost;
            }
        });
        return leftOverEnergy;
    }

}