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
            Vector3 pos = Positons[index].Coords.ToUnityVector();
            MouseState state = MouseStates[index];

            if (Vector3.Distance(hit.point, pos) < .8f)
            {
                //set its MouseState to Clicked if we click
                if (mouseButtonDown)
                {
                    MouseStates[index] = new MouseState
                    {
                        CurrentState = MouseState.State.Clicked
                    };
                }
                //set its MouseState to Hovered if we hover
                else if (state.CurrentState != MouseState.State.Clicked)
                {
                    if (state.CurrentState != MouseState.State.Hovered)
                    {
                        MouseStates[index] = new MouseState
                        {
                            CurrentState = MouseState.State.Hovered
                        };
                    }
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
                        MouseStates[index] = new MouseState
                        {
                            CurrentState = MouseState.State.Neutral
                        };
                    }

                }
                //set it to neutral if it was Hovered
                else
                {
                    if (state.CurrentState != MouseState.State.Neutral)
                    {
                        MouseStates[index] = new MouseState
                        {
                            CurrentState = MouseState.State.Neutral
                        };
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
