using Unity.Entities;
using Cell;
using Generic;
using Unit;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using UnityEngine;
using Unity.Jobs;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(SpawnUnitsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class ManalithSystem : JobComponentSystem
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
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<Manalith.Component>(),
            ComponentType.ReadWrite<FactionComponent.Component>()
        );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CellAttributesComponent.HasAuthority>(),
            ComponentType.ReadOnly<CellAttributesComponent.Component>(),
            ComponentType.ReadOnly<IsCircleCell.Component>()
        );

        
        var NormalUnitDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(Manalith.Component)
            },
            All = new ComponentType[]
            {
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Energy.Component>()
            }
        };
        m_NonManalithUnitData = GetEntityQuery(NormalUnitDesc);
        
        m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            //ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Energy.Component>()
        );
        
        m_ManalithUnitData = GetEntityQuery(
            ComponentType.ReadOnly<Manalith.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadWrite<FactionComponent.Component>(),
            ComponentType.ReadOnly<Actions.Component>(),
            ComponentType.ReadWrite<Vision.Component>(),
            //ComponentType.ReadOnly<Health.Component>(),
            ComponentType.ReadWrite<Energy.Component>()
        );
        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }

    public void UpdateManaliths(WorldIndexShared worldIndex)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref Manalith.Component manalith, ref FactionComponent.Component faction, ref Energy.Component energy, ref Vision.Component vision, in SpatialEntityId entityId) =>
        {
            var manalithComp = manalith;
            manalithComp.CombinedEnergyGain = 0;

            for (int cci = 0; cci < manalithComp.Manalithslots.Count; cci++)
            {
                var slot = manalithComp.Manalithslots[cci];

                //unit on slot
                if (slot.CorrespondingCell.UnitOnCellId != 0)
                {
                    bool unitAlive = false;

                    manalithComp = CompareUnitSlotState(worldIndex, manalithComp, ref unitAlive, cci, slot);

                    if (!unitAlive)
                    {
                        UpdateManalithSlots(ref manalithComp, cci, 0, 0);
                    }
                }

                manalithComp = UpdateManalithWithUnits(worldIndex, manalithComp, cci, slot);
            }

            if (manalithComp.StateChange)
            {
                var oldFact = faction.Faction;
                uint fact = UpdateFaction(manalithComp, worldIndex);

                faction = new FactionComponent.Component
                {
                    Faction = fact
                };

                for (int cci = 0; cci < manalithComp.Manalithslots.Count; cci++)
                {
                    var slot = manalithComp.Manalithslots[cci];
                    UpdateUnit(worldIndex, slot.CorrespondingCell.UnitOnCellId, faction.Faction, ref manalithComp);
                }

                manalith.CombinedEnergyGain += energy.EnergyIncome;
                vision.RequireUpdate = true;

                manalithComp.StateChange = false;

                manalith = manalithComp;

                if (oldFact != faction.Faction)
                {
                    componentUpdateSystem.SendEvent(
                     new Manalith.ManalithFactionChangeEvent.Event(),
                     entityId.EntityId);
                }
            }
        })
        .WithoutBurst()
        .Run();
    }

    Manalith.Component UpdateManalithWithUnits(WorldIndexShared worldIndex, Manalith.Component manalithComp, int slotIndex, ManalithSlot slot)
    {
        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref CubeCoordinate.Component unitCoord, in SpatialEntityId unitId, in FactionComponent.Component faction) =>
        {
            if (Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) == Vector3fext.ToUnityVector(slot.CorrespondingCell.CubeCoordinate))
            {
                UpdateManalithSlots(ref manalithComp, slotIndex, unitId.EntityId.Id, faction.Faction);
            }
        })
        .WithoutBurst()
        .Run();

        return manalithComp;
    }

    Manalith.Component CompareUnitSlotState(WorldIndexShared worldIndex, Manalith.Component manalithComp, ref bool unitAlive, int slotIndex, ManalithSlot slot)
    {
        var alive = unitAlive;

        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref CubeCoordinate.Component unitCoord, ref SpatialEntityId unitId, ref Energy.Component energy) =>
        {
            if (slot.CorrespondingCell.UnitOnCellId == unitId.EntityId.Id)
            {
                alive = true;
                //if unit is still alive and has walked off the slot
                if (Vector3fext.ToUnityVector(unitCoord.CubeCoordinate) != Vector3fext.ToUnityVector(slot.CorrespondingCell.CubeCoordinate))
                {
                    UpdateManalithSlots(ref manalithComp, slotIndex, 0, 0);
                    energy.Harvesting = false;
                }
            }
        })
        .WithoutBurst()
        .Run();

        unitAlive = alive;

        return manalithComp;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
    }   

    public void UpdateManalithSlots(ref Manalith.Component manalith, int slotIndex, long unitId, uint faction)
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

        slot.OccupyingFaction = faction;
        manalith.Manalithslots[slotIndex] = slot;
        manalith.Manalithslots = manalith.Manalithslots;
        manalith.StateChange = true;
    }

    public uint UpdateFaction(Manalith.Component manalith, WorldIndexShared worldIndex)
    {
        int player0Units = 0;
        int player1Units = 0;
        int player2Units = 0;

        uint player1Faction = (worldIndex.Value - 1) * 2 + 1;
        uint player2Faction = (worldIndex.Value - 1) * 2 + 2;

        for (int cci = 0; cci < manalith.Manalithslots.Count; cci++)
        {
            var slot = manalith.Manalithslots[cci];

            if (slot.OccupyingFaction == 0)
            {
                //check if there is a neutral unit on the cell because Occupying faction == 0 is the default state
                if(slot.CorrespondingCell.IsTaken)
                    player0Units++;
            }
            else if (slot.OccupyingFaction == player1Faction)
            {
                player1Units++;
            }
            else if (slot.OccupyingFaction == player2Faction)
            {
                player2Units++;
            }
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

    public void UpdateUnit(WorldIndexShared worldIndex, long inUnitId, uint faction, ref Manalith.Component node)
    {
        var m_node = node;

        Entities.WithSharedComponentFilter(worldIndex).ForEach((ref SpatialEntityId unitId, ref Energy.Component energy, in FactionComponent.Component unitFaction) =>
        {
            if (unitId.EntityId.Id == inUnitId)
            {
                if (unitFaction.Faction == faction)
                {
                    m_node.CombinedEnergyGain += energy.EnergyIncome;
                    for (int i = m_node.Manalithslots.Count-1; i >= 0; i--)
                    {
                        var slot = m_node.Manalithslots[i];
                        if (slot.CorrespondingCell.UnitOnCellId == inUnitId)
                        {
                            slot.EnergyGained = energy.EnergyIncome;
                            energy.Harvesting = true;
                            //Debug.Log("SetHarvestingTrue on Unit: " + unitId.EntityId.Id);
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
        })
        .WithoutBurst()
        .Run();

        node = m_node;
    }
}
