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

        /*
        m_MouseStateData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<MarkerState>(),
            ComponentType.ReadOnly<MouseVariables>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadWrite<MouseState>()
        );
        */

        m_AuthoritativePlayerData = GetEntityQuery(
            ComponentType.ReadWrite<PlayerState.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
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
        if (eventSystem.IsPointerOverGameObject() || m_AuthoritativePlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() == 0)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var playerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>();
        var playerEntity = m_AuthoritativePlayerData.GetSingletonEntity();
        var playerEffects = EntityManager.GetComponentObject<PlayerEffects>(playerEntity);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100, settings.MouseRayCastLayerMask))
        {
            /*
            float dist = Vector3.Distance(ray.origin, hit.point);
            Debug.DrawRay(ray.origin, ray.direction * dist, Color.red);
            */

            Vector2 mapCenter = new Vector2(gameState.MapCenter.X, gameState.MapCenter.Y);
            Vector2 mouseHitXZPos = (mapCenter - new Vector2(hit.point.x, hit.point.z)) * -1;
            Vector3f posToCubeCoord = CellGridMethods.PosToCube(mouseHitXZPos);
            Vector3 CubeCoordToWorldPos = CellGridMethods.CubeToPos(posToCubeCoord, gameState.MapCenter);

            var mouseLeftClick = Input.GetButtonDown("Fire1");
            var mouseRightClick = Input.GetButtonDown("Fire2");

            if (mouseLeftClick)
            {
                m_PlayerStateSystem.ResetInputCoolDown(0.3f);
                PlaySFXAndIncrement(playerEffects, hit.point);
            }
            else
                m_PlayerStateSystem.SetHoveredCoordinates(posToCubeCoord, CubeCoordToWorldPos);

            EntityCommandBuffer.ParallelWriter ECBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            JobHandle jobHandle = Entities.ForEach((Entity entity, int entityInQueryIndex, ref MouseState mouseState, in SpatialEntityId id, in CubeCoordinate.Component coord) =>
            {
                if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(posToCubeCoord))
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
                    else if (mouseState.CurrentState != MouseState.State.Clicked)
                    {
                        if (mouseState.CurrentState != MouseState.State.Hovered)
                        {
                            ECBuffer.AddComponent(entityInQueryIndex, entity, new HoveredState());
                            mouseState.CurrentState = MouseState.State.Hovered;
                        }
                    }
                }
                else if (mouseState.CurrentState != MouseState.State.Clicked && mouseState.CurrentState != MouseState.State.Neutral)
                {
                    if (mouseState.CurrentState == MouseState.State.Hovered)
                        ECBuffer.RemoveComponent(entityInQueryIndex, entity, typeof(HoveredState));
                    mouseState.CurrentState = MouseState.State.Neutral;
                }

                if (mouseState.ClickEvent == 1 && !mouseLeftClick)
                {
                    ECBuffer.RemoveComponent(entityInQueryIndex, entity, typeof(ClickEvent));
                    mouseState.CurrentState = MouseState.State.Clicked;
                    mouseState.ClickEvent = 0;
                }

                if (mouseState.RightClickEvent == 1 && !mouseRightClick)
                {
                    ECBuffer.RemoveComponent(entityInQueryIndex, entity, typeof(RightClickEvent));
                    mouseState.RightClickEvent = 0;
                }
            })
            .Schedule(inputDeps);

            return jobHandle;
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
