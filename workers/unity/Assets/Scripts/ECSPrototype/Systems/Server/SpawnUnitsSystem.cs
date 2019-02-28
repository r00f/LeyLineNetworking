using Unity.Entities;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Core.Commands;
using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;

namespace LeyLineHybridECS
{
    [UpdateAfter(typeof(InitializePlayerSystem))]
    public class SpawnUnitsSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentDataArray<Improbable.Position.Component> Position;
            public ComponentDataArray<Cells.UnitToSpawn.Component> UnitToSpawn;
            public ComponentDataArray<Cells.CellAttributesComponent.Component> CellData;
            public readonly ComponentDataArray<Generic.CubeCoordinate.Component> CoordinateData;
            public ComponentDataArray<WorldCommands.CreateEntity.CommandSender> CreateEntitySender;
        }

        [Inject] private Data m_Data;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Generic.GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;


        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Generic.FactionComponent.Component> Faction;
            public readonly ComponentDataArray<Player.PlayerState.Component> Playerstate;
            public readonly ComponentDataArray<OwningWorker.Component> OwningWorker;
        }

        [Inject] private PlayerData m_PlayerData;

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
                        for (int pi = 0; pi < m_PlayerData.Length; pi++)
                        {
                            var playerFaction = m_PlayerData.Faction[pi];
                            var owningWorker = m_PlayerData.OwningWorker[pi];

                            if(unitToSpawn.Faction == playerFaction.Faction)
                            {
                                Debug.Log("SPAWNUNIT");

                                var entity = LeyLineEntityTemplates.Unit(owningWorker.WorkerId, unitToSpawn.UnitName, position, coord, playerFaction);

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
                    }
                }
            }
        }
    }
}