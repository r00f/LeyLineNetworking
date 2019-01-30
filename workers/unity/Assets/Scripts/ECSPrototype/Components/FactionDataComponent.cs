using UnityEngine;
using System.Collections;
using Unity.Entities;
using System;

namespace LeyLineHybridECS
{
    [Serializable]
    public struct Faction : IComponentData
    {
        public int Value;
    }

    public class FactionDataComponent : ComponentDataWrapper<Faction> { }
}
