using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
public class UnitAnimationSystem : ComponentSystem
{
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Health.Component> HealthData;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<Position.Component> Positions;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coordinates;
        public readonly ComponentArray<Unit_BaseDataSet> BaseDataSets;
        public ComponentArray<AnimatorComponent> AnimatorComponents;
        public ComponentArray<Transform> Transforms;
    }

    [Inject] UnitData m_UnitData;

    public struct TransformData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentArray<Transform> Transforms;
    }

    [Inject] TransformData m_TransformData;

    public struct GameStateData
    {
        public readonly int Length;
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] GameStateData m_GameStateData;


    [Inject] ActionEffectsSystem m_ActionEffectsSystem;

    [Inject] HandleCellGridRequestsSystem m_CellGridSystem;

    [Inject] UISystem m_UISystem;

    GameObject GarbageCollection;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        GarbageCollection = GameObject.FindGameObjectWithTag("GarbageCollection");
    }

    protected override void OnUpdate()
    {
        if (m_GameStateData.Length == 0)
            return;

        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var serverPosition = m_UnitData.Positions[i];
            var transform = m_UnitData.Transforms[i];
            var animatorComponent = m_UnitData.AnimatorComponents[i];
            var actions = m_UnitData.ActionsData[i];
            var healthComponent = m_UnitData.HealthData[i];
            var unitId = m_UnitData.EntityIds[i].EntityId.Id;
            var worldIndex = m_UnitData.WorldIndexData[i].Value;
            var coord = m_UnitData.Coordinates[i].CubeCoordinate;
            var basedata = m_UnitData.BaseDataSets[i];

            ECSAction currentlockedAction = null;

            if(actions.LockedAction.Index >= 0)
            {
                if (actions.LockedAction.Index < basedata.Actions.Count)
                {
                    currentlockedAction = basedata.Actions[actions.LockedAction.Index];
                }
                else {
                    currentlockedAction = basedata.SpawnActions[actions.LockedAction.Index - basedata.Actions.Count];
                        }
            }
            else
            {
                switch(actions.LockedAction.Index)
                {
                    case -3:
                        break;
                    case -2:
                        currentlockedAction = basedata.BasicMove;
                        break;
                    case -1:
                        currentlockedAction = basedata.BasicAttack;
                        break;
                }

                    
            }


            if (animatorComponent.AnimationEvents.EventTrigger)
            {
                if (currentlockedAction)
                {
                    var actionEffectType = actions.LockedAction.Effects[0].EffectType;

                    HashSet<Vector3f> coordsToTrigger = new HashSet<Vector3f> { actions.LockedAction.Targets[0].TargetCoordinate };
                    Vector3 targetPos = m_CellGridSystem.CoordinateToWorldPosition(worldIndex, actions.LockedAction.Targets[0].TargetCoordinate);

                    if (actions.LockedAction.Targets[0].Mods.Count != 0)
                    {
                        foreach (Vector3f c in actions.LockedAction.Targets[0].Mods[0].Coordinates)
                        {
                            coordsToTrigger.Add(c);
                        }
                    }

                    if (currentlockedAction.ProjectileFab)
                    {
                        float targetYoffset = 0;
                        if(currentlockedAction.Targets[0] is ECSATarget_Unit)
                        {
                            targetYoffset = 1.3f;
                        }
                        m_ActionEffectsSystem.LaunchProjectile(currentlockedAction.ProjectileFab, actionEffectType, coordsToTrigger, animatorComponent.ProjectileSpawnOrigin.position, targetPos, targetYoffset);
                    }
                    else
                    {
                        m_ActionEffectsSystem.TriggerActionEffect(actions.LockedAction.Effects[0].EffectType, coordsToTrigger);
                    }
                }
                animatorComponent.AnimationEvents.EventTrigger = false;
            }

            if(animatorComponent.LastHealth != healthComponent.CurrentHealth)
            {
                if(animatorComponent.LastHealth > healthComponent.CurrentHealth)
                {
                    if(animatorComponent.ActionEffectTrigger)
                    {
                        Debug.Log("ActionEffectTrigger");
                        if(healthComponent.CurrentHealth == 0)
                        {
                            Debug.Log("Death");

                            //move props out of skeleton
                            foreach (Transform t in animatorComponent.Props)
                            {
                                t.parent = animatorComponent.Animator.transform;
                            }

                            animatorComponent.Animator.transform.parent = GarbageCollection.transform;

                            //disable animator
                            animatorComponent.Animator.enabled = false;


                            //set all rigidbodies to non kinematic
                            foreach (Rigidbody r in animatorComponent.RagdollRigidBodies)
                            {
                                r.isKinematic = false;
                            }
                        }
                        //iterate on when healthchange feedback is being triggered: right now only works with basic attack when in meelee range
                        else
                        {
                            m_UISystem.SetHealthFloatText(unitId, animatorComponent.LastHealth - healthComponent.CurrentHealth);
                            animatorComponent.Animator.SetTrigger("GetHit");
                        }
                        animatorComponent.LastHealth = healthComponent.CurrentHealth;
                        animatorComponent.ActionEffectTrigger = false;
                    }
                }
                else
                {
                    //Debug.Log("Gained Health");
                    animatorComponent.LastHealth = healthComponent.CurrentHealth;
                }

            }


            if (animatorComponent.Animator.GetInteger("ActionIndexInt") != actions.LockedAction.Index)
                animatorComponent.Animator.SetInteger("ActionIndexInt", actions.LockedAction.Index);

            if (animatorComponent.Animator.GetFloat("ActionIndex") != actions.LockedAction.Index)
                animatorComponent.Animator.SetFloat("ActionIndex", actions.LockedAction.Index);

            if (m_GameStateData.GameState[0].CurrentState != GameStateEnum.planning)
            {
                if(actions.LockedAction.Index != -3)
                {
                    if (!animatorComponent.ExecuteTriggerSet)
                    {
                        animatorComponent.Animator.SetTrigger("Execute");
                        animatorComponent.ExecuteTriggerSet = true;
                    }

                    //set Initial Target Positions
                    if (!animatorComponent.InitialValuesSet)
                    {
                        if (actions.LockedAction.Index != -2)
                        {
                            animatorComponent.RotationTarget = m_CellGridSystem.CoordinateToWorldPosition(worldIndex, actions.LockedAction.Targets[0].TargetCoordinate);
                        }

                        animatorComponent.DestinationPosition = m_CellGridSystem.CoordinateToWorldPosition(worldIndex, actions.LockedAction.Targets[0].TargetCoordinate);

                        //GetTargetPosition(actions.LockedAction.Targets[0].TargetId);
                        animatorComponent.InitialValuesSet = true;
                    }

                    if (actions.LockedAction.Index == -2)
                    {
                        if (animatorComponent.RotationTarget != serverPosition.Coords.ToUnityVector())
                            animatorComponent.RotationTarget = serverPosition.Coords.ToUnityVector();
                    }

                    Vector3 targetDirection = RotateTowardsDirection(animatorComponent.RotateTransform, animatorComponent.RotationTarget, 3);
                    animatorComponent.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
            else
            {
                if (animatorComponent.InitialValuesSet)
                {
                    animatorComponent.InitialValuesSet = false;
                }

                if (animatorComponent.DestinationReachTriggerSet)
                {
                    animatorComponent.Animator.ResetTrigger("DestinationReached");
                    animatorComponent.DestinationReachTriggerSet = false;
                }
                if (animatorComponent.ExecuteTriggerSet)
                {
                    animatorComponent.Animator.ResetTrigger("Execute");
                    animatorComponent.ExecuteTriggerSet = false;
                }
            }

            if (transform.position != serverPosition.Coords.ToUnityVector())
            {
                //move
                transform.position = Vector3.MoveTowards(transform.position, serverPosition.Coords.ToUnityVector(), Time.deltaTime);
            }

            if (transform.position == animatorComponent.DestinationPosition)
            {
                if (!animatorComponent.DestinationReachTriggerSet)
                {
                    animatorComponent.LastStationaryCoordinate = coord;
                    animatorComponent.Animator.SetTrigger("DestinationReached");
                    animatorComponent.DestinationPosition = Vector3.zero;
                    animatorComponent.DestinationReachTriggerSet = true;
                }
            }
        }
    }

    public Vector3 RotateTowardsDirection(Transform originTransform, Vector3 targetPosition, float rotationSpeed)
    {
        Vector3 targetDir = targetPosition - originTransform.position;
        targetDir.y = 0;
        float rotSpeed = Time.deltaTime * rotationSpeed;
        Vector3 direction = Vector3.RotateTowards(originTransform.forward, targetDir, rotSpeed, 0.0f);
        
        return direction;
    }

}
