﻿using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Entities;
using Generic;
using Unit;
using System.Collections.Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(UnitAnimationSystem))]
public class ActionEffectsSystem : ComponentSystem
{
    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<MarkerState> MarkerStateData;
        public ComponentArray<MarkerGameObjects> MarkerGameObjects;
    }

    [Inject] CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public ComponentArray<AnimatorComponent> AnimatorComponents;
    }

    [Inject] UnitData m_UnitData;

    [Inject] HighlightingSystem m_HighlightingSystem;

    protected override void OnUpdate()
    {

        //Debug.Log(m_UnitData.Length);

    }

    public void TriggerActionEffect(EffectTypeEnum inEffectType, HashSet<Vector3f> inCubeCoordinates)
    {
        UpdateInjectedComponentGroups();

        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var animatorComp = m_UnitData.AnimatorComponents[i];

            Debug.Log(animatorComp.LastStationaryCoordinate);

            if (inCubeCoordinates.Contains(animatorComp.LastStationaryCoordinate))
            {
                Debug.Log("Set Unit actionEffectTrigger");
                animatorComp.ActionEffectTrigger = true;
            }
        }

        //set effectType / instanciate particle effect on cell if action requires it
        for(int i = 0; i < m_CellData.Length; i++)
        {
            var coord = m_CellData.Coords[i].CubeCoordinate;
            var references = m_CellData.MarkerGameObjects[i];

            if(inCubeCoordinates.Contains(coord))
            {
                references.EffectType = inEffectType;
            }
        }

    }

    public void LaunchProjectile(Projectile projectileFab, EffectTypeEnum inEffectOnDetonation, HashSet<Vector3f> coordsToTrigger, Vector3 startPos, Vector3 targetPos, float yOffset = 0)
    {
        //save targetPosition / targetYOffset on units?
        Vector3 offSetTarget = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);

        List<Vector3> travellingPoints = new List<Vector3>();

        if(projectileFab.MaxHeight > 0)
        {
            travellingPoints.AddRange(m_HighlightingSystem.CalculateSinusPath(startPos, offSetTarget, projectileFab.MaxHeight));
        }
        else
        {
            travellingPoints.Add(startPos);
            travellingPoints.Add(offSetTarget);
        }

        Projectile go = Object.Instantiate(projectileFab, startPos, Quaternion.identity);
        go.EffectOnDetonation = inEffectOnDetonation;
        go.TravellingCurve = travellingPoints;
        go.CoordinatesToTrigger = coordsToTrigger;
        go.IsTravelling = true;
    }
}
