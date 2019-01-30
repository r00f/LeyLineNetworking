using UnityEngine;
using System.Collections;

namespace LeyLineHybridECS
{
    public class PlayerEnergy : MonoBehaviour
    {
        public float TotalEnergy;
        public float CurrentEnergy;
        public float TotalIncome;
        public float TotalUpkeep;

        public GameObject EnergyBarPrefab;
        public GameObject EnergyBarInstance;
    }
}

