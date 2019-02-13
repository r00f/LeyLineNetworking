using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using Generic;
using Unit;
using Improbable;
using System; 

namespace LeyLineHybridECS
{
    public class MovementSystem : ComponentSystem
    {
        public struct UnitData
        {
            public readonly int Length;
            public ComponentDataArray<CellsToMark.Component> CellsToMark;
            public ComponentDataArray<CubeCoordinate.Component> CubeCoordinates;
            public ComponentDataArray<CurrentPath.Component> CurrentPaths;
            public ComponentDataArray<Position.Component> Positions;
        }

        [Inject] private UnitData m_UnitData;

        public struct GameStateData
        {
            public readonly ComponentDataArray<GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            //terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        }

        protected override void OnUpdate()
        {

            for (int i = 0; i < m_UnitData.Length; ++i)
            {
                var currentPath = m_UnitData.CurrentPaths[i];
                var position = m_UnitData.Positions[i];
                var coord = m_UnitData.CubeCoordinates[i];
                var cellsToMark = m_UnitData.CellsToMark[i];

                if (m_GameStateData.GameState[0].CurrentState == GameStateEnum.moving)
                {

                    if (currentPath.Path.CellAttributes.Count != 0)
                    {
                        if (position.Coords.ToUnityVector() != currentPath.Path.CellAttributes[0].Position.ToUnityVector())
                        {
                            Vector3 newPos = Vector3.MoveTowards(position.Coords.ToUnityVector(), currentPath.Path.CellAttributes[0].Position.ToUnityVector(), Time.deltaTime);
                            position.Coords = new Coordinates(newPos.x, newPos.y, newPos.z);
                            m_UnitData.Positions[i] = position;
                        }
                        else
                        {
                            coord.CubeCoordinate = currentPath.Path.CellAttributes[0].CubeCoordinate;
                            m_UnitData.CubeCoordinates[i] = coord;
                            currentPath.Path.CellAttributes.RemoveAt(0);
                        }
                    }
                }
                else if (m_GameStateData.GameState[0].CurrentState == GameStateEnum.calculate_energy)
                {
                    cellsToMark.CachedPaths = new Dictionary<Cells.CellAttribute, CellAttributeList>();
                    cellsToMark.CellsInRange = new List<Cells.CellAttributes>();

                    m_UnitData.CellsToMark[i] = cellsToMark;
                }
            }
        }
    }
}
