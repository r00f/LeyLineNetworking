using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Improbable;
using Improbable.Gdk.Core;

public class UnitAnimationSystem : ComponentSystem
{
    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentDataArray<Position.Component> Positions;
        public ComponentArray<AnimatorComponent> AnimatorComponents;
        public ComponentArray<Transform> Transforms;
    }

    [Inject] private UnitData m_UnitData;

    public struct TransformData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentArray<Transform> Transforms;
    }

    [Inject] private TransformData m_TransformData;

    public struct GameStateData
    {
        public readonly ComponentDataArray<GameState.Component> GameState;
    }

    [Inject] private GameStateData m_GameStateData;


    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var serverPosition = m_UnitData.Positions[i];
            var transform = m_UnitData.Transforms[i];
            var animatorComponent = m_UnitData.AnimatorComponents[i];
            var actions = m_UnitData.ActionsData[i];

            if (animatorComponent.Animator.GetFloat("ActionIndex") != actions.LockedAction.Index)
                animatorComponent.Animator.SetFloat("ActionIndex", actions.LockedAction.Index);

            if (m_GameStateData.GameState[0].CurrentState != GameStateEnum.planning)
            {
                if (actions.LockedAction.Index != -3 && !animatorComponent.ExecuteTriggerSet)
                {
                    animatorComponent.Animator.SetTrigger("Execute");
                    animatorComponent.ExecuteTriggerSet = true;
                }


                if(actions.LockedAction.Index != -3)
                {
                    Vector3 rotateTarget;

                    if (transform.position != serverPosition.Coords.ToUnityVector())
                    {
                        //move
                        transform.position = Vector3.MoveTowards(transform.position, serverPosition.Coords.ToUnityVector(), Time.deltaTime);
                        rotateTarget = serverPosition.Coords.ToUnityVector();
                        
                    }
                    else
                    {
                        //if moveAction
                        if(actions.LockedAction.Index == -2)
                        {
                            if (transform.position == GetTargetPosition(actions.LockedAction.Targets[0].TargetId))
                            {
                                if(!animatorComponent.DestinationReachTriggerSet)
                                {
                                    animatorComponent.Animator.SetTrigger("DestinationReached");
                                    animatorComponent.DestinationReachTriggerSet = true;
                                }
                            }
                        }

                        rotateTarget = GetTargetPosition(actions.LockedAction.Targets[0].TargetId);
                    }

                    Vector3 targetDirection = RotateTowardsDirection(animatorComponent.RotateTransform, rotateTarget, 3);
                    animatorComponent.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
            else
            {
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
