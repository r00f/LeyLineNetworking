using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{

    [CreateAssetMenu]
    public class ECS_MoveAlongPathEffect : ECSActionEffect
    {
        public int MovementSpeed;
        public int pathIndex;
    }
}