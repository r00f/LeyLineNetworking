using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Improbable;
using Improbable.Gdk.Core;
using Unit;
using Generic;
using Cells;
using Player;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem))]
    public class CellStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<MouseState> CellMouseStateData;
            public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
            public readonly ComponentDataArray<CellAttributesComponent.Component> Celldata;
            public ComponentDataArray<MarkerState> MarkerStateData;
        }

        [Inject] private Data m_CellData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
        }

        [Inject] private UnitData m_UnitData;

        bool resetCellVisuals;

        protected override void OnUpdate()
        {

            if (resetCellVisuals)
            {
                for (int i = 0; i < m_CellData.Length; ++i)
                {
                    MouseState cellMouseState = m_CellData.CellMouseStateData[i];
                    MarkerState markerState = m_CellData.MarkerStateData[i];

                    if (markerState.CurrentState != (MarkerState.State)(int)cellMouseState.CurrentState)
                    {
                        m_CellData.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = (MarkerState.State)(int)cellMouseState.CurrentState,
                            IsSet = 0
                        };
                    }
                }
                resetCellVisuals = false;
            }
            else
            {
                for (int i = 0; i < m_CellData.Length; ++i)
                {
                    MouseState cellMouseState = m_CellData.CellMouseStateData[i];
                    MarkerState markerState = m_CellData.MarkerStateData[i];
                    Vector3f coords = m_CellData.CoordinateData[i].CubeCoordinate;

                    for (int ui = 0; ui < m_UnitData.Length; ui++)
                    {
                        MouseState unitMouseState = m_UnitData.MouseStateData[ui];
                        var cellsToMark = m_UnitData.CellsToMarkData[ui];
                        var faction = m_UnitData.FactionData[ui].Faction;
                        var actionData = m_UnitData.ActionsData[ui];

                        foreach (CellAttribute c in cellsToMark.CachedPaths.Keys)
                        {
                            if (c.CubeCoordinate == coords)
                            {
                                if(cellMouseState.CurrentState != MouseState.State.Hovered)
                                {
                                    if (!c.IsTaken)
                                    {
                                        if (markerState.CurrentState != MarkerState.State.Reachable)
                                        {
                                            m_CellData.MarkerStateData[i] = new MarkerState
                                            {
                                                CurrentState = MarkerState.State.Reachable,
                                                IsSet = 0
                                            };
                                        }
                                    }
                                }
                                else
                                {
                                    m_CellData.MarkerStateData[i] = new MarkerState
                                    {
                                        CurrentState = MarkerState.State.Hovered,
                                        IsSet = 0
                                    };
                                }
                            }
                        }
                    }
                }

                /*
                if (markerState.CurrentState != (MarkerState.State)(int)cellMouseState.CurrentState && markerState.CurrentState != MarkerState.State.Reachable)
                {
                    m_CellData.MarkerStateData[i] = new MarkerState
                    {
                        CurrentState = (MarkerState.State)(int)cellMouseState.CurrentState,
                        IsSet = 0
                    };
                }
                */
            }
        }

        public void ResetCellVisuals()
        {
            resetCellVisuals = true;
        }
    }
}

