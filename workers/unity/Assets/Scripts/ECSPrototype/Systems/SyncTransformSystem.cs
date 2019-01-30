using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace LeyLineHybridECS
{
    [UpdateAfter(typeof(MovementSystem))]
    public class SyncTransformSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentArray<Heading3D> HeadingData;
            public readonly ComponentDataArray<Position3D> PositionData;
            public ComponentArray<Transform> OutputData;
        }

        [Inject] private Data m_Data;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Data.Length; i++)
            {
                var position = m_Data.PositionData[i];
                var heading = m_Data.HeadingData[i];
                float3 h = heading.Value;
                m_Data.OutputData[i].position = new float3(position.Value.x, position.Value.y, position.Value.z);


                //rotate towards heading if it is not 0
                if(!heading.Value.Equals( new float3(0f,0f,0f)))
                {
                    float3 targetDir = new Vector3(h.x, 0, h.z) - new Vector3(position.Value.x, 0, position.Value.z);
                    float3 newDir = Vector3.RotateTowards(m_Data.OutputData[i].forward, new Vector3(targetDir.x, 0, targetDir.z), 3 * Time.deltaTime, 0.0f);
                    m_Data.OutputData[i].rotation = quaternion.LookRotation(newDir, new float3(0f, 1f, 0f));
                }
            }
        }
    }

}
