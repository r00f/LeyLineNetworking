using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using System;
using LeyLineHybridECS;
using Generic;
using Unit;
using Cell;
using Player;
using Improbable.Gdk.ReactiveComponents;
using Improbable;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(PlayerStateSystem)), UpdateAfter(typeof(SendActionRequestSystem)), UpdateAfter(typeof(GameStateSystem))]
public class HighlightingSystem : ComponentSystem
{
    public struct ActiveUnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentArray<Unit_BaseDataSet> BaseDataSets;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<SpatialEntityId> IDs;
        public readonly ComponentDataArray<MouseState> MouseStates;
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

    [Inject]
    HandleCellGridRequestsSystem m_CellGrid;

    ECSActionTarget Target = null;

    protected override void OnUpdate()
    {
        var playerState = m_PlayerStateData.PlayerState[0];
        var playerWorldIndex = m_PlayerStateData.WorldIndex[0].Value;
        var playerHighlightingData = m_PlayerStateData.HighlightingData[0];

        for (int i = 0; i < m_GameStateData.Length; i++)
        {
            var gameStateWorldIndex = m_GameStateData.WorldIndex[i].Value;
            var gameState = m_GameStateData.GameState[i].CurrentState;

            if(playerWorldIndex == gameStateWorldIndex)
            {
                if(gameState != GameStateEnum.planning)
                {
                    for (int c = 0; c < m_CellData.Length; c++)
                    {
                        var cellMarkerState = m_CellData.MarkerStateData[c];

                        if (cellMarkerState.IsTarget == 1)
                            cellMarkerState.IsTarget = 0;

                        m_CellData.MarkerStateData[c] = cellMarkerState;
                    }

                    for (int u = 0; u < m_ActiveUnitData.Length; u++)
                    {
                        var lineRenderer = m_ActiveUnitData.LineRenderers[u];
                        ClearLineRenderer(lineRenderer);
                    }
                }
            }
        }

        if(playerState.CurrentState == PlayerStateEnum.waiting_for_target)
        {
            //set coord to something that can never be a cubeCooord on the map
            Vector3f hoveredCoord = new Vector3f(999,999,999);
            Vector3 hoveredPosition = new Vector3();
            float hoveredOffset = 0;

            for (int i = 0; i < m_ActiveUnitData.Length; i++)
            {
                var iD = m_ActiveUnitData.IDs[i].EntityId.Id;
                var actions = m_ActiveUnitData.BaseDataSets[i];
                var occCoord = m_ActiveUnitData.Coords[i].CubeCoordinate;
                var worldIndex = m_ActiveUnitData.WorldIndexData[i].Value;
                var mouseStateClick = m_ActiveUnitData.MouseStates[i].ClickEvent;
                int actionID = playerState.SelectedActionId;
                var lineRendererComp = m_ActiveUnitData.LineRenderers[i];

                if (iD == playerState.SelectedUnitId)
                {
                    #region Gather target information into HighlightingDataComponent

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

                    #endregion

                    #region Execute with gathered Data

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
                            m_PlayerStateData.PlayerState[0] = playerState;
                            HighlightReachable();
                        }
                        //use path linerenderer
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
                        //use arcing linerenderer
                    }

                    if (playerHighlightingData.IsUnitTarget == 1)
                    {
                        //if target wants a Unit, loop over units to find which one is hovered
                        for (int u = 0; u < m_UnitData.Length; u++)
                        {
                            var position = m_UnitData.Transforms[u].position;
                            var unitCoord = m_UnitData.Coords[u].CubeCoordinate;
                            var unitMouseState = m_UnitData.MouseStates[u].CurrentState;
                            var unitClickEvent = m_UnitData.MouseStates[u].ClickEvent;
                            var hoveredLineRendererComp = m_UnitData.LineRenderers[u];
                            //var cellMarkerState = m_UnitData.MarkerStateData[c];

                            //if (cellMarkerState.CurrentState == MarkerState.State.Reachable)
                            //{
                            if (unitMouseState == MouseState.State.Hovered)
                            {
                                hoveredCoord = unitCoord;
                                hoveredPosition = position;
                                hoveredOffset = hoveredLineRendererComp.arcOffset.y;
                                /*
                                    if (cellMarkerState.IsTarget == 0)
                                        cellMarkerState.IsTarget = 1;
                                */
                            }
                            else if (unitMouseState != MouseState.State.Clicked)
                            {
                                //check if cell to reset is not a target of another unit
                                bool contains = false;

                                foreach (long l in playerState.UnitTargets.Keys)
                                {
                                    if (l != playerState.SelectedUnitId)
                                    {
                                        foreach (Vector3f coord in playerState.UnitTargets[l].CubeCoordinates)
                                        {
                                            if (unitCoord == coord)
                                            {
                                                contains = true;
                                            }
                                        }
                                    }
                                }
                                /*
                                if (cellMarkerState.IsTarget == 1 && !contains)
                                {
                                    cellMarkerState.IsTarget = 0;
                                }
                                */
                            }
                            else if (unitClickEvent == 1)
                            {
                                hoveredCoord = unitCoord;
                                hoveredPosition = position;
                                hoveredOffset = hoveredLineRendererComp.arcOffset.y;
                                var targetList = new CubeCoordinateList(new List<Vector3f>());
                                targetList.CubeCoordinates.Add(hoveredCoord);

                                if (!playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
                                {
                                    playerState.UnitTargets.Add(playerState.SelectedUnitId, targetList);
                                }

                                playerState.UnitTargets = playerState.UnitTargets;
                                m_PlayerStateData.PlayerState[0] = playerState;

                                foreach (CubeCoordinateList l in playerState.UnitTargets.Values)
                                {
                                    foreach (Vector3f v in l.CubeCoordinates)
                                    {
                                        /*
                                            if (v == unitCoord && cellMarkerState.IsTarget == 0)
                                            {
                                                cellMarkerState.IsTarget = 1;
                                            }
                                        }
                                        */
                                    }
                                }
                            }
                            else
                            {
                                //lineRendererComp.lineRenderer.positionCount = 0;
                            }
                        }
                        //}

                    }
                    else
                    {
                        //if target wants a Cell, loop over cells to find which one is hovered
                        for (int c = 0; c < m_CellData.Length; c++)
                        {
                            var position = m_CellData.Transforms[c].position;
                            var cellCoord = m_CellData.Coords[c].CubeCoordinate;
                            var cellmouseState = m_CellData.MouseStates[c].CurrentState;
                            var cellClickEvent = m_CellData.MouseStates[c].ClickEvent;
                            var cellMarkerState = m_CellData.MarkerStateData[c];


                            if (cellMarkerState.CurrentState == MarkerState.State.Reachable)
                            {
                                if (cellmouseState == MouseState.State.Hovered)
                                {
                                    hoveredCoord = cellCoord;
                                    hoveredPosition = position;
                                }
                            }

                            foreach (long l in playerState.UnitTargets.Keys)
                            {
                                if (l == playerState.SelectedUnitId)
                                {
                                    foreach (Vector3f coord in playerState.UnitTargets[l].CubeCoordinates)
                                    {
                                        if (cellCoord == coord)
                                        {
                                            if (cellMarkerState.IsTarget == 0)
                                                cellMarkerState.IsTarget = 1;
                                        }
                                    }
                                }
                            }

                            //check if cell to reset is not a target of another unit
                            bool contains = false;

                            foreach (long l in playerState.UnitTargets.Keys)
                            {
                                foreach (Vector3f coord in playerState.UnitTargets[l].CubeCoordinates)
                                {
                                    if (cellCoord == coord)
                                    {
                                        contains = true;
                                    }
                                }
                            }

                            if (cellMarkerState.IsTarget == 1 && !contains)
                            {
                                cellMarkerState.IsTarget = 0;
                            }

                            m_CellData.MarkerStateData[c] = cellMarkerState;
                        }
                    }

                    if(playerHighlightingData.PathLine == 1)
                    {
                        //use path Line
                        CellAttributeList Path = new CellAttributeList();
                        Path = m_CellGrid.FindPath(hoveredCoord, m_PlayerStateData.PlayerState[0].CachedPaths);

                        if (Path.CellAttributes.Count > 0)
                        {
                            //Debug.Log(Path.CellAttributes[Path.CellAttributes.Count - 1].CubeCoordinate);
                            UpdatePathLineRenderer(Path, lineRendererComp);
                        }
                    }
                    else
                    {
                        //use Arc Line
                        if(hoveredPosition != Vector3.zero)
                            UpdateArcLineRenderer(hoveredOffset, hoveredPosition, lineRendererComp);
                    }

                    if (playerHighlightingData.AoERadius > 0)
                    {
                        List<CellAttributes> Area = new List<CellAttributes>();
                        //aoe highlighting code stuff
                        Area = m_CellGrid.GetRadius(hoveredCoord, playerHighlightingData.AoERadius, worldIndex);
                        CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());

                        for(int c = 0; c < m_CellData.Length; c++)
                        {
                            var cellMarkerState = m_CellData.MarkerStateData[c];
                            var cellCoord = m_CellData.Coords[c].CubeCoordinate;

                            foreach (CellAttributes cell in Area)
                            {
                                if(cell.Cell.CubeCoordinate == cellCoord)
                                {
                                    cubeCoordList.CubeCoordinates.Add(cell.Cell.CubeCoordinate);
                                }
                            }
                        }
                        //prevent from setting list if aoeRadius is 0

                        if (cubeCoordList.CubeCoordinates.Count != 1)
                        {
                            playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
                            playerState.UnitTargets = playerState.UnitTargets;
                            m_PlayerStateData.PlayerState[0] = playerState;
                            //Debug.Log("Set unitTarget list with length: " + playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates.Count);
                        }
                    }
                    else
                    {
                        CubeCoordinateList cubeCoordList = new CubeCoordinateList(new List<Vector3f>());
                        for (int c = 0; c < m_CellData.Length; c++)
                        {
                            var cellMarkerState = m_CellData.MarkerStateData[c];
                            var cellCoord = m_CellData.Coords[c].CubeCoordinate;

                            if (hoveredCoord == cellCoord)
                            {
                                cubeCoordList.CubeCoordinates.Add(cellCoord);
                            }
                        }

                        playerState.UnitTargets[playerState.SelectedUnitId] = cubeCoordList;
                        playerState.UnitTargets = playerState.UnitTargets;
                        m_PlayerStateData.PlayerState[0] = playerState;
                    }

                    #endregion

                }
            }
        }
        else {
            ResetHighlights();
        }
    }

    public void HandleMods(List<ECSActionSecondaryTargets> inMods, ref HighlightingDataComponent highLightingData)
    {
        highLightingData.PathLine = 0;
        highLightingData.AoERadius = 0;

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
        float[] ypositions = CalculateSinusPoints(inLineRendererComp.lineRenderer.positionCount);
        float xstep = 1.0f / inLineRendererComp.lineRenderer.positionCount;

        for (int i = 0; i < ypositions.Length; i++)
        {
            float sinYpos = ypositions[i] * zenithHeight + (inLineRendererComp.arcOffset.y + (targetYOffset * (xstep * i)) - (inLineRendererComp.arcOffset.y * (xstep * i)));
            inLineRendererComp.lineRenderer.SetPosition(i, inLineRendererComp.transform.position + new Vector3(distance.x * (xstep * i), sinYpos, distance.z * (xstep * i)));

        }
    }

    void ClearLineRenderer(LineRendererComponent inLineRendererComp)
    {
        inLineRendererComp.lineRenderer.positionCount = 0;
    }

    public void ResetHighlights()
    {
        for(int i = 0; i< m_CellData.Length; i++)
        {
            var markerState = m_CellData.MarkerStateData[i];
            var cellMouseState = m_CellData.MouseStates[i];
            var cubeCoord = m_CellData.Coords[i].CubeCoordinate;

            if (markerState.CurrentState != (MarkerState.State)(int)cellMouseState.CurrentState)
            {
                markerState.CurrentState = (MarkerState.State)(int)cellMouseState.CurrentState;
                markerState.IsSet = 0;
                m_CellData.MarkerStateData[i] = markerState;
            }
        }
    }

    public void HighlightReachable()
    {
        var playerState = m_PlayerStateData.PlayerState[0];
        List<Vector3f> targetsToClear = new List<Vector3f>();

        //remove target from player UnitTargets Dict
        if (playerState.UnitTargets.ContainsKey(playerState.SelectedUnitId))
        {
            targetsToClear = playerState.UnitTargets[playerState.SelectedUnitId].CubeCoordinates;
            playerState.UnitTargets.Remove(playerState.SelectedUnitId);
            playerState.UnitTargets = playerState.UnitTargets;
            m_PlayerStateData.PlayerState[0] = playerState;
        }

        for (int a = 0; a < m_CellData.Length; a++)
        {
            var cellMarker = m_CellData.MarkerStateData[a];
            var cellCoord = m_CellData.Coords[a].CubeCoordinate;

            //reset IsTarget if this unit had one

            foreach (Vector3f coord in targetsToClear)
            {
                if (cellCoord == coord)
                {
                    cellMarker.IsTarget = 0;
                }
            }

            foreach (CellAttribute c in playerState.CellsInRange)
            {
                if (cellCoord == c.CubeCoordinate)
                {
                    cellMarker.CurrentState = MarkerState.State.Reachable;
                    cellMarker.IsSet = 0;
                }
            }
            m_CellData.MarkerStateData[a] = cellMarker;
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
                ClearLineRenderer(lineRendererComp);
            }
        }
        ResetHighlights();
        playerstate.CellsInRange.Clear();
        playerstate.CachedPaths.Clear();
        m_PlayerStateData.PlayerState[0] = playerstate;
        Target = null;
    }

    public float[] CalculateSinusPoints(int numberOfPositions)
    {
        float[] yPosArray = new float[numberOfPositions];
        float xStep = 1.0f / numberOfPositions;
        int i = 0;
        for (float x = 0.0f; x < 1.0f; x += xStep)
        {
            if (i < yPosArray.Length)
            {
                yPosArray[i] = (float)Math.Sin(x * Math.PI);
                i++;
            }
        }
        return yPosArray;
    }

}

