using Unity.Entities;
using Improbable.Gdk.Core;
using Unit;
using Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class TimerSystem : ComponentSystem
{
    public struct TimerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public ComponentDataArray<TurnTimer.Component> Timers;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
    }

    [Inject] TimerData m_TimerData;

    protected override void OnUpdate()
    {

    }

    public void SubstractTurnDurations(uint worldIndex)
    {
        UpdateInjectedComponentGroups();
        for (int i = 0; i < m_TimerData.Length; i++)
        {
            var timer = m_TimerData.Timers[i];
            var timerWorldIndex = m_TimerData.WorldIndexData[i].Value;

            if(worldIndex == timerWorldIndex)
            {
                for(int t = 0; t < timer.Timers.Count; t++)
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

                timer.Timers = timer.Timers;
                m_TimerData.Timers[i] = timer;
            }
        }
    }

    public void AddTimedEffect(long inId, ActionEffect inEffect)
    {
        for (int i = 0; i < m_TimerData.Length; i++)
        {
            var timer = m_TimerData.Timers[i];
            var timerId = m_TimerData.EntityIds[i].EntityId.Id;

            if (timerId == inId)
            {
                var Timer = new Timer
                {
                    TurnDuration = inEffect.TurnDuration,
                    Effect = inEffect
                };
                timer.Timers.Add(Timer);

                timer.Timers = timer.Timers;
                m_TimerData.Timers[i] = timer;
            }

        }
    }

}
