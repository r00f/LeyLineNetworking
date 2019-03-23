using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    public class ECSATarget_Tile : ECSActionTarget
    {
        public bool requireEmpty;
        public Cell MainTarget;
    }
}
