using Unity.Entities;
using Cell;
using Unit;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unity.Jobs;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem)), UpdateAfter(typeof(SpawnUnitsSystem))]
public class UnitLifeCycleSystem : JobComponentSystem
{
    public struct UnitStateData : ISystemStateComponentData
    {
        public FactionComponent.Component FactionState;
        public CubeCoordinate.Component CubeCoordState;
        public WorldIndexShared WorldIndexState;
        public long EntityId;
    }

    HandleCellGridRequestsSystem m_CellGridSystem;
    GameStateSystem m_GameStateSystem;
    EntityQuery m_PlayerData;
    EntityQuery m_CellData;
    EntityQuery m_UnitAddedData;
    EntityQuery m_UnitRemovedData;
    private EntityQuery m_ManalithData;
    EntityQuery m_UnitChangedData;
    private EntityQuery m_ManalithUnitAddedData;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_CellGridSystem = World.GetExistingSystem<HandleCellGridRequestsSystem>();
        m_GameStateSystem = World.GetExistingSystem<GameStateSystem>();

        /*
        var unitAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[] 
            {
                typeof(UnitStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadOnly<Health.Component>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>()
            }
        };
        m_UnitAddedData = GetEntityQuery(unitAddedDesc);

        var manalithUnitAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(UnitStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<Manalith.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Actions.Component>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>()
            }
        };

        m_ManalithUnitAddedData = GetEntityQuery(manalithUnitAddedDesc);

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

        m_ManalithData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadWrite<Manalith.Component>(),
        ComponentType.ReadWrite<FactionComponent.Component>()
        );

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<PlayerAttributes.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<Vision.Component>()
        );

        m_UnitChangedData = GetEntityQuery(
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<UnitStateData>()
            );

        m_CellData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadWrite<CellAttributesComponent.Component>()
            );
            */
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithNone<UnitStateData>().ForEach((Entity e, in WorldIndexShared unitWorldIndex, in CubeCoordinate.Component unitCubeCoordinate, in FactionComponent.Component unitFaction, in SpatialEntityId unitEntityId) =>
        {
            var coordComp = unitCubeCoordinate;
            var coord = Vector3fext.ToUnityVector(unitCubeCoordinate.CubeCoordinate);
            var worldIndex = unitWorldIndex;
            var id = unitEntityId.EntityId.Id;

            Entities.WithSharedComponentFilter(unitWorldIndex).ForEach((ref CubeCoordinate.Component cellCubeCoordinate, ref CellAttributesComponent.Component cellAttribute, in WorldIndexShared cellWorldIndex) =>
            {
                if (coord == Vector3fext.ToUnityVector(cellCubeCoordinate.CubeCoordinate))
                {
                    cellAttribute.CellAttributes = m_GameStateSystem.SetCellAttributes(cellAttribute.CellAttributes, true, id, cellWorldIndex);
                }
            })
            .WithoutBurst()
            .Run();

            EntityManager.AddComponentData(e, new UnitStateData { CubeCoordState = coordComp, WorldIndexState = worldIndex, EntityId = unitEntityId.EntityId.Id, FactionState = unitFaction });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.ForEach((Entity e, in CubeCoordinate.Component unitCubeCoord, in UnitStateData unitState, in WorldIndexShared unitWorldIndex, in FactionComponent.Component unitFaction, in SpatialEntityId unitEntityId) =>
        {
            if (Vector3fext.ToUnityVector(unitState.CubeCoordState.CubeCoordinate) != Vector3fext.ToUnityVector(unitCubeCoord.CubeCoordinate) || unitState.WorldIndexState.Value != unitWorldIndex.Value)
            {
                EntityManager.SetComponentData(e, new UnitStateData { CubeCoordState = unitCubeCoord, WorldIndexState = unitWorldIndex, EntityId = unitEntityId.EntityId.Id, FactionState = unitFaction});
            }
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<CubeCoordinate.Component>().ForEach((Entity e, in UnitStateData unitState) =>
        {
            var unitStateVar = unitState;
            var unitCubeCoordinate = Vector3fext.ToUnityVector(unitState.CubeCoordState.CubeCoordinate);
            var unitWorldIndex = unitState.WorldIndexState;

            //Tell player to update his vision
            Entities.WithAll<PlayerAttributes.Component>().ForEach((ref Vision.Component p_Vision, in FactionComponent.Component p_Faction) =>
            {
                if(p_Faction.Faction == unitStateVar.FactionState.Faction)
                    p_Vision.RequireUpdate = true;
            })
            .WithoutBurst()
            .Run();

            Entities.WithSharedComponentFilter(unitWorldIndex).ForEach((ref CellAttributesComponent.Component cellAttribute, in CubeCoordinate.Component cellCubeCoordinate) =>
            {
                if (unitCubeCoordinate == Vector3fext.ToUnityVector(cellCubeCoordinate.CubeCoordinate) && cellAttribute.CellAttributes.Cell.UnitOnCellId == unitStateVar.EntityId)
                {
                    cellAttribute.CellAttributes = m_GameStateSystem.SetCellAttributes(cellAttribute.CellAttributes, false, 0, unitWorldIndex);
                }
            })
            .WithoutBurst()
            .Run();

            EntityManager.RemoveComponent<UnitStateData>(e);
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        return inputDeps;
    }
}
