using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Improbable;
using Improbable.Gdk.Core;

namespace LeyLineHybridECS
{
    public class CellStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<MouseState> CellMouseStateData;
            public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
            public readonly ComponentDataArray<Cells.CellAttributesComponent.Component> Celldata;
            public ComponentDataArray<MarkerState> MarkerStateData;
        }

        [Inject] private Data m_Data;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<MouseState> UnitMouseStateData;
            public readonly ComponentDataArray<Unit.CellsToMark.Component> CellsToMarkData;
            public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        }

        [Inject] private UnitData m_UnitData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<Player.PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<Player.PlayerState.Component> PlayerStateData;
            public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        }

        [Inject] private PlayerData m_PlayerData;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Data.Length; ++i)
            {
                MouseState cellMouseState = m_Data.CellMouseStateData[i];
                MarkerState markerState = m_Data.MarkerStateData[i];
                Vector3f coords = m_Data.CoordinateData[i].CubeCoordinate;

                for (int ui = 0; ui < m_UnitData.Length; ++ui)
                {
                    MouseState unitMouseState = m_UnitData.UnitMouseStateData[ui];
                    var cellsToMark = m_UnitData.CellsToMarkData[ui];
                    var faction = m_UnitData.FactionData[ui].Faction;

                    if(faction == m_PlayerData.FactionData[0].Faction)
                    {
                        foreach (Cells.CellAttribute c in cellsToMark.CachedPaths.Keys)
                        {
                            if (c.CubeCoordinate == coords)
                            {
                                if (unitMouseState.CurrentState == MouseState.State.Clicked && !c.IsTaken)
                                {
                                    m_Data.MarkerStateData[i] = new MarkerState
                                    {
                                        CurrentState = MarkerState.State.Reachable,
                                        IsSet = 0
                                    };
                                }
                                else if (m_PlayerData.PlayerStateData[0].CurrentState != Player.PlayerStateEnum.unit_selected && markerState.CurrentState != (MarkerState.State)(int)cellMouseState.CurrentState)
                                {
                                    m_Data.MarkerStateData[i] = new MarkerState
                                    {
                                        CurrentState = (MarkerState.State)(int)cellMouseState.CurrentState,
                                        IsSet = 0
                                    };
                                }
                            }
                        }
                    }
                }

                if (markerState.CurrentState != (MarkerState.State)(int)cellMouseState.CurrentState && markerState.CurrentState != MarkerState.State.Reachable)
                {
                    m_Data.MarkerStateData[i] = new MarkerState
                    {
                        CurrentState = (MarkerState.State)(int)cellMouseState.CurrentState,
                        IsSet = 0
                    };
                }
            }
        }
    }

}

