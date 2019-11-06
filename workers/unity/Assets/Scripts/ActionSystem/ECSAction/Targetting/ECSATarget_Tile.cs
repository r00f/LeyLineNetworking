using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
namespace LeyLineHybridECS
{
    [System.Serializable]
    [CreateAssetMenu]
    public class ECSATarget_Tile : ECSActionTarget
    {
        public bool requireEmpty;
        public Vector3f MainTargetCoordinate;
    }
}
