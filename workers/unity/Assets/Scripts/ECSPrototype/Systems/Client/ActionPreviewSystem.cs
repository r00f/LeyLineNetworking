using FMODUnity;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unit;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
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

        Entities.WithAll<ClientActionRequest.HasAuthority>().ForEach((UnitComponentReferences unitComponentReferences, AnimatorComponent animator, in ClientActionRequest.Component actionRequest, in Actions.Component actions, in SpatialEntityId id, in FactionComponent.Component faction, in CubeCoordinate.Component coord) =>
        {
            if (actionRequest.ActionId < 0 || gameState.CurrentState != GameStateEnum.planning)
            {
                animator.PlayActionSFX = true;

                if (animator.CurrentPreviewAction)
                {
                    if (animator.Animator)
                    {
                        animator.Animator.fireEvents = false;
                        animator.Animator.SetInteger("Armor", 0);
                        animator.Animator.ResetTrigger("Execute");
                        animator.Animator.SetTrigger("CancelAction");
                        animator.Animator.speed = 1;
                    }

                    foreach (AnimStateEffectHandler a in animator.AnimStateEffectHandlers)
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

                    foreach (StudioEventEmitter g in animator.CharacterEffects)
                        g.gameObject.SetActive(false);

                    if (animator.MovePreviewUnitDupe)
                        Object.Destroy(animator.MovePreviewUnitDupe.gameObject);

                    animator.CurrentPreviewIndex = actionRequest.ActionId;
                    animator.CurrentPreviewIndex = -3;
                    animator.ResumePreviewAnimation = false;
                    animator.CurrentPreviewAction = null;
                }
                else if(animator.Animator && !animator.Animator.fireEvents && (animator.Animator.layerCount < 2 || !animator.Animator.GetAnimatorTransitionInfo(1).anyState))
                {
                    animator.Animator.fireEvents = true;
                }
            }
            else
            {
                if (playerState.SelectedUnitId == id.EntityId.Id)
                {
                    if(playerState.CurrentState == PlayerStateEnum.waiting_for_target)
                        animator.CurrentPreviewTarget = CellGridMethods.CubeToPos(highlightingData.HoveredCoordinate, gameState.MapCenter);

                    if (animator.CurrentPreviewIndex != actionRequest.ActionId)
                    {
                        foreach (StudioEventEmitter g in animator.CharacterEffects)
                            g.gameObject.SetActive(false);

                        if(animator.Animator)
                        {
                            animator.Animator.ResetTrigger("Execute");
                            animator.Animator.SetTrigger("CancelAction");
                        }

                        if (actionRequest.ActionId < unitComponentReferences.BaseDataSetComp.Actions.Count)
                            animator.CurrentPreviewAction = unitComponentReferences.BaseDataSetComp.Actions[actionRequest.ActionId];
                        else
                            animator.CurrentPreviewAction = unitComponentReferences.BaseDataSetComp.SpawnActions[actionRequest.ActionId - unitComponentReferences.BaseDataSetComp.Actions.Count];

                        if (animator.MovePreviewUnitDupe)
                            Object.Destroy(animator.MovePreviewUnitDupe.gameObject);

                        foreach (Renderer r in unitComponentReferences.MeshMatComponent.AllMesheRenderers)
                        {
                            var mats = new Material[r.materials.Length];

                            for (int i = 0; i < mats.Length; i++)
                                mats[i] = settings.ActionPreviewMat;

                            r.materials = mats;
                        }

                        if (animator.CurrentPreviewAction.Effects[0] is ECS_MoveAlongPathEffect && unitComponentReferences.MeshMatComponent)
                        {
                                animator.MovePreviewUnitDupe = Object.Instantiate(unitComponentReferences.MeshMatComponent, animator.transform.position, animator.Animator.transform.rotation, animator.Animator.transform.parent);
                                
                                foreach (Renderer r in animator.MovePreviewUnitDupe.AllMesheRenderers)
                                {
                                    var mats = new Material[r.materials.Length];

                                    for (int i = 0; i < mats.Length; i++)
                                        mats[i] = settings.ActionPreviewMat;

                                    r.materials = mats;
                                }

                                animator.MovePreviewUnitDupe.gameObject.SetActive(false);
                        }
                        else
                        {
                            animator.PlayActionSFX = true;
                            animator.AnimationEvents.EventTrigger = false;
                            animator.ResumePreviewAnimation = false;
                        }

                        animator.CurrentPreviewIndex = actionRequest.ActionId;
                    }
                }

                if(animator.Animator && animator.CurrentPreviewAction)
                {
                    if(animator.CurrentPreviewAction.Effects[0] is ECS_MoveAlongPathEffect)
                    {
                        //animate path movement for dupe
                        if (animator.MovePreviewUnitDupe && Vector3fext.ToUnityVector(actionRequest.TargetCoordinate) != Vector3.zero)
                        {
                            if (unitComponentReferences.LinerendererComp.lineRenderer.positionCount > 0)
                            {
                                if(!animator.MovePreviewUnitDupe.gameObject.activeSelf)
                                {
                                    animator.MovePreviewUnitDupe.gameObject.SetActive(true);
                                    animator.MovePreviewUnitDupe.Animator.SetInteger("ActionIndexInt", actionRequest.ActionId);
                                    animator.MovePreviewUnitDupe.Animator.SetTrigger("Execute");
                                }

                                Vector3[] linePosArray = new Vector3[unitComponentReferences.LinerendererComp.lineRenderer.positionCount];
                                unitComponentReferences.LinerendererComp.lineRenderer.GetPositions(linePosArray);

                                m_UnitAnimationSystem.MoveObjectAlongPath(animator.MovePreviewUnitDupe, linePosArray, actions.ActionsList[actionRequest.ActionId].Effects[0].MoveAlongPathNested.TimePerCell, animator.RotationSpeed, false);
                            }
                        }
                    }
                    else if (animator.AnimationEvents)
                    {
                        animator.Animator.SetInteger("ActionIndexInt", actionRequest.ActionId);

                        if (animator.CurrentPreviewAction.Targets[0].targettingRange > 0)
                        {
                            animator.Animator.SetInteger("Armor", 0);
                            animator.Animator.SetTrigger("Execute");

                            var currentClip = animator.Animator.GetCurrentAnimatorClipInfo(1);
                            var currentState = animator.Animator.GetCurrentAnimatorStateInfo(1);

                            var nextClip = animator.Animator.GetNextAnimatorClipInfo(1);
                            var nextState = animator.Animator.GetNextAnimatorStateInfo(1);
                            //nextClip[0].clip.length * nextState.normalizedTime actual clip time

                            var stoptime = .1f;

                            if (nextClip.Length > 0)
                                animator.CurrentPreviewAnimTime = nextState.normalizedTime;
                            else if (currentClip.Length > 0)
                                animator.CurrentPreviewAnimTime = currentState.normalizedTime;

                            if (!animator.ResumePreviewAnimation)
                            {
                                if (animator.CurrentPreviewAnimTime < stoptime)
                                    animator.Animator.speed = 1.1f - animator.CurrentPreviewAnimTime / stoptime;
                                else
                                    animator.Animator.speed = 0;

                                if (Vector3fext.ToUnityVector(actionRequest.TargetCoordinate) != Vector3.zero)
                                    animator.ResumePreviewAnimation = true;
                            }
                            else
                                animator.Animator.speed = 1;
                        }
                        else if (!animator.ResumePreviewAnimation)
                        {
                            //self target currently only used for armoring
                            animator.Animator.speed = 1;
                            animator.Animator.SetTrigger("Execute");
                            animator.Animator.SetInteger("Armor", 10);
                            animator.ResumePreviewAnimation = true;
                        }

                        if (animator.AnimationEvents.EventTrigger && animator.ResumePreviewAnimation)
                        {
                            if (animator.CurrentPreviewAction.ProjectileFab)
                            {
                                float targetYoffset = 0;

                                if (animator.CurrentPreviewAction.Targets[0] is ECSATarget_Unit)
                                    targetYoffset = 1.3f;

                                m_ActionEffectsSystem.LaunchProjectile(faction.Faction, playerVision, animator.CurrentPreviewAction.ProjectileFab, animator.ProjectileSpawnOrigin, animator.CurrentPreviewTarget, actions.ActionsList[actionRequest.ActionId], id.EntityId.Id, coord.CubeCoordinate, targetYoffset, true, animator.PlayActionSFX);
                            }
                            animator.PlayActionSFX = false;
                            unitComponentReferences.AnimatorComp.AnimationEvents.EventTriggered = true;
                            animator.AnimationEvents.EventTrigger = false;
                        }
                    }

                    if (animator.RotateTransform)
                    {
                        Vector3 targetDirection = m_UnitAnimationSystem.RotateTowardsDirection(animator.RotateTransform, animator.CurrentPreviewTarget, animator.RotationSpeed);
                        animator.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);
                    }
                }
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }
}
