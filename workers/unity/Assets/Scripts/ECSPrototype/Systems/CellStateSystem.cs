using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace LeyLineHybridECS
{
    public class CellStateSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public ComponentDataArray<MarkerState> MarkerStateData;
            public ComponentDataArray<MouseState> MouseStateData;
        }

        [Inject] private Data m_Data;

        protected override void OnUpdate()
        {
            for (int i = 0; i < m_Data.Length; ++i)
            {
                MouseState mouseState = m_Data.MouseStateData[i];
                MarkerState markerState = m_Data.MarkerStateData[i];

                //sets Marker state to MouseState - don't know if neccesairy???
                if (markerState.CurrentState != (MarkerState.State)(int)mouseState.CurrentState && markerState.CurrentState != MarkerState.State.Reachable)
                {
                    m_Data.MarkerStateData[i] = new MarkerState
                    {
                        CurrentState = (MarkerState.State)(int)mouseState.CurrentState,
                        IsSet = 0
                    };
                }
            }
        }
    }

}

