using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{
    public class Unit_BaseDataSet : MonoBehaviour
    {
        [Range(0, 20)]
        public uint VisionRange;
        public uint BaseHealth;
        public uint UpkeepCost;
        public uint SpawnCost;
        public uint MovementRange;
        public ECSAction BasicMove;
        public ECSAction BasicAttack;

        public List<ECSAction> Actions;

    }
}