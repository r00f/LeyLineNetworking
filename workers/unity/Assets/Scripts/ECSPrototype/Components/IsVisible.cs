using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace LeyLineHybridECS
{
    public struct IsVisible : IComponentData
    {
        public byte Value;
        public byte RequireUpdate;
        public float LerpSpeed;
    }
}