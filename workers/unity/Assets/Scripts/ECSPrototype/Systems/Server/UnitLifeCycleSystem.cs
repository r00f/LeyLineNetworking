using UnityEngine;
using System.Collections;
using Unity.Entities;
using Cells;
using Unit;
using Generic;
using Improbable.Gdk.Core;


[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(AddComponentsSystem))]
public class UnitLifeCycleSystem : ComponentSystem
{
    public struct UnitStateData : ISystemStateComponentData
    {
        public CubeCoordinate.Component CubeCoordState;
        public WorldIndex.Component WorldIndexState;
    }
   

    private struct UnitAddedData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly EntityArray Entities;
        public readonly ComponentDataArray<ServerPath.Component> PathData;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public SubtractiveComponent<UnitStateData> UnitState;
    }

    [Inject] private UnitAddedData m_UnitAddedData;

    private struct UnitChangedData
    {
        public readonly int Length;
        public EntityArray Entities;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
        public ComponentDataArray<UnitStateData> UnitState;
    }

    [Inject] private UnitChangedData m_UnitChangedData;

    public struct UnitRemovedData
    {
        public readonly int Length;
        public readonly EntityArray Entities;
        public SubtractiveComponent<CubeCoordinate.Component> SubstractiveCoordinateData;
        public readonly ComponentDataArray<UnitStateData> UnitState;
    }

    [Inject] private UnitRemovedData m_UnitRemovedData;

    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        public readonly ComponentDataArray<CubeCoordinate.Component> CubeCoordinateData;
    }

    [Inject] private CellData m_CellData;

    [Inject] private HandleCellGridRequestsSystem m_CellGridSystem;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_UnitAddedData.Length; i++)
        {
            var unitWorldIndex = m_UnitAddedData.WorldIndexData[i].Value;
            var unitCubeCoordinate = m_UnitAddedData.CoordinateData[i].CubeCoordinate;
            var unitEntityId = m_UnitAddedData.EntityIds[i];

            for (int ci = 0; ci < m_CellData.Length; ci++)
            {
                var cellCubeCoordinate = m_CellData.CubeCoordinateData[ci];
                var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;
                var cellAtt = m_CellData.CellAttributes[ci];

                if (unitCubeCoordinate == cellCubeCoordinate.CubeCoordinate && unitWorldIndex == cellWorldIndex)
                {
                    cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, unitEntityId.EntityId, cellWorldIndex);
                    m_CellData.CellAttributes[ci] = cellAtt;
                }
            }
            PostUpdateCommands.AddComponent(m_UnitAddedData.Entities[i], new UnitStateData { CubeCoordState = m_UnitAddedData.CoordinateData[i], WorldIndexState = m_UnitAddedData.WorldIndexData[i] });
        }

        for (int i = 0; i < m_UnitChangedData.Length; i++)
        {
            if (m_UnitChangedData.UnitState[i].CubeCoordState.CubeCoordinate != m_UnitChangedData.CoordinateData[i].CubeCoordinate || m_UnitChangedData.UnitState[i].WorldIndexState.Value != m_UnitChangedData.WorldIndexData[i].Value)
            {
                PostUpdateCommands.SetComponent(m_UnitChangedData.Entities[i], new UnitStateData { CubeCoordState = m_UnitChangedData.CoordinateData[i], WorldIndexState = m_UnitChangedData.WorldIndexData[i] });
            }
        }

        for (int i = 0; i < m_UnitRemovedData.Length; i++)
        {
            var unitCubeCoordinate = m_UnitRemovedData.UnitState[i].CubeCoordState.CubeCoordinate;
            var unitWorldIndex = m_UnitRemovedData.UnitState[i].WorldIndexState.Value;

            for (int ci = 0; ci <m_CellData.Length; ci++)
            {
                var cellCubeCoordinate = m_CellData.CubeCoordinateData[ci];
                var cellAtt = m_CellData.CellAttributes[ci];
                var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;

                if (unitCubeCoordinate == cellCubeCoordinate.CubeCoordinate && unitWorldIndex == cellWorldIndex)
                {
                    cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, new EntityId(), cellWorldIndex);
                    m_CellData.CellAttributes[ci] = cellAtt;
                }
            }
            PostUpdateCommands.RemoveComponent<UnitStateData>(m_UnitRemovedData.Entities[i]);

        }
    }
}
