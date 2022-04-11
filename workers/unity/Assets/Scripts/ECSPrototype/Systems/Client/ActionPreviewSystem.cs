using FMODUnity;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unit;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup)), UpdateBefore(typeof(UnitAnimationSystem))]
public class ActionPreviewSystem : JobComponentSystem
{
    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    ActionEffectsSystem m_ActionEffectsSystem;
    UnitAnimationSystem m_UnitAnimationSystem;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_PlayerData = GetEntityQuery(
            ComponentType.ReadOnly<Vision.Component>(),
            ComponentType.ReadOnly<PlayerState.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<HighlightingDataComponent>()
        );

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>()
        );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        settings = Resources.Load<Settings>("Settings");
        m_ActionEffectsSystem = World.GetExistingSystem<ActionEffectsSystem>();
        m_UnitAnimationSystem = World.GetExistingSystem<UnitAnimationSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_GameStateData.CalculateEntityCount() != 1 || m_PlayerData.CalculateEntityCount() == 0)
            return inputDeps;

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
        var highlightingData = m_PlayerData.GetSingleton<HighlightingDataComponent>();
        var playerVision = m_PlayerData.GetSingleton<Vision.Component>();

        Entities.WithAll<ClientActionRequest.HasAuthority>().ForEach((UnitComponentReferences unitComponentReferences, AnimatorComponent animatorComp, in ClientActionRequest.Component actionRequest, in Actions.Component actions, in SpatialEntityId id, in FactionComponent.Component faction, in CubeCoordinate.Component coord) =>
        {
            if (actionRequest.ActionId < 0 || gameState.CurrentState != GameStateEnum.planning)
            {
                animatorComp.PlayActionSFX = true;

                if (animatorComp.CurrentPreviewAction)
                {
                    if (animatorComp.AnimationEvents)
                    {
                        animatorComp.AnimationEvents.EventTrigger = false;
                        animatorComp.AnimationEvents.EventTriggered = false;
                    }

                    if (animatorComp.Animator)
                    {
                        animatorComp.Animator.fireEvents = false;
                        animatorComp.Animator.SetInteger("Armor", 0);
                        animatorComp.Animator.ResetTrigger("Execute");
                        animatorComp.Animator.SetTrigger("CancelAction");
                        animatorComp.Animator.speed = 1;
                    }

                    foreach (AnimStateEffectHandler a in animatorComp.AnimStateEffectHandlers)
                    {
                        if(a.IsActiveState)
                        {
                            a.CurrentEffectOnTimestamps.Clear();
                            a.CurrentEffectOffTimestamps.Clear();
                            a.IsActiveState = false;
                        }
                    }

                    for(int i = 0; i < unitComponentReferences.MeshMatComponent.AllMesheRenderers.Count; i++)
                        unitComponentReferences.MeshMatComponent.AllMesheRenderers[i].materials = unitComponentReferences.MeshMatComponent.AllMeshMaterials[i].ToArray();

                    foreach (StudioEventEmitter g in animatorComp.CharacterEffects)
                        g.gameObject.SetActive(false);

                    if (animatorComp.MovePreviewUnitDupe)
                        Object.Destroy(animatorComp.MovePreviewUnitDupe.gameObject);

                    Debug.Log("Clear CurrentPreviewAction");

                    animatorComp.CurrentPreviewIndex = actionRequest.ActionId;
                    animatorComp.CurrentPreviewIndex = -3;
                    animatorComp.ResumePreviewAnimation = false;
                    animatorComp.CurrentPreviewAction = null;
                }
                else if (animatorComp.Animator && !animatorComp.Animator.fireEvents && !animatorComp.Animator.GetBool("CancelAction") && (animatorComp.Animator.layerCount < 2 || animatorComp.Animator.GetCurrentAnimatorStateInfo(1).IsName("Empty")))
                {
                    animatorComp.Animator.fireEvents = true;
                }
            }
            else
            {
                if (playerState.SelectedUnitId == id.EntityId.Id && unitComponentReferences.MeshMatComponent)
                {
                    if(playerState.CurrentState == PlayerStateEnum.waiting_for_target)
                        animatorComp.CurrentPreviewTarget = CellGridMethods.CubeToPos(highlightingData.HoveredCoordinate, gameState.MapCenter);

                    if (animatorComp.CurrentPreviewIndex != actionRequest.ActionId)
                    {
                        foreach (StudioEventEmitter g in animatorComp.CharacterEffects)
                            g.gameObject.SetActive(false);

                        if(animatorComp.Animator)
                        {
                            Debug.Log("CancelAction on preview Action Change");
                            animatorComp.Animator.ResetTrigger("Execute");
                            animatorComp.Animator.SetTrigger("CancelAction");
                        }

                        if (actionRequest.ActionId < unitComponentReferences.BaseDataSetComp.Actions.Count)
                            animatorComp.CurrentPreviewAction = unitComponentReferences.BaseDataSetComp.Actions[actionRequest.ActionId];
                        else
                            animatorComp.CurrentPreviewAction = unitComponentReferences.BaseDataSetComp.SpawnActions[actionRequest.ActionId - unitComponentReferences.BaseDataSetComp.Actions.Count];

                        if (animatorComp.MovePreviewUnitDupe)
                            Object.Destroy(animatorComp.MovePreviewUnitDupe.gameObject);
                   
                        if(!(animatorComp.CurrentPreviewAction.Effects[0] is ECS_MoveAlongPathEffect))
                        {
                            foreach (Renderer r in unitComponentReferences.MeshMatComponent.AllMesheRenderers)
                            {
                                var mats = new Material[r.materials.Length];

                                for (int i = 0; i < mats.Length; i++)
                                    mats[i] = settings.HolographicMaterials[(int)animatorComp.CurrentPreviewAction.ActionExecuteStep];

                                r.materials = mats;
                            }

                            foreach (AnimationClip c in animatorComp.Animator.runtimeAnimatorController.animationClips)
                            {
                                if (c.name == animatorComp.CurrentPreviewAction.AnimatorStateName)
                                {
                                    animatorComp.FirstEventTiming = c.events[0].time;
                                    Debug.Log("Clip length = " + c.length + ", first event time = " + c.events[0].time);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < unitComponentReferences.MeshMatComponent.AllMesheRenderers.Count; i++)
                                unitComponentReferences.MeshMatComponent.AllMesheRenderers[i].materials = unitComponentReferences.MeshMatComponent.AllMeshMaterials[i].ToArray();

                            animatorComp.MovePreviewUnitDupe = Object.Instantiate(unitComponentReferences.MeshMatComponent, animatorComp.transform.position, animatorComp.Animator.transform.rotation, animatorComp.Animator.transform.parent);

                            foreach (Renderer r in animatorComp.MovePreviewUnitDupe.AllMesheRenderers)
                            {
                                var mats = new Material[r.materials.Length];

                                for (int i = 0; i < mats.Length; i++)
                                    mats[i] = settings.HolographicMaterials[(int) animatorComp.CurrentPreviewAction.ActionExecuteStep];

                                r.materials = mats;
                            }

                            animatorComp.MovePreviewUnitDupe.gameObject.SetActive(false);
                        }

                        Debug.Log("Reset ResumePreviewAction. CurrentPreviewIndex = " + animatorComp.CurrentPreviewIndex + ", actionId = " + actionRequest.ActionId);

                        animatorComp.PlayActionSFX = true;
                        animatorComp.AnimationEvents.EventTrigger = false;
                        animatorComp.ResumePreviewAnimation = false;
                        animatorComp.CurrentPreviewIndex = actionRequest.ActionId;
                    }
                }

                if(animatorComp.Animator && animatorComp.CurrentPreviewAction)
                {
                    if(animatorComp.CurrentPreviewAction.Effects[0] is ECS_MoveAlongPathEffect)
                    {
                        //animate path movement for dupe
                        if (animatorComp.MovePreviewUnitDupe && Vector3fext.ToUnityVector(actionRequest.TargetCoordinate) != Vector3.zero)
                        {
                            if (unitComponentReferences.LinerendererComp.lineRenderer.positionCount > 0)
                            {
                                if(!animatorComp.MovePreviewUnitDupe.gameObject.activeSelf)
                                {
                                    animatorComp.MovePreviewUnitDupe.gameObject.SetActive(true);
                                    animatorComp.MovePreviewUnitDupe.Animator.SetInteger("ActionIndexInt", actionRequest.ActionId);
                                    animatorComp.MovePreviewUnitDupe.Animator.SetTrigger("Execute");
                                }

                                Vector3[] linePosArray = new Vector3[unitComponentReferences.LinerendererComp.lineRenderer.positionCount];
                                unitComponentReferences.LinerendererComp.lineRenderer.GetPositions(linePosArray);

                                m_UnitAnimationSystem.MoveObjectAlongPath(animatorComp.MovePreviewUnitDupe, linePosArray, actions.ActionsList[actionRequest.ActionId].Effects[0].MoveAlongPathNested.TimePerCell, animatorComp.RotationSpeed, false);
                            }
                        }
                    }
                    else if (animatorComp.AnimationEvents)
                    {
                        var currentStateInfo = animatorComp.Animator.GetCurrentAnimatorStateInfo(1);
                        var nextStateInfo = animatorComp.Animator.GetNextAnimatorStateInfo(1);

                        if (!nextStateInfo.IsName(animatorComp.CurrentPreviewAction.AnimatorStateName) && !currentStateInfo.IsName(animatorComp.CurrentPreviewAction.AnimatorStateName))
                            animatorComp.Animator.CrossFadeInFixedTime(animatorComp.CurrentPreviewAction.AnimatorStateName, 0.2f, 1, animatorComp.FirstEventTiming - 0.2f);

                        if (!animatorComp.ResumePreviewAnimation)
                        {
                            if (animatorComp.CurrentPreviewAction.Targets[0].targettingRange > 0)
                            {
                                animatorComp.Animator.SetInteger("Armor", 0);
                                if (Vector3fext.ToUnityVector(actionRequest.TargetCoordinate) != Vector3.zero)
                                {
                                    animatorComp.Animator.ResetTrigger("CancelAction");
                                    animatorComp.Animator.speed = 1;
                                    animatorComp.ResumePreviewAnimation = true;
                                }
                                else if (currentStateInfo.IsName(animatorComp.CurrentPreviewAction.AnimatorStateName))
                                {

                                    animatorComp.Animator.ResetTrigger("CancelAction");
                                    if (currentStateInfo.length * currentStateInfo.normalizedTime == animatorComp.FirstEventTiming)
                                        animatorComp.Animator.speed = 0;
                                    else
                                        animatorComp.Animator.PlayInFixedTime(animatorComp.CurrentPreviewAction.AnimatorStateName, 1, animatorComp.FirstEventTiming);
                                }
                            }
                            else
                            {
                                //self target currently only used for armoring
                                animatorComp.Animator.speed = 1;
                                animatorComp.Animator.SetTrigger("Execute");
                                animatorComp.Animator.SetInteger("Armor", 10);
                                animatorComp.ResumePreviewAnimation = true;
                            }
                        }
                        if (animatorComp.AnimationEvents.EventTrigger && animatorComp.ResumePreviewAnimation)
                        {
                            if (animatorComp.CurrentPreviewAction.ProjectileFab)
                            {
                                float targetYoffset = 0;

                                if (animatorComp.CurrentPreviewAction.Targets[0] is ECSATarget_Unit)
                                    targetYoffset = 1.3f;

                                m_ActionEffectsSystem.LaunchProjectile(faction.Faction, playerVision, animatorComp.CurrentPreviewAction.ProjectileFab, animatorComp.ProjectileSpawnOrigin, animatorComp.CurrentPreviewTarget, actions.ActionsList[actionRequest.ActionId], id.EntityId.Id, coord.CubeCoordinate, targetYoffset, true, animatorComp.PlayActionSFX);
                            }
                            animatorComp.PlayActionSFX = false;
                            unitComponentReferences.AnimatorComp.AnimationEvents.EventTriggered = true;
                            animatorComp.AnimationEvents.EventTrigger = false;
                        }
                    }

                    if (animatorComp.RotateTransform)
                    {
                        Vector3 targetDirection = m_UnitAnimationSystem.RotateTowardsDirection(animatorComp.RotateTransform, animatorComp.CurrentPreviewTarget, animatorComp.RotationSpeed);
                        animatorComp.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);
                    }
                }
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }
}
