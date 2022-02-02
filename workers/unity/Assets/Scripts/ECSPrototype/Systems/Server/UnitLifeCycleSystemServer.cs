using Unity.Entities;
using Cell;
using Unit;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCellGridRequestsSystem))]
public class UnitLifeCycleSystemServer : JobComponentSystem
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
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.WithNone<UnitStateData>().ForEach((Entity e, in WorldIndexShared unitWorldIndex, in CubeCoordinate.Component unitCubeCoordinate, in FactionComponent.Component unitFaction, in SpatialEntityId unitEntityId, in StartRotation.Component startRotation) =>
        {
            var coordComp = unitCubeCoordinate;
            var coord = Vector3fext.ToUnityVector(unitCubeCoordinate.CubeCoordinate);
            var worldIndex = unitWorldIndex;
            var id = unitEntityId.EntityId.Id;

            Entities.WithSharedComponentFilter(unitWorldIndex).ForEach((CurrentMapState mapData) =>
            {
                var cell = mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(coordComp.CubeCoordinate)];
                cell.IsTaken = true;
                cell.UnitOnCellId = id;
                mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(coordComp.CubeCoordinate)] = cell;
            })
            .WithoutBurst()
            .Run();

            var unitStateData = new UnitStateData { CubeCoordState = coordComp, WorldIndexState = worldIndex, EntityId = unitEntityId.EntityId.Id, FactionState = unitFaction };
            var unitVision = new UnitVision
            {
                InitialWaitTime = .1f,
                VisionRange = startRotation.VisionRange,
                Vision = new FixedList512<Vector2i>()
            };

            EntityManager.AddComponents(e, new ComponentTypes(typeof(UnitStateData), typeof(UnitVision)));
            EntityManager.SetComponentData(e, unitVision);
            EntityManager.SetComponentData(e, unitStateData);
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.ForEach((Entity e, in WorldIndexShared unitWorldIndex, in CubeCoordinate.Component cubeCoord, in UnitStateData unitState, in FactionComponent.Component unitFaction, in SpatialEntityId entityId, in Health.Component health, in IncomingActionEffects.Component incomingEffects) =>
        {
            if (Vector3fext.ToUnityVector(unitState.CubeCoordState.CubeCoordinate) != Vector3fext.ToUnityVector(cubeCoord.CubeCoordinate))
            {
                var unitDict = new Dictionary<Vector3f, long>();

                if (incomingEffects.MoveEffects.Count != 0 && health.CurrentHealth > 0 && incomingEffects.MoveEffects[0].EffectType == EffectTypeEnum.move_along_path)
                {
                    if (!unitDict.ContainsKey(cubeCoord.CubeCoordinate))
                        unitDict.Add(cubeCoord.CubeCoordinate, entityId.EntityId.Id);
                    if (!unitDict.ContainsKey(incomingEffects.MoveEffects[0].MoveAlongPathNested.OriginCoordinate))
                        unitDict.Add(incomingEffects.MoveEffects[0].MoveAlongPathNested.OriginCoordinate, entityId.EntityId.Id);
                }

                Entities.WithSharedComponentFilter(unitWorldIndex).ForEach((CurrentMapState currentMapState) =>
                {
                    for (int i = 0; i < unitDict.Count; i++)
                    {
                        MapCell cell = currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitDict.ElementAt(i).Key)];

                        if (cell.UnitOnCellId != unitDict.ElementAt(i).Value)
                        {
                            cell.IsTaken = true;
                            cell.UnitOnCellId = unitDict.ElementAt(i).Value;
                        }
                        else if (cell.IsTaken)
                        {
                            cell.IsTaken = false;
                            cell.UnitOnCellId = 0;
                        }
                        currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitDict.ElementAt(i).Key)] = cell;
                    }
                })
                .WithoutBurst()
                .Run();

                EntityManager.SetComponentData(e, new UnitStateData { WorldIndexState = unitWorldIndex, CubeCoordState = cubeCoord, EntityId = entityId.EntityId.Id, FactionState = unitFaction });
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

            Entities.WithSharedComponentFilter(unitWorldIndex).ForEach((CurrentMapState mapData) =>
            {
                var cell = mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitStateVar.CubeCoordState.CubeCoordinate)];
                cell.IsTaken = false;
                cell.UnitOnCellId = 0;
                mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitStateVar.CubeCoordState.CubeCoordinate)] = cell;
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
