using Unity.Entities;
using Cell;
using Generic;
using Unit;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Collections;

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

        #region Deprecated Vars
        /*
        #region gameStateVars
        var gameStateWorldIndexes = m_GameStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
        #endregion

        #region unitDataVars
        var unitsWorldIndex = m_UnitData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var unitsID = m_UnitData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
        var unitsFaction = m_UnitData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        var unitsAction = m_UnitData.ToComponentDataArray<Actions.Component>(Allocator.TempJob);
        var unitsEnergy = m_UnitData.ToComponentDataArray<Energy.Component>(Allocator.TempJob);
        #endregion

        #region manaLithDataVars
        var manaLithsWorldIndex = m_ManalithData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var manaLithsCircle_Cells = m_ManalithData.ToComponentDataArray<Manalith.Component>(Allocator.TempJob);
        var manaLithsFaction = m_ManalithData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
        #endregion

        #region cellDataVars
        var cellsCircle_Cells = m_CellData.ToComponentDataArray<IsCircleCell.Component>(Allocator.TempJob);
        var cellsWorldindex = m_CellData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var cellsAttributes = m_CellData.ToComponentDataArray<CellAttributesComponent.Component>(Allocator.TempJob);
        #endregion
        */
        #endregion

        //reset unit harvesting if they have a locked moveaction
        Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState) =>
        {
            var worldIndex = gameStateWorldIndex.Value;
            var currentState = gameState.CurrentState;
            Entities.With(m_UnitData).ForEach((ref WorldIndex.Component unitWorldIndex, ref Energy.Component energy, ref Actions.Component actions) =>
            {

                var lockedAction = actions.LockedAction;

                if (worldIndex == unitWorldIndex.Value)
                {
                    if (currentState == GameStateEnum.move && energy.Harvesting && lockedAction.Index == -2)
                    {
                        energy.Harvesting = false;
                    }
                }
            });
            Entities.With(m_ManalithData).ForEach((ref Manalith.Component circleCellsref, ref FactionComponent.Component factionref, ref WorldIndex.Component windex) =>
            {
                var manalithWorldIndex = windex.Value;
                var faction = factionref;
                var manalithComp = circleCellsref;

                if (worldIndex == manalithWorldIndex)
                {
                    if (currentState == GameStateEnum.calculate_energy)
                    {
                        //update CircleCells
                        manalithComp.CombinedEnergyGain = manalithComp.BaseIncome;
                        Entities.With(m_CellData).ForEach((ref WorldIndex.Component cellWindex, ref CellAttributesComponent.Component cellAtt) =>

                        {
                            var cellWorldIndex = cellWindex.Value;


                            if (manalithWorldIndex == cellWorldIndex)
                            {
                                for (int cci = 0; cci < manalithComp.CircleAttributeList.CellAttributes.Count; cci++)
                                {
                                    if (Vector3fext.ToUnityVector(manalithComp.CircleAttributeList.CellAttributes[cci].CubeCoordinate) == Vector3fext.ToUnityVector(cellAtt.CellAttributes.Cell.CubeCoordinate))
                                    {
                                        if (manalithComp.CircleAttributeList.CellAttributes[cci].UnitOnCellId != cellAtt.CellAttributes.Cell.UnitOnCellId)
                                        {
                                            manalithComp.CircleAttributeList.CellAttributes[cci] = new CellAttribute
                                            {
                                                UnitOnCellId = cellAtt.CellAttributes.Cell.UnitOnCellId,
                                                CubeCoordinate = manalithComp.CircleAttributeList.CellAttributes[cci].CubeCoordinate
                                            };

                                            //workaround for a weird bug where inspector is not updated 
                                            manalithComp.CircleAttributeList = manalithComp.CircleAttributeList;

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
                                        }

                                        UpdateUnit(cellAtt.CellAttributes.Cell.UnitOnCellId, faction.Faction, ref manalithComp);
                                    }
                                }
                            }
                        });
                    }
                }
                factionref = faction;
                circleCellsref = manalithComp;
            });
        });
        #region dispose
        /*
        #region gameStateVarsDispose
        gameStateWorldIndexes.Dispose();
        gameStates.Dispose();
        #endregion

        #region unitDataVarsDispose
        unitsWorldIndex.Dispose();
        unitsID.Dispose();
        unitsFaction.Dispose();
        unitsAction.Dispose();
        unitsEnergy.Dispose();
        #endregion

        #region manaLithDataVarsDispose
        manaLithsWorldIndex.Dispose();
        manaLithsCircle_Cells.Dispose();
        manaLithsFaction.Dispose();
        #endregion

        #region cellDataVarsDispose
        cellsCircle_Cells.Dispose();
        cellsWorldindex.Dispose();
        cellsAttributes.Dispose();
        #endregion
        */
        #endregion
    }

    public uint UpdateFaction(Manalith.Component circleCells, uint worldIndex)
    {
        int player1Units = 0;
        int player2Units = 0;


        for (int cci = 0; cci < circleCells.CircleAttributeList.CellAttributes.Count; cci++)
        {
            Entities.With(m_UnitData).ForEach((ref SpatialEntityId unitId, ref FactionComponent.Component unitFaction) =>
            {


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
            });
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
                }
                else
                    energy.Harvesting = false;
            }
        });
        node = m_node;
    }



}
