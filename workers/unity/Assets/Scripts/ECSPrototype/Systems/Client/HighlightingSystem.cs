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
using Unity.Jobs;
using UnityEngine;
using Action = Unit.Action;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class HighlightingSystem : JobComponentSystem
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
    EntityQuery m_RightClickedUnitData;
    ILogDispatcher m_LogDispatcher;

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        settings = Resources.Load<Settings>("Settings");

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<MarkerState>()
            );

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<MapData.Component>()
            );

        m_ActiveUnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<UnitDataSet>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadWrite<LineRendererComponent>()
            );

        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<LineRendererComponent>(),
            ComponentType.ReadOnly<Transform>()
            );

        m_MarkerStateData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<MarkerState>()
            );

        m_PlayerStateData = GetEntityQuery(
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<PlayerState.Component>(),
            ComponentType.ReadWrite<PlayerPathing.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<PlayerEnergy.Component>(),
            ComponentType.ReadWrite<HighlightingDataComponent>(),
            ComponentType.ReadWrite<PlayerEffects>()
            );

        m_HoveredData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<HoveredState>()
            );

        m_RightClickedUnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<RightClickEvent>(),
            ComponentType.ReadWrite<LineRendererComponent>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
        m_LogDispatcher = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PlayerStateData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() != 1)
            return inputDeps;

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var gameStateEntity = m_GameStateData.GetSingletonEntity();
        var mapData = EntityManager.GetComponentObject<CurrentMapState>(gameStateEntity);

        #region playerStateData
        var playerPathing = m_PlayerStateData.GetSingleton<PlayerPathing.Component>();
        var playerState = m_PlayerStateData.GetSingleton<PlayerState.Component>();
        var playerHighlightingData = m_PlayerStateData.GetSingleton<HighlightingDataComponent>();
        var playerFaction = m_PlayerStateData.GetSingleton<FactionComponent.Component>();
        var playerEntity = m_PlayerStateData.GetSingletonEntity();
        var playerEffects = EntityManager.GetComponentObject<PlayerEffects>(playerEntity);
        #endregion

        //var activeUnitLineRenderers = m_ActiveUnitData.ToComponentArray<LineRendererComponent>();
        if (gameState.CurrentState == GameStateEnum.planning)
        {
            Entities.ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId unitId, ref RightClickEvent r) =>
            {
                ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id);
            })
            .WithoutBurst()
            .Run();

            if (Vector3fext.ToUnityVector(playerHighlightingData.HoveredCoordinate) != Vector3fext.ToUnityVector(playerHighlightingData.LastHoveredCoordinate))
            {
                if (playerState.CurrentState == PlayerStateEnum.waiting_for_target && playerHighlightingData.TargetRestrictionIndex != 2)
                {
                    HashSet<Vector3f> currentActionTargetHash = new HashSet<Vector3f>();

                    if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
                        currentActionTargetHash = new HashSet<Vector3f>(playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Keys);

                    currentActionTargetHash.Add(playerHighlightingData.LastHoveredCoordinate);

                    playerState = FillUnitTargetsList(mapData, playerState.SelectedAction, playerHighlightingData, playerState, playerPathing, playerFaction.Faction);
                    playerPathing = UpdateSelectedUnit(playerState, playerPathing, playerHighlightingData, mapData);
                    SetNumberOfTargets(playerState, currentActionTargetHash);

                    m_PlayerStateData.SetSingleton(playerPathing);
                    m_PlayerStateData.SetSingleton(playerState);
                    /*
                    playerStates[0] = playerState;
                    playerPathings[0] = playerPathing;
                    m_PlayerStateData.CopyFromComponentDataArray(playerPathings);
                    m_PlayerStateData.CopyFromComponentDataArray(playerStates);
                    */
                }
                else
                {
                    if (playerHighlightingData.ResetHighlightsBuffer > 0)
                    {
                        playerHighlightingData.ResetHighlightsBuffer -= Time.DeltaTime;
                    }
                    else
                        ResetHighlights(ref playerState, playerHighlightingData, playerEffects);

                    //ResetHighlights(ref playerState, playerHighlightingData);
                }

                playerHighlightingData.LastHoveredCoordinate = playerHighlightingData.HoveredCoordinate;
            }
        }
        else
        {

            Entities.ForEach((Entity e, ref MarkerState markerState) =>
            {
                if (markerState.NumberOfTargets > 0 || markerState.CurrentState == MarkerState.State.Hovered)
                {
                    markerState.NumberOfTargets = 0;
                    commandBuffer.AddComponent(e, new RequireMarkerUpdate());
                }
            })
            .WithoutBurst()
            .Run();

            Entities.ForEach((LineRendererComponent lineRenderer) =>
            {
                lineRenderer.lineRenderer.positionCount = 0;
            })
            .WithoutBurst()
            .Run();
        }

        SetSingleton(playerHighlightingData);

        return inputDeps;

    }

    public PlayerState.Component FillUnitTargetsList(CurrentMapState mapData, Action inAction, HighlightingDataComponent inHinghlightningData, PlayerState.Component playerState, PlayerPathing.Component playerPathing, uint playerFaction)
    {
        if(inAction.Effects[0].EffectType == EffectTypeEnum.move_along_path)
        {
            if (!m_PathFindingSystem.ValidatePathTargetClient(mapData, playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate, inAction, playerState.SelectedUnitId, playerFaction, playerPathing.CachedMapPaths))
            {
                playerState.TargetValid = false;
                //if hovered cell / unit is not valid
                return playerState;
            }
            else
            {
                playerState.TargetValid = true;
            }
        }
        else
        {
            if (!m_PathFindingSystem.ValidateTargetClient(mapData, playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate, inAction, playerState.SelectedUnitId, playerFaction))
            {
                playerState.TargetValid = false;
                //if hovered cell / unit is not valid
                return playerState;
            }
            else
            {
                playerState.TargetValid = true;
            }
        }

        if (inHinghlightningData.AoERadius > 0)
        {
            playerState.UnitTargets[playerState.SelectedUnitId] = new CubeCoordinateList(ConvertCoordinatleListToDict(CellGridMethods.CircleDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.AoERadius)), (int) inAction.ActionExecuteStep, inAction.Effects[0].DealDamageNested.DamageAmount, inAction.Effects[0].GainArmorNested.ArmorAmount, Convert.ToBoolean(inHinghlightningData.IsUnitTarget));
        }
        else if (inHinghlightningData.RingRadius > 0)
        {
            playerState.UnitTargets[playerState.SelectedUnitId] = new CubeCoordinateList(ConvertCoordinatleListToDict(CellGridMethods.RingDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.RingRadius)), (int) inAction.ActionExecuteStep, inAction.Effects[0].DealDamageNested.DamageAmount, inAction.Effects[0].GainArmorNested.ArmorAmount, Convert.ToBoolean(inHinghlightningData.IsUnitTarget));
        }
        else if (inHinghlightningData.LineAoE == 1)
        {
            playerState.UnitTargets[playerState.SelectedUnitId] = new CubeCoordinateList(ConvertCoordinatleListToDict(CellGridMethods.LineDraw(new List<Vector3f>(), playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate)), (int) inAction.ActionExecuteStep, inAction.Effects[0].DealDamageNested.DamageAmount, inAction.Effects[0].GainArmorNested.ArmorAmount, Convert.ToBoolean(inHinghlightningData.IsUnitTarget));
        }
        else if(inHinghlightningData.ConeExtent != 0)
        {
            playerState.UnitTargets[playerState.SelectedUnitId] = new CubeCoordinateList(ConvertCoordinatleListToDict(CellGridMethods.ConeDraw(playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate, inHinghlightningData.ConeRadius, inHinghlightningData.ConeExtent)), (int)inAction.ActionExecuteStep, inAction.Effects[0].DealDamageNested.DamageAmount, inAction.Effects[0].GainArmorNested.ArmorAmount, Convert.ToBoolean(inHinghlightningData.IsUnitTarget));
        }
        else
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new Dictionary<Vector3f, bool>(), (int)inAction.ActionExecuteStep, inAction.Effects[0].DealDamageNested.DamageAmount, inAction.Effects[0].GainArmorNested.ArmorAmount, Convert.ToBoolean(inHinghlightningData.IsUnitTarget));
            cubeCoordList.CubeCoordinates.Add(inHinghlightningData.HoveredCoordinate, true);

            HashSet<long> unitIds = new HashSet<long>(playerState.UnitTargets.Keys);

            if (unitIds.Contains(playerState.SelectedUnitId))
            {
                playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Clear();
            }

            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
        }

        Entities.ForEach((Entity e, ref CubeCoordinate.Component unitCoord, in SpatialEntityId unitId, in FactionComponent.Component unitFaction) =>
        {
            if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
            {
                if (playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Keys.Contains(unitCoord.CubeCoordinate))
                {
                    //if target is not valid, remove it from UnitTargets Dict
                   
                    if (playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.ContainsKey(unitCoord.CubeCoordinate) && !m_PathFindingSystem.ValidateUnitTarget((ApplyToRestrictionsEnum)inHinghlightningData.EffectRestrictionIndex, playerState.SelectedUnitId, playerFaction, unitId.EntityId.Id, unitFaction.Faction))
                    {
                        //Debug.Log("TargetInvalid - Remove from playerTargets");
                        //playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Remove(unitCoord.CubeCoordinate);

                        playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates[unitCoord.CubeCoordinate] = false;

                        if(Vector3fext.ToUnityVector(playerState.SelectedUnitCoordinate) == Vector3fext.ToUnityVector(unitCoord.CubeCoordinate))
                            playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Remove(unitCoord.CubeCoordinate);
                    }
                }
            }
        })
        .WithoutBurst()
        .Run();

        playerState.TargetDictChange = true;
        return playerState;
    }

    public Dictionary<Vector3f, bool> ConvertCoordinatleListToDict(List<Vector3f> coordinateList)
    {
        var dict = new Dictionary<Vector3f, bool>();

        foreach(Vector3f v3 in coordinateList)
        {
            if(!dict.ContainsKey(v3))
                dict.Add(v3, true);
        }

        return dict;
    }

    public HighlightingDataComponent GatherHighlightingInformation(Entity e, int actionID, HighlightingDataComponent playerHighlightingData)
    {
        var h = playerHighlightingData;
        ECSActionTarget target = null;
        ECSActionEffect effect = null;

        //var id = EntityManager.GetComponentData<SpatialEntityId>(e).EntityId.Id;
        var actions = EntityManager.GetComponentObject<UnitDataSet>(e);

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

        Entities.ForEach((Entity e, ref MarkerState markerState, ref CubeCoordinate.Component coord) =>
        {
            uint nOfTargets = 0;
            int turnStepIndex = 0;

            foreach (long l in playerState.UnitTargets.Keys)
            {
                if(!playerState.UnitTargets[l].IsUnitTarget)
                {
                    HashSet<Vector3f> targetCoordsHash = new HashSet<Vector3f>(playerState.UnitTargets[l].CubeCoordinates.Keys);
                    int tI = playerState.UnitTargets[l].TurnStepIndex;

                    if (targetCoordsHash.Contains(coord.CubeCoordinate))
                    {
                        turnStepIndex = tI;
                        nOfTargets += 1;
                        commandBuffer.AddComponent(e, new RequireMarkerUpdate());
                    }
                }
            }

            markerState.TurnStepIndex = turnStepIndex;
            markerState.NumberOfTargets = nOfTargets;

            if(lastHoveredCoords.Contains(coord.CubeCoordinate))
            {
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }

        })
        .WithoutBurst()
        .Run();
    }

    public PlayerPathing.Component UpdateSelectedUnit(PlayerState.Component inPlayerState, PlayerPathing.Component inPlayerPathing, HighlightingDataComponent playerHighlightingData, CurrentMapState inMapData)
    {
        /*
        m_LogDispatcher.HandleLog(LogType.Warning,
        new LogEvent("InPlayerState")
        .WithField("InPlayerStateCachedPathsCount = ", inPlayerState.CachedPaths.Count));
        */
        var playerPathing = inPlayerPathing;
        var playerState = inPlayerState;

        Entities.ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId iD, ref CubeCoordinate.Component occCoord, ref MouseState mouseState, in FactionComponent.Component faction, in Actions.Component actions) =>
        {
            if (iD.EntityId.Id == playerState.SelectedUnitId)
            {
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

                        playerPathing.CachedMapPaths = m_PathFindingSystem.GetAllMapPathsInRadius(range, m_PathFindingSystem.GetMapRadius(inMapData, occCoord.CubeCoordinate, range), occCoord.CubeCoordinate, inMapData);

                        /*
                        List<CellAttributes> go = m_PathFindingSystem.GetRadiusClient(occCoord.CubeCoordinate, range);
                        playerPathing.CachedPaths = m_PathFindingSystem.GetAllPathsInRadius(range, go, occCoord.CubeCoordinate);
                        */

                        /*
                        m_LogDispatcher.HandleLog(LogType.Warning,
                        new LogEvent("OccupiedCoordinate")
                        .WithField("OccCoord", Vector3fext.ToUnityVector(occCoord.CubeCoordinate)));
                        */

                        foreach (MapCell key in playerPathing.CachedMapPaths.Keys)
                        {
                            playerPathing.CellsInRange.Add(key);
                            playerPathing.CoordinatesInRange.Add(key.AxialCoordinate);
                        }

                        playerPathing.CoordinatesInRange = playerPathing.CoordinatesInRange;
                        playerPathing.CellsInRange = playerPathing.CellsInRange;
                        playerPathing.CachedMapPaths = playerPathing.CachedMapPaths;

                        /*
                        m_LogDispatcher.HandleLog(LogType.Warning,
                        new LogEvent("CellsinRange")
                        .WithField("CellsInRangeCount", playerPathing.CellsInRange.Count));
                        */
                    }
                    else
                    {
                        foreach (KeyValuePair<Vector2i, MapCell> c in m_PathFindingSystem.GetMapRadius(inMapData, occCoord.CubeCoordinate, playerHighlightingData.Range))
                        {
                            playerPathing.CellsInRange.Add(c.Value);
                            playerPathing.CoordinatesInRange.Add(c.Key);
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

                            //hotfixed haardcoded lineYOffset
                            playerHighlightingData.LineYOffset = 2f;

                            //removed code because of forbidden loop da loop
                            /*
                            Entities.With(m_UnitData).ForEach((LineRendererComponent hoveredLineRendererComp, ref CubeCoordinate.Component unitCoord) =>
                            {
                                if (Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(playerHighlightingData.HoveredCoordinate))
                                {
                                    playerHighlightingData.LineYOffset = hoveredLineRendererComp.arcOffset.y;
                                }
                            });
                            */
                        }
                        else
                            playerHighlightingData.LineYOffset = 0;

                        if (playerHighlightingData.PathLine == 1)
                        {
                            MapCellList Path = new MapCellList();
                            Path = m_PathFindingSystem.FindPathFromCachedPaths(inMapData, playerHighlightingData.HoveredCoordinate, playerPathing.CachedMapPaths);

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
                            if (Path.Cells.Count > 0)
                            {
                                UpdatePathLineRenderer(Path, lineRendererComp);
                            }
                        }
                        else if (playerHighlightingData.LineAoE == 0)
                        {
                            //use Arc Line
                            if (playerHighlightingData.HoveredPosition != Vector3.zero)
                            {
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
        })
        .WithoutBurst()
        .Run();

        return playerPathing;
    }

    void HandleMods(List<ECSActionSecondaryTargets> inMods, ref HighlightingDataComponent highLightingData)
    {
        highLightingData.LineAoE = 0;
        highLightingData.PathLine = 0;
        highLightingData.AoERadius = 0;
        highLightingData.RingRadius = 0;
        highLightingData.ConeExtent = 0;
        highLightingData.ConeRadius = 0;

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
            else if (t is SecondaryCone)
            {
                SecondaryCone secCone = t as SecondaryCone;
                highLightingData.ConeRadius = secCone.radius;
                highLightingData.ConeExtent = secCone.extent;
            }
        }
    }

    void UpdatePathLineRenderer(MapCellList inPath, LineRendererComponent inLineRendererComp)
    {
        Color turnStepColor = settings.TurnStepLineColors[2];
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(turnStepColor, 0f),new GradientColorKey(turnStepColor, .9f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, .9f), new GradientAlphaKey(0f, 1f)}
        );
        inLineRendererComp.lineRenderer.colorGradient = gradient;
        inLineRendererComp.lineRenderer.widthCurve = inLineRendererComp.PathLineWidthCurve;

        inLineRendererComp.lineRenderer.positionCount = inPath.Cells.Count + 1;
        inLineRendererComp.lineRenderer.SetPosition(0, inLineRendererComp.transform.position + inLineRendererComp.pathOffset);

        for (int pi = 1; pi <= inPath.Cells.Count; pi++)
        {
             inLineRendererComp.lineRenderer.SetPosition(pi, Vector3fext.ToUnityVector(inPath.Cells[pi - 1].Position) + inLineRendererComp.pathOffset);
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

    public void ResetHighlights(ref PlayerState.Component playerState, HighlightingDataComponent playerHigh, PlayerEffects playerEffects)
    {
        var p = playerState;

        foreach (LineRenderer r in playerEffects.RangeOutlineRenderers)
            r.gameObject.SetActive(false);

        playerEffects.Shapes.Clear();
        playerEffects.Edges.Clear();

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        HashSet<long> unitIdHash = new HashSet<long>(playerState.UnitTargets.Keys);

        Entities.ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId unitId, in ClientActionRequest.Component clientActionRequest) =>
        {
            //actions.CurrentSelected condition prevents line from being cleared instantly if invalid target is selected / rightclick action deselect is used
            if (clientActionRequest.ActionId == -3 && Vector3fext.ToUnityVector(clientActionRequest.TargetCoordinate) == Vector3.zero)
            {
                if (unitIdHash.Contains(unitId.EntityId.Id) && playerHigh.TargetRestrictionIndex != 2)
                {
                    ResetUnitHighLights(e, ref p, unitId.EntityId.Id);
                }
            }
        })
        .WithoutBurst()
        .Run();

        playerState = p;

        Entities.ForEach((Entity e, ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
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
        })
        .WithoutBurst()
        .Run();
    }

    public void ResetHighlightsNoIn()
    {
        var playerHigh = m_PlayerStateData.GetSingleton<HighlightingDataComponent>();
        var playerState = m_PlayerStateData.GetSingleton<PlayerState.Component>();

        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        Entities.ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId unitId, ref Actions.Component actions) =>
        {
            if(actions.CurrentSelected.Index != -3)
            {
                actions.CurrentSelected = actions.NullAction;
            }
            //actions.CurrentSelected condition prevents line from being cleared instantly if invalid target is selected / rightclick action deselect is used
            if (actions.LockedAction.Index == -3)
            {
                if (playerState.UnitTargets.ContainsKey(unitId.EntityId.Id) && playerHigh.TargetRestrictionIndex != 2)
                {
                    ResetUnitHighLights(e, ref playerState, unitId.EntityId.Id);
                    playerState.UnitTargets.Remove(unitId.EntityId.Id);
                    playerState.TargetDictChange = true;
                }
            }
        })
        .WithoutBurst()
        .Run();

        Entities.ForEach((Entity e, ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
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
        })
        .WithoutBurst()
        .Run();


        playerState.UnitTargets = playerState.UnitTargets;

        m_PlayerStateData.SetSingleton(playerState);
        m_PlayerStateData.SetSingleton(playerHigh);

    }

    public void ResetUnitHighLights(Entity e, ref PlayerState.Component playerState, long unitId)
    {
        if (EntityManager.GetComponentObject<LineRendererComponent>(e))
        {
            var lineRendererComp = EntityManager.GetComponentObject<LineRendererComponent>(e);
            lineRendererComp.lineRenderer.positionCount = 0;
        }

        if (playerState.UnitTargets.ContainsKey(unitId) && playerState.UnitTargets[unitId].CubeCoordinates.Count != 0)
        {
            ResetMarkerNumberOfTargets(playerState.UnitTargets[unitId].CubeCoordinates.Keys.ToList());
            playerState.UnitTargets.Remove(unitId);
            playerState.TargetDictChange = true;
        }
    }

    public void ResetMarkerNumberOfTargets(List<Vector3f> cubeCoords)
    {
        HashSet<Vector3f> coordHash = new HashSet<Vector3f>(cubeCoords);
        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        Entities.ForEach((Entity e, ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
        {
            if (coordHash.Contains(coord.CubeCoordinate) && markerState.NumberOfTargets > 0)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                markerState.NumberOfTargets = 0;
                commandBuffer.AddComponent(e, new RequireMarkerUpdate());
            }
        })
        .WithoutBurst()
        .Run();
    }

    public void HighlightReachable(ref PlayerState.Component playerState, ref PlayerPathing.Component playerPathing, PlayerEffects playerEffects, CurrentMapState mapData)
    {
        EntityCommandBuffer commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        foreach(Vector2i coord in playerPathing.CoordinatesInRange)
        {
            for (int c = 0; c < 6; c++)
            {
                if (!playerPathing.CoordinatesInRange.Contains(CellGridMethods.CubeToAxial(CellGridMethods.CubeNeighbour(CellGridMethods.AxialToCube(coord), (uint) c))))
                {
                    var centerPos = mapData.CoordinateCellDictionary[coord].Position;
                    if (c == 0)
                    {
                        playerEffects.Edges.Add(new HexEdgePositionPair
                        {
                            A = Vector3fext.ToUnityVector(CellGridMethods.PosToCorner(centerPos, 0, playerEffects.RangeLineYOffset)),
                            B = Vector3fext.ToUnityVector(CellGridMethods.PosToCorner(centerPos, 5, playerEffects.RangeLineYOffset))
                        });
                    }
                    else
                    {
                        playerEffects.Edges.Add(new HexEdgePositionPair
                        {
                            A = Vector3fext.ToUnityVector(CellGridMethods.PosToCorner(centerPos, c, playerEffects.RangeLineYOffset)),
                            B = Vector3fext.ToUnityVector(CellGridMethods.PosToCorner(centerPos, c - 1, playerEffects.RangeLineYOffset))
                        });
                    }
                }
            }
        }

        //Move edges from all edges into a shape and sort by distance
        for (int i = 0; i < playerEffects.RangeOutlineRenderers.Count; i++)
        {
            if (playerEffects.Edges.Count == 0)
                break;

            playerEffects.Shapes.Add(new HexOutlineShape
            {
                Edges = SortEdgeByDistance(ref playerEffects.Edges),
                Positions = new HashSet<Vector3>()
            });
        }

        for (int i = 0; i < playerEffects.Shapes.Count; i++)
        {
            foreach (HexEdgePositionPair edge in playerEffects.Shapes[i].Edges)
            {
                playerEffects.Shapes[i].Positions.Add(edge.A);
                playerEffects.Shapes[i].Positions.Add(edge.B);
            }

            playerEffects.RangeOutlineRenderers[i].gameObject.SetActive(true);
            playerEffects.RangeOutlineRenderers[i].positionCount = playerEffects.Shapes[i].Positions.Count;
            playerEffects.RangeOutlineRenderers[i].SetPositions(playerEffects.Shapes[i].Positions.ToArray());
        }

        //remove selected unit target from player UnitTargets Dict 
        if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
        {
            playerState.UnitTargets.Remove(playerState.SelectedUnitId);
            playerState.TargetDictChange = true;
        }
    }

    List<HexEdgePositionPair> SortEdgeByDistance(ref List<HexEdgePositionPair> edgeList)
    {
        List<HexEdgePositionPair> output = new List<HexEdgePositionPair>
            {
                edgeList[NearestEdge(new Vector3(), edgeList)]
            };

        edgeList.Remove(output[0]);

        int x = 0;
        for (int i = 0; i < edgeList.Count + x; i++)
        {
            if (i >= 5)
            {
                double closestRemainingDistance = NearestEdgeDist(output[output.Count - 1].B, edgeList);
                double firstLastAddedDistance = Vector3.Distance(output[0].B, output[output.Count - 1].A);

                if(closestRemainingDistance > firstLastAddedDistance)
                {
                    return output;
                }
            }

            output.Add(edgeList[NearestEdge(output[output.Count - 1].B, edgeList)]);
            edgeList.Remove(output[output.Count - 1]);
            x++;
        }

        return output;
    }

    Vector3 EdgeCenterPos(HexEdgePositionPair edge)
    {
        return (edge.A + edge.B) / 2;
    }

    int NearestEdge(Vector3 srcPos, List<HexEdgePositionPair> lookIn)
    {
        KeyValuePair<double, int> distanceListIndex = new KeyValuePair<double, int>();
        for (int i = 0; i < lookIn.Count; i++)
        {
            double distance = Vector3.Distance(srcPos, lookIn[i].A);
            if (i == 0)
            {
                distanceListIndex = new KeyValuePair<double, int>(distance, i);
            }
            else
            {
                if (distance < distanceListIndex.Key)
                {
                    distanceListIndex = new KeyValuePair<double, int>(distance, i);
                }
            }
        }
        return distanceListIndex.Value;
    }

    double NearestEdgeDist(Vector3 srcEdge, List<HexEdgePositionPair> lookIn)
    {
        KeyValuePair<double, int> distanceListIndex = new KeyValuePair<double, int>();
        for (int i = 0; i < lookIn.Count; i++)
        {
            double distance = Vector3.Distance(srcEdge, lookIn[i].A);
            if (i == 0)
            {
                distanceListIndex = new KeyValuePair<double, int>(distance, i);
            }
            else
            {
                if (distance < distanceListIndex.Key)
                {
                    distanceListIndex = new KeyValuePair<double, int>(distance, i);
                }
            }
        }
        return distanceListIndex.Key;
    }

    /*
    HexEdgePositionPair NextEdge(Vector3 srcPos, List<HexEdgePositionPair> lookIn)
    {
        for (int i = 0; i < lookIn.Count; i++)
        {
            if ((int)srcPos.x == (int)lookIn[i].A.x && (int)srcPos.z == (int)lookIn[i].A.z)
                return lookIn[i];
        }
        return lookIn[0];
    }

    Vector3[] SortByDistance(List<Vector3> pointList)
    {
        List<Vector3> output = new List<Vector3>
            {
                pointList[NearestPoint(new Vector3(0, 0, 0), pointList)]
            };
        pointList.Remove(output[0]);
        int x = 0;
        for (int i = 0; i < pointList.Count + x; i++)
        {
            output.Add(pointList[NearestPoint(output[output.Count - 1], pointList)]);
            pointList.Remove(output[output.Count - 1]);
            x++;
        }
        return output.ToArray();
    }

    int NearestPoint(Vector3 srcPt, List<Vector3> lookIn)
    {
        KeyValuePair<double, int> smallestDistance = new KeyValuePair<double, int>();
        for (int i = 0; i < lookIn.Count; i++)
        {
            double distance = Math.Sqrt(Math.Pow(srcPt.x - lookIn[i].x, 2) + Math.Pow(srcPt.y - lookIn[i].y, 2) + Math.Pow(srcPt.z - lookIn[i].z, 2));
            if (i == 0)
            {
                smallestDistance = new KeyValuePair<double, int>(distance, i);
            }
            else
            {
                if (distance < smallestDistance.Key)
                {
                    smallestDistance = new KeyValuePair<double, int>(distance, i);
                }
            }
        }
        return smallestDistance.Value;
    }
    */

    public void SetSelfTarget(long entityID, Action action, Vector3f coord, LineRendererComponent lineRendererComp)
    {
        var playerState = m_PlayerStateData.GetSingleton<PlayerState.Component>();

        playerState.UnitTargets.Remove(entityID);

        lineRendererComp.lineRenderer.positionCount = 0;
        CubeCoordinateList list = new CubeCoordinateList(new Dictionary<Vector3f, bool>(), (int) action.ActionExecuteStep, action.Effects[0].DealDamageNested.DamageAmount, action.Effects[0].GainArmorNested.ArmorAmount, true);
        list.CubeCoordinates.Add(coord, true);
        playerState.UnitTargets.Add(entityID, list);

        playerState.TargetDictChange = true;

        m_PlayerStateData.SetSingleton(playerState);
        SetNumberOfTargets(playerState, new HashSet<Vector3f>());
    }

    public int CheckPlayerEnergy(uint playerFaction, uint energyCost = 0)
    {
        int leftOverEnergy = 0;
        Entities.ForEach((ref PlayerEnergy.Component energyComp, in FactionComponent.Component faction) =>
        {
            if (playerFaction == faction.Faction)
            {
                leftOverEnergy = (int)energyComp.Energy - (int)energyCost;
            }
        })
        .WithoutBurst()
        .Run();
        return leftOverEnergy;
    }

}
