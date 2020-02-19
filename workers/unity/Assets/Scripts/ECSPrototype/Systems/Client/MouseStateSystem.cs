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

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(HighlightingSystem))]
public class MouseStateSystem : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem entityCommandBufferSystem;

    Settings settings;
    EntityQuery m_AuthoritativePlayerData;
    EntityQuery m_MouseStateData;
    //PathFindingSystem m_PathFindingSystem;
    EventSystem eventSystem;

    PlayerStateSystem m_PlayerStateSystem;

    Vector2 MapCenter = new Vector2(28f, 28f);

    protected override void OnCreate()
    {
        base.OnCreate();
        entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        eventSystem = Object.FindObjectOfType<EventSystem>();
        settings = Resources.Load<Settings>("Settings");
        

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
            ComponentType.ReadOnly<PlayerState.ComponentAuthority>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadWrite<HighlightingDataComponent>()
        );
        m_AuthoritativePlayerData.SetFilter(PlayerState.ComponentAuthority.Authoritative);
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_PlayerStateSystem = World.GetExistingSystem<PlayerStateSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_AuthoritativePlayerData.CalculateEntityCount() == 0)
        {
            Debug.Log("AuthPlayerCount = 0");
            return inputDeps;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        ComponentDataFromEntity<CellAttributesComponent.Component> myTypeFromEntity = GetComponentDataFromEntity<CellAttributesComponent.Component>(true);

        if (Physics.Raycast(ray, out RaycastHit hit) && !eventSystem.IsPointerOverGameObject())
        {
            float dist = Vector3.Distance(ray.origin, hit.point);
            Debug.DrawRay(ray.origin, ray.direction * dist, Color.red);

            Vector2 mouseHitXZPos = (MapCenter - new Vector2(hit.point.x, hit.point.z)) * -1;

            Vector3f posToCubeCoord = CellGridMethods.PosToCube(mouseHitXZPos);

            Vector3 CubeCoordToWorldPos = CellGridMethods.CubeToPos(posToCubeCoord, MapCenter);

            if(Input.GetButtonDown("Fire1"))
            {
                m_PlayerStateSystem.ResetClickCoolDown(0.3f);
                //instantiate mouse particle at hitPos
                Object.Instantiate(settings.MouseClickPS, hit.point, Quaternion.identity);
            }
            else
            {
                m_PlayerStateSystem.SetHoveredCoordinates(posToCubeCoord, CubeCoordToWorldPos);
            }

            var mouseStateJob = new MouseStateJob
            {
                HoveredCoord = posToCubeCoord,
                CellAttributes = myTypeFromEntity,
                PlayerEntities = m_AuthoritativePlayerData.ToEntityArray(Allocator.TempJob),
                MouseLeftButtonDown = Input.GetButtonDown("Fire1"),
                Hit = hit,
                PlayerStates = m_AuthoritativePlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob),
                PlayerFactions = m_AuthoritativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob),
                ECBuffer = entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            return mouseStateJob.Schedule(this, inputDeps);
        }
        else
            return inputDeps;
    }

    struct MouseStateJob : IJobForEachWithEntity<Position.Component, MouseState, MouseVariables, CubeCoordinate.Component, SpatialEntityId, MarkerState>
    {
        [ReadOnly] public ComponentDataFromEntity<CellAttributesComponent.Component> CellAttributes;
        public Vector3f HoveredCoord;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<Entity> PlayerEntities;
        public RaycastHit Hit;
        public bool MouseLeftButtonDown;
        public EntityCommandBuffer.Concurrent ECBuffer;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<FactionComponent.Component> PlayerFactions;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<PlayerState.Component> PlayerStates;

        public void Execute(Entity entity, int index, ref Position.Component pos, ref MouseState mouseState, ref MouseVariables mouseVars, ref CubeCoordinate.Component coord, ref SpatialEntityId id, ref MarkerState markerState)
        {
            var playerFaction = PlayerFactions[0];
            var playerState = PlayerStates[0];

            if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(HoveredCoord))
            {
                if (MouseLeftButtonDown && playerState.CurrentState != PlayerStateEnum.ready)
                {
                    ECBuffer.AddComponent(index, entity, new ClickEvent());
                    mouseState.ClickEvent = 1;
                }
                else if (mouseState.CurrentState != MouseState.State.Clicked)
                {
                    if (mouseState.CurrentState != MouseState.State.Hovered)
                    {
                        mouseState.CurrentState = MouseState.State.Hovered;
                    }
                }
            }
            else if(mouseState.CurrentState != MouseState.State.Clicked && mouseState.CurrentState != MouseState.State.Neutral)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
            }

            if (mouseState.ClickEvent == 1 && !MouseLeftButtonDown)
            {
                ECBuffer.RemoveComponent(index, entity, typeof(ClickEvent));
                mouseState.CurrentState = MouseState.State.Clicked;
                mouseState.ClickEvent = 0;
            }
        }
    }

}
