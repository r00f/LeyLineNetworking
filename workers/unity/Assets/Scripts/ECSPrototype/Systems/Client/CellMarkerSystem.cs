using Unity.Entities;
using Improbable.Gdk.Core;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
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

                if(isSet == 0)
                {
                    MarkerState markerState = m_Data.MarkerStateData[i];
                    MarkerGameObjects markerGameObject = m_Data.MarkerGameObjectsData[i];
                    if (markerState.CurrentState == MarkerState.State.Neutral)
                    {

                        if (markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(false);
                        if (markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(false);
                        if (markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(false);

                        m_Data.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = MarkerState.State.Neutral,
                            IsSet = 1
                        };

                    }
                    else if (markerState.CurrentState == MarkerState.State.Clicked)
                    {
                        if (!markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(true);
                        if (markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(false);
                        if (markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(false);

                        m_Data.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = MarkerState.State.Clicked,
                            IsSet = 1
                        };

                    }
                    else if (markerState.CurrentState == MarkerState.State.Hovered)
                    {
                        if (markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(false);
                        if (!markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(true);
                        if (markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(false);

                        m_Data.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = MarkerState.State.Hovered,
                            IsSet = 1
                        };
                    }
                    else if (markerState.CurrentState == MarkerState.State.Reachable)
                    {
                        if (markerGameObject.ClickedMarker.activeSelf)
                            markerGameObject.ClickedMarker.SetActive(false);
                        if (markerGameObject.HoveredMarker.activeSelf)
                            markerGameObject.HoveredMarker.SetActive(false);
                        if (!markerGameObject.ReachableMarker.activeSelf)
                            markerGameObject.ReachableMarker.SetActive(true);

                        m_Data.MarkerStateData[i] = new MarkerState
                        {
                            CurrentState = MarkerState.State.Reachable,
                            IsSet = 1
                        };
                    }


                }


                //var marker = EntityManager.Instantiate()

            }
            /*
            foreach (var entity in GetEntities<Data>())
            {
                var state = entity.MarkerState;
                
                if(state.CurrentState == MarkerState.State.Neutral)
                {
                    if (state.ClickedMarker.activeSelf)
                        state.ClickedMarker.SetActive(false);
                    if (state.HoveredMarker.activeSelf)
                        state.HoveredMarker.SetActive(false);
                    if (state.ReachableMarker.activeSelf)
                        state.ReachableMarker.SetActive(false);
                }
                else if (state.CurrentState == MarkerState.State.Hovered)
                {
                    if (state.ClickedMarker.activeSelf)
                        state.ClickedMarker.SetActive(false);
                    if (!state.HoveredMarker.activeSelf)
                        state.HoveredMarker.SetActive(true);
                    if (state.ReachableMarker.activeSelf)
                        state.ReachableMarker.SetActive(false);
                }
                else if (state.CurrentState == MarkerState.State.Clicked)
                {
                    //Debug.Log("CLICKED");
                    if (!state.ClickedMarker.activeSelf)
                        state.ClickedMarker.SetActive(true);
                    if (state.HoveredMarker.activeSelf)
                        state.HoveredMarker.SetActive(false);
                    if (state.ReachableMarker.activeSelf)
                        state.ReachableMarker.SetActive(false);
                }
                else if (state.CurrentState == MarkerState.State.Reachable)
                {
                    if (state.ClickedMarker.activeSelf)
                        state.ClickedMarker.SetActive(false);
                    if (state.HoveredMarker.activeSelf)
                        state.HoveredMarker.SetActive(false);
                    if (!state.ReachableMarker.activeSelf)
                        state.ReachableMarker.SetActive(true);
                }
                
            /*
            for (int i = 0; i < state.MarkerObjects.Count; i++)
            {
                if (i == stateInt - 1)
                {
                    if (!state.MarkerObjects[i].activeSelf)
                        state.MarkerObjects[i].SetActive(true);
                }
                else
                {
                    if (state.MarkerObjects[i].activeSelf)
                        state.MarkerObjects[i].SetActive(false);
                }
            }
            */
        }
        
    }
}

