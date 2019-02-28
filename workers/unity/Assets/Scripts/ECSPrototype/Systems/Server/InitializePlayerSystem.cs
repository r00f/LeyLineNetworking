using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using LeyLineHybridECS;
using Player;
using Generic;

public class InitializePlayerSystem : ComponentSystem
{
    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<PlayerAttributes.Component> PlayerAttributes;
        public ComponentDataArray<FactionComponent.Component> FactionComponent;
    }

    [Inject] private PlayerData m_PlayerData;

    public struct CellData
    {
        public readonly int Length;
        public ComponentDataArray<Cells.UnitToSpawn.Component> UnitToSpawnData;
    }

    [Inject] private CellData m_CellData;

    protected override void OnUpdate()
    {
        for (int i = 0; i < m_PlayerData.Length; i++)
        {
            var factionComp = m_PlayerData.FactionComponent[i];
            var playerAttributes = m_PlayerData.PlayerAttributes[i];

            if (factionComp.Faction == 0)
            {
                factionComp.Faction = (uint)m_PlayerData.Length;

                if (factionComp.Faction == 1)
                {
                    factionComp.TeamColor = TeamColorEnum.blue;
                }
                else if(factionComp.Faction == 2)
                {
                    factionComp.TeamColor = TeamColorEnum.red;
                }

                m_PlayerData.FactionComponent[i] = factionComp;

                for (int ci = 0; ci < m_CellData.Length; ci++)
                {
                    var unitToSpawn = m_CellData.UnitToSpawnData[ci];

                    if(unitToSpawn.IsSpawn)
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
}
