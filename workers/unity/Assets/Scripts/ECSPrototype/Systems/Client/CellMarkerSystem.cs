using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(HighlightingSystem))]
    public class CellMarkerSystem : ComponentSystem
    {
        struct Data
        {
            public readonly int Length;
            public ComponentDataArray<MarkerState> MarkerStateData;
            public ComponentArray<MarkerGameObjects> MarkerGameObjectsData;
        }

        [Inject]
        private Data m_Data;

        protected override void OnUpdate()
        {
            for(int i = 0; i < m_Data.Length; i++)
            {
                int isSet = m_Data.MarkerStateData[i].IsSet;
                MarkerState markerState = m_Data.MarkerStateData[i];
                MarkerGameObjects markerGameObject = m_Data.MarkerGameObjectsData[i];

                if(markerState.IsTarget == 1)
                {
                    if(!markerGameObject.TargetMarker.activeSelf)
                        markerGameObject.TargetMarker.SetActive(true);
                }
                else
                {
                    if (markerGameObject.TargetMarker.activeSelf)
                        markerGameObject.TargetMarker.SetActive(false);
                }

                if (isSet == 0)
                {
                    switch (markerState.CurrentState)
                    {
                        case MarkerState.State.Neutral:
                            if (markerGameObject.ClickedMarker.activeSelf)
                                markerGameObject.ClickedMarker.SetActive(false);
                            if (markerGameObject.HoveredMarker.activeSelf)
                                markerGameObject.HoveredMarker.SetActive(false);
                            if (markerGameObject.ReachableMarker.activeSelf)
                                markerGameObject.ReachableMarker.SetActive(false);
                            break;
                        case MarkerState.State.Clicked:
                            if (!markerGameObject.ClickedMarker.activeSelf && markerState.IsTarget == 0)
                                markerGameObject.ClickedMarker.SetActive(true);
                            if (markerGameObject.HoveredMarker.activeSelf)
                                markerGameObject.HoveredMarker.SetActive(false);
                            if (markerGameObject.ReachableMarker.activeSelf)
                                markerGameObject.ReachableMarker.SetActive(false);
                            break;
                        case MarkerState.State.Hovered:
                            if (markerGameObject.ClickedMarker.activeSelf)
                                markerGameObject.ClickedMarker.SetActive(false);
                            if (!markerGameObject.HoveredMarker.activeSelf)
                                markerGameObject.HoveredMarker.SetActive(true);
                            if (markerGameObject.ReachableMarker.activeSelf)
                                markerGameObject.ReachableMarker.SetActive(false);
                            break;
                        case MarkerState.State.Reachable:
                            if (markerGameObject.ClickedMarker.activeSelf)
                                markerGameObject.ClickedMarker.SetActive(false);
                            if (markerGameObject.HoveredMarker.activeSelf)
                                markerGameObject.HoveredMarker.SetActive(false);
                            if (!markerGameObject.ReachableMarker.activeSelf)
                                markerGameObject.ReachableMarker.SetActive(true);
                            break;
                    }

                    markerState.IsSet = 1;
                    m_Data.MarkerStateData[i] = markerState;
                }
            }
        }
    }
}

