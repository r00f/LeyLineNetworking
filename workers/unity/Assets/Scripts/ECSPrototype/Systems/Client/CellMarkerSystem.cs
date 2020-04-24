using Unity.Entities;
using Improbable.Gdk.Core;
using UnityEngine;
using Generic;
using Player;
using Cell;
using Improbable;
using Unity.Collections;

namespace LeyLineHybridECS
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HighlightingSystem))]
    public class CellMarkerSystem : ComponentSystem
    {
        EntityQuery m_PlayerStateData;
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        EntityQuery m_CellData;
        EntityQuery m_NewCellData;
        Settings settings;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_UnitData = GetEntityQuery(
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<MouseState>(),
                ComponentType.ReadOnly<MarkerState>()
                //ComponentType.ReadWrite<UnitMarkerGameObjects>()
                );

            m_CellData = GetEntityQuery(
                ComponentType.ReadOnly<CellAttributesComponent.Component>(),
                ComponentType.ReadWrite<MarkerState>(),
                ComponentType.ReadWrite<MarkerGameObjects>()
                );

            m_NewCellData = GetEntityQuery(
                ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<CellAttributesComponent.Component>(),
                ComponentType.ReadWrite<MarkerGameObjects>()
                );

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<GameState.Component>()
            );

            m_PlayerStateData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<PlayerState.ComponentAuthority>()
            );

            m_PlayerStateData.SetFilter(PlayerState.ComponentAuthority.Authoritative);

        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            settings = Resources.Load<Settings>("Settings");

        }

        protected override void OnUpdate()
        {

            if (m_PlayerStateData.CalculateEntityCount() == 0)
                return;

            Entities.With(m_NewCellData).ForEach((MarkerGameObjects markerGameObjects, ref CellAttributesComponent.Component cellAtt) =>
            {
                int colorIndex = cellAtt.CellAttributes.CellMapColorIndex;
                markerGameObjects.MapMarkerRenderer.material.SetColor("_BaseColor", settings.MapCellColors[colorIndex]);
            });

            var playerWorldIndexes = m_PlayerStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
            var playerWorldIndex = playerWorldIndexes[0];

            Entities.With(m_GameStateData).ForEach((ref WorldIndex.Component gameStateWorldIndex, ref GameState.Component gameState)=>
            {
                var g = gameState;

                if (gameStateWorldIndex.Value == playerWorldIndex.Value)
                {
                    Entities.With(m_CellData).ForEach((MarkerGameObjects markerGameObjects, ref MarkerState markerState, ref CellAttributesComponent.Component cellAttributes) =>
                    {
                        if (g.CurrentState != GameStateEnum.planning)
                        {
                            markerGameObjects.ReachableMarker.SetActive(false);
                            markerGameObjects.TargetMarker.SetActive(false);
                            markerGameObjects.ClickedMarker.SetActive(false);
                            markerGameObjects.HoveredMarker.SetActive(false);
                        }
                        else
                        {

                            switch (markerState.NumberOfTargets)
                            {
                                case 0:
                                    if (markerGameObjects.TargetMarker.activeSelf)
                                        markerGameObjects.TargetMarker.SetActive(false);
                                    break;
                                case 1:
                                    if (!markerGameObjects.TargetMarker.activeSelf)
                                        markerGameObjects.TargetMarker.SetActive(true);



                                    markerGameObjects.TargetMarkerRenderer.material.color = settings.TurnStepLineColors[markerState.TurnStepIndex];
                                    break;
                                case 2:
                                    markerGameObjects.TargetMarkerRenderer.material.color = markerGameObjects.TargetColors[1];
                                    break;
                                case 3:
                                    markerGameObjects.TargetMarkerRenderer.material.color = markerGameObjects.TargetColors[2];
                                    break;
                                case 4:
                                    markerGameObjects.TargetMarkerRenderer.material.color = markerGameObjects.TargetColors[3];
                                    break;
                            }

                            if (markerState.IsSet == 0)
                            {
                                //Debug.Log("markerstateSet = 0");
                                switch (markerState.CurrentState)
                                {
                                    case MarkerState.State.Neutral:
                                        markerGameObjects.ClickedMarker.SetActive(false);
                                        markerGameObjects.HoveredMarker.SetActive(false);
                                        markerGameObjects.ReachableMarker.SetActive(false);
                                        break;
                                    case MarkerState.State.Clicked:
                                        if (markerState.NumberOfTargets == 0)
                                            markerGameObjects.ClickedMarker.SetActive(true);
                                        markerGameObjects.HoveredMarker.SetActive(false);
                                        markerGameObjects.ReachableMarker.SetActive(false);
                                        break;
                                    case MarkerState.State.Hovered:
                                        if(!cellAttributes.CellAttributes.Cell.IsTaken)
                                        {
                                            markerGameObjects.ClickedMarker.SetActive(false);
                                            markerGameObjects.HoveredMarker.SetActive(true);
                                            markerGameObjects.ReachableMarker.SetActive(false);
                                        }
                                        break;
                                    case MarkerState.State.Reachable:
                                        markerGameObjects.ClickedMarker.SetActive(false);
                                        markerGameObjects.HoveredMarker.SetActive(false);
                                        markerGameObjects.ReachableMarker.SetActive(true);
                                        break;
                                }
                                markerState.IsSet = 1;
                            }
                        }
                    });
                    /*
                    Entities.With(m_UnitData).ForEach((ref MarkerState markerState, ref FactionComponent.Component faction, ref MouseState mouseState) =>
                    {
                        if (g.CurrentState != GameStateEnum.planning)
                        {
                            unitMarkerGameObjects.AttackTargetMarker.SetActive(false);
                            unitMarkerGameObjects.DefenseMarker.SetActive(false);
                            unitMarkerGameObjects.HealTargetMarker.SetActive(false);
                        }
                        else
                        {
                            int colorIndex = 0;

                            if (faction.TeamColor == TeamColorEnum.blue)
                            {
                                colorIndex = 1;
                            }
                            else if (faction.TeamColor == TeamColorEnum.red)
                            {
                                colorIndex = 2;
                            }

                            if (markerState.NumberOfTargets > 0 && markerState.TargetTypeSet == 0)
                            {
                                switch (markerState.CurrentTargetType)
                                {
                                    case MarkerState.TargetType.Neutral:
                                        //change when we implement other target types
                                        unitMarkerGameObjects.AttackTargetMarker.SetActive(true);
                                        unitMarkerGameObjects.DefenseMarker.SetActive(false);
                                        unitMarkerGameObjects.HealTargetMarker.SetActive(false);
                                        break;
                                    case MarkerState.TargetType.AttackTarget:
                                        unitMarkerGameObjects.AttackTargetMarker.SetActive(true);
                                        unitMarkerGameObjects.DefenseMarker.SetActive(false);
                                        unitMarkerGameObjects.HealTargetMarker.SetActive(false);
                                        break;
                                    case MarkerState.TargetType.DefenseTarget:
                                        unitMarkerGameObjects.AttackTargetMarker.SetActive(false);
                                        unitMarkerGameObjects.DefenseMarker.SetActive(true);
                                        unitMarkerGameObjects.HealTargetMarker.SetActive(false);
                                        break;
                                    case MarkerState.TargetType.HealTarget:
                                        unitMarkerGameObjects.AttackTargetMarker.SetActive(false);
                                        unitMarkerGameObjects.DefenseMarker.SetActive(false);
                                        unitMarkerGameObjects.HealTargetMarker.SetActive(true);
                                        break;
                                }
                                markerState.TargetTypeSet = 1;
                            }
                            else if (markerState.NumberOfTargets == 0)
                            {
                                if (markerState.TargetTypeSet == 1)
                                {
                                    markerState.TargetTypeSet = 0;
                                }
                                unitMarkerGameObjects.AttackTargetMarker.SetActive(false);
                                unitMarkerGameObjects.DefenseMarker.SetActive(false);
                                unitMarkerGameObjects.HealTargetMarker.SetActive(false);
                            }

                            if (markerState.IsSet == 0)
                            {
                                switch (markerState.CurrentState)
                                {
                                    case MarkerState.State.Neutral:
                                        unitMarkerGameObjects.Outline.enabled = false;
                                        markerState.IsSet = 1;
                                        break;
                                    case MarkerState.State.Clicked:
                                        unitMarkerGameObjects.Outline.enabled = false;
                                        //markerGameObject.Outline.enabled = true;
                                        //markerGameObject.Outline.color = colorIndex;
                                        markerState.IsSet = 1;
                                        break;
                                    case MarkerState.State.Hovered:
                                        unitMarkerGameObjects.Outline.enabled = true;
                                        unitMarkerGameObjects.Outline.color = colorIndex;
                                        markerState.IsSet = 1;
                                        break;
                                    case MarkerState.State.Reachable:
                                        unitMarkerGameObjects.Outline.enabled = true;
                                        if (mouseState.CurrentState == MouseState.State.Hovered)
                                        {
                                            unitMarkerGameObjects.Outline.color = colorIndex;
                                        }
                                        else
                                        {
                                            unitMarkerGameObjects.Outline.color = 0;
                                        }
                                        break;
                                }
                            }
                        }
                    });
                    */
                }
            });

            playerWorldIndexes.Dispose();
        }
    }
}

