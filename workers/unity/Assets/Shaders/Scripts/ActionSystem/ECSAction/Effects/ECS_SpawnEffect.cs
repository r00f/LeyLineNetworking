using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    [System.Serializable]
    public class ECS_SpawnEffect : ECSActionEffect
    {
        public string UnitNameToSpawn;
    }
}