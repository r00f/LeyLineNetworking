using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using UnityEngine.EventSystems;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class MouseStateSystem : JobComponentSystem
{
    public struct Data
    {
        public readonly int Length;
        public readonly ComponentDataArray<Position.Component> PositonData;
        public ComponentDataArray<MouseState> MouseStateData;
    }

    [Inject] private Data m_Data;

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
        public RaycastHit hit;
        public bool mouseButtonDown;

        public void Execute(int index)
        {
            MouseState state = MouseStates[index];
            Vector3 pos = Positons[index].Coords.ToUnityVector() + new Vector3(0, state.yOffset, 0);

            Vector3 hitDist = hit.point - pos;
            float hitSquared = hitDist.sqrMagnitude;

            if (hitSquared < state.Distance * state.Distance)
            {
                //set its MouseState to Clicked if we click
                if (mouseButtonDown)
                {
                    state.CurrentState = MouseState.State.Clicked;
                    state.ClickEvent = 1;
                    MouseStates[index] = state;
                }
                //set its MouseState to Hovered if we hover
                else if (state.CurrentState != MouseState.State.Clicked)
                {
                    if (state.CurrentState != MouseState.State.Hovered)
                    {
                        state.CurrentState = MouseState.State.Hovered;
                        MouseStates[index] = state;
                    }
                }
                else if (state.ClickEvent == 1)
                {
                    state.CurrentState = MouseState.State.Clicked;
                    state.ClickEvent = 0;
                    MouseStates[index] = state;
                }
            }
            //if the mouse is anywhere but over the collider 
            else
            {
                //and this entities MouseState is clicked
                if (state.CurrentState == MouseState.State.Clicked)
                {
                    //set it to Neutral if we click
                    if (mouseButtonDown)
                    {
                        state.CurrentState = MouseState.State.Neutral;
                        MouseStates[index] = state;
                    }

                }
                //set it to neutral if it was Hovered
                else
                {
                    if (state.CurrentState != MouseState.State.Neutral)
                    {
                        state.CurrentState = MouseState.State.Neutral;
                        MouseStates[index] = state;
                    }
                }
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
                Positons = m_Data.PositonData,
                MouseStates = m_Data.MouseStateData,
            }.Schedule(m_Data.Length, 1, inputDeps);

            return mouseStateJob;
        }
        else
            return inputDeps;
    }
}
