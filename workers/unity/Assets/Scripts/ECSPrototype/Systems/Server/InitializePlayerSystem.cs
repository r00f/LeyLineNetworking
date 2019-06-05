using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;
using Player;
using Generic;
using System.Collections.Generic;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk;
using Cell;
using Improbable;
using Improbable.Gdk.ReactiveComponents;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class InitializePlayerSystem : ComponentSystem
    {
        
        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<PlayerAttributes.Component> PlayerAttributes;
            public readonly ComponentDataArray<PlayerStateData> WorldIndexState;
            public ComponentDataArray<FactionComponent.Component> FactionComponent;
        }

        [Inject] PlayerData m_PlayerData;

        public struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<CellAttributesComponent.Component>> AuthorativeData;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<IsSpawn.Component> IsSpawnData;
            public ComponentDataArray<UnitToSpawn.Component> UnitToSpawnData;
        }

        [Inject] CellData m_CellData;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public readonly ComponentDataArray<Position.Component> Positions;
            public ComponentDataArray<GameState.Component> GameStates;
        }

        [Inject] GameStateData m_GameStateData;

        public struct PlayerStateData : ISystemStateComponentData
        {
            public WorldIndex.Component WorldIndexState;
        }

        private struct PlayerAddedData
        {
            public readonly int Length;
            public readonly EntityArray Entities;
            public readonly ComponentDataArray<Authoritative<PlayerAttributes.Component>> AuthorativeData;
            public ComponentDataArray<Position.Component> Positions;
            public ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<FactionComponent.Component> FactionComponent;
            public SubtractiveComponent<PlayerStateData> WorldIndexState;
        }

        [Inject] PlayerAddedData m_PlayerAddedData;


        public struct PlayerRemovedData
        {
            public readonly int Length;
            public readonly EntityArray Entities;
            public SubtractiveComponent<Authoritative<WorldIndex.Component>> SubstractiveAuth;
            public SubtractiveComponent<PlayerAttributes.Component> SubtractivePlayerData;
            public SubtractiveComponent<WorldIndex.Component> SubstractiveCoordinateData;
            public readonly ComponentDataArray<PlayerStateData> WorldIndexState;
        }

        [Inject] PlayerRemovedData m_PlayerRemovedData;

        protected override void OnUpdate()
        {

            for (int i = 0; i < m_PlayerAddedData.Length; i++)
            {
                var worldIndex = m_PlayerAddedData.WorldIndexData[i];
                var pos = m_PlayerAddedData.Positions[i];

                if(worldIndex.Value == 0)
                {
                    uint wIndex = SetPlayerWorld();

                    worldIndex.Value = wIndex;
                    m_PlayerAddedData.WorldIndexData[i] = worldIndex;

                    //Debug.Log(wIndex + ", " + m_PlayerAddedData.WorldIndexData[i].Value);

                    for (int gi = 0; gi < m_GameStateData.Length; gi++)
                    {
                        var mapWorldIndex = m_GameStateData.WorldIndexData[gi].Value;
                        var gameStatePos = m_GameStateData.Positions[gi];

                        if (wIndex == mapWorldIndex)
                            pos.Coords = gameStatePos.Coords;
                    }

                    m_PlayerAddedData.Positions[i] = pos;
                }

                PostUpdateCommands.AddComponent(m_PlayerAddedData.Entities[i], new PlayerStateData { WorldIndexState = worldIndex });
            }

            for (int i = 0; i < m_PlayerRemovedData.Length; i++)
            {
                var worldIndex = m_PlayerRemovedData.WorldIndexState[i].WorldIndexState.Value;

                for (int gi = 0; gi < m_GameStateData.Length; gi++)
                {
                    var gameStateWorldIndex = m_GameStateData.WorldIndexData[gi].Value;

                    if (gameStateWorldIndex == worldIndex)
                    {
                        var gameState = m_GameStateData.GameStates[gi];
                        gameState.PlayersOnMapCount--;
                        m_GameStateData.GameStates[gi] = gameState;
                    }
                }

                PostUpdateCommands.RemoveComponent<PlayerStateData>(m_PlayerRemovedData.Entities[i]);
            }

            
            for(int i = 0; i < m_PlayerData.Length; i++)
            {
                var worldIndex = m_PlayerData.WorldIndexState[i];
                var factionComp = m_PlayerData.FactionComponent[i];
                var playerAttributes = m_PlayerData.PlayerAttributes[i];

                if (worldIndex.WorldIndexState.Value != 0 && factionComp.Faction == 0)
                {
                    factionComp = SetPlayerFaction(worldIndex.WorldIndexState.Value);
                    m_PlayerData.FactionComponent[i] = factionComp;

                    for (int ci = 0; ci < m_CellData.Length; ci++)
                    {
                        var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;
                        var unitToSpawn = m_CellData.UnitToSpawnData[ci];

                        if (cellWorldIndex == worldIndex.WorldIndexState.Value)
                        {
                            if (unitToSpawn.Faction == factionComp.Faction)
                            {
                                unitToSpawn.UnitName = playerAttributes.HeroName;
                                unitToSpawn.TeamColor = factionComp.TeamColor;
                                m_CellData.UnitToSpawnData[ci] = unitToSpawn;
                            }
                        }
                    }
                }
            }
            
        }

        public uint SetPlayerWorld()
        {
            uint sortIndex = 1;

            for (int i = 0; i < m_GameStateData.Length; i++)
            {
                var gameState = m_GameStateData.GameStates[i];
                var mapWorldIndex = m_GameStateData.WorldIndexData[i].Value;

                //Debug.Log("SetPlayerWorldLoop " + m_GameStateData.WorldIndexData[i].Value);

                if (mapWorldIndex == sortIndex)
                {
                    if (gameState.PlayersOnMapCount < 2)
                    {
                        gameState.PlayersOnMapCount++;
                        m_GameStateData.GameStates[i] = gameState;
                        return mapWorldIndex;
                    }
                    else
                    {
                        sortIndex++;
                        i = -1;
                    }
                }
            }

            return 0;
        }

        public FactionComponent.Component SetPlayerFaction(uint worldIndex)
        {
            FactionComponent.Component factionComponent = new FactionComponent.Component();
            for (int i = 0; i < m_GameStateData.Length; i++)
            {
                var gameState = m_GameStateData.GameStates[i];
                var mapWorldIndex = m_GameStateData.WorldIndexData[i].Value;

                if (mapWorldIndex == worldIndex)
                {
                    if (gameState.PlayersOnMapCount == 1)
                    {
                        factionComponent.Faction = (mapWorldIndex - 1) * 2 + 1;
                    }
                    else
                    {
                        for (int pi = 0; pi < m_PlayerData.Length; pi++)
                        {
                            var factionC = m_PlayerData.FactionComponent[pi];
                            var pWorldIndex = m_PlayerData.WorldIndexData[pi].Value;

                            if (pWorldIndex == mapWorldIndex && factionC.Faction != 0)
                            {
                                if (factionC.Faction % 2 == 1)
                                {
                                    factionComponent.Faction = (mapWorldIndex - 1) * 2 + 2;
                                }
                                else if (factionC.Faction % 2 == 0)
                                {
                                    factionComponent.Faction = (mapWorldIndex - 1) * 2 + 1;
                                }
                            }
                        }
                    }
                }
            }
            if (factionComponent.Faction % 2 == 1)
            {
                factionComponent.TeamColor = TeamColorEnum.blue;
            }
            else if (factionComponent.Faction % 2 == 0)
            {
                factionComponent.TeamColor = TeamColorEnum.red;
            }
            return factionComponent;
        }
        
    }
   
}