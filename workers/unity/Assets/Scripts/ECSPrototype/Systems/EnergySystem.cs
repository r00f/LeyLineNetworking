using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace LeyLineHybridECS
{
    public class EnergySystem : ComponentSystem
    {
        EntityQuery m_UnitData;
        EntityQuery m_PlayerData;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_UnitData = GetEntityQuery(
            ComponentType.ReadOnly<UnitEnergy>(),
            ComponentType.ReadWrite<Faction>()
            );

            m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<Faction>()
            );
        }

        protected override void OnUpdate()
        {
            /*
            if (GameStateSystem.CurrentState == GameStateSystem.State.CalculateEnergy)
            {
                for (int pi = 0; pi < m_PlayerData.Length; pi++)
                {
                    m_PlayerData.PlayerEnergyData[pi].TotalIncome = 0;

                    //Income Calculation do this once at the beginning of each turn
                    for (int ui = 0; ui < m_UnitData.Length; ui++)
                    {
                        //if the player and the unit share the same Faction
                        if (m_UnitData.FactionData[ui].Value == m_PlayerData.FactionData[pi].Value)
                        {

                            m_PlayerData.PlayerEnergyData[pi].TotalIncome += m_UnitData.UnitEnergyData[ui].Income;
                            m_PlayerData.PlayerEnergyData[pi].TotalIncome -= m_UnitData.UnitEnergyData[ui].Upkeep;

                            if (m_PlayerData.PlayerEnergyData[pi].CurrentEnergy + m_PlayerData.PlayerEnergyData[pi].TotalIncome <= m_PlayerData.PlayerEnergyData[pi].TotalEnergy)
                            {
                                m_PlayerData.PlayerEnergyData[pi].CurrentEnergy += m_PlayerData.PlayerEnergyData[pi].TotalIncome;
                            }
                            else
                            {
                                m_PlayerData.PlayerEnergyData[pi].CurrentEnergy = m_PlayerData.PlayerEnergyData[pi].TotalEnergy;
                            }

                        }

                    }
                }
            }
            */

        }

    }


}

