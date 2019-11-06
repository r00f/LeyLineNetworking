using Unity.Entities;
using Cell;
using Unit;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(InitializePlayerSystem))]
public class UnitLifeCycleSystem : ComponentSystem
{
    public struct UnitStateData : ISystemStateComponentData
    {
        public CubeCoordinate.Component CubeCoordState;
        public WorldIndex.Component WorldIndexState;
        public long EntityId;
    }

    HandleCellGridRequestsSystem m_CellGridSystem;
    EntityQuery m_CellData;
    EntityQuery m_UnitAddedData;
    EntityQuery m_UnitRemovedData;
    EntityQuery m_UnitChangedData;


    protected override void OnCreate()
    {
        base.OnCreate();
        m_CellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();

        var unitAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[] 
            {
                typeof(UnitStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Health.Component>(),
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>()
            }
        };

        m_UnitAddedData = GetEntityQuery(unitAddedDesc);

        var unitRemovedDesc = new EntityQueryDesc
        {
            None = new ComponentType[] 
            {
                ComponentType.ReadOnly<CubeCoordinate.Component>()
            },
            All = new ComponentType[]
            {
                typeof(UnitStateData)
            }
        };

        m_UnitRemovedData = GetEntityQuery(unitRemovedDesc);

        m_UnitChangedData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<UnitStateData>()
            );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );



    }

    protected override void OnUpdate()
    {
        #region CellData
        var cellWorldIndexData = m_CellData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var cellCoordData = m_CellData.ToComponentDataArray<CubeCoordinate.Component>(Allocator.TempJob);
        var cellAttributesData = m_CellData.ToComponentDataArray<CellAttributesComponent.Component>(Allocator.TempJob);
        #endregion

        #region UnitAddedData
        var unitAddedEntities = m_UnitAddedData.ToEntityArray(Allocator.TempJob);
        var unitAddedWorldIndexData = m_UnitAddedData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var unitAddedCubeCoordinateData = m_UnitAddedData.ToComponentDataArray<CubeCoordinate.Component>(Allocator.TempJob);
        var unitAddedEntityIdData = m_UnitAddedData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
        #endregion

        #region UnitChangedData
        var unitChangedEntities = m_UnitChangedData.ToEntityArray(Allocator.TempJob);
        var unitChangedUnitStateData = m_UnitChangedData.ToComponentDataArray<UnitStateData>(Allocator.TempJob);
        var unitChangedIdData = m_UnitChangedData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
        var unitChangedCoordData = m_UnitChangedData.ToComponentDataArray<CubeCoordinate.Component>(Allocator.TempJob);
        var unitChangedWorldIndexData = m_UnitChangedData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        #endregion

        #region UnitRemovedData
        var unitRemovedEntities = m_UnitRemovedData.ToEntityArray(Allocator.TempJob);
        var unitRemovedStateData = m_UnitRemovedData.ToComponentDataArray<UnitStateData>(Allocator.TempJob);
        #endregion

        for (int i = 0; i < unitAddedEntities.Length; i++)
        {
            var unitWorldIndex = unitAddedWorldIndexData[i].Value;
            var unitCubeCoordinate = Vector3fext.ToUnityVector(unitAddedCubeCoordinateData[i].CubeCoordinate);
            var unitEntityId = unitAddedEntityIdData[i];

            for (int ci = 0; ci < cellWorldIndexData.Length; ci++)
            {
                var cellCubeCoordinate = Vector3fext.ToUnityVector(cellCoordData[ci].CubeCoordinate);
                var cellWorldIndex = cellWorldIndexData[ci].Value;
                var cellAtt = cellAttributesData[ci];

                if (unitCubeCoordinate == cellCubeCoordinate && unitWorldIndex == cellWorldIndex)
                {
                    cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, unitEntityId.EntityId.Id, cellWorldIndex);
                    cellAttributesData[ci] = cellAtt;
                }
            }
            PostUpdateCommands.AddComponent(unitAddedEntities[i], new UnitStateData { CubeCoordState = unitAddedCubeCoordinateData[i], WorldIndexState = unitAddedWorldIndexData[i], EntityId = unitEntityId.EntityId.Id});
        }

        for (int i = 0; i < unitChangedEntities.Length; i++)
        {
            var unitEntityId = unitChangedIdData[i];
            if (Vector3fext.ToUnityVector(unitChangedUnitStateData[i].CubeCoordState.CubeCoordinate) != Vector3fext.ToUnityVector(unitChangedCoordData[i].CubeCoordinate) || unitChangedUnitStateData[i].WorldIndexState.Value != unitChangedWorldIndexData[i].Value)
            {
                PostUpdateCommands.SetComponent(unitChangedEntities[i], new UnitStateData { CubeCoordState = unitChangedCoordData[i], WorldIndexState = unitChangedWorldIndexData[i], EntityId = unitEntityId.EntityId.Id});
            }
        }

        for (int i = 0; i < unitRemovedEntities.Length; i++)
        {
            var unitState = unitRemovedStateData[i];
            var unitCubeCoordinate = Vector3fext.ToUnityVector(unitState.CubeCoordState.CubeCoordinate);
            var unitWorldIndex = unitState.WorldIndexState.Value;


            for (int ci = 0; ci < cellAttributesData.Length; ci++)
            {
                var cellCubeCoordinate = Vector3fext.ToUnityVector(cellCoordData[ci].CubeCoordinate);
                var cellAtt = cellAttributesData[ci];
                var cellWorldIndex = cellWorldIndexData[ci].Value;

                if (unitCubeCoordinate == cellCubeCoordinate && unitWorldIndex == cellWorldIndex && cellAtt.CellAttributes.Cell.UnitOnCellId == unitState.EntityId)
                {
                    //Debug.Log("CleanUpUnit with id: " + unitState.EntityId);
                    cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, 0, cellWorldIndex);
                    cellAttributesData[ci] = cellAtt;
                }
            }
            PostUpdateCommands.RemoveComponent<UnitStateData>(unitRemovedEntities[i]);

        }


        #region CellData
        cellWorldIndexData.Dispose();
        cellCoordData.Dispose();
        cellAttributesData.Dispose();
        #endregion

        #region UnitAddedData
        unitAddedEntities.Dispose();
        unitAddedWorldIndexData.Dispose();
        unitAddedCubeCoordinateData.Dispose();
        unitAddedEntityIdData.Dispose();
        #endregion

        #region UnitChangedData
        unitChangedEntities.Dispose();
        unitChangedUnitStateData.Dispose();
        unitChangedIdData.Dispose();
        unitChangedCoordData.Dispose();
        unitChangedWorldIndexData.Dispose();
        #endregion

        #region UnitRemovedData
        unitRemovedEntities.Dispose();
        unitRemovedStateData.Dispose();
        #endregion
    }
}
