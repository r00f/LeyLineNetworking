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

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class UnitLifeCycleSystemClient : JobComponentSystem
{
    public struct UnitStateData : ISystemStateComponentData
    {
        public FactionComponent.Component FactionState;
        public CubeCoordinate.Component CubeCoordState;
        public long EntityId;
    }

    EntityQuery m_GameStateData;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadWrite<CurrentMapState>()
        );

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_GameStateData.CalculateEntityCount() != 1)
            return inputDeps;

        var gameStateEntity = m_GameStateData.GetSingletonEntity();
        var currentMapState = EntityManager.GetComponentObject<CurrentMapState>(gameStateEntity);

        //added units mapstate update
        Entities.WithNone<UnitStateData>().ForEach((Entity e, in CubeCoordinate.Component unitCubeCoordinate, in FactionComponent.Component unitFaction, in SpatialEntityId unitEntityId, in StartRotation.Component startRotation) =>
        {
            var cell = currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitCubeCoordinate.CubeCoordinate)];
            cell.IsTaken = true;
            cell.UnitOnCellId = unitEntityId.EntityId.Id;
            currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitCubeCoordinate.CubeCoordinate)] = cell;

            var unitStateData = new UnitStateData { CubeCoordState = unitCubeCoordinate, EntityId = unitEntityId.EntityId.Id, FactionState = unitFaction };

            EntityManager.AddComponents(e, new ComponentTypes(typeof(UnitStateData), typeof(UnitVision)));
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        //deleted units mapstate update
        Entities.WithNone<CubeCoordinate.Component>().ForEach((Entity e, in UnitStateData unitState) =>
        {
            var cell = currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitState.CubeCoordState.CubeCoordinate)];
            if (cell.UnitOnCellId == unitState.EntityId)
            {
                cell.IsTaken = false;
                cell.UnitOnCellId = 0;
                currentMapState.CoordinateCellDictionary[CellGridMethods.CubeToAxial(unitState.CubeCoordState.CubeCoordinate)] = cell;
                EntityManager.RemoveComponent<UnitStateData>(e);
            }
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        //moved units mapstate update
        Entities.ForEach((Entity e, in CubeCoordinate.Component cubeCoord, in UnitStateData unitState, in FactionComponent.Component unitFaction, in SpatialEntityId entityId, in Health.Component health, in IncomingActionEffects.Component incomingEffects) =>
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
                EntityManager.SetComponentData(e, new UnitStateData { CubeCoordState = cubeCoord,  EntityId = entityId.EntityId.Id, FactionState = unitFaction});
            }
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        return inputDeps;
    }
}
