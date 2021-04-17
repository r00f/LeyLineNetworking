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
    [DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(HighlightingSystem))]
    public class CellMarkerSystem : ComponentSystem
    {
        EntityQuery m_PlayerStateData;
        EntityQuery m_GameStateData;
        EntityQuery m_UnitData;
        EntityQuery m_RequireMarkerUpdateData;
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

            m_RequireMarkerUpdateData = GetEntityQuery(
                ComponentType.ReadWrite<RequireMarkerUpdate>(),
                ComponentType.ReadOnly<CellAttributesComponent.Component>(),
                ComponentType.ReadWrite<MarkerState>(),
                ComponentType.ReadWrite<MarkerGameObjects>()
                );

            m_NewCellData = GetEntityQuery(
                ComponentType.ReadOnly<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<CellAttributesComponent.Component>(),
                ComponentType.ReadWrite<MarkerGameObjects>(),
                ComponentType.ReadWrite<IsVisibleReferences>()
                );

            m_GameStateData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<GameState.Component>()
            );

            m_PlayerStateData = GetEntityQuery(
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<PlayerState.HasAuthority>()
            );

        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            settings = Resources.Load<Settings>("Settings");
        }

        protected override void OnUpdate()
        {
            /*
            if(m_NewCellData.CalculateEntityCount() > 0)
            {
                Entities.With(m_NewCellData).ForEach((Entity e, MarkerGameObjects markerGameObjects, ref CellAttributesComponent.Component cellAtt) =>
                {
                    int colorIndex = cellAtt.CellAttributes.CellMapColorIndex;
                    var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(e);

                    float offsetMultiplier = UIRef.MinimapComponent.Map.sizeDelta.x / isVisibleRef.MiniMapTilePrefab.TileRect.sizeDelta.x / 2;
                    //Instantiate MiniMapTile into Map
                    Vector3 pos = CellGridMethods.CubeToPos(cellAtt.CellAttributes.Cell.CubeCoordinate, new Vector2f(0f, 0f));
                    Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);
                    isVisibleRef.MiniMapTileInstance = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, UIRef.MiniMapTilesPanel.transform);
                    isVisibleRef.MiniMapTileInstance.TileRect.anchoredPosition = invertedPos;

                    isVisibleRef.MiniMapTileInstance.TileColor = settings.MapCellColors[colorIndex];
                    //init gray if not water
                    if (cellAtt.CellAttributes.CellMapColorIndex != 5)
                        isVisibleRef.MiniMapTileInstance.TileImage.color = isVisibleRef.MiniMapTilePrefab.TileInvisibleColor;
                    //init blue if water
                    else
                        isVisibleRef.MiniMapTileInstance.TileImage.color = isVisibleRef.MiniMapTileInstance.TileColor;

                    isVisibleRef.MiniMapTileInstance.TileColor = settings.MapCellColors[colorIndex];
                });
                
            }
            */
            if (m_GameStateData.CalculateEntityCount() == 0)
                return;


            //CHANGE TO REACTIVE COMPONENT FLAG ISSET > RequireMarkerStateUpdate Component
            if (m_RequireMarkerUpdateData.CalculateEntityCount() > 0)
            {
                var gameStateData = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);
                var gameState = gameStateData[0];

                //Debug.Log("ReqMarkerUpdateCount: " + m_RequireMarkerUpdateData.CalculateEntityCount());

                Entities.With(m_RequireMarkerUpdateData).ForEach((Entity e, MarkerGameObjects markerGameObjects, ref MarkerState markerState, ref CellAttributesComponent.Component cellAttributes) =>
                {
                    if (gameState.CurrentState != GameStateEnum.planning)
                    {
                        markerGameObjects.ReachableMarker.SetActive(false);
                        markerGameObjects.TargetMarker.SetActive(false);
                        markerGameObjects.ClickedMarker.SetActive(false);
                        markerGameObjects.HoveredMarker.SetActive(false);
                        PostUpdateCommands.RemoveComponent<RequireMarkerUpdate>(e);
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
                                markerGameObjects.TargetMarkerRenderer.material.color = settings.TurnStepLineColors[markerState.TurnStepIndex] + new Color(0, 0, 0, 0.2f);
                                //markerGameObjects.TargetMarkerRenderer.material.color = markerGameObjects.TargetColors[1];
                                break;
                            case 3:
                                markerGameObjects.TargetMarkerRenderer.material.color = settings.TurnStepLineColors[markerState.TurnStepIndex] + new Color(0, 0, 0, 0.4f);

                                //markerGameObjects.TargetMarkerRenderer.material.color = markerGameObjects.TargetColors[2];
                                break;
                            case 4:
                                markerGameObjects.TargetMarkerRenderer.material.color = settings.TurnStepLineColors[markerState.TurnStepIndex] + new Color(0, 0, 0, 0.6f);

                                //markerGameObjects.TargetMarkerRenderer.material.color = markerGameObjects.TargetColors[3];
                                break;
                        }

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
                                if (!cellAttributes.CellAttributes.Cell.IsTaken)
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

                        PostUpdateCommands.RemoveComponent<RequireMarkerUpdate>(e);

                    }
                });
                gameStateData.Dispose();
            }
        }
    }
}

