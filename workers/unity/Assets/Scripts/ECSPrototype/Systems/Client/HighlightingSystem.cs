using Cell;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using LeyLineHybridECS;
using Player;
using System;
using System.Collections.Generic;
using Unit;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(PlayerStateSystem)), UpdateAfter(typeof(SendActionRequestSystem)), UpdateAfter(typeof(GameStateSystem))]
public class HighlightingSystem : ComponentSystem
{
    public struct ActiveUnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Unit_BaseDataSet> BaseDataSets;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<SpatialEntityId> IDs;
        public ComponentDataArray<MouseState> MouseStates;
        public ComponentArray<LineRendererComponent> LineRenderers;


    }

    [Inject]
    ActiveUnitData m_ActiveUnitData;



    public struct PlayerStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
        public ComponentDataArray<HighlightingDataComponent> HighlightingData;
        public ComponentDataArray<PlayerState.Component> PlayerState;
    }

    [Inject]
    PlayerStateData m_PlayerStateData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndex;
        public readonly ComponentDataArray<GameState.Component> GameState;

    }
    [Inject]
    GameStateData m_GameStateData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentArray<Transform> Transforms;
        public ComponentDataArray<MarkerState> MarkerStateData;
    }

    [Inject]
    private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<Health.Component> HealthAttributes;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentArray<LineRendererComponent> LineRenderers;
        public readonly ComponentArray<Transform> Transforms;
    }

    [Inject]
    private UnitData m_UnitData;

    public struct MarkerStateData
    {
        public readonly int Length;
        public ComponentDataArray<MarkerState> MarkerStates;
        public ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
    }

    [Inject]
    private MarkerStateData m_MarkerStateData;

    [Inject]
    HandleCellGridRequestsSystem m_CellGrid;

    ECSActionTarget Target = null;

    protected override void OnUpdate()
    {
        var playerState = m_PlayerStateData.PlayerState[0];
        var playerWorldIndex = m_PlayerStateData.WorldIndex[0].Value;
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];
        int actionID = playerState.SelectedActionId;

        HashSet<Vector3f> coordsInRangeHash = new HashSet<Vector3f>();

        foreach(CellAttribute c in playerState.CellsInRange)
        {
            coordsInRangeHash.Add(c.CubeCoordinate);
        }

        for (int j = 0; j < m_GameStateData.Length; j++)
        {
            var gameStateWorldIndex = m_GameStateData.WorldIndex[j].Value;
            var gameState = m_GameStateData.GameState[j].CurrentState;

            if(playerWorldIndex == gameStateWorldIndex)
            {

                if (gameState != GameStateEnum.planning)
                {
                    #region ResetMarkers

                    for (int m = 0; m < m_MarkerStateData.Length; m++)
                    {
                        var markerState = m_MarkerStateData.MarkerStates[m];

                        if (markerState.IsTarget == 1)
                            markerState.IsTarget = 0;

                        m_MarkerStateData.MarkerStates[m] = markerState;
                    }

                    for (int u = 0; u < m_ActiveUnitData.Length; u++)
                    {
                        var lineRenderer = m_ActiveUnitData.LineRenderers[u];
                        lineRenderer.lineRenderer.positionCount = 0;
                    }
                    #endregion
                }
                else
                {
                    if (playerState.CurrentState == PlayerStateEnum.waiting_for_target)
                    {
                        #region GetHovered
                        float hoveredOffset = 0;
                        playerHighlightingData.HoveredCoordinate = new Vector3f(999, 999, 999);
                        playerHighlightingData.HoveredPosition = new Vector3(0, 0, 0);
                        //set hovered position / cubeCoordinate

                        for (int m = 0; m < m_MarkerStateData.Length; m++)
                        {
                            var coord = m_MarkerStateData.Coords[m].CubeCoordinate;
                            var markerState = m_MarkerStateData.MarkerStates[m];
                            var mouseState = m_MarkerStateData.MouseStates[m].CurrentState;

                            if (coordsInRangeHash.Contains(coord))
                            {
                                if (playerHighlightingData.IsUnitTarget == 1)
                                {
                                    if (markerState.IsUnit == 1)
                                    {
                                        if ((mouseState == MouseState.State.Hovered || mouseState == MouseState.State.Clicked) && coord != playerHighlightingData.HoveredCoordinate)
                                        {
                                            Vector2 XZ = m_CellGrid.CubeCoordToXZ(coord);
                                            playerHighlightingData.HoveredCoordinate = coord;
                                            playerHighlightingData.HoveredPosition = new Vector3(XZ.x, 3, XZ.y);
                                        }
                                    }
                                }
                                else
                                {
                                    if (markerState.IsUnit == 0)
                                    {
                                        if ((mouseState == MouseState.State.Hovered || mouseState == MouseState.State.Clicked) && coord != playerHighlightingData.HoveredCoordinate)
                                        {
                                            Vector2 XZ = m_CellGrid.CubeCoordToXZ(coord);
                                            playerHighlightingData.HoveredCoordinate = coord;
                                            playerHighlightingData.HoveredPosition = new Vector3(XZ.x, 3, XZ.y);
                                        }
                                    }
                                }

                            }

                            bool otherUnitTarget = false;

                            foreach (long l in playerState.UnitTargets.Keys)
                            {
                                HashSet<Vector3f> targetCoordsHash = new HashSet<Vector3f>(playerState.UnitTargets[l].CubeCoordinates);
                                if (l != playerState.SelectedUnitId)
                                {
                                    if (targetCoordsHash.Contains(coord))
                                    {
                                        otherUnitTarget = true;
                                    }
                                }
                            }

                            foreach (long l in playerState.UnitTargets.Keys)
                            {
                                HashSet<Vector3f> targetCoordsHash = new HashSet<Vector3f>(playerState.UnitTargets[l].CubeCoordinates);

                                if (l == playerState.SelectedUnitId)
                                {
                                    if (targetCoordsHash.Contains(coord))
                                    {
                                        markerState.IsTarget = 1;
                                    }
                                    else if (otherUnitTarget == false)
                                    {
                                        markerState.IsTarget = 0;
                                    }
                                }
                            }

                            m_MarkerStateData.MarkerStates[m] = markerState;
                            m_PlayerStateData.HighlightingData[0] = playerHighlightingData;
                        }
                        #endregion

                        for (int i = 0; i < m_ActiveUnitData.Length; i++)
                        {
                            var iD = m_ActiveUnitData.IDs[i].EntityId.Id;
                            var actions = m_ActiveUnitData.BaseDataSets[i];
                            var occCoord = m_ActiveUnitData.Coords[i].CubeCoordinate;
                            var worldIndex = m_ActiveUnitData.WorldIndexData[i].Value;
                            var mouseState = m_ActiveUnitData.MouseStates[i];
                            var lineRendererComp = m_ActiveUnitData.LineRenderers[i];

                            if (iD == playerState.SelectedUnitId)
                            {
                                if(mouseState.CurrentState == MouseState.State.Clicked)
                                {
                                    mouseState.CurrentState = MouseState.State.Neutral;
                                    m_ActiveUnitData.MouseStates[i] = mouseState;
                                }

                                #region Highlight Reachable
                                if (playerHighlightingData.PathingRange == 1)
                                {
                                    if (playerState.CellsInRange.Count == 0)
                                    {
                                        List<CellAttributes> go = m_CellGrid.GetRadius(occCoord, playerHighlightingData.Range, worldIndex);
                                        playerState.CachedPaths = m_CellGrid.GetAllPathsInRadius(playerHighlightingData.Range, go, occCoord);

                                        foreach (CellAttribute key in playerState.CachedPaths.Keys)
                                        {
                                            playerState.CellsInRange.Add(key);
                                        }

                                        playerState.CellsInRange = playerState.CellsInRange;
                                        m_PlayerStateData.PlayerState[0] = playerState;
                                        HighlightReachable();
                                    }
                                }
                                else
                                {
                                    if (playerState.CellsInRange.Count == 0)
                                    {
                                        List<CellAttributes> go = m_CellGrid.GetRadius(occCoord, playerHighlightingData.Range, worldIndex);
                                        foreach (CellAttributes c in go)
                                        {
                                            playerState.CellsInRange.Add(c.Cell);
                                        }
                                        m_PlayerStateData.PlayerState[0] = playerState;
                                        HighlightReachable();
                                    }
                                }
                                #endregion

                                #region Update Visuals
                                if (playerHighlightingData.HoveredCoordinate != playerHighlightingData.LastHoveredCoordinate)
                                {
                                    if (playerHighlightingData.AoERadius > 0)
                                    {
                                        List<Vector3f> area = new List<Vector3f>();
                                        area = m_CellGrid.CircleDraw(playerHighlightingData.HoveredCoordinate, playerHighlightingData.AoERadius);
                                        CubeCoordinateList cubeCoordList = new CubeCoordinateList(area);

                                        //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
                                        if (cubeCoordList.CubeCoordinates.Count != 1 && playerHighlightingData.HoveredCoordinate != new Vector3f(999, 999, 999))
                                        {
                                            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
                                            playerState.UnitTargets = playerState.UnitTargets;
                                        }
                                    }
                                    else if (playerHighlightingData.RingRadius > 0)
                                    {
                                        List<Vector3f> ring = new List<Vector3f>();
                                        ring = m_CellGrid.RingDraw(playerHighlightingData.HoveredCoordinate, playerHighlightingData.RingRadius);
                                        CubeCoordinateList cubeCoordList = new CubeCoordinateList(ring);

                                        //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
                                        if (cubeCoordList.CubeCoordinates.Count != 1 && playerHighlightingData.HoveredCoordinate != new Vector3f(999, 999, 999))
                                        {
                                            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
                                            playerState.UnitTargets = playerState.UnitTargets;
                                        }
                                    }
                                    else if (playerHighlightingData.LineAoE == 1)
                                    {
                                        CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
                                        cubeCoordList.CubeCoordinates = m_CellGrid.LineDraw(occCoord, playerHighlightingData.HoveredCoordinate);
                                        //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
                                        if (cubeCoordList.CubeCoordinates.Count != 1 && playerHighlightingData.HoveredCoordinate != new Vector3f(999, 999, 999))
                                        {
                                            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
                                            playerState.UnitTargets = playerState.UnitTargets;
                                        }
                                    }
                                    else
                                    {
                                        CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
                                        cubeCoordList.CubeCoordinates.Add(playerHighlightingData.HoveredCoordinate);
                            
                                        //workaround for target still being set when we should not be in the waiting_for_target playerstate anymore
                                        if (playerHighlightingData.HoveredCoordinate != new Vector3f(999, 999, 999))
                                        {
                                            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
                                            playerState.UnitTargets = playerState.UnitTargets;
                                        }
                                        else if(playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
                                        {
                                            playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Clear();
                                            playerState.UnitTargets = playerState.UnitTargets;
                                        }
                                    }

                                    if (playerHighlightingData.IsUnitTarget == 1)
                                    {
                                        //if target wants a Unit, loop over units to find which one is hovered
                                        for (int u = 0; u < m_UnitData.Length; u++)
                                        {
                                            var unitCoord = m_UnitData.Coords[u].CubeCoordinate;
                                            var hoveredLineRendererComp = m_UnitData.LineRenderers[u];

                                            if (unitCoord == playerHighlightingData.HoveredCoordinate)
                                            {
                                                hoveredOffset = hoveredLineRendererComp.arcOffset.y;
                                            }
                                        }
                                    }

                                    if (playerHighlightingData.PathLine == 1)
                                    {
                                        CellAttributeList Path = new CellAttributeList();
                                        Path = m_CellGrid.FindPath(playerHighlightingData.HoveredCoordinate, m_PlayerStateData.PlayerState[0].CachedPaths);

                                        if (Path.CellAttributes.Count > 0)
                                        {
                                            UpdatePathLineRenderer(Path, lineRendererComp);
                                        }
                                    }

                                    else if (playerHighlightingData.LineAoE == 0)
                                    {
                                        //use Arc Line
                                        if (playerHighlightingData.HoveredPosition != Vector3.zero)
                                            UpdateArcLineRenderer(hoveredOffset, playerHighlightingData.HoveredPosition, lineRendererComp);
                                        else// if (mouseState.CurrentState != MouseState.State.Neutral)
                                            lineRendererComp.lineRenderer.positionCount = 0;
                                    }

                                    else
                                    {
                                        lineRendererComp.lineRenderer.positionCount = 0;
                                    }

                                    playerHighlightingData.LastHoveredCoordinate = playerHighlightingData.HoveredCoordinate;
                                    m_PlayerStateData.HighlightingData[0] = playerHighlightingData;
                                    m_PlayerStateData.PlayerState[0] = playerState;
                                }
                                #endregion
                            }
                        }
                    }
                    else
                    {
                        ResetHighlights();
                    }
                }
            }
        }

  
    }

    public void GatherHighlightingInformation(long unitID, int actionID)
    {
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var iD = m_ActiveUnitData.IDs[i].EntityId.Id;
            var actions = m_ActiveUnitData.BaseDataSets[i];

            if (iD == unitID)
            {
                if (Target == null)
                {
                    if (actionID < 0)
                    {
                        switch (actionID)
                        {
                            case -3:
                                break;
                            case -2:
                                Target = actions.BasicMove.Targets[0];
                                break;
                            case -1:
                                Target = actions.BasicAttack.Targets[0];
                                break;
                        }
                    }
                    else
                    {
                        if (actionID < actions.Actions.Count)
                        {
                            Target = actions.Actions[actionID].Targets[0];
                        }
                        else
                        {
                            Target = actions.SpawnActions[actionID - actions.Actions.Count].Targets[0];
                        }
                    }
                }

                playerHighlightingData.Range = (uint)Target.targettingRange;

                switch (Target.HighlighterToUse)
                {
                    case ECSActionTarget.HighlightDef.Radius:
                        playerHighlightingData.PathingRange = 0;
                        break;
                    case ECSActionTarget.HighlightDef.Path:
                        playerHighlightingData.PathingRange = 1;
                        break;
                }

                if (Target is ECSATarget_Unit)
                {
                    playerHighlightingData.IsUnitTarget = 1;
                }
                else if (Target is ECSATarget_Tile)
                {
                    playerHighlightingData.IsUnitTarget = 0;
                }

                HandleMods(Target.SecondaryTargets, ref playerHighlightingData);

                m_PlayerStateData.HighlightingData[0] = playerHighlightingData;
            }
        }
    }

    void HandleMods(List<ECSActionSecondaryTargets> inMods, ref HighlightingDataComponent highLightingData)
    {
        highLightingData.LineAoE = 0;
        highLightingData.PathLine = 0;
        highLightingData.AoERadius = 0;
        highLightingData.RingRadius = 0;

        foreach (ECSActionSecondaryTargets t in inMods)
        {
            if(t is SecondaryPath)
            {
                highLightingData.PathLine = 1;
            }
            else if (t is SecondaryArea)
            {
                SecondaryArea secAoE = t as SecondaryArea;
                highLightingData.AoERadius = (uint)secAoE.areaSize;
            } 
            else if( t is SecondaryLine)
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
            inLineRendererComp.lineRenderer.SetPosition(pi, inPath.CellAttributes[pi - 1].Position.ToUnityVector() + inLineRendererComp.pathOffset);
        }
    }

    void UpdateArcLineRenderer(float targetYOffset, Vector3 inTarget, LineRendererComponent inLineRendererComp)
    {
        Color arcColorFaded = new Color(inLineRendererComp.arcColor.r, inLineRendererComp.arcColor.g, inLineRendererComp.arcColor.b, 0.1f);
        inLineRendererComp.lineRenderer.startColor = arcColorFaded;
        inLineRendererComp.lineRenderer.endColor = inLineRendererComp.arcColor;

        Vector3 distance = ((inTarget + new Vector3 (0, targetYOffset, 0)) - (inLineRendererComp.transform.position + inLineRendererComp.arcOffset));
        uint numberOfSmoothPoints = 6 + (2*(uint)Mathf.RoundToInt(distance.magnitude)/2);
        float zenithHeight = 1;
        inLineRendererComp.lineRenderer.positionCount = 3 + (int)numberOfSmoothPoints;
        float[] ypositions = CalculateSinusPoints(inLineRendererComp.lineRenderer.positionCount - 1);
        float xstep = 1.0f / (inLineRendererComp.lineRenderer.positionCount - 1);

        for (int i = 0; i <= ypositions.Length; i++)
        {
            if(i == ypositions.Length)
            {
                inLineRendererComp.lineRenderer.SetPosition(i, inTarget + new Vector3(0, targetYOffset, 0));
            }
            else
            {
                float sinYpos = ypositions[i] * zenithHeight + (inLineRendererComp.arcOffset.y + (targetYOffset * (xstep * i)) - (inLineRendererComp.arcOffset.y * (xstep * i)));
                inLineRendererComp.lineRenderer.SetPosition(i, inLineRendererComp.transform.position + new Vector3(distance.x * (xstep * i), sinYpos, distance.z * (xstep * i)));
            }
        }
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
        var playerState = m_PlayerStateData.PlayerState[0];

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var actions = m_ActiveUnitData.ActionsData[i];
            var unitId = m_ActiveUnitData.IDs[i].EntityId.Id;
            var lineRenderer = m_ActiveUnitData.LineRenderers[i].lineRenderer;

            if (actions.LockedAction.Index == -3)
            {
                if (playerState.UnitTargets.ContainsKey(unitId))
                {
                    lineRenderer.positionCount = 0;
                    playerState.UnitTargets.Remove(unitId);
                    playerState.UnitTargets = playerState.UnitTargets;
                    m_PlayerStateData.PlayerState[0] = playerState;
                }
            }
        }

        for (int i = 0; i < m_MarkerStateData.Length; i++)
        {
            var markerState = m_MarkerStateData.MarkerStates[i];
            var mouseState = m_MarkerStateData.MouseStates[i];

            if (mouseState.CurrentState == MouseState.State.Clicked)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
                m_MarkerStateData.MouseStates[i] = mouseState;
            }

            if (markerState.CurrentState != (MarkerState.State)(int)mouseState.CurrentState)
            {
                markerState.CurrentState = (MarkerState.State)(int)mouseState.CurrentState;
                markerState.IsSet = 0;
                m_MarkerStateData.MarkerStates[i] = markerState;
            }
        }
    }

    public void HighlightReachable()
    {
        var playerState = m_PlayerStateData.PlayerState[0];
        List<Vector3f> targetsToClear = new List<Vector3f>();

        //remove selected unit target from player UnitTargets Dict 
        if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
        {
            targetsToClear = playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates;
            playerState.UnitTargets.Remove(playerState.SelectedUnitId);
            playerState.UnitTargets = playerState.UnitTargets;
            m_PlayerStateData.PlayerState[0] = playerState;
        }

        for (int i = 0; i < m_MarkerStateData.Length; i++)
        {
            var marker = m_MarkerStateData.MarkerStates[i];
            var coord = m_MarkerStateData.Coords[i].CubeCoordinate;

            if(marker.CurrentState == MarkerState.State.Hovered || marker.CurrentState == MarkerState.State.Clicked)
            {
                marker.CurrentState = MarkerState.State.Neutral;
                marker.IsSet = 0;
            }

            //reset IsTarget if this unit had one

            foreach (Vector3f c in targetsToClear)
            {
                if (coord == c)
                {
                    marker.IsTarget = 0;
                }
            }

            foreach (CellAttribute c in playerState.CellsInRange)
            {
                if (coord == c.CubeCoordinate)
                {
                    marker.CurrentState = MarkerState.State.Reachable;
                    marker.IsSet = 0;
                }
            }

            m_MarkerStateData.MarkerStates[i] = marker;
        }
    }

    public void ClearPlayerState()
    {
        UpdateInjectedComponentGroups();
        var playerstate = m_PlayerStateData.PlayerState[0];

        for (int i = 0; i < m_ActiveUnitData.Length; i++)
        {
            var iD = m_ActiveUnitData.IDs[i].EntityId.Id;
            var lineRendererComp = m_ActiveUnitData.LineRenderers[i];

            if(iD == playerstate.SelectedUnitId)
            {
                lineRendererComp.lineRenderer.positionCount = 0;
            }
        }
        ResetHighlights();
        playerstate.CellsInRange.Clear();
        playerstate.CachedPaths.Clear();
        m_PlayerStateData.PlayerState[0] = playerstate;
        Target = null;
    }
}

