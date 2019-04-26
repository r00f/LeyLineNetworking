﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    [System.Serializable]
    public class ECS_DealDamageEffect : ECSActionEffect
    {
        public uint damageAmount;
    }
        
}