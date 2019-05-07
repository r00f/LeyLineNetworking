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

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(PlayerStateSystem))]
public class HighlightingSystem : ComponentSystem
{
    public struct ActiveUnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<SpatialEntityId> IDs;
        public readonly ComponentDataArray<MouseState> MouseStates;

    }
    [Inject]
    ActiveUnitData m_ActiveUnitData;

    public struct PlayerStateData
    {
        public readonly int Length;
        public ComponentDataArray<PlayerState.Component> PlayerState;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
    }
    [Inject]
    PlayerStateData m_PlayerStateData;

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

    protected override void OnUpdate()
    {
        var playerState = m_PlayerStateData.PlayerState[0];
        if(playerState.CurrentState == PlayerStateEnum.waiting_for_target)
        {
            
            Vector3f hoveredCoord = new Vector3f();
            for (int i = 0; i< m_CellData.Length; i++)
            {
                var cellCoord = m_CellData.Coords[i].CubeCoordinate;
                var cellmouseState = m_CellData.MouseStates[i].CurrentState;
                if(cellmouseState == MouseState.State.Hovered)
                {
                    hoveredCoord = cellCoord;
                }
            }

            for(int i = 0; i < m_ActiveUnitData.Length; i++)
            {
                var iD = m_ActiveUnitData.IDs[i].EntityId.Id;
                var actions = m_ActiveUnitData.ActionsData[i];
                var occCoord = m_ActiveUnitData.Coords[i].CubeCoordinate;
                var worldIndex = m_ActiveUnitData.WorldIndexData[i].Value;
                var mouseStateClick = m_ActiveUnitData.MouseStates[i].ClickEvent;

               

                if (iD == playerState.SelectedUnitId)
                {
                    

                    CellAttributeList Path = new CellAttributeList();
                    List<CellAttributes>  Area = new List<CellAttributes>();
                    uint targettingRange = (uint)actions.CurrentSelected.Targets[0].Targettingrange;
                    switch (actions.CurrentSelected.Targets[0].Higlighter)
                    {
                        
                        case UseHighlighterEnum.no_pathing:
                            
                            if (playerState.CellsInRange.Count == 0)
                            {
                                playerState.CellsInRange = m_CellGrid.GetRadius(occCoord, targettingRange, worldIndex);
                                m_PlayerStateData.PlayerState[0] = playerState;
                            }
                            //use arcing linerenderer
                            break;
                        case UseHighlighterEnum.pathing:
                            
                            if(playerState.CellsInRange.Count == 0)
                            {
                                playerState.CellsInRange = m_CellGrid.GetRadius(occCoord, targettingRange, worldIndex);
                                playerState.CachedPaths = m_CellGrid.GetAllPathsInRadius(targettingRange, playerState.CellsInRange, occCoord);
                                m_PlayerStateData.PlayerState[0] = playerState;
                            }
                            //use path linerenderer
                            break;

                    }
                    HandleMods(occCoord, hoveredCoord, actions.CurrentSelected.Targets[0].Mods, out Path, out Area, worldIndex);

                    for(int a = 0; a< m_CellData.Length; a++)
                    {
                        var cellMarker = m_CellData.MarkerStateData[a];
                        var cellCoord = m_CellData.Coords[a].CubeCoordinate;
                        foreach(CellAttributes c in playerState.CellsInRange)
                        {
                            if(cellCoord == c.Cell.CubeCoordinate)
                            {
                                //Debug.Log("AyayaReachable");
                                cellMarker.CurrentState = MarkerState.State.Reachable;
                                cellMarker.IsSet = 0;
                            }
                        }
                        if(cellCoord== hoveredCoord)
                        {
                            //Debug.Log("AyayaHoverd");
                            cellMarker.CurrentState = MarkerState.State.Hovered;
                            cellMarker.IsSet = 0;
                        }
                        foreach(CellAttributes c in Area)
                        {
                            if (cellCoord == c.Cell.CubeCoordinate)
                            {
                                cellMarker.CurrentState = MarkerState.State.Hovered;
                                cellMarker.IsSet = 0;
                            }
                        }
                        m_CellData.MarkerStateData[a] = cellMarker;
                    }
                }
            }
        }
    }


    public void HandleMods(Vector3f occupiedCoord, Vector3f hoveringCoord, List<TargetMod> inMods, out CellAttributeList Path, out List<CellAttributes> Radius, uint windex)
    {
        Path = new CellAttributeList();
        Radius = new List<CellAttributes>();

        foreach(TargetMod t in inMods)
        {
            switch (t.ModType)
            {
                case ModTypeEnum.aoe:
                    Radius = m_CellGrid.GetRadius(hoveringCoord, (uint)t.AoeNested.Radius, windex);
                    break;
                case ModTypeEnum.path:
                    Path = m_CellGrid.FindPath(hoveringCoord, m_PlayerStateData.PlayerState[0].CachedPaths);
                    break;
            }
        }

    }
    //public void HighlightOnSelect()
}
