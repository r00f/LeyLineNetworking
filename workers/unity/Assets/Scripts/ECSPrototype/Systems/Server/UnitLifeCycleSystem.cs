using UnityEngine;
using System.Collections;
using Unity.Entities;
using Cells;
using Improbable.Gdk.Core;


[UpdateAfter(typeof(HandleCellGridRequestsSystem))]
public class UnitLifeCycleSystem : ComponentSystem
{
    public struct UnitStateData : ISystemStateComponentData
    {
        public Generic.CubeCoordinate.Component CubeCoordState;
    }

    private struct UnitAddedData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly EntityArray Entities;
        public readonly ComponentDataArray<Unit.ServerPath.Component> PathData;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
        public SubtractiveComponent<UnitStateData> CoordinateState;
    }

    [Inject] private UnitAddedData m_UnitAddedData;

    private struct UnitChangedData
    {
        public readonly int Length;
        public EntityArray Entities;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
        public ComponentDataArray<UnitStateData> CoordinateState;
    }

    [Inject] private UnitChangedData m_UnitChangedData;

    public struct UnitRemovedData
    {
        public readonly int Length;
        public readonly EntityArray Entities;
        public SubtractiveComponent<Generic.CubeCoordinate.Component> SubstractiveCoordinateData;
        public readonly ComponentDataArray<UnitStateData> CoordinateState;
    }

    [Inject] private UnitRemovedData m_UnitRemovedData;

    public struct CellData
    {
        public readonly int Length;
        public ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
        public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CubeCoordinateData;
    }

    [Inject] private CellData m_CellData;

    [Inject] private HandleCellGridRequestsSystem m_CellGridSystem;

    protected override void OnUpdate()
    {

        for (int i = 0; i < m_UnitAddedData.Length; i++)
        {
            //Debug.Log("Added Unit");

            var unitCubeCoordinate = m_UnitAddedData.CoordinateData[i].CubeCoordinate;
            var unitEntityId = m_UnitAddedData.EntityIds[i];

            for (int ci = 0; ci < m_CellData.Length; ci++)
            {
                var cellCubeCoordinate = m_CellData.CubeCoordinateData[ci];

                if (unitCubeCoordinate == cellCubeCoordinate.CubeCoordinate)
                {
                    var cellAtt = m_CellData.CellAttributes[ci];

                    //Debug.Log("Set cellAttributes at coordinate: " + cellCubeCoordinate.CubeCoordinate);

                    cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, true, unitEntityId.EntityId);

                    m_CellData.CellAttributes[ci] = cellAtt;

                    //m_CellGridSystem.UpdateNeighbours(m_CellData.CellAttributes[ci].CellAttributes.Cell, m_CellData.CellAttributes[ci].CellAttributes.Neighbours);
                }
            }

            PostUpdateCommands.AddComponent(m_UnitAddedData.Entities[i], new UnitStateData { CubeCoordState = m_UnitAddedData.CoordinateData[i] });
        }

        for (int i = 0; i < m_UnitChangedData.Length; i++)
        {
            if (m_UnitChangedData.CoordinateState[i].CubeCoordState.CubeCoordinate != m_UnitChangedData.CoordinateData[i].CubeCoordinate)
            {
                //Debug.Log("Update Unit state data");
                // there are some changed to the component, do some stuff
                PostUpdateCommands.SetComponent(m_UnitChangedData.Entities[i], new UnitStateData { CubeCoordState = m_UnitChangedData.CoordinateData[i] });
            }
        }


        for (int i = 0; i < m_UnitRemovedData.Length; i++)
        {
            //var coordinate = m_UnitData.CoordinateData[i];
            //var entity = m_UnitData.Entities[i];

            var unitCubeCoordinate = m_UnitRemovedData.CoordinateState[i].CubeCoordState.CubeCoordinate;

            for(int ci = 0; ci <m_CellData.Length; ci++)
            {
                var cellCubeCoordinate = m_CellData.CubeCoordinateData[ci];

                if(unitCubeCoordinate == cellCubeCoordinate.CubeCoordinate)
                {
                    var cellAtt = m_CellData.CellAttributes[ci];

                    //Debug.Log("Clean up cell at coordinate: " + cellCubeCoordinate.CubeCoordinate);

                    cellAtt.CellAttributes = m_CellGridSystem.SetCellAttributes(cellAtt.CellAttributes, false, new EntityId());

                    m_CellData.CellAttributes[ci] = cellAtt;

                    //m_CellGridSystem.UpdateNeighbours(m_CellData.CellAttributes[ci].CellAttributes.Cell, m_CellData.CellAttributes[ci].CellAttributes.Neighbours);
                }
            }


            //Debug.Log("Removed Unit");

            PostUpdateCommands.RemoveComponent<UnitStateData>(m_UnitRemovedData.Entities[i]);

        }
    }
}
