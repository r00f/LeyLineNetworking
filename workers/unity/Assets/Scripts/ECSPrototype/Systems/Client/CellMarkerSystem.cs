using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HighlightingSystem))]
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
                    if(!markerGameObject.HoveredMarker.activeSelf)
                        markerGameObject.HoveredMarker.SetActive(true);
                }
                else
                {
                    if (markerGameObject.HoveredMarker.activeSelf)
                        markerGameObject.HoveredMarker.SetActive(false);
                }

                if (isSet == 0)
                {
                    if (markerState.CurrentState == MarkerState.State.Neutral)
                    {
                        if (markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(false);
                        if (markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(false);
                        if (markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(false);

                        markerState.CurrentState = MarkerState.State.Neutral;
                        markerState.IsSet = 1;

                    }
                    else if (markerState.CurrentState == MarkerState.State.Clicked)
                    {
                        if (!markerGameObject.ClickedMarker.activeSelf && markerState.IsTarget == 0)
                            markerGameObject.ClickedMarker.SetActive(true);
                        if (markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(false);
                        if (markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(false);

                        markerState.CurrentState = MarkerState.State.Clicked;
                        markerState.IsSet = 1;

                    }
                    else if (markerState.CurrentState == MarkerState.State.Hovered)
                    {
                        if (markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(false);
                        if (!markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(true);
                        if (markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(false);

                        markerState.CurrentState = MarkerState.State.Hovered;
                        markerState.IsSet = 1;

                    }
                    else if (markerState.CurrentState == MarkerState.State.Reachable)
                    {
                        if (markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(false);
                        if (markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(false);
                        if (!markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(true);

                        markerState.CurrentState = MarkerState.State.Reachable;
                        markerState.IsSet = 1;

                    }

                    m_Data.MarkerStateData[i] = markerState;
                }
            }
        }
    }
}

