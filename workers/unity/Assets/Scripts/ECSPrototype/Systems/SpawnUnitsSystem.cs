using Unity.Entities;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Core.Commands;

namespace LeyLineHybridECS
{
    public class SpawnUnitsSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<Improbable.Position.Component> Position;
            public ComponentDataArray<Cells.UnitToSpawn.Component> UnitToSpawn;
            public ComponentDataArray<Cells.CubeCoordinate.Component> CoordinateData;
            public ComponentDataArray<WorldCommands.CreateEntity.CommandSender> CreateEntitySender;
        }

        [Inject] private Data m_Data;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Generic.GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;


        protected override void OnUpdate()
        {
            if (m_GameStateData.GameState[0].CurrentState == Generic.GameStateEnum.spawning)
            {
                for (int i = 0; i < m_Data.Length; ++i)
                {
                    var coord = m_Data.CoordinateData[i].CubeCoordinate;
                    var position = m_Data.Position[i];
                    var unitToSpawn = m_Data.UnitToSpawn[i];
                    var requestSender = m_Data.CreateEntitySender[i];

                    if (unitToSpawn.UnitName.Length != 0)
                    {
                        var entity = LeyLineEntityTemplates.Unit(unitToSpawn.UnitName, position, coord, 1);

                        requestSender.RequestsToSend.Add(WorldCommands.CreateEntity.CreateRequest
                        (
                            entity
                        ));

                        m_Data.CreateEntitySender[i] = requestSender;

                        var unitToSpawnComponent = m_Data.UnitToSpawn[i];
                        unitToSpawnComponent.UnitName = "";
                        m_Data.UnitToSpawn[i] = unitToSpawnComponent;
                    }
                }

                //GameStateSystem.CurrentState = GameStateSystem.State.Attacking;

            }
        }
    }
}