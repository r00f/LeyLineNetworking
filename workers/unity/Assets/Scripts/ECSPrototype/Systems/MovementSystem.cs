using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using System; 

namespace LeyLineHybridECS
{
    public class MovementSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public ComponentDataArray<MovementData> MovementData;
            public ComponentArray<Heading3D> HeadingData;
            public ComponentDataArray<Position3D> Position3DData;
            public ComponentArray<OccupiedCell> OccupiedCellData;
            public ComponentArray<PathLists> PathListsData;
            public ComponentArray<IsIdle> IsIdleData;
        }

        [Inject] private Data m_Data;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            //terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        }

        protected override void OnUpdate()
        {

            for (int i = 0; i < m_Data.Length; ++i)
            {
                var heading = m_Data.HeadingData[i];
                var occupiedCell = m_Data.OccupiedCellData[i];
                var position = m_Data.Position3DData[i];
                var pathLists = m_Data.PathListsData[i];
                var movementData = m_Data.MovementData[i];
                var isIdle = m_Data.IsIdleData[i];

                //if the GameState is executing and the unit has a Path Planned
                if (GameStateSystem.CurrentState == GameStateSystem.State.Moving && pathLists.CurrentPath.Count != 0)
                {
                    if (isIdle.Value)
                    {
                        movementData.PathIndex = 0;
                        occupiedCell.Cell.GetComponent<UnitOnCell>().Value = null;
                        occupiedCell.Cell.GetComponent<IsTaken>().Value = false;
                        occupiedCell.Cell = pathLists.CurrentPath[0];
                        occupiedCell.Cell.GetComponent<UnitOnCell>().Value = occupiedCell.GetComponent<Unit>();
                        occupiedCell.gameObject.GetComponent<UnitVisionData>().RequireUpdate = true;
                        heading.Value = pathLists.CurrentPath[0].GetComponent<Position3DDataComponent>().Value.Value;
                        isIdle.Value = false;
                    }

                    if(!position.Value.Equals(heading.Value))
                    {
                        m_Data.Position3DData[i] = new Position3D{

                            Value = Vector3.MoveTowards(position.Value, heading.Value, Time.deltaTime * movementData.Speed)
                        };

                    }
                    else if (m_Data.MovementData[i].PathIndex < pathLists.CurrentPath.Count - 1 && !pathLists.CurrentPath[m_Data.MovementData[i].PathIndex + 1].GetComponent<IsTaken>().Value)
                    {
                        //movementData.PathIndex++;
                        MovementData tmpMovementData = new MovementData
                        {
                            Range = movementData.Range,
                            Speed = movementData.Speed,
                            PathIndex = movementData.PathIndex + 1
                        };
                        m_Data.MovementData[i] = tmpMovementData;
                        occupiedCell.Cell.GetComponent<UnitOnCell>().Value = null;
                        occupiedCell.Cell = pathLists.CurrentPath[m_Data.MovementData[i].PathIndex];
                        occupiedCell.Cell.GetComponent<UnitOnCell>().Value = occupiedCell.GetComponent<Unit>();
                        heading.Value = pathLists.CurrentPath[m_Data.MovementData[i].PathIndex].GetComponent<Position3DDataComponent>().Value.Value;
                        occupiedCell.gameObject.GetComponent<UnitVisionData>().RequireUpdate = true;
                    }
                    else
                    {
                        occupiedCell.Cell.GetComponent<UnitOnCell>().Value = null;
                        occupiedCell.Cell = pathLists.CurrentPath[movementData.PathIndex];
                        occupiedCell.Cell.GetComponent<UnitOnCell>().Value = occupiedCell.GetComponent<Unit>();
                        heading.Value = pathLists.CurrentPath[movementData.PathIndex].GetComponent<Position3DDataComponent>().Value.Value;
                        pathLists.CurrentPath[movementData.PathIndex].GetComponent<IsTaken>().Value = true;
                        occupiedCell.gameObject.GetComponent<UnitVisionData>().RequireUpdate = true;
                        MovementData tmpMovementData = new MovementData
                        {
                            Range = movementData.Range,
                            Speed = movementData.Speed,
                            PathIndex = 0
                        };
                        m_Data.MovementData[i] = tmpMovementData;
                        pathLists.CurrentPath.Clear();
                        isIdle.Value = true;
                    }

                }
            }

        }
    }

}
