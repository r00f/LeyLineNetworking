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

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class MouseStateSystem : JobComponentSystem
{
    public struct MouseStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Position.Component> PositonData;
        public readonly ComponentDataArray<MouseVariables> MouseVariables;
        public ComponentDataArray<MouseState> MouseStates;
    }

    [Inject] MouseStateData m_MouseStateData;

    public struct AuthoritativePlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Authoritative<PlayerState.Component>> AuthorativeData;
        public readonly ComponentDataArray<PlayerState.Component> PlayerStateData;
    }

    [Inject] AuthoritativePlayerData m_AuthoritativePlayerData;


    EventSystem eventSystem;

    protected override void OnCreateManager()
    {
        eventSystem = Object.FindObjectOfType<EventSystem>();
    }

    struct MouseStateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [ReadOnly] public ComponentDataArray<Position.Component> Positons;
        [NativeDisableParallelForRestriction]
        public ComponentDataArray<MouseState> MouseStates;
        [NativeDisableParallelForRestriction]
        public ComponentDataArray<MouseVariables> MouseVars;
        public PlayerState.Component PlayerState;
        public RaycastHit hit;
        public bool mouseButtonDown;

        public void Execute(int index)
        {
            MouseState state = MouseStates[index];
            MouseVariables vars = MouseVars[index];
            Vector3 pos = Positons[index].Coords.ToUnityVector() + new Vector3(0, vars.yOffset, 0);
            Vector3 hitDist = hit.point - pos;
            float hitSquared = hitDist.sqrMagnitude;

            if (hitSquared < vars.Distance * vars.Distance)
            {
                if (mouseButtonDown && PlayerState.CurrentState != PlayerStateEnum.ready)
                {
                    state.ClickEvent = 1;
                    MouseStates[index] = state;
                }
                else if (state.CurrentState != MouseState.State.Clicked)
                {
                    //when cell is occupied, over unit instead
                    if (state.CurrentState != MouseState.State.Hovered)
                    {
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
                PlayerState = m_AuthoritativePlayerData.PlayerStateData[0]
            }.Schedule(m_MouseStateData.Length, 1, inputDeps);

            return mouseStateJob;
        }
        else
            return inputDeps;
    }
}
