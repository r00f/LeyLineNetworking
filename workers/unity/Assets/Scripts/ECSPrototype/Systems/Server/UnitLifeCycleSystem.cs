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
        Entities.With(m_UnitAddedData).ForEach((Entity e, ref WorldIndex.Component unitWorldIndex, ref CubeCoordinate.Component unitCubeCoordinate, ref SpatialEntityId unitEntityId) =>
        {
            var coordComp = unitCubeCoordinate;
            var coord = Vector3fext.ToUnityVector(unitCubeCoordinate.CubeCoordinate);
            var worldIndex = unitWorldIndex;
            var id = unitEntityId.EntityId.Id;

            Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component cellCubeCoordinate, ref WorldIndex.Component cellWorldIndex, ref CellAttributesComponent.Component cellAttribute) =>
            {
                if (coord == Vector3fext.ToUnityVector(cellCubeCoordinate.CubeCoordinate) && worldIndex.Value == cellWorldIndex.Value)
                {
                    cellAttribute.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAttribute.CellAttributes, true, id, cellWorldIndex.Value);
                }
            });

            PostUpdateCommands.AddComponent(e, new UnitStateData { CubeCoordState = coordComp, WorldIndexState = worldIndex, EntityId = unitEntityId.EntityId.Id });
        });


        Entities.With(m_UnitChangedData).ForEach((Entity e, ref SpatialEntityId unitEntityId, ref CubeCoordinate.Component unitCubeCoord, ref UnitStateData unitState, ref WorldIndex.Component unitWorldIndex) =>
        {
            if (Vector3fext.ToUnityVector(unitState.CubeCoordState.CubeCoordinate) != Vector3fext.ToUnityVector(unitCubeCoord.CubeCoordinate) || unitState.WorldIndexState.Value != unitWorldIndex.Value)
            {
                PostUpdateCommands.SetComponent(e, new UnitStateData { CubeCoordState = unitCubeCoord, WorldIndexState = unitWorldIndex, EntityId = unitEntityId.EntityId.Id });
            }

        });


        Entities.With(m_UnitRemovedData).ForEach((Entity e, ref UnitStateData unitState) =>
        {
            var unitStateVar = unitState;
            var unitCubeCoordinate = Vector3fext.ToUnityVector(unitState.CubeCoordState.CubeCoordinate);
            var unitWorldIndex = unitState.WorldIndexState.Value;

            Entities.With(m_CellData).ForEach((ref CubeCoordinate.Component cellCubeCoordinate, ref WorldIndex.Component cellWorldIndex, ref CellAttributesComponent.Component cellAttribute) =>
            {
                if (unitCubeCoordinate == Vector3fext.ToUnityVector(cellCubeCoordinate.CubeCoordinate) && unitWorldIndex == cellWorldIndex.Value && cellAttribute.CellAttributes.Cell.UnitOnCellId == unitStateVar.EntityId)
                {
                    //Debug.Log("CleanUpUnit with id: " + unitState.EntityId);
                    cellAttribute.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAttribute.CellAttributes, false, 0, cellWorldIndex.Value);
                }
            });

            PostUpdateCommands.RemoveComponent<UnitStateData>(e);
        });
    }
}
