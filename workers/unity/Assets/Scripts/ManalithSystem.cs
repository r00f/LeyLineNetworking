using Unity.Entities;
using Cell;
using Generic;
using Unit;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Improbable.Gdk.ReactiveComponents;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class ManalithSystem : ComponentSystem
{
    public struct ManalithData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CircleCells.Component> CircleCells;
        public ComponentDataArray<FactionComponent.Component> Factions;
    }

    [Inject] ManalithData m_ManaLithData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<CellAttributesComponent.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<CellAttributesComponent.Component> Cells;
        public readonly ComponentDataArray<IsCircleCell.Component> IsCircleCellData;
    }

    [Inject] CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<SpatialEntityId> Ids;
        public readonly ComponentDataArray<FactionComponent.Component> Factions;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public ComponentDataArray<Energy.Component> EnergyData;
    }

    [Inject] UnitData m_UnitData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<GameState.Component> GameStates;
    }

    [Inject] GameStateData m_GameStateData;

    protected override void OnUpdate()
    {
        //reset unit harvesting if they have a locked moveaction
        for (int i = 0; i < m_GameStateData.Length; i++)
        {
            var gameStateWorldIndex = m_GameStateData.WorldIndexData[i].Value;
            var gameState = m_GameStateData.GameStates[i].CurrentState;

            for (int ui = 0; ui < m_UnitData.Length; ui++)
            {
                var unitWorldIndex = m_UnitData.WorldIndexData[ui].Value;
                var energy = m_UnitData.EnergyData[ui];
                var lockedAction = m_UnitData.ActionsData[ui].LockedAction;

                if (gameStateWorldIndex == unitWorldIndex)
                {
                    if (gameState == GameStateEnum.move && energy.Harvesting && lockedAction.Index == -2)
                    {
                        energy.Harvesting = false;
                        m_UnitData.EnergyData[ui] = energy;
                    }
                }
            }

            for (int mi = 0; mi < m_ManaLithData.Length; mi++)
            {
                var circleCells = m_ManaLithData.CircleCells[mi];
                var faction = m_ManaLithData.Factions[mi];
                var manalithWorldIndex = m_ManaLithData.WorldIndexData[mi].Value;

                if (gameStateWorldIndex == manalithWorldIndex)
                {
                    if (gameState == GameStateEnum.calculate_energy)
                    {
                        //update CircleCells
                        for (int ci = 0; ci < m_CellData.Length; ci++)
                        {
                            var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;
                            var cellAtt = m_CellData.Cells[ci];

                            if (manalithWorldIndex == cellWorldIndex)
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

                                            m_ManaLithData.CircleCells[mi] = circleCells;

                                            uint fact = UpdateFaction(circleCells, manalithWorldIndex);
                                            TeamColorEnum tColor = new TeamColorEnum();

                                            //if odd
                                            if (fact == 0)
                                            {
                                                tColor = TeamColorEnum.blue;
                                            }
                                            else if (fact % 2 == 1)
                                            {
                                                tColor = TeamColorEnum.blue;
                                            }
                                            else if (fact % 2 == 0)
                                            {
                                                tColor = TeamColorEnum.red;
                                            }

                                            faction = new FactionComponent.Component
                                            {
                                                Faction = fact,
                                                TeamColor = tColor
                                            };

                                            m_ManaLithData.Factions[mi] = faction;
                                        }

                                        UpdateUnit(cellAtt.CellAttributes.Cell.UnitOnCellId, faction.Faction);
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
                var unitFaction = m_UnitData.Factions[ui];

                if (unitId.EntityId.Id == circleCells.CircleAttributeList.CellAttributes[cci].UnitOnCellId)
                {
                    if (unitFaction.Faction == (worldIndex - 1) * 2 + 1)
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

        if (player1Units > player2Units)
        {
            return (worldIndex - 1) * 2 + 1;
        }
        else if (player1Units < player2Units)
        {
            return (worldIndex - 1) * 2 + 2;
        }
        else
        {
            return 0;
        }
    }

    public void UpdateUnit(long inUnitId, uint faction)
    {
        for (int ui = 0; ui < m_UnitData.Length; ui++)
        {
            var energy = m_UnitData.EnergyData[ui];
            var unitId = m_UnitData.Ids[ui].EntityId;
            var unitFaction = m_UnitData.Factions[ui];

            if (unitId.Id == inUnitId)
            {
                if (unitFaction.Faction == faction)
                    energy.Harvesting = true;
                else
                    energy.Harvesting = false;
            }

            m_UnitData.EnergyData[ui] = energy;
        }
    }

}
