using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;
using Player;
using Generic;
using System.Collections.Generic;
using Improbable.Gdk.PlayerLifecycle;


namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HandleCreatePlayerRequestSystem))]
    public class InitializePlayerSystem : ComponentSystem
    {
        public struct PlayerData
        {
            public readonly int Length;
            public readonly ComponentDataArray<Authoritative<WorldIndex.Component>> AuthorativeData;
            public readonly ComponentDataArray<PlayerAttributes.Component> PlayerAttributes;
            public ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<FactionComponent.Component> FactionComponent;
        }

        [Inject] private PlayerData m_PlayerData;

        public struct CellData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<Cells.UnitToSpawn.Component> UnitToSpawnData;
        }

        [Inject] private CellData m_CellData;

        public struct GameStateData
        {
            public readonly int Length;
            public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<GameState.Component> GameStates;
        }

        [Inject] private GameStateData m_GameStateData;

        public struct PlayerStateData : ISystemStateComponentData
        {
            public WorldIndex.Component WorldIndexState;
        }

        private struct PlayerAddedData
        {
            public readonly int Length;
            public readonly EntityArray Entities;
            public readonly ComponentDataArray<Authoritative<WorldIndex.Component>> AuthorativeData;
            public readonly ComponentDataArray<PlayerAttributes.Component> PlayerAttributes;
            public ComponentDataArray<WorldIndex.Component> WorldIndexData;
            public ComponentDataArray<FactionComponent.Component> FactionComponent;
            public SubtractiveComponent<PlayerStateData> WorldIndexState;
        }

        [Inject] private PlayerAddedData m_PlayerAddedData;


        public struct PlayerRemovedData
        {
            public readonly int Length;
            public readonly EntityArray Entities;
            public SubtractiveComponent<Authoritative<WorldIndex.Component>> SubstractiveAuth;
            public SubtractiveComponent<PlayerAttributes.Component> SubtractivePlayerData;
            public SubtractiveComponent<WorldIndex.Component> SubstractiveCoordinateData;
            public readonly ComponentDataArray<PlayerStateData> WorldIndexState;
        }

        [Inject] private PlayerRemovedData m_PlayerRemovedData;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                var worldIndex = m_PlayerData.WorldIndexData[i];
                var factionComp = m_PlayerData.FactionComponent[i];
                var playerAttributes = m_PlayerData.PlayerAttributes[i];

                if (factionComp.Faction == 0)
                {
                    //if (worldIndex.Value == 0)
                    //{
                       // worldIndex.Value = SetPlayerWorld(worldIndex);
                        //m_PlayerData.WorldIndexData[i] = worldIndex;
                    //}

                    factionComp = SetPlayerFaction(factionComp, worldIndex.Value);
                    m_PlayerData.FactionComponent[i] = factionComp;

                    for (int ci = 0; ci < m_CellData.Length; ci++)
                    {
                        var cellWorldIndex = m_CellData.WorldIndexData[ci].Value;

                        if (cellWorldIndex == worldIndex.Value)
                        {
                            var unitToSpawn = m_CellData.UnitToSpawnData[ci];

                            if (unitToSpawn.IsSpawn)
                            {
                                if (unitToSpawn.Faction == factionComp.Faction)
                                {
                                    unitToSpawn.UnitName = playerAttributes.HeroName;
                                    unitToSpawn.TeamColor = factionComp.TeamColor;
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < m_PlayerAddedData.Length; i++)
            {
                var worldIndex = m_PlayerAddedData.WorldIndexData[i];
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
        }

        public uint SetPlayerWorld(WorldIndex.Component worldIndexComponent)
        {
            uint sortIndex = 1;
            uint worldIndex = 0;

            for (int i = 0; i < m_GameStateData.Length; i++)
            {
                var gameState = m_GameStateData.GameStates[i];
                var mapWorldIndex = m_GameStateData.WorldIndexData[i].Value;

                if (mapWorldIndex == sortIndex)
                {
                    if (gameState.PlayersOnMapCount < 2)
                    {
                        worldIndex = mapWorldIndex;
                        gameState.PlayersOnMapCount++;
                        m_GameStateData.GameStates[i] = gameState;
                    }
                    else
                    {
                        sortIndex++;
                        i = -1;
                    }
                }
            }

            return worldIndex;
        }

        public FactionComponent.Component SetPlayerFaction(FactionComponent.Component factionComponent, uint worldIndex)
        {
            for (int i = 0; i < m_GameStateData.Length; i++)
            {
                var gameState = m_GameStateData.GameStates[i];
                var mapWorldIndex = m_GameStateData.WorldIndexData[i].Value;

                if (mapWorldIndex == worldIndex)
                {
                    Debug.Log("A");
                    if (gameState.PlayersOnMapCount == 1)
                    {
                        Debug.Log("B");
                        factionComponent.Faction = (mapWorldIndex - 1) * 2 + 1;
                    }
                    else
                    {
                        Debug.Log("C");
                        for (int pi = 0; pi < m_PlayerData.Length; pi++)
                        {
                            var factionC = m_PlayerData.FactionComponent[pi];
                            var pWorldIndex = m_PlayerData.WorldIndexData[pi].Value;

                            if (pWorldIndex == mapWorldIndex && factionC.Faction != 0)
                            {
                                Debug.Log("D");
                                //if player on map has an odd Faction set joining players faction to be even
                                if (factionC.Faction % 2 == 1)
                                {
                                    factionComponent.Faction = (mapWorldIndex - 1) * 2 + 2;
                                }
                                //if even
                                else if (factionC.Faction % 2 == 0)
                                {
                                    factionComponent.Faction = (mapWorldIndex - 1) * 2 + 1;
                                }
                            }
                        }
                    }
                }
            }

            //if odd
            if (factionComponent.Faction % 2 == 1)
            {
                factionComponent.TeamColor = TeamColorEnum.blue;
            }
            //if even
            else if (factionComponent.Faction % 2 == 0)
            {
                factionComponent.TeamColor = TeamColorEnum.red;
            }

            return factionComponent;
        }
    }
}