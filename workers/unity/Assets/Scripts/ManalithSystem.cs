using UnityEngine;
using System.Collections;
using Unity.Entities;
using Cells;
using Generic;
using Improbable;
using Improbable.Gdk.Core;

public class ManalithSystem : ComponentSystem
{

    public struct ManalithData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CircleCells.Component> CircleCells;
        public ComponentDataArray<FactionComponent.Component> Factions;
    }

    [Inject] private ManalithData m_ManaLithData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> Cells;
    }

    [Inject] private CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> Ids;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentDataArray<Unit.ServerPath.Component> Paths;
    }

    [Inject] private UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameStates;
    }

    [Inject] private GameStateData m_GameStateData;

    protected override void OnUpdate()
    {
        for(int i = 0; i < m_ManaLithData.Length; i++)
        {
            var circleCells = m_ManaLithData.CircleCells[i];
            var faction = m_ManaLithData.Factions[i];
            var manalithWorldIndex = m_ManaLithData.WorldIndexData[i].Value;

            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;
                var gameState = m_GameStateData.GameStates[gi].CurrentState;

                if (manalithWorldIndex == gameStateWorldIndex)
                {
                    if(gameState == GameStateEnum.moving || gameState == GameStateEnum.calculate_energy)
                    {
                        //update CircleCells
                        for (int ci = 0; ci < m_CellData.Length; ci++)
                        {
                            var cellAtt = m_CellData.Cells[ci];
                            var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;

                            if(cellWorldIndex == manalithWorldIndex)
                            {
                                for (int cci = 0; cci < circleCells.CircleAttributeList.CellAttributes.Count; cci++)
                                {
                                    if (circleCells.CircleAttributeList.CellAttributes[cci].CubeCoordinate == cellAtt.CellAttributes.Cell.CubeCoordinate)
                                    {
                                        if (circleCells.CircleAttributeList.CellAttributes[cci].UnitOnCellId != cellAtt.CellAttributes.Cell.UnitOnCellId)
                                        {
                                            circleCells.CircleAttributeList.CellAttributes[cci] = new CellAttribute
                                            {
                                                UnitOnCellId = cellAtt.CellAttributes.Cell.UnitOnCellId,
                                                CubeCoordinate = circleCells.CircleAttributeList.CellAttributes[cci].CubeCoordinate
                                            };

                                            //workaround for a weird bug where inspector is not updated 
                                            circleCells.CircleAttributeList = circleCells.CircleAttributeList;

                                            m_ManaLithData.CircleCells[i] = circleCells;

                                            uint fact = UpdateFaction(circleCells, manalithWorldIndex);
                                            TeamColorEnum tColor = new TeamColorEnum();
                                            //if odd
                                            if(fact%2 == 1)
                                            {
                                                tColor = TeamColorEnum.blue;

                                            }
                                            else if(fact% 2 == 0)
                                            {
                                                tColor = TeamColorEnum.red;
                                            }

                                            faction = new FactionComponent.Component
                                            {
                                                Faction = fact,
                                                TeamColor = tColor
                                            };

                                            m_ManaLithData.Factions[i] = faction;
                                        }
                                    }
                                }
                            } 
                        }
                    }
                }
            }
        }
    }

    public uint UpdateFaction(CircleCells.Component circleCells, uint worldIndex)
    {
        int player1Units = 0;
        int player2Units = 0;

        for (int cci = 0; cci < circleCells.CircleAttributeList.CellAttributes.Count; cci++)
        {
            for (int ui = 0; ui < m_UnitData.Length; ui++)
            {
                var unitId = m_UnitData.Ids[ui];

                if (unitId.EntityId == circleCells.CircleAttributeList.CellAttributes[cci].UnitOnCellId)
                {
                    var unitFaction = m_UnitData.Factions[ui];

                    if(unitFaction.Faction == (worldIndex - 1) * 2 + 1)
                    {
                        player1Units++;
                    }
                    else if (unitFaction.Faction == (worldIndex - 1) * 2 + 2)
                    {
                        player2Units++;
                    }
                }
            }
        }

        if(player1Units > player2Units)
        {
            return (worldIndex - 1) * 2 + 1;
        }
        else if(player1Units < player2Units)
        {
            return (worldIndex - 1) * 2 + 2;
        }
        else
        {
            return 0;
        }
    }
}
