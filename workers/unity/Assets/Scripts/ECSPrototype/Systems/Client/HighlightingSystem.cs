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
        //public readonly ComponentDataArray<Actions.Component> ActionsData;
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
        public ComponentDataArray<MarkerState> MarkerStateData;
    }

    [Inject]
    private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<MouseState> MouseStates;
        public readonly ComponentDataArray<Health.Component> HealthAttributes;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
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

            for (int c = 0; c < m_CellData.Length; c++)
            {
                var cellCoord = m_CellData.Coords[c].CubeCoordinate;
                var cellmouseState = m_CellData.MouseStates[c].CurrentState;
                var cellMarkerState = m_CellData.MarkerStateData[c];

                if (cellMarkerState.CurrentState == MarkerState.State.Reachable)
                {
                    if(cellmouseState == MouseState.State.Hovered)
                    {
                        hoveredCoord = cellCoord;
                        if (cellMarkerState.IsTarget == 0)
                            cellMarkerState.IsTarget = 1;
                    }
                    else if (cellmouseState != MouseState.State.Clicked)
                    {
                        if (cellMarkerState.IsTarget == 1)
                        {
                            cellMarkerState.IsTarget = 0;
                        }
                    }
                }
                m_CellData.MarkerStateData[c] = cellMarkerState;
            }

            for(int i = 0; i < m_ActiveUnitData.Length; i++)
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
                    List<CellAttributes> Area = new List<CellAttributes>();
                    uint targettingRange = (uint)Target.targettingRange;
                    switch (Target.HighlighterToUse)
                    {
                        case ECSActionTarget.HighlightDef.Radius:

                            if (playerState.CellsInRange.Count == 0)
                            {
                                List<CellAttributes> go = m_CellGrid.GetRadius(occCoord, targettingRange, worldIndex);
                                foreach(CellAttributes c in go)
                                {
                                    playerState.CellsInRange.Add(c.Cell);
                                }
                                m_PlayerStateData.PlayerState[0] = playerState;
                                HighlightReachable();
                            }
                            //use arcing linerenderer
                            break;
                        case ECSActionTarget.HighlightDef.Path:

                            if (playerState.CellsInRange.Count == 0)
                            {
                                List<CellAttributes> go = m_CellGrid.GetRadius(occCoord, targettingRange, worldIndex);
                                playerState.CachedPaths = m_CellGrid.GetAllPathsInRadius(targettingRange, go, occCoord);
                                foreach(CellAttribute key in playerState.CachedPaths.Keys)
                                {
                                    playerState.CellsInRange.Add(key);
                                }
                                m_PlayerStateData.PlayerState[0] = playerState;
                                HighlightReachable();
                            }
                            //use path linerenderer
                            break;
                    }
                    HandleMods(occCoord, hoveredCoord, Target.SecondaryTargets, out Area, worldIndex, lineRendererComp);
                }
            }
        }
        else {
            ResetHighlights();
        }
    }

    public void HandleMods(Vector3f occupiedCoord, Vector3f hoveringCoord, List<ECSActionSecondaryTargets> inMods, out List<CellAttributes> Radius, uint windex, LineRendererComponent inLineRendererComp)
    {
        CellAttributeList Path = new CellAttributeList();
        Radius = new List<CellAttributes>();

        foreach(ECSActionSecondaryTargets t in inMods)
        {
            if(t is SecondaryAoE)
            {
                SecondaryAoE go = t as SecondaryAoE;
                Radius = m_CellGrid.GetRadius(hoveringCoord, (uint)go.areaSize, windex);
            }
            if(t is SecondaryPath)
            {
                Path = m_CellGrid.FindPath(hoveringCoord, m_PlayerStateData.PlayerState[0].CachedPaths);
                
                if(Path.CellAttributes.Count > 0)
                {
                    Debug.Log(Path.CellAttributes[Path.CellAttributes.Count - 1].CubeCoordinate);
                    UpdateLineRenderer(Path, inLineRendererComp);
                }
            }
        }

    }

    void UpdateLineRenderer(CellAttributeList inPath, LineRendererComponent inLineRendererComp)
    {
        inLineRendererComp.lineRenderer.positionCount = inPath.CellAttributes.Count + 1;
        inLineRendererComp.lineRenderer.SetPosition(0, inLineRendererComp.transform.position + inLineRendererComp.offset);

        for (int pi = 1; pi <= inPath.CellAttributes.Count; pi++)
        {
            inLineRendererComp.lineRenderer.SetPosition(pi, inPath.CellAttributes[pi - 1].Position.ToUnityVector() + inLineRendererComp.offset);
        }
    }

    void UpdateLineRenderer(Vector3 inTarget, LineRendererComponent inLineRendererComp)
    {
        /*
        inLineRendererComp.lineRenderer.enabled = true;
        inLineRendererComp.lineRenderer.positionCount = inPath.CellAttributes.Count + 1;
        inLineRendererComp.lineRenderer.SetPosition(0, inLineRendererComp.transform.position + inLineRendererComp.offset);

        for (int pi = 1; pi <= inPath.CellAttributes.Count; pi++)
        {
            inLineRendererComp.lineRenderer.SetPosition(pi, inPath.CellAttributes[pi - 1].Position.ToUnityVector() + inLineRendererComp.offset);
        }
        */
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
        for (int a = 0; a < m_CellData.Length; a++)
        {
            var playerState = m_PlayerStateData.PlayerState[0];
            var cellMarker = m_CellData.MarkerStateData[a];
            var cellCoord = m_CellData.Coords[a].CubeCoordinate;

            //TODO: save unit / targets dictionary on player and use that to set IsTarget on cellMarkers
            if (cellMarker.IsTarget == 1)
            {
                cellMarker.IsTarget = 0;
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
}

