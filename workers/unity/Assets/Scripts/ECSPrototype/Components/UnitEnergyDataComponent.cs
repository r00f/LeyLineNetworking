using UnityEngine;
using System.Collections;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{
    [Serializable]
    public struct UnitEnergy : IComponentData
    {
        public int SpawnCost;
        public int CurrentActionCost;
        public int Upkeep;
        public int Income;
    }

    //public class UnitEnergyDataComponent : ComponentDataWrapper<UnitEnergy> { }
}
