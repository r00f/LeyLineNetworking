using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UI;

namespace LeyLineHybridECS
{
    public class EnergySystem : ComponentSystem
    {
        public struct PlayerData
        {
            public readonly int Length;
            public ComponentArray<PlayerEnergy> PlayerEnergyData;
            public ComponentDataArray<Faction> FactionData;
        }

        [Inject] private PlayerData m_PlayerData;

        public struct UnitData
        {
            public readonly int Length;
            public ComponentDataArray<UnitEnergy> UnitEnergyData;
            public ComponentDataArray<Faction> FactionData;
        }

        [Inject] private UnitData m_UnitData;

        protected override void OnUpdate()
        {

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

            for (int i = 0; i < m_PlayerData.Length; i++)
            {
                if(m_PlayerData.PlayerEnergyData[i].EnergyBarInstance)
                {
                    var maxEnergy = m_PlayerData.PlayerEnergyData[i].TotalEnergy;
                    var currentEnergy = m_PlayerData.PlayerEnergyData[i].CurrentEnergy;
                    var energyIncome = m_PlayerData.PlayerEnergyData[i].TotalIncome;
                    var energyFill = m_PlayerData.PlayerEnergyData[i].EnergyBarInstance.transform.GetChild(0).GetChild(1).GetComponent<Image>();
                    var incomeEnergyFill = m_PlayerData.PlayerEnergyData[i].EnergyBarInstance.transform.GetChild(0).GetChild(0).GetComponent<Image>();


                    energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, currentEnergy / maxEnergy, .1f);

                    if (energyFill.fillAmount >= currentEnergy / maxEnergy - .01f)
                        incomeEnergyFill.fillAmount = Mathf.Lerp(incomeEnergyFill.fillAmount, (currentEnergy + energyIncome) / maxEnergy, .1f);
                }


            }
        }

    }


}

