using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    [System.Serializable]
    public class SecondaryCone : ECSActionSecondaryTargets
    {
        public uint radius;
        public uint extent;
        public bool wtf;
    }
}
