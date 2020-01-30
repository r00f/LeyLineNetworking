using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Generic;

namespace LeyLineHybridECS
{
    [System.Serializable]
    public class ECSActionSecondaryTargets : ScriptableObject
    {
        public List<Vector3f> MySecondaryTargetCoordinates;
    }
}