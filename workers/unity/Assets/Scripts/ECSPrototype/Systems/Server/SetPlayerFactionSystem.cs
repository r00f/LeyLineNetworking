using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using LeyLineHybridECS;

public class SetPlayerFactionSystem : ComponentSystem
{
    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Player.EnergyComponent.Component> EnergyComponent;
        public ComponentDataArray<Generic.FactionComponent.Component> FactionComponent;
    }

    [Inject] private PlayerData m_PlayerData;

    protected override void OnUpdate()
    {

        for (int i = 0; i < m_PlayerData.Length; i++)
        {
            var faction = m_PlayerData.FactionComponent[i].Faction;

            if(faction == 0)
            {
                var factionComponent = m_PlayerData.FactionComponent[i];
                factionComponent.Faction = (uint)m_PlayerData.Length;
                m_PlayerData.FactionComponent[i] = factionComponent;
            }
        }
    }
}
