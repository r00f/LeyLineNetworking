using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using UnityEngine.EventSystems;
using Improbable.Gdk.ReactiveComponents;
using Player;
using Cell;
using Unit;
using Generic;
using System.Collections.Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class MouseStateSystem : JobComponentSystem
{
    public struct MouseStateData
    {
        public readonly int Length;
        public EntityArray EntityData;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<SpatialEntityId> EntityIDs;
        public readonly ComponentDataArray<MarkerState> MarkerStates;
        public readonly ComponentDataArray<Position.Component> PositonData;
        public readonly ComponentDataArray<MouseVariables> MouseVariables;
        public ComponentDataArray<MouseState> MouseStates;
    }

    [Inject] MouseStateData m_MouseStateData;

    public struct AuthoritativePlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<FactionComponent.Component> Faction;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
        public ComponentDataArray<HighlightingDataComponent> HighlightingData;
    }

    [Inject] AuthoritativePlayerData m_AuthoritativePlayerData;

    EventSystem eventSystem;

    public class EntityBarrier : BarrierSystem { }

    [Inject] private EntityBarrier _EntityBarrier;


    protected override void OnCreateManager()
    {
        eventSystem = Object.FindObjectOfType<EventSystem>();
    }

    struct MouseStateJob : IJobParallelFor
    {
        [ReadOnly] public ComponentDataArray<Position.Component> Positons;
        public ComponentDataArray<MouseState> MouseStates;
        public ComponentDataArray<MouseVariables> MouseVars;
        public ComponentDataArray<MarkerState> MarkerStates;
        public ComponentDataArray<SpatialEntityId> Ids;
        public ComponentDataArray<CubeCoordinate.Component> Coords;
        public EntityArray Entities;
        public PlayerState.Component PlayerState;
        [NativeDisableParallelForRestriction]
        public ComponentDataArray<HighlightingDataComponent> HighlightingData;
        public RaycastHit hit;
        public bool mouseButtonDown;
        //public HandleCellGridRequestsSystem CellGridSystem;
        public FactionComponent.Component Faction;
        [NativeDisableParallelForRestriction]
        public EntityCommandBuffer CommandBuffer;

        public void Execute(int index)
        {
            var entity = Entities[index];
            HighlightingDataComponent highLighting = HighlightingData[0];
            CubeCoordinate.Component coord = Coords[index];
            MarkerState markerState = MarkerStates[index];
            MouseState state = MouseStates[index];
            MouseVariables vars = MouseVars[index];
            Vector3 pos = Positons[index].Coords.ToUnityVector() + new Vector3(0, vars.yOffset, 0);
            Vector3 hitDist = hit.point - pos;
            float hitSquared = hitDist.sqrMagnitude;
            var id = Ids[index].EntityId.Id;
           
            if (hitSquared < vars.Distance * vars.Distance)
            {
                if (mouseButtonDown && PlayerState.CurrentState != PlayerStateEnum.ready)
                {
                    //add reactive component ClickEvent to flag this object as a clicked object
                    //other arrays should be able to be culled to the clicked object
                    CommandBuffer.AddComponent(entity, new ClickEvent());
                    state.ClickEvent = 1;
                    MouseStates[index] = state;
                }
                else if (state.CurrentState != MouseState.State.Clicked)
                {
                    //when cell is occupied, over unit instead
                    if (state.CurrentState != MouseState.State.Hovered)
                    {
                        if (highLighting.IsUnitTarget == 1)
                        {
                            if (markerState.IsUnit == 1)
                            {
                                //if (CellGridSystem.ValidateUnitTarget(id, PlayerState.SelectedUnitId, Faction.Faction, (UnitRequisitesEnum)HighlightingData.TargetRestrictionIndex))
                                //{
                                highLighting.HoveredCoordinate = coord.CubeCoordinate;
                                highLighting.HoveredPosition = pos - new Vector3(0, vars.yOffset, 0);
                                //}
                            }
                            else if (highLighting.HoveredPosition != Vector3.zero)
                            {
                                highLighting.HoveredCoordinate = new Vector3f(999, 999, 999);
                                highLighting.HoveredPosition = new Vector3(0, 0, 0);
                            }
                        }
                        else
                        {
                            if (markerState.IsUnit == 0)
                            {
                                highLighting.HoveredCoordinate = coord.CubeCoordinate;
                                highLighting.HoveredPosition = pos;
                            }
                        }

                        HighlightingData[0] = highLighting;
                        //Debug.Log(HighlightingData[0].HoveredCoordinate);
                        state.CurrentState = MouseState.State.Hovered;
                        MouseStates[index] = state;
                    }
                }
            } 
            else
            {
                if (state.CurrentState == MouseState.State.Clicked)
                {
                    if (mouseButtonDown)
                    {
                        state.CurrentState = MouseState.State.Neutral;
                        MouseStates[index] = state;
                    }
                }
                else
                {
                    if (state.CurrentState != MouseState.State.Neutral)
                    {
                        state.CurrentState = MouseState.State.Neutral;
                        MouseStates[index] = state;
                    }
                }
            }

            if (state.ClickEvent == 1 && !mouseButtonDown)
            {
                CommandBuffer.RemoveComponent<ClickEvent>(entity);
                state.CurrentState = MouseState.State.Clicked;
                state.ClickEvent = 0;
                MouseStates[index] = state;
            }
        }
        
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit) && !eventSystem.IsPointerOverGameObject())
        {
            var mouseStateJob = new MouseStateJob
            {
                mouseButtonDown = Input.GetButtonDown("Fire1"),
                hit = hit,
                Positons = m_MouseStateData.PositonData,
                MouseStates = m_MouseStateData.MouseStates,
                MouseVars = m_MouseStateData.MouseVariables,
                MarkerStates = m_MouseStateData.MarkerStates,
                PlayerState = m_AuthoritativePlayerData.PlayerStateData[0],
                HighlightingData = m_AuthoritativePlayerData.HighlightingData,
                Ids = m_MouseStateData.EntityIDs,
                Coords = m_MouseStateData.Coords,
                Entities = m_MouseStateData.EntityData,
                CommandBuffer = _EntityBarrier.CreateCommandBuffer()

            }.Schedule(m_MouseStateData.Length, 1, inputDeps);
            return mouseStateJob;
        }
        else
            return inputDeps;
    }
}
