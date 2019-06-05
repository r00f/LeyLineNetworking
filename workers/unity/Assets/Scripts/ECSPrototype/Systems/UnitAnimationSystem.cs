using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;

[UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(GameStateSystem))]
public class UnitAnimationSystem : ComponentSystem
{
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Health.Component> HealthData;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<Position.Component> Positions;
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
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] GameStateData m_GameStateData;

    [Inject] HandleCellGridRequestsSystem m_CellGridSystem;

    GameObject GarbageCollection;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        GarbageCollection = GameObject.FindGameObjectWithTag("GarbageCollection");
    }

    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var serverPosition = m_UnitData.Positions[i];
            var transform = m_UnitData.Transforms[i];
            var animatorComponent = m_UnitData.AnimatorComponents[i];
            var actions = m_UnitData.ActionsData[i];
            var healthComponent = m_UnitData.HealthData[i];

            if(animatorComponent.LastHealth != healthComponent.CurrentHealth)
            {
                if(animatorComponent.LastHealth > healthComponent.CurrentHealth)
                {
                    if(animatorComponent.TriggerEnter)
                    {
                        if(healthComponent.CurrentHealth == 0)
                        {
                            Debug.Log("Death");

                            //move all Ragdoll GOs into GarbageCollection


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
                        else
                        {
                            Debug.Log("LostHealth");
                            animatorComponent.Animator.SetTrigger("GetHit");
                        }
                        animatorComponent.TriggerEnter = false;
                        animatorComponent.LastHealth = healthComponent.CurrentHealth;
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
                        //Debug.Log("Set OneTime anim vars");

                        if (actions.LockedAction.Index != -2)
                        {
                            animatorComponent.RotationTarget = GetTargetPosition(actions.LockedAction.Targets[0].TargetId);
                        }

                        //convert cube Coordinate to position
                        Vector2 XZPos = m_CellGridSystem.CubeCoordToXZ(actions.LockedAction.Targets[0].TargetCoordinate);
                        //Find correct YPos
                        float YPos = 3;
                        animatorComponent.DestinationPosition = new Vector3(XZPos.x, YPos, XZPos.y);
                            
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
                    animatorComponent.Animator.SetTrigger("DestinationReached");
                    animatorComponent.DestinationPosition = Vector3.zero;
                    animatorComponent.DestinationReachTriggerSet = true;
                }
            }
        }
    }

    public Vector3 GetTargetPosition(long Id)
    {
        for (int i = 0; i < m_TransformData.Length; i++)
        {
            var transform = m_TransformData.Transforms[i];
            var id = m_TransformData.EntityIds[i].EntityId.Id;

            if (Id == id)
            {
                return transform.position;
            }
        }
        return new Vector3();
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
