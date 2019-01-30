using Unity.Entities;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    public class SpawnUnitsSystem : ComponentSystem
    {

        struct CellData
        {
            public Position3D Position;
            public UnitToSpawn UnitToSpawn;
            public Cell Cell;
        }

        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<Position3D> Position;
            public ComponentArray<UnitToSpawn> UnitToSpawn;
            public ComponentArray<Cell> Cell;
        }

        [Inject] private Data m_Data;

        protected override void OnUpdate()
        {
            if(GameStateSystem.CurrentState == GameStateSystem.State.Spawning)
            {
                var unitSpawnData = new List<UnitSpawnData>();

                for (int i = 0; i < m_Data.Length; ++i)
                {
                    var position = m_Data.Position[i];
                    var unitToSpawn = m_Data.UnitToSpawn[i];
                    var cell = m_Data.Cell[i];

                    if (unitToSpawn.Unit)
                    {
                        UnitSpawnData spawn = new UnitSpawnData()
                        {
                            Position = position.Value,
                            UnitToSpawn = unitToSpawn.Unit,
                            Cell = cell
                        };
                        unitSpawnData.Add(spawn);

                        m_Data.UnitToSpawn[i].Unit = null;
                    }
                }

                foreach (UnitSpawnData u in unitSpawnData)
                {
                    UnitSpawnSystem.SpawnUnit(u);
                }

                //when all Units have been spawned, move to attack phase
                if(unitSpawnData.Count == 0)
                {
                    GameStateSystem.CurrentState = GameStateSystem.State.Attacking;
                }

            }
        }
    }
}