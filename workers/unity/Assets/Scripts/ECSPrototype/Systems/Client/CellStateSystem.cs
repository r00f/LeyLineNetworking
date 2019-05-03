using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;
using Unit;
using Generic;
using Cell;
using Player;
using Improbable.Gdk.ReactiveComponents;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class CellStateSystem : ComponentSystem
    {
        public struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<MouseState> CellMouseStateData;
            public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
            public readonly ComponentDataArray<CellAttributesComponent.Component> Celldata;
            public ComponentDataArray<MarkerState> MarkerStateData;
        }

        [Inject] private CellData m_CellData;

        public struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<ClientPath.Component>> AuthorativeData;
            public readonly ComponentDataArray<SpatialEntityId> EntityIds;
            public readonly ComponentDataArray<MouseState> MouseStateData;
            public readonly ComponentDataArray<Actions.Component> ActionsData;
            public readonly ComponentDataArray<CellsToMark.Component> CellsToMarkData;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
        }

        [Inject] private UnitData m_UnitData;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<GameState.Component> GameState;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        }

        [Inject] private GameStateData m_GameStateData;

        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
            public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
        }

        [Inject] private PlayerData m_PlayerData;

        protected override void OnUpdate()
        {
            if(m_PlayerData.PlayerStateData.Length == 0)
            {
                return;
            }
            var playerState = m_PlayerData.PlayerStateData[0];

            ResetCellVisuals();

            if (playerState.CurrentState == PlayerStateEnum.waiting_for_target)
            {
                for (int ui = 0; ui < m_UnitData.Length; ui++)
                {
                    MouseState unitMouseState = m_UnitData.MouseStateData[ui];
                    var cellsToMark = m_UnitData.CellsToMarkData[ui];
                    var faction = m_UnitData.FactionData[ui].Faction;
                    var actionData = m_UnitData.ActionsData[ui];
                    var unitId = m_UnitData.EntityIds[ui].EntityId.Id;

                    if(playerState.SelectedUnitId == unitId)
                    {
                        for (int i = 0; i < m_CellData.Length; i++)
                        {
                            MouseState cellMouseState = m_CellData.CellMouseStateData[i];
                            MarkerState markerState = m_CellData.MarkerStateData[i];
                            Vector3f coords = m_CellData.CoordinateData[i].CubeCoordinate;

                            foreach (CellAttribute c in cellsToMark.CachedPaths.Keys)
                            {
                                if (c.CubeCoordinate == coords)
                                {
                                    if (cellMouseState.CurrentState != MouseState.State.Hovered)
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
                }
            }
            else
            {


            }
        }

        public void ResetCellVisuals()
        {
            for (int i = 0; i < m_CellData.Length; i++)
            {
                MouseState cellMouseState = m_CellData.CellMouseStateData[i];
                MarkerState markerState = m_CellData.MarkerStateData[i];
                var celldata = m_CellData.Celldata[i];
                var gameState = m_GameStateData.GameState[0].CurrentState;

                if (gameState != GameStateEnum.planning)
                {
                    if (m_CellData.MarkerStateData[i].CurrentState != MarkerState.State.Neutral)
                    {
                        m_CellData.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = MarkerState.State.Neutral,
                            IsSet = 0
                        };
                    }
                }
                else if(celldata.CellAttributes.Cell.IsTaken && m_CellData.MarkerStateData[i].CurrentState != MarkerState.State.Reachable)
                {
                    if (m_CellData.MarkerStateData[i].CurrentState != MarkerState.State.Neutral)
                    {
                        m_CellData.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = MarkerState.State.Neutral,
                            IsSet = 0
                        };
                    }
                }
                else if (markerState.CurrentState != (MarkerState.State)(int)cellMouseState.CurrentState)
                {
                    m_CellData.MarkerStateData[i] = new MarkerState
                    {
                        CurrentState = (MarkerState.State)(int)cellMouseState.CurrentState,
                        IsSet = 0
                    };
                }
            }
        }
    }
}

