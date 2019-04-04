using Unity.Entities;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Core.Commands;
using Improbable.PlayerLifecycle;
using Improbable.Gdk.Core;
using Generic;
using Cells;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(InitializePlayerSystem))]
    public class SpawnUnitsSystem : ComponentSystem
    {
        public struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<CellAttributesComponent.Component>> AuthorativeData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<Improbable.Position.Component> Position;
            public readonly ComponentDataArray<CellAttributesComponent.Component> CellAttributes;
            public ComponentDataArray<UnitToSpawn.Component> UnitToSpawn;
            public readonly ComponentDataArray<CubeCoordinate.Component> CoordinateData;
            public ComponentDataArray<WorldCommands.CreateEntity.CommandSender> CreateEntitySender;
        }

        [Inject] private CellData m_Data;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<GameState.Component>> AuthorativeData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<GameState.Component> GameState;
        }

        [Inject] private GameStateData m_GameStateData;


        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<FactionComponent.Component> Faction;
            public readonly ComponentDataArray<Player.PlayerState.Component> Playerstate;
            public readonly ComponentDataArray<OwningWorker.Component> OwningWorker;
        }

        [Inject] private PlayerData m_PlayerData;

        protected override void OnUpdate()
        {
            for (int gi = 0; gi < m_GameStateData.Length; gi++)
            {
                var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                for (int pi = 0; pi < m_PlayerData.Length; pi++)
                {
                    var playerFaction = m_PlayerData.Faction[pi];
                    var owningWorker = m_PlayerData.OwningWorker[pi];
                    var worldIndex = m_PlayerData.WorldIndexData[pi].Value;

                    if (worldIndex == gameStateWorldIndex)
                    {
                        var gameState = m_GameStateData.GameState[gi].CurrentState;
                        if (gameState == GameStateEnum.spawning)
                        {
                            for (int i = 0; i < m_Data.Length; ++i)
                            {
                                var coord = m_Data.CoordinateData[i].CubeCoordinate;
                                var position = m_Data.Position[i];
                                var requestSender = m_Data.CreateEntitySender[i];
                                var cell = m_Data.CellAttributes[i];
                                var unitToSpawn = m_Data.UnitToSpawn[i];
                                var cellWorldIndex = m_Data.WorldIndexData[i].Value;

                                if(cellWorldIndex == gameStateWorldIndex)
                                {
                                    if (unitToSpawn.UnitName.Length != 0)
                                    {
                                        if (unitToSpawn.Faction == playerFaction.Faction)
                                        {
                                            //Debug.Log("SPAWNUNIT");
                                            var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName).GetComponent<Unit_BaseDataSet>();
                                            var entity = LeyLineEntityTemplates.Unit(owningWorker.WorkerId, unitToSpawn.UnitName, position, coord, playerFaction, worldIndex, Stats);

                                            requestSender.RequestsToSend.Add(WorldCommands.CreateEntity.CreateRequest
                                            (
                                                entity
                                            ));

                                            m_Data.CreateEntitySender[i] = requestSender;

                                            unitToSpawn.UnitName = "";
                                            //unitToSpawn.Faction = 0;
                                            m_Data.UnitToSpawn[i] = unitToSpawn;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}