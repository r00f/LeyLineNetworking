using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using UnityEngine.EventSystems;
//using Improbable.Gdk.ReactiveComponents;
using Player;
using Cell;
using Unit;
using Generic;
using System.Collections.Generic;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(HighlightingSystem))]
public class MouseStateSystem : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem entityCommandBufferSystem;

    Settings settings;
    EntityQuery m_AuthoritativePlayerData;
    //EntityQuery m_MouseStateData;
    EntityQuery m_GameStateData;
    //PathFindingSystem m_PathFindingSystem;
    EventSystem eventSystem;

    PlayerStateSystem m_PlayerStateSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        eventSystem = Object.FindObjectOfType<EventSystem>();
        settings = Resources.Load<Settings>("Settings");

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>()
        );

        m_AuthoritativePlayerData = GetEntityQuery(
            ComponentType.ReadWrite<PlayerState.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<Vision.Component>(),
            ComponentType.ReadWrite<HighlightingDataComponent>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PlayerStateSystem = World.GetExistingSystem<PlayerStateSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (eventSystem.IsPointerOverGameObject() || m_AuthoritativePlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() != 1 || !Camera.main)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var playerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
        var playerVision = m_AuthoritativePlayerData.GetSingleton<Vision.Component>();
        var playerEntity = m_AuthoritativePlayerData.GetSingletonEntity();
        var playerEffects = EntityManager.GetComponentObject<PlayerEffects>(playerEntity);
        var gameStateEntity = m_GameStateData.GetSingletonEntity();
        var mapData = EntityManager.GetComponentObject<CurrentMapState>(gameStateEntity);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100, settings.MouseRayCastLayerMask))
        {
            Vector2 mapCenter = new Vector2(gameState.MapCenter.X, gameState.MapCenter.Y);
            Vector2 mouseHitXZPos = (mapCenter - new Vector2(hit.point.x, hit.point.z)) * -1;
            Vector3f posToCubeCoord = CellGridMethods.PosToCube(mouseHitXZPos);
            Vector3 cubeCoordToWorldPos = CellGridMethods.CubeToPos(posToCubeCoord, gameState.MapCenter);

            var mouseLeftClick = Input.GetButtonDown("Fire1");
            var mouseRightClick = Input.GetButtonDown("Fire2");

            bool noVisibleUnitOnHoveredCell = mapData.CoordinateCellDictionary[CellGridMethods.CubeToAxial(posToCubeCoord)].UnitOnCellId == 0 || !playerVision.CellsInVisionrange.Contains(CellGridMethods.CubeToAxial(posToCubeCoord));

            if (mouseLeftClick)
            {
                m_PlayerStateSystem.ResetInputCoolDown(0.3f);
                PlaySFXAndIncrement(playerEffects, hit.point);
            }
            else
                m_PlayerStateSystem.SetHoveredCoordinates(posToCubeCoord, cubeCoordToWorldPos);

            EntityCommandBuffer.ParallelWriter ECBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            JobHandle hoverJobHandle = Entities.WithNone<Manalith.Component>().ForEach((Entity entity, int entityInQueryIndex, ref MouseState mouseState, in SpatialEntityId id, in CubeCoordinate.Component coord) =>
            {
                if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(posToCubeCoord))
                {
                    if (mouseState.CurrentState != MouseState.State.Hovered)
                    {
                        ECBuffer.AddComponent(entityInQueryIndex, entity, new HoveredState());
                        mouseState.CurrentState = MouseState.State.Hovered;
                    }
                }
                else if (mouseState.CurrentState == MouseState.State.Hovered)
                {
                    mouseState.CurrentState = MouseState.State.Neutral;
                    ECBuffer.RemoveComponent<HoveredState>(entityInQueryIndex, entity);
                }
            })
            .Schedule(inputDeps);

            JobHandle manalithJobHandle = Entities.ForEach((Entity entity, int entityInQueryIndex, ref MouseState mouseState, in SpatialEntityId id, in Manalith.Component manalith, in CubeCoordinate.Component coord) =>
            {
                bool anyManalithCellHovered = false;

                if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(posToCubeCoord))
                {
                    anyManalithCellHovered = true;
                }

                if (noVisibleUnitOnHoveredCell)
                {
                    foreach (Vector3f c in manalith.CircleCoordinatesList)
                    {
                        if (Vector3fext.ToUnityVector(c) == Vector3fext.ToUnityVector(posToCubeCoord) || Vector3fext.ToUnityVector(c) == Vector3fext.ToUnityVector(posToCubeCoord))
                        {
                            anyManalithCellHovered = true;
                        }
                    }
                }

                if (anyManalithCellHovered)
                {
                    if (mouseState.CurrentState != MouseState.State.Hovered)
                    {
                        ECBuffer.AddComponent(entityInQueryIndex, entity, new HoveredState());
                        mouseState.CurrentState = MouseState.State.Hovered;
                    }
                }
                else if (mouseState.CurrentState == MouseState.State.Hovered)
                {
                    ECBuffer.RemoveComponent<HoveredState>(entityInQueryIndex, entity);
                    mouseState.CurrentState = MouseState.State.Neutral;
                }
            })
            .WithoutBurst()
            .Schedule(hoverJobHandle);

            JobHandle clickJobHandle = Entities.WithAll<HoveredState>().ForEach((Entity entity, int entityInQueryIndex, ref MouseState mouseState, in SpatialEntityId id, in CubeCoordinate.Component coord) =>
            {
                if (mouseRightClick && playerState.CurrentState != PlayerStateEnum.ready)
                {
                    ECBuffer.AddComponent(entityInQueryIndex, entity, new RightClickEvent());
                    mouseState.RightClickEvent = 1;
                }

                if (mouseLeftClick && playerState.CurrentState != PlayerStateEnum.ready)
                {
                    ECBuffer.AddComponent(entityInQueryIndex, entity, new ClickEvent());
                    mouseState.ClickEvent = 1;
                }

                if (mouseState.ClickEvent == 1 && !mouseLeftClick)
                {
                    ECBuffer.RemoveComponent<ClickEvent>(entityInQueryIndex, entity);
                    mouseState.CurrentState = MouseState.State.Clicked;
                    mouseState.ClickEvent = 0;
                }

                if (mouseState.RightClickEvent == 1 && !mouseRightClick)
                {
                    ECBuffer.RemoveComponent<RightClickEvent>(entityInQueryIndex, entity);
                    mouseState.RightClickEvent = 0;
                }
            })
            .Schedule(manalithJobHandle);

            return clickJobHandle;
        }
        else
            return inputDeps;
    }

    void PlaySFXAndIncrement(PlayerEffects playerEffects, Vector3 position)
    {
        if (playerEffects.MouseClickSFXComponents.Count == 0)
            return;

        playerEffects.MouseClickSFXComponents[playerEffects.CurrentMouseClickIndex].transform.position = position;
        playerEffects.MouseClickSFXComponents[playerEffects.CurrentMouseClickIndex].PS.Play();
        playerEffects.MouseClickSFXComponents[playerEffects.CurrentMouseClickIndex].SoundEmitter.Play();

        if (playerEffects.CurrentMouseClickIndex + 1 < playerEffects.MouseClickSFXComponents.Count)
            playerEffects.CurrentMouseClickIndex++;
        else
            playerEffects.CurrentMouseClickIndex = 0;
    }
}
