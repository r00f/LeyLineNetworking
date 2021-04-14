using Unity.Entities;
using Cell;
using Generic;
using Unit;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Collections;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class ManalithSystem : ComponentSystem
{
    private ComponentUpdateSystem componentUpdateSystem;
    private EntityQuery m_NonManalithUnitData;
    private EntityQuery m_ManalithData;
    private EntityQuery m_CellData;
    private EntityQuery m_UnitData;
    private EntityQuery m_ManalithUnitData;
    private EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();

        componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

        m_ManalithData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<Manalith.Component>(),
            ComponentType.ReadWrite<FactionComponent.Component>()
        );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CellAttributesComponent.HasAuthority>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<IsCircleCell.Component>()
        );

        
        var NormalUnitDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(ManalithUnit.Component)
            },
            All = new ComponentType[]
            {
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Energy.Component>()
            }
        };
        m_NonManalithUnitData = GetEntityQuery(NormalUnitDesc);
        


        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            //ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Energy.Component>()
        );
        

        m_ManalithUnitData = GetEntityQuery(
            ComponentType.ReadOnly<ManalithUnit.Component>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadWrite<Vision.Component>(),
            //ComponentType.ReadOnly<Health.Component>(),
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

        //Entities.With(m_ManalithData).ForEach((ref FactionComponent.Component factionref) =>
        //{
            //factionref.Faction = 0;
        //});
    }

    public void UpdateManaliths(uint worldIndex)
    {
        Entities.With(m_ManalithData).ForEach((ref SpatialEntityId entityId, ref Manalith.Component circleCellsref, ref FactionComponent.Component factionref, ref WorldIndex.Component windex) =>
        {
            var manalithWorldIndex = windex.Value;
            var faction = factionref;
            var manalithComp = circleCellsref;

            if (worldIndex == manalithWorldIndex)
            {
                manalithComp.CombinedEnergyGain = manalithComp.BaseIncome;

                for (int cci = 0; cci < manalithComp.Manalithslots.Count; cci++)
                {
                    var slot = manalithComp.Manalithslots[cci];

                    //unit on slot
                    if (slot.CorrespondingCell.UnitOnCellId != 0)
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
                                    if (Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) != Vector3fext.ToUnityVector(slot.CorrespondingCell.CubeCoordinate))
                                    {
                                        UpdateManalithSlots(ref manalithComp, cci, 0);
                                        energy.Harvesting = false;
                                    }
                                }
                            }
                        });
                        if (!unitAlive)
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

                if (manalithComp.StateChange)
                {
                    var oldFact = factionref.Faction;
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

                    UpdateManalithUnit(manalithComp.ManalithUnitId, faction.Faction, ref manalithComp);

                    manalithComp.StateChange = false;

                    factionref = faction;
                    circleCellsref = manalithComp;

                    if (oldFact != faction.Faction)
                    {
                        componentUpdateSystem.SendEvent(
                         new Manalith.ManalithFactionChangeEvent.Event(),
                         entityId.EntityId);


                        //Vision.req
                    }

                    //Apply manalith Faction to manalithUnitFaction

                }
            }
        });
    }

    protected override void OnUpdate()
    {

    }   

    public void UpdateManalithSlots(ref Manalith.Component manalith, int slotIndex, long unitId)
    {
        var slot = manalith.Manalithslots[slotIndex];

        bool isTaken;

        if (unitId == 0)
            isTaken = false;
        else
            isTaken = true;

        slot.CorrespondingCell = new CellAttribute
        {
            IsTaken = isTaken,
            UnitOnCellId = unitId,
            CubeCoordinate = manalith.Manalithslots[slotIndex].CorrespondingCell.CubeCoordinate
        };

        manalith.Manalithslots[slotIndex] = slot;
        manalith.Manalithslots = manalith.Manalithslots;
        manalith.StateChange = true;
    }

    public uint UpdateFaction(Manalith.Component circleCells, uint worldIndex)
    {
        int player0Units = 0;
        int player1Units = 0;
        int player2Units = 0;

        uint player1Faction = (worldIndex - 1) * 2 + 1;
        uint player2Faction = (worldIndex - 1) * 2 + 2;

        for (int cci = 0; cci < circleCells.Manalithslots.Count; cci++)
        {
            var slot = circleCells.Manalithslots[cci];
            //slot.EnergyGained = 0;
            slot.OccupyingFaction = 0;

            Entities.With(m_NonManalithUnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction, ref Health.Component unitHealth) =>
            {
                if (unitId.EntityId.Id == slot.CorrespondingCell.UnitOnCellId)
                {
                    if (unitFaction.Faction == 0)
                    {
                        player0Units++;
                    }
                    else if (unitFaction.Faction == player1Faction)
                    {
                        player1Units++;
                    }
                    else if (unitFaction.Faction == player2Faction)
                    {
                        player2Units++;
                    }
                    slot.OccupyingFaction = unitFaction.Faction;
                }
            });

            circleCells.Manalithslots[cci] = slot;
            circleCells.Manalithslots = circleCells.Manalithslots;
        }

        int biggestUnitCount = Mathf.Max(player0Units, player1Units, player2Units);

        if (biggestUnitCount == 0 || player1Units == player2Units || biggestUnitCount == player0Units)
            return 0;
        else if (biggestUnitCount == player1Units)
            return player1Faction;
        else if (biggestUnitCount == player2Units)
            return player2Faction;
        else
            return 0;
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

        //Update ManalithUnits
        Entities.With(m_ManalithUnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction, ref Energy.Component energy) =>
        {
            if (unitId.EntityId.Id == inUnitId)
            {
                unitFaction.Faction = faction;
            }
        });

        node = m_node;
    }

    public void UpdateManalithUnit(long inUnitId, uint faction, ref Manalith.Component node)
    {
        Entities.With(m_ManalithUnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction, ref Energy.Component energy, ref Vision.Component vision) =>
        {
            if (unitId.EntityId.Id == inUnitId)
            {
                unitFaction.Faction = faction;
                vision.RequireUpdate = true;
            }
        });
    }
}
