using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class UICleanupSystem : ComponentSystem
{
    EntityQuery m_HeadUIData;

    protected override void OnCreate()
    {
        m_HeadUIData = GetEntityQuery(
        ComponentType.ReadWrite<UnitHeadUI>()
        );
    }

    protected override void OnUpdate()
    {
        Entities.With(m_HeadUIData).ForEach((UnitHeadUI headUI) =>
        {
            if(headUI.FlagForDestruction)
            {
                Object.Destroy(headUI.gameObject, headUI.DestroyWaitTime);
            }
        });
    }
}