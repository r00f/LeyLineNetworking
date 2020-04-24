using Unity.Entities;
using Cell;
using Generic;
using Unit;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class ManalithSystem : ComponentSystem
{
    private EntityQuery m_ManalithData;
    private EntityQuery m_CellData;
    private EntityQuery m_UnitData;
    private EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_ManalithData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadWrite<Manalith.Component>(),
            ComponentType.ReadWrite<FactionComponent.Component>()
        );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CellAttributesComponent.ComponentAuthority>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<IsCircleCell.Component>()
        );

        m_CellData.SetFilter(CellAttributesComponent.ComponentAuthority.Authoritative);


        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Energy.Component>()
        );
        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>()
        );

    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        Entities.With(m_ManalithData).ForEach((ref FactionComponent.Component factionref) =>
        {
            factionref.Faction = 0;
        });

    }


    protected override void OnUpdate()
    {
        //reset unit harvesting if they have a locked moveaction
        Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
        {
            var worldIndex = gameStateWorldIndex.Value;
            var currentState = gameState.CurrentState;

            Entities.With(m_ManalithData).ForEach((ref Manalith.Component circleCellsref, ref FactionComponent.Component factionref, ref WorldIndex.Component windex) =>
            {
                var manalithWorldIndex = windex.Value;
                var faction = factionref;
                var manalithComp = circleCellsref;

                if (worldIndex == manalithWorldIndex)
                {
                    if (currentState == GameStateEnum.calculate_energy)
                    {
                        manalithComp.CombinedEnergyGain = manalithComp.BaseIncome;

                        for (int cci = 0; cci < manalithComp.Manalithslots.Count; cci++)
                        {
                            var slot = manalithComp.Manalithslots[cci];

                            //unit on slot
                            if(slot.CorrespondingCell.UnitOnCellId != 0)
                            {
                                bool unitAlive = false;

                                Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref CubeCoordinate.Component unitCoord, ref SpatialEntityId unitId, ref Energy.Component energy) =>
                                {
                                    if (worldIndex == unitWorldIndex.Value)
                                    {
                                        if (slot.CorrespondingCell.UnitOnCellId == unitId.EntityId.Id)
                                        {
                                            unitAlive = true;
                                            //if unit is still alive and has walked off the slot
                                            if(Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) != Vector3fext.ToUnityVector(slot.CorrespondingCell.CubeCoordinate))
                                            {
                                                UpdateManalithSlots(ref manalithComp, cci, 0);
                                                energy.Harvesting = false;
                                            }
                                        }
                                    }
                                });
                                if(!unitAlive)
                                {
                                    UpdateManalithSlots(ref manalithComp, cci, 0);
                                }
                            }

                            Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref CubeCoordinate.Component unitCoord, ref SpatialEntityId unitId) =>
                            {
                                if (worldIndex == unitWorldIndex.Value)
                                {
                                    if (Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(slot.CorrespondingCell.CubeCoordinate))
                                    {
                                        UpdateManalithSlots(ref manalithComp, cci, unitId.EntityId.Id);
                                    }
                                }
                            });
                        }

                        if(manalithComp.StateChange)
                        {
                            //Debug.Log("ManalithStateChange: Update manalith facion and units on slots.");
                            uint fact = UpdateFaction(manalithComp, manalithWorldIndex);
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

                            for (int cci = 0; cci < manalithComp.Manalithslots.Count; cci++)
                            {
                                var slot = manalithComp.Manalithslots[cci];

                                UpdateUnit(slot.CorrespondingCell.UnitOnCellId, faction.Faction, ref manalithComp);
                            }

                            factionref = faction;
                            circleCellsref = manalithComp;

                            manalithComp.StateChange = false;
                        }
                    }
                }
            });
        });
    }   

    public void UpdateManalithSlots(ref Manalith.Component manalith, int slotIndex, long unitId)
    {
        var slot = manalith.Manalithslots[slotIndex];

        slot.CorrespondingCell = new CellAttribute
        {
            UnitOnCellId = unitId,
            CubeCoordinate = manalith.Manalithslots[slotIndex].CorrespondingCell.CubeCoordinate
        };
        manalith.Manalithslots[slotIndex] = slot;
        manalith.Manalithslots = manalith.Manalithslots;
        manalith.StateChange = true;
    }

    public uint UpdateFaction(Manalith.Component circleCells, uint worldIndex)
    {
        int player1Units = 0;
        int player2Units = 0;

        for (int cci = 0; cci < circleCells.Manalithslots.Count; cci++)
        {
            var slot = circleCells.Manalithslots[cci];
            //slot.EnergyGained = 0;
            slot.OccupyingFaction = 0;

            Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction, ref Health.Component unitHealth) =>
            {
                if (unitId.EntityId.Id == slot.CorrespondingCell.UnitOnCellId)
                {
                    if (unitFaction.Faction == (worldIndex - 1) * 2 + 1)
                    {
                        player1Units++;
                        slot.OccupyingFaction = unitFaction.Faction;
                    }
                    else if (unitFaction.Faction == (worldIndex - 1) * 2 + 2)
                    {
                        player2Units++;
                        slot.OccupyingFaction = unitFaction.Faction;
                    }
                }
            });

            circleCells.Manalithslots[cci] = slot;
            circleCells.Manalithslots = circleCells.Manalithslots;
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

    public void UpdateUnit(long inUnitId, uint faction, ref Manalith.Component node)
    {
        var m_node = node;
        Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction, ref Energy.Component energy) =>
        {
            

            if (unitId.EntityId.Id == inUnitId)
            {
                if (unitFaction.Faction == faction)
                {
                    energy.Harvesting = true;
                    m_node.CombinedEnergyGain += energy.EnergyIncome;
                    for (int i = m_node.Manalithslots.Count-1; i >= 0; i--)
                    {
                        var slot = m_node.Manalithslots[i];
                        if (slot.CorrespondingCell.UnitOnCellId == inUnitId)
                        {
                            slot.EnergyGained = energy.EnergyIncome;
                        }
                        m_node.Manalithslots[i] = slot;
                    }
                    m_node.Manalithslots = m_node.Manalithslots;
                }
                else
                {
                    for (int i = m_node.Manalithslots.Count - 1; i >= 0; i--)
                    {
                        var slot = m_node.Manalithslots[i];
                        if (slot.CorrespondingCell.UnitOnCellId == inUnitId)
                        {
                            slot.EnergyGained = 0;
                        }
                        m_node.Manalithslots[i] = slot;
                    }
                    m_node.Manalithslots = m_node.Manalithslots;
                    energy.Harvesting = false;
                }
            }
        });
        node = m_node;
    }



}
