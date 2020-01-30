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
    PathFindingSystem m_PathFindingSystem;
    EntityQuery m_ActiveUnitData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    EntityQuery m_MarkerStateData;
    EntityQuery m_PlayerStateData;
    EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<MouseState>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<MarkerState>()
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
            ComponentType.ReadWrite<MouseState>(),
            ComponentType.ReadWrite<MarkerState>()
            );

        m_PlayerStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<PlayerState.Component>(),
            ComponentType.ReadOnly<PlayerState.ComponentAuthority>(),
            ComponentType.ReadOnly<PlayerEnergy.Component>(),
            ComponentType.ReadWrite<HighlightingDataComponent>()
            );

        m_PlayerStateData.SetFilter(PlayerState.ComponentAuthority.Authoritative);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PathFindingSystem = World.GetExistingSystem<PathFindingSystem>();
    }

    protected override void OnUpdate()
    {
        if (m_PlayerStateData.CalculateEntityCount() == 0)
            return;

        #region playerStateData
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerWorldIndexes = m_PlayerStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerStateData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
        var playerFactions = m_PlayerStateData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

        var playerState = playerStates[0];
        var playerWorldIndex = playerWorldIndexes[0].Value;
        var playerHighlightingData = playerHighlightingDatas[0];
        var playerFaction = playerFactions[0];
        #endregion

        var activeUnitLineRenderers = m_ActiveUnitData.ToComponentArray<LineRendererComponent>();

        //used to increase performance - does not work for clearing old AoE when selecting new action yet
        //HashSet<Vector3f> extendedCoordsInRangeHash = new HashSet<Vector3f>(m_CellGrid.CircleDraw(playerState.SelectedUnitCoordinate, playerHighlightingData.Range + playerHighlightingData.AoERadius + playerHighlightingData.RingRadius));

        Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
        {
            if (playerWorldIndex == gameStateWorldIndex.Value)
            {
                if (gameState.CurrentState == GameStateEnum.planning)
                {
                    //Debug.Log("lastHovered: " + Vector3fext.ToUnityVector(playerHighlightingData.LastHoveredCoordinate) + "currHovered: " + Vector3fext.ToUnityVector(playerHighlightingData.HoveredCoordinate));
                    if (Vector3fext.ToUnityVector(playerHighlightingData.HoveredCoordinate) != Vector3fext.ToUnityVector(playerHighlightingData.LastHoveredCoordinate) && playerHighlightingData.ClickCoolDown <= 0)
                    {
                        if (playerState.CurrentState == PlayerStateEnum.waiting_for_target && playerHighlightingData.TargetRestrictionIndex != 2)
                        {
                            playerState = FillUnitTargetsList(playerHighlightingData, playerState);
                            SetNumberOfTargets(playerState);
                            UpdateSelectedUnit();
                        }
                        else
                        {
                            ResetHighlights();
                        }

                        playerHighlightingData.LastHoveredCoordinate = playerHighlightingData.HoveredCoordinate;
                    }

                    if(playerHighlightingData.ClickCoolDown >= 0)
                        playerHighlightingData.ClickCoolDown -= Time.deltaTime;
                }
                else
                {

                    Entities.With(m_MarkerStateData).ForEach((ref MarkerState markerState) =>
                    {
                        if (markerState.NumberOfTargets > 0)
                        {
                            markerState.NumberOfTargets = 0;
                        }
                    });

                    Entities.With(m_ActiveUnitData).ForEach((LineRendererComponent lineRenderer) =>
                    {
                        lineRenderer.lineRenderer.positionCount = 0;
                    });
                }
            }
        });

        #region playerStateData
        playerHighlightingDatas[0] = playerHighlightingData;
        m_PlayerStateData.CopyFromComponentDataArray(playerHighlightingDatas);
        playerStates[0] = playerState;
        m_PlayerStateData.CopyFromComponentDataArray(playerStates);

        playerStates.Dispose();
        playerWorldIndexes.Dispose();
        playerHighlightingDatas.Dispose();
        playerFactions.Dispose();
        #endregion

    }

    public PlayerState.Component FillUnitTargetsList(HighlightingDataComponent inHinghlightningData, PlayerState.Component playerState)
    {
        //var hoveredCoordToUnityVector3 = Vector3fext.ToUnityVector(inHinghlightningData.HoveredCoordinate);

        if (inHinghlightningData.AoERadius > 0)
        {
            List<Vector3f> area = new List<Vector3f>();
            
            area = CellGridMethods.CircleDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.AoERadius);
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(area);

            if (cubeCoordList.CubeCoordinates.Count != 1 /* && hoveredCoordToUnityVector3 != new Vector3(999, 999, 999)*/)
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
        }
        else if (inHinghlightningData.RingRadius > 0)
        {
            List<Vector3f> ring = new List<Vector3f>();
            ring = CellGridMethods.RingDraw(inHinghlightningData.HoveredCoordinate, inHinghlightningData.RingRadius);
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(ring);

            //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
            if (cubeCoordList.CubeCoordinates.Count != 1 /* && hoveredCoordToUnityVector3 != new Vector3(999, 999, 999)*/)
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;

            }
        }
        else if (inHinghlightningData.LineAoE == 1)
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
            cubeCoordList.CubeCoordinates = CellGridMethods.LineDraw(playerState.SelectedUnitCoordinate, inHinghlightningData.HoveredCoordinate);
            //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
            if (cubeCoordList.CubeCoordinates.Count != 1 /*&& hoveredCoordToUnityVector3 != new Vector3(999, 999, 999)*/)
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
        }
        else
        {
            CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
            cubeCoordList.CubeCoordinates.Add(inHinghlightningData.HoveredCoordinate);

            HashSet<long> unitIds = new HashSet<long>(playerState.UnitTargets.Keys);


            //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
            /*if (hoveredCoordToUnityVector3 != new Vector3(999, 999, 999))
            {
                
            }
            else */
            if (unitIds.Contains(playerState.SelectedUnitId))
            {
                playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Clear();
            }

            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            /*
            if (unitIds.Contains(playerState.SelectedUnitId) && inHinghlightningData.TargetRestrictionIndex != 2)
            {
                
            }
            else
            {
                playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
            }
            */

        }

        Entities.With(m_UnitData).ForEach((Entity e, ref CubeCoordinate.Component unitCoord, ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction) =>
        {
            if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
            {
                if (playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Contains(unitCoord.CubeCoordinate))
                {
                    //if target is not valid, remove it from UnitTargets Dict
                    if (!m_PathFindingSystem.ValidateTarget(e, (UnitRequisitesEnum)inHinghlightningData.EffectRestrictionIndex, playerState.SelectedUnitId, unitFaction.Faction))
                    {
                        playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Remove(unitCoord.CubeCoordinate);
                    }
                }
            }
        });

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
                    case -2:
                        target = actions.BasicMove.Targets[0];
                        effect = actions.BasicMove.Effects[0];
                        break;
                    case -1:
                        target = actions.BasicAttack.Targets[0];
                        effect = actions.BasicAttack.Effects[0];
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

    public void SetNumberOfTargets(PlayerState.Component playerState)
    {
        Entities.With(m_MarkerStateData).ForEach((ref MarkerState markerState, ref CubeCoordinate.Component coord) =>
        {
            uint nOfTargets = 0;

            foreach (long l in playerState.UnitTargets.Keys)
            {
                HashSet<Vector3f> targetCoordsHash = new HashSet<Vector3f>(playerState.UnitTargets[l].CubeCoordinates);
                if (targetCoordsHash.Contains(coord.CubeCoordinate))
                {
                    nOfTargets += 1;
                }

                //start for setting different target markers depending on effectType - need to solve multiple targets problem
                /*
                for(int u = 0; u < m_ActiveUnitData.Length; u++)
                {
                    var unitId = m_ActiveUnitData.EntityIds[u].EntityId.Id;
                    var selectedAction = m_ActiveUnitData.ActionsData[u].CurrentSelected;

                    if(l == unitId && selectedAction.Index != -3)
                    {
                        markerState.CurrentTargetType = (MarkerState.TargetType)(int)selectedAction.Effects[0].EffectType;
                    }
                }
                */
            }
            markerState.NumberOfTargets = nOfTargets;
        });
    }

    public void UpdateSelectedUnit()
    {
        //Debug.Log("UpdateSelectedUnit");
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerStateData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

        var playerState = playerStates[0];
        var playerHighlightingData = playerHighlightingDatas[0];

        Entities.With(m_ActiveUnitData).ForEach((LineRendererComponent lineRendererComp, ref SpatialEntityId iD, ref CubeCoordinate.Component occCoord, ref WorldIndex.Component worldIndex, ref MouseState mouseState, ref FactionComponent.Component faction)=>
        {
            if (iD.EntityId.Id == playerState.SelectedUnitId)
            {
                if (mouseState.CurrentState == MouseState.State.Clicked)
                {
                    mouseState.CurrentState = MouseState.State.Neutral;
                }
                
                if (playerState.CellsInRange.Count == 0)
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
                        playerState.CachedPaths = m_PathFindingSystem.GetAllPathsInRadius(range, go, occCoord.CubeCoordinate);

                        foreach (CellAttribute key in playerState.CachedPaths.Keys)
                        {
                            playerState.CellsInRange.Add(key);
                        }

                        playerState.CellsInRange = playerState.CellsInRange;
                        playerStates[0] = playerState;
                        HighlightReachable();
                    }
                    else
                    {
                        List<CellAttributes> go = m_PathFindingSystem.GetRadius(occCoord.CubeCoordinate, playerHighlightingData.Range, worldIndex.Value);
                        foreach (CellAttributes c in go)
                        {
                            playerState.CellsInRange.Add(c.Cell);
                        }
                        playerState.CellsInRange = playerState.CellsInRange;
                        HighlightReachable();
                    }

                }
                
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
                    Path = m_PathFindingSystem.FindPath(playerHighlightingData.HoveredCoordinate, playerState.CachedPaths);

                    if (Path.CellAttributes.Count > 0)
                    {
                        UpdatePathLineRenderer(Path, lineRendererComp);
                    }
                }
                else if (playerHighlightingData.LineAoE == 0)
                {

                    //Debug.Log("HoveredPos: " + playerHighlightingData.HoveredPosition);
                    //use Arc Line
                    if (playerHighlightingData.HoveredPosition != Vector3.zero)
                    {
                        //Debug.Log("UpdateArc: " + playerHighlightingData.HoveredPosition);
                        UpdateArcLineRenderer(playerHighlightingData.LineYOffset, playerHighlightingData.HoveredPosition, lineRendererComp);
                    }
                    else// if (mouseState.CurrentState != MouseState.State.Neutral)
                        lineRendererComp.lineRenderer.positionCount = 0;
                }
                else
                {
                    Debug.Log("SetLineRendererpositionCountTo 0");
                    lineRendererComp.lineRenderer.positionCount = 0;
                }
                
            }
        });
        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        playerStates.Dispose();
        playerHighlightingDatas.Dispose();
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
        Color pathColorFaded = new Color(inLineRendererComp.pathColor.r, inLineRendererComp.pathColor.g, inLineRendererComp.pathColor.b, 0.1f);
        inLineRendererComp.lineRenderer.startColor = inLineRendererComp.pathColor;
        inLineRendererComp.lineRenderer.endColor = pathColorFaded;

        inLineRendererComp.lineRenderer.positionCount = inPath.CellAttributes.Count + 1;
        inLineRendererComp.lineRenderer.SetPosition(0, inLineRendererComp.transform.position + inLineRendererComp.pathOffset);

        for (int pi = 1; pi <= inPath.CellAttributes.Count; pi++)
        {
             inLineRendererComp.lineRenderer.SetPosition(pi, Vector3fext.ToUnityVector(inPath.CellAttributes[pi - 1].Position) + inLineRendererComp.pathOffset);
        }
    }

    void UpdateArcLineRenderer(float targetYOffset, Vector3 inTarget, LineRendererComponent inLineRendererComp)
    {
        Color arcColorFaded = new Color(inLineRendererComp.arcColor.r, inLineRendererComp.arcColor.g, inLineRendererComp.arcColor.b, 0.1f);
        inLineRendererComp.lineRenderer.startColor = arcColorFaded;
        inLineRendererComp.lineRenderer.endColor = inLineRendererComp.arcColor;

        Vector3 arcOrigin = inLineRendererComp.transform.position + inLineRendererComp.arcOffset;
        Vector3 arcTarget = new Vector3(inTarget.x, inTarget.y + targetYOffset, inTarget.z);

        Vector3[] arcPath = CalculateSinusPath(arcOrigin, arcTarget, 1);
        inLineRendererComp.lineRenderer.positionCount = arcPath.Length;

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

    public void ResetHighlights()
    {
        if (m_PlayerStateData.CalculateEntityCount() == 0)
            return;

        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerHighlightingDatas = m_PlayerStateData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

        HashSet<long> unitIdHash = new HashSet<long>(playerStates[0].UnitTargets.Keys);

        Entities.With(m_ActiveUnitData).ForEach((Entity e, LineRendererComponent lineRendererComp, ref SpatialEntityId unitId, ref Actions.Component actions) =>
        {
            //actions.CurrentSelected condition prevents line from being cleared instantly if invalid target is selected / rightclick action deselect is used
            if (actions.LockedAction.Index == -3 && actions.CurrentSelected.Index == -3)
            {
                if (unitIdHash.Contains(unitId.EntityId.Id) && playerHighlightingDatas[0].TargetRestrictionIndex != 2)
                {
                    ResetUnitHighLights(e, playerStates[0], unitId.EntityId.Id);
                    playerStates[0].UnitTargets.Remove(unitId.EntityId.Id);
                }
            }
        });

        Entities.With(m_MarkerStateData).ForEach((ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
        {
            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
            }
            if (markerState.CurrentState != (MarkerState.State)(int)mouseState.CurrentState)
            {
                markerState.CurrentState = (MarkerState.State)(int)mouseState.CurrentState;
                markerState.IsSet = 0;
            }
        });

        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        playerStates.Dispose();
        playerHighlightingDatas.Dispose();
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

        Entities.With(m_MarkerStateData).ForEach((ref MarkerState markerState, ref MouseState mouseState, ref CubeCoordinate.Component coord) =>
        {
            if (coordHash.Contains(coord.CubeCoordinate) && markerState.NumberOfTargets > 0)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                markerState.NumberOfTargets -= 1;
            }
        });
    }

    public void HighlightReachable()
    {
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);

        //remove selected unit target from player UnitTargets Dict 
        if (playerStates[0].UnitTargets.ContainsKey(playerStates[0].SelectedUnitId))
        {
            playerStates[0].UnitTargets.Remove(playerStates[0].SelectedUnitId);
            //m_PlayerStateData.PlayerState[0] = playerState;
        }
        m_PlayerStateData.CopyFromComponentDataArray(playerStates);

        Entities.With(m_MarkerStateData).ForEach((ref MarkerState marker, ref CubeCoordinate.Component coord) =>
        {
            if (marker.CurrentState == MarkerState.State.Hovered || marker.CurrentState == MarkerState.State.Clicked)
            {
                marker.CurrentState = MarkerState.State.Neutral;
                marker.IsSet = 0;
            }

            foreach (CellAttribute c in playerStates[0].CellsInRange)
            {
                if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(c.CubeCoordinate))
                {
                    marker.CurrentState = MarkerState.State.Reachable;
                    marker.IsSet = 0;
                }
            }
        });

        playerStates.Dispose();
    }

    public void ClearPlayerState()
    {
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
        var playerState = playerStates[0];
        playerState.CellsInRange.Clear();
        playerState.CachedPaths.Clear();
        playerState.CellsInRange = playerState.CellsInRange;
        playerState.CachedPaths = playerState.CachedPaths;
        playerStates[0] = playerState;

        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        playerStates.Dispose();

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

    public void SetSelfTarget(long entityID)
    {
        var playerStates = m_PlayerStateData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);

        playerStates[0].UnitTargets.Remove(entityID);

        Entities.With(m_ActiveUnitData).ForEach((LineRendererComponent lineRendererComp, ref SpatialEntityId iD, ref CubeCoordinate.Component coord) =>
        {
            if (iD.EntityId.Id == entityID)
            {
                lineRendererComp.lineRenderer.positionCount = 0;
                CubeCoordinateList list = new CubeCoordinateList(new List<Vector3f> { coord.CubeCoordinate });
                playerStates[0].UnitTargets.Add(iD.EntityId.Id, list);
            }
        });

        playerStates[0].CellsInRange.Clear();
        playerStates[0].CachedPaths.Clear();

        m_PlayerStateData.CopyFromComponentDataArray(playerStates);
        SetNumberOfTargets(playerStates[0]);
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