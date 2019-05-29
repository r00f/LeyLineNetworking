using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;
using Generic;

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

        [Inject] private Data m_Data;

        struct UnitData
        {
            public readonly int Length;
            public readonly ComponentDataArray<FactionComponent.Component> FactionData;
            public ComponentDataArray<MarkerState> MarkerStateData;
            public ComponentArray<UnitMarkerGameObjects> MarkerGameObjectsData;
        }

        [Inject] private UnitData m_UnitData;

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
                                markerGameObject.ClickedMarker.SetActive(false);
                                markerGameObject.HoveredMarker.SetActive(false);
                                markerGameObject.ReachableMarker.SetActive(false);
                            break;
                        case MarkerState.State.Clicked:
                            if (markerState.IsTarget == 0)
                                markerGameObject.ClickedMarker.SetActive(true);
                                markerGameObject.HoveredMarker.SetActive(false);
                                markerGameObject.ReachableMarker.SetActive(false);
                            break;
                        case MarkerState.State.Hovered:
                                markerGameObject.ClickedMarker.SetActive(false);
                                markerGameObject.HoveredMarker.SetActive(true);
                                markerGameObject.ReachableMarker.SetActive(false);
                            break;
                        case MarkerState.State.Reachable:
                                markerGameObject.ClickedMarker.SetActive(false);
                                markerGameObject.HoveredMarker.SetActive(false);
                                markerGameObject.ReachableMarker.SetActive(true);
                            break;
                    }

                    markerState.IsSet = 1;
                    m_Data.MarkerStateData[i] = markerState;
                }
            }

            for (int i = 0; i < m_UnitData.Length; i++)
            {
                int isSet = m_UnitData.MarkerStateData[i].IsSet;
                MarkerState markerState = m_UnitData.MarkerStateData[i];
                UnitMarkerGameObjects markerGameObject = m_UnitData.MarkerGameObjectsData[i];
                var teamColor = m_UnitData.FactionData[i].TeamColor;
                int colorIndex = 0;

                if (teamColor == TeamColorEnum.blue)
                {
                    colorIndex = 1;
                }
                else if (teamColor == TeamColorEnum.red)
                {
                    colorIndex = 2;
                }

                if (markerState.TargetTypeSet == 0)
                {
                    if (markerState.IsTarget == 1)
                    {
                        switch (markerState.CurrentTargetType)
                        {
                            case MarkerState.TargetType.Neutral:
                                markerGameObject.AttackTargetMarker.SetActive(false);
                                markerGameObject.DefenseMarker.SetActive(false);
                                markerGameObject.HealTargetMarker.SetActive(false);
                                break;
                            case MarkerState.TargetType.AttackTarget:
                                markerGameObject.AttackTargetMarker.SetActive(true);
                                markerGameObject.DefenseMarker.SetActive(false);
                                markerGameObject.HealTargetMarker.SetActive(false);
                                break;
                            case MarkerState.TargetType.DefenseTarget:
                                markerGameObject.AttackTargetMarker.SetActive(false);
                                markerGameObject.DefenseMarker.SetActive(true);
                                markerGameObject.HealTargetMarker.SetActive(false);
                                break;
                            case MarkerState.TargetType.HealTarget:
                                markerGameObject.AttackTargetMarker.SetActive(false);
                                markerGameObject.DefenseMarker.SetActive(false);
                                markerGameObject.HealTargetMarker.SetActive(true);
                                break;
                        }

                        if (!markerGameObject.AttackTargetMarker.activeSelf)
                            markerGameObject.AttackTargetMarker.SetActive(true);
                    }
                    else
                    {
                        if (markerGameObject.AttackTargetMarker.activeSelf)
                            markerGameObject.AttackTargetMarker.SetActive(false);
                    }
                    markerState.TargetTypeSet = 1;
                }

                if (isSet == 0)
                {
                    switch (markerState.CurrentState)
                    {
                        case MarkerState.State.Neutral:
                            markerGameObject.Outline.enabled = false;
                            break;
                        case MarkerState.State.Clicked:
                            markerGameObject.Outline.enabled = false;
                            //markerGameObject.Outline.enabled = true;
                            //markerGameObject.Outline.color = colorIndex;
                            break;
                        case MarkerState.State.Hovered:
                            markerGameObject.Outline.enabled = true;
                            markerGameObject.Outline.color = colorIndex;
                            break;
                        case MarkerState.State.Reachable:
                            markerGameObject.Outline.enabled = true;
                            markerGameObject.Outline.color = 0;
                            break;
                    }
                    markerState.IsSet = 1;
                }
                m_Data.MarkerStateData[i] = markerState;
            }
        }
    }
}

