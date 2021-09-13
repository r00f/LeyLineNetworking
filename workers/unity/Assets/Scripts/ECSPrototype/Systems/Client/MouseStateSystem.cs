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
    EntityQuery m_MouseStateData;
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


        m_MouseStateData = GetEntityQuery(
            ComponentType.ReadOnly<CubeCoordinate.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>(),
            ComponentType.ReadOnly<MarkerState>(),
            ComponentType.ReadOnly<MouseVariables>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadWrite<MouseState>()
        );

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
        if (m_AuthoritativePlayerData.CalculateEntityCount() == 0 || m_GameStateData.CalculateEntityCount() == 0)
        {
            //Debug.Log("AuthPlayerCount = 0");
            return inputDeps;
        }

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();



        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        ComponentDataFromEntity<CellAttributesComponent.Component> myTypeFromEntity = GetComponentDataFromEntity<CellAttributesComponent.Component>(true);

        if (Physics.Raycast(ray, out RaycastHit hit, 100, settings.MouseRayCastLayerMask))
        {
            float dist = Vector3.Distance(ray.origin, hit.point);
            Debug.DrawRay(ray.origin, ray.direction * dist, Color.red);

            Vector2 mapCenter = new Vector2(gameState.MapCenter.X, gameState.MapCenter.Y);

            Vector2 mouseHitXZPos = (mapCenter - new Vector2(hit.point.x, hit.point.z)) * -1;

            Vector3f posToCubeCoord = CellGridMethods.PosToCube(mouseHitXZPos);

            Vector3 CubeCoordToWorldPos = CellGridMethods.CubeToPos(posToCubeCoord, gameState.MapCenter);

            if (!eventSystem.IsPointerOverGameObject())
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    m_PlayerStateSystem.ResetInputCoolDown(0.3f);
                    //instantiate mouse particle at hitPos
                    Object.Instantiate(settings.MouseClickPS, hit.point, Quaternion.identity);
                }
                else
                {
                    m_PlayerStateSystem.SetHoveredCoordinates(posToCubeCoord, CubeCoordToWorldPos);
                }
            }

            var mouseStateJob = new MouseStateJob
            {
                PointerOverGO = eventSystem.IsPointerOverGameObject(),
                HoveredCoord = posToCubeCoord,
                CellAttributes = myTypeFromEntity,
                PlayerEntities = m_AuthoritativePlayerData.ToEntityArray(Allocator.TempJob),
                MouseLeftButtonDown = Input.GetButtonDown("Fire1"),
                MouseRightButtonDown = Input.GetButtonDown("Fire2"),
                Hit = hit,
                PlayerState = m_AuthoritativePlayerData.GetSingleton<PlayerState.Component>(),
                ECBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()
            };
            return mouseStateJob.Schedule(this, inputDeps);
        }
        else
        {
            return inputDeps;
        }
    }

    struct MouseStateJob : IJobForEachWithEntity<Position.Component, MouseState, MouseVariables, CubeCoordinate.Component, SpatialEntityId, MarkerState>
    {
        public bool PointerOverGO;
        [ReadOnly] public ComponentDataFromEntity<CellAttributesComponent.Component> CellAttributes;
        public Vector3f HoveredCoord;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<Entity> PlayerEntities;
        public RaycastHit Hit;
        public bool MouseLeftButtonDown;
        public bool MouseRightButtonDown;
        public EntityCommandBuffer.ParallelWriter ECBuffer;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public PlayerState.Component PlayerState;

        public void Execute(Entity entity, int index, ref Position.Component pos, ref MouseState mouseState, ref MouseVariables mouseVars, ref CubeCoordinate.Component coord, ref SpatialEntityId id, ref MarkerState markerState)
        {
            //Only MouseState Updates if cursor is not over a UI element
            if(!PointerOverGO)
            {
                if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(HoveredCoord))
                {
                    if (MouseRightButtonDown && PlayerState.CurrentState != PlayerStateEnum.ready)
                    {
                        ECBuffer.AddComponent(index, entity, new RightClickEvent());
                        mouseState.RightClickEvent = 1;
                    }

                    if (MouseLeftButtonDown && PlayerState.CurrentState != PlayerStateEnum.ready)
                    {
                        ECBuffer.AddComponent(index, entity, new ClickEvent());
                        mouseState.ClickEvent = 1;
                    }
                    else if (mouseState.CurrentState != MouseState.State.Clicked)
                    {
                        if (mouseState.CurrentState != MouseState.State.Hovered)
                        {
                            ECBuffer.AddComponent(index, entity, new HoveredState());
                            mouseState.CurrentState = MouseState.State.Hovered;
                        }
                    }
                }
                else if (mouseState.CurrentState != MouseState.State.Clicked && mouseState.CurrentState != MouseState.State.Neutral)
                {
                    if (mouseState.CurrentState == MouseState.State.Hovered)
                        ECBuffer.RemoveComponent(index, entity, typeof(HoveredState));
                    mouseState.CurrentState = MouseState.State.Neutral;
                }
            }

            if (mouseState.ClickEvent == 1 && !MouseLeftButtonDown)
            {
                ECBuffer.RemoveComponent(index, entity, typeof(ClickEvent));
                mouseState.CurrentState = MouseState.State.Clicked;
                mouseState.ClickEvent = 0;
            }

            if (mouseState.RightClickEvent == 1 && !MouseRightButtonDown)
            {
                ECBuffer.RemoveComponent(index, entity, typeof(RightClickEvent));
                mouseState.RightClickEvent = 0;
            }

        }
    }

}
