using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Unity.Entities;
using Generic;
using Unit;
using System.Collections.Generic;
using Unity.Collections;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateAfter(typeof(UnitAnimationSystem))]
public class ActionEffectsSystem : ComponentSystem
{
    HighlightingSystem m_HighlightingSystem;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_HighlightingSystem = World.GetExistingSystem<HighlightingSystem>();


        m_UnitData = GetEntityQuery(
        ComponentType.ReadWrite<AnimatorComponent>()
        );

        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<MarkerState>(),
        ComponentType.ReadWrite<MarkerGameObjects>()
        );
    }

    protected override void OnUpdate()
    {

        //Debug.Log(m_UnitData.Length);

    }

    public void TriggerActionEffect(EffectTypeEnum inEffectType, HashSet<Vector3f> inCubeCoordinates)
    {
        Entities.With(m_UnitData).ForEach((AnimatorComponent animatorComp) =>
        {
            if (inCubeCoordinates.Contains(animatorComp.LastStationaryCoordinate))
            {
                //Debug.Log("Set Unit actionEffectTrigger from actionEffectsSystem");
                animatorComp.ActionEffectTrigger = true;
            }
        });

        Entities.With(m_CellData).ForEach((MarkerGameObjects references, ref CubeCoordinate.Component coord) =>
        {
            if (inCubeCoordinates.Contains(coord.CubeCoordinate))
            {
                references.EffectType = inEffectType;
            }
        });
    }

    public void LaunchProjectile(Projectile projectileFab, EffectTypeEnum inEffectOnDetonation, HashSet<Vector3f> coordsToTrigger, Transform spawnTransform, Vector3 targetPos, float yOffset = 0)
    {
        //save targetPosition / targetYOffset on units?
        Vector3 offSetTarget = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);

        List<Vector3> travellingPoints = new List<Vector3>();

        //if(projectileFab.MaxHeight > 0)
        //{

        //THIS USES SINUS CALC FOR STRAIGHT LINES -- CHANGE METHOD TO HANDLE STRAIGHT LINES WHITOUT CALCULATING SINUS STUFF
        travellingPoints.AddRange(m_HighlightingSystem.CalculateSinusPath(spawnTransform.position, offSetTarget, projectileFab.MaxHeight));
        //Debug.Log(travellingPoints.Count);
        //}
        //else
        //{
            //travellingPoints.Add(spawnTransform.position);
            //travellingPoints.Add(offSetTarget);
        //}

        //Quaternion lookRotation = new Quaternion();
        Vector3 distance = offSetTarget - spawnTransform.position;
        //lookRotation.SetLookRotation(distance);

        /*

        if (projectileFab.BaseJoint)
        {
            projectileFab.BaseJoint.connectedBody = spawnTransform.GetComponent<Rigidbody>();
        }
        */

        foreach (SpringJoint s in projectileFab.SpringJoints)
        {
            s.maxDistance = distance.magnitude / projectileFab.SpringJoints.Count;
        }

        Projectile projectile = Object.Instantiate(projectileFab, spawnTransform.position, spawnTransform.rotation, spawnTransform.root);

        projectile.SpawnTransform = spawnTransform;
        projectile.EffectOnDetonation = inEffectOnDetonation;
        projectile.TravellingCurve = travellingPoints;
        projectile.CoordinatesToTrigger = coordsToTrigger;
        projectile.IsTravelling = true;
    }
}
