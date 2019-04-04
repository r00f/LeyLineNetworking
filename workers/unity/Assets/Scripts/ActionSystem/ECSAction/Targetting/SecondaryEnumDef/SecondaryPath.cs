using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LeyLineHybridECS
{
    [CreateAssetMenu]
    public class SecondaryPath : ECSActionSecondaryTargets
    {
        public bool respectTaken;
        public int costPerTile;
    }
}