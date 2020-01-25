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
            //Debug.Log("mouseHitXZ: " + mouseHitXZPos.x + ", " + mouseHitXZPos.y);

            Vector3f posToCubeCoord = PixelToCube(mouseHitXZPos);

            if(Input.GetButtonDown("Fire1"))
            {
                //instantiate mouse particle at hitPos
                Object.Instantiate(settings.MouseClickPS, hit.point, Quaternion.identity);

            }

            //set to center of hovered cell instead of hit.point
            m_PlayerStateSystem.SetHoveredCoordinates(posToCubeCoord, hit.point);

            //HighlightingDataComponent h = m_AuthoritativePlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
            //Debug.Log("posToCubeCoord: " + posToCubeCoord.X + ", " + posToCubeCoord.Y + ", " + posToCubeCoord.Z);

            var mouseStateJob = new MouseStateJob
            {
                //PathFinding = m_PathFindingSystem,
                HoveredCoord = posToCubeCoord,
                CellAttributes = myTypeFromEntity,
                PlayerEntities = m_AuthoritativePlayerData.ToEntityArray(Allocator.TempJob),
                MouseLeftButtonDown = Input.GetButtonDown("Fire1"),
                //MouseRightButtonDown = Input.GetButtonDown("Fire2"),
                Hit = hit,
                PlayerStates = m_AuthoritativePlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob),
                PlayerFactions = m_AuthoritativePlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob),
                //HighlightingDatas = m_AuthoritativePlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob),
                ECBuffer = entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            return mouseStateJob.Schedule(this, inputDeps);
        }
        else
            return inputDeps;
    }

    public Vector3f PixelToCube(Vector2 point)
    {
        var q = (2f / 3 * point.x);
        var r = (-1f / 3 * point.x + Mathf.Sqrt(3) / 3 * point.y);
        return CellGridMethods.CubeRound(CellGridMethods.AxialToCube(new Vector2(q,r)));
    }


    struct MouseStateJob : IJobForEachWithEntity<Position.Component, MouseState, MouseVariables, CubeCoordinate.Component, SpatialEntityId, MarkerState>
    {
        [ReadOnly] public ComponentDataFromEntity<CellAttributesComponent.Component> CellAttributes;
        //public PathFindingSystem PathFinding;
        public Vector3f HoveredCoord;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<Entity> PlayerEntities;
        public RaycastHit Hit;
        public bool MouseLeftButtonDown;
        //public bool MouseRightButtonDown;
        public EntityCommandBuffer.Concurrent ECBuffer;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<FactionComponent.Component> PlayerFactions;
        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
        public NativeArray<PlayerState.Component> PlayerStates;
        //[NativeDisableParallelForRestriction, DeallocateOnJobCompletion]
       // public NativeArray<HighlightingDataComponent> HighlightingDatas;

        public void Execute(Entity entity, int index, ref Position.Component pos, ref MouseState mouseState, ref MouseVariables mouseVars, ref CubeCoordinate.Component coord, ref SpatialEntityId id, ref MarkerState markerState)
        {
            var playerFaction = PlayerFactions[0];
            var playerState = PlayerStates[0];
            //var highLighting = HighlightingDatas[0];
            Vector3 position = pos.Coords.ToUnityVector() + new Vector3(0, mouseVars.yOffset, 0);
            Vector3 hitDist = Hit.point - position;
            float hitSquared = hitDist.sqrMagnitude;

            //long unitOnCellId;

            //if (markerState.IsUnit == 0)
                //unitOnCellId = CellAttributes[entity].CellAttributes.Cell.UnitOnCellId;
            //else
                //unitOnCellId = 0;

            //highLighting.HoveredCoordinate = HoveredCoord;

            //reset clicked mouseStates on rightclick
            /*
            if(mouseState.CurrentState == MouseState.State.Clicked && MouseRightButtonDown)
            {
                Debug.Log("RightMouseDown!");
                mouseState.CurrentState = MouseState.State.Neutral;
            }
            

            if (playerState.CurrentState == PlayerStateEnum.waiting_for_target && MouseRightButtonDown)
            {
                Debug.Log("ResetMouseStates MouseRight");
                mouseState.CurrentState = MouseState.State.Neutral;
            }
            */

            if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(HoveredCoord)    /*hitSquared < mouseVars.Distance * mouseVars.Distance*/)
            {
                //Debug.Log("Hit a Coordinate!");
                if (MouseLeftButtonDown && playerState.CurrentState != PlayerStateEnum.ready)
                {
                    //add reactive component ClickEvent to flag this object as a clicked object
                    //other arrays should be able to be culled to the clicked object
                    ECBuffer.AddComponent(index, entity, new ClickEvent());
                    mouseState.ClickEvent = 1;
                }
                else if (mouseState.CurrentState != MouseState.State.Clicked)
                {
                    if (mouseState.CurrentState != MouseState.State.Hovered)
                    {
                        /*
                        if (playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                        {
                            //if (Vector3fext.ToUnityVector(highLighting.HoveredCoordinate) != Vector3fext.ToUnityVector(coord.CubeCoordinate))
                            //{
                                //highLighting.HoveredCoordinate = coord.CubeCoordinate;
                                //highLighting.HoveredPosition = position;
                            //}
                            //else
                            //{
                                //highLighting.LastHoveredCoordinate = new Vector3f(999, 999, 999);
                            //}
                        }
                        else
                        {
                            /*
                            if (highLighting.IsUnitTarget == 1)
                            {
                                if (markerState.IsUnit == 1)
                                {
                                    //Debug.Log("HighlightingWantsUnit and markerState is a unit: " + Vector3fext.ToUnityVector(coord.CubeCoordinate));
                                    //if(PathFinding.ValidateTarget(entity, (UnitRequisitesEnum)highLighting.TargetRestrictionIndex, playerState.SelectedUnitId, playerFaction.Faction))
                                    //{
                                    highLighting.HoveredCoordinate = coord.CubeCoordinate;
                                    highLighting.HoveredPosition = position - new Vector3(0, mouseVars.yOffset, 0);

                                    //}
                                    //if (CellGridSystem.ValidateUnitTarget(id, PlayerState.SelectedUnitId, Faction.Faction, (UnitRequisitesEnum)HighlightingData.TargetRestrictionIndex))
                                }
                                else
                                {
                                    //this gets called sometimes even tho we're hovering a unit
                                    //Debug.Log("reset coord because it's not a unit");
                                    if (unitOnCellId == 0)
                                    {
                                        highLighting.HoveredCoordinate = new Vector3f(999, 999, 999);
                                        highLighting.HoveredPosition = new Vector3(0, 0, 0);
                                    }
                                    else
                                    {
                                        highLighting.HoveredCoordinate = coord.CubeCoordinate;
                                        highLighting.HoveredPosition = position - new Vector3(0, mouseVars.yOffset, 0);
                                    }
                                }
                            }
                            else if (markerState.IsUnit == 0)
                            {
                                highLighting.HoveredCoordinate = coord.CubeCoordinate;
                                highLighting.HoveredPosition = position;
                            }

                        }
                        //ECBuffer.SetComponent(index, PlayerEntities[0], highLighting);
                        */
                        mouseState.CurrentState = MouseState.State.Hovered;
                    }
                }
            }
            else if(mouseState.CurrentState != MouseState.State.Clicked && mouseState.CurrentState != MouseState.State.Neutral)
            {
                mouseState.CurrentState = MouseState.State.Neutral;
            }
            /*
            else
            {
                if (mouseState.CurrentState == MouseState.State.Clicked)
                {
                    if (MouseLeftButtonDown || MouseRightButtonDown)
                    {
                        mouseState.CurrentState = MouseState.State.Neutral;
                    }
                }
                else
                {
                    if (mouseState.CurrentState != MouseState.State.Neutral)
                    {
                        mouseState.CurrentState = MouseState.State.Neutral;
                    }
                }
            }
            */

            if (mouseState.ClickEvent == 1 && !MouseLeftButtonDown)
            {
                ECBuffer.RemoveComponent(index, entity, typeof(ClickEvent));
                mouseState.CurrentState = MouseState.State.Clicked;
                mouseState.ClickEvent = 0;
            }

        }
    }
}
