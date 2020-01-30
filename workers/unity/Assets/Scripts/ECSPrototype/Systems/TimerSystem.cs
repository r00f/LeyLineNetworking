using Unity.Entities;
using Improbable.Gdk.Core;
using Unit;
using Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class TimerSystem : ComponentSystem
{
    EntityQuery m_TimerData;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TimerData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadWrite<TurnTimer.Component>()
        );
    }
    protected override void OnUpdate()
    {

    }

    public void SubstractTurnDurations(uint worldIndex)
    {
        Entities.With(m_TimerData).ForEach((ref TurnTimer.Component timer, ref WorldIndex.Component timerWorldIndex) =>
        {
            if (worldIndex == timerWorldIndex.Value)
            {
                for (int t = 0; t < timer.Timers.Count; t++)
                {
                    if (timer.Timers[t].TurnDuration > 1)
                    {
                        var localTimer = timer.Timers[t];
                        localTimer.TurnDuration--;
                        timer.Timers[t] = localTimer;
                    }
                    else
                    {
                        timer.Timers.Remove(timer.Timers[t]);
                    }
                }

                //timer.Timers = timer.Timers;
            }
        });
    }

    public void AddTimedEffect(long inId, ActionEffect inEffect)
    {
        //UpdateInjectedComponentGroups();

        Entities.With(m_TimerData).ForEach((ref TurnTimer.Component timer, ref SpatialEntityId timerId) =>
        {

            if (timerId.EntityId.Id == inId)
            {
                var Timer = new Timer
                {
                    TurnDuration = inEffect.TurnDuration,
                    Effect = inEffect
                };
                timer.Timers.Add(Timer);

                //timer.Timers = timer.Timers;

            }

        });
    }

}
