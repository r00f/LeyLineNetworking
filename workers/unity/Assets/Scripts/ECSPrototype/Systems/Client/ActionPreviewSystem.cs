using FMODUnity;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using System.Collections;
using System.Collections.Generic;
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

        Entities.ForEach((UnitComponentReferences unitComponentReferences, AnimatorComponent animator, in ClientActionRequest.Component actionRequest, in Actions.Component actions, in SpatialEntityId id, in FactionComponent.Component faction, in CubeCoordinate.Component coord) =>
        {
            if (actionRequest.ActionId <= 0 || gameState.CurrentState != GameStateEnum.planning)
            {
                animator.PlayActionSFX = true;

                if (animator.CurrentPreviewAction)
                {
                    for(int i = 0; i < unitComponentReferences.AllMesheRenderers.Count; i++)
                    {
                        unitComponentReferences.AllMesheRenderers[i].materials = unitComponentReferences.AllMeshMaterials[i].ToArray();
                    }

                    foreach (StudioEventEmitter g in animator.CharacterEffects)
                        g.gameObject.SetActive(false);

                    animator.CurrentPreviewIndex = actionRequest.ActionId;

                    animator.Animator.ResetTrigger("Execute");
                    animator.Animator.SetTrigger("CancelAction");
                    animator.CurrentPreviewIndex = 0;
                    animator.ResumePreviewAnimation = false;
                    animator.Animator.speed = 1;
                    animator.CurrentPreviewAction = null;
                }
            }
            else
            {
                if (playerState.CurrentState == PlayerStateEnum.waiting_for_target && playerState.SelectedUnitId == id.EntityId.Id)
                {
                    animator.CurrentPreviewTarget = CellGridMethods.CubeToPos(highlightingData.HoveredCoordinate, gameState.MapCenter);

                    if (animator.CurrentPreviewIndex != actionRequest.ActionId)
                    {
                        if (actionRequest.ActionId < unitComponentReferences.BaseDataSetComp.Actions.Count)
                            animator.CurrentPreviewAction = unitComponentReferences.BaseDataSetComp.Actions[actionRequest.ActionId];
                        else
                            animator.CurrentPreviewAction = unitComponentReferences.BaseDataSetComp.SpawnActions[actionRequest.ActionId - unitComponentReferences.BaseDataSetComp.Actions.Count];

                        foreach (Renderer r in unitComponentReferences.AllMesheRenderers)
                        {
                            var mats = new Material[r.materials.Length];

                            for (int i = 0; i < mats.Length; i++)
                            {
                                mats[i] = settings.ActionPreviewMat;
                            }

                            r.materials = mats;
                        }

                        animator.PlayActionSFX = true;
                        animator.AnimationEvents.EventTrigger = false;
                        animator.ResumePreviewAnimation = false;
                        animator.CurrentPreviewIndex = actionRequest.ActionId;
                    }
                }
                if (animator.Animator && animator.AnimationEvents && animator.CurrentPreviewAction)
                {
                    animator.Animator.SetInteger("ActionIndexInt", actionRequest.ActionId);
                    animator.Animator.SetTrigger("Execute");

                    var currentClip = animator.Animator.GetCurrentAnimatorClipInfo(1);
                    var currentState = animator.Animator.GetCurrentAnimatorStateInfo(1);

                    var nextClip = animator.Animator.GetNextAnimatorClipInfo(1);
                    var nextState = animator.Animator.GetNextAnimatorStateInfo(1);
                    //nextClip[0].clip.length * nextState.normalizedTime actual clip time

                    var stoptime = .1f;

                    if (nextClip.Length > 0)
                    {
                        animator.CurrentPreviewAnimTime = nextState.normalizedTime;
                    }
                    else if (currentClip.Length > 0)
                    {
                        animator.CurrentPreviewAnimTime = currentState.normalizedTime;
                    }

                    if (!animator.ResumePreviewAnimation)
                    {
                        if (Input.GetButtonDown("Fire1"))
                            animator.ResumePreviewAnimation = true;

                        if (animator.CurrentPreviewAnimTime < stoptime)
                            animator.Animator.speed = 1.1f - animator.CurrentPreviewAnimTime / stoptime;
                        else
                            animator.Animator.speed = 0;
                    }
                    else
                    {
                        animator.Animator.speed = 1;
                    }

                    if (animator.AnimationEvents.EventTrigger && animator.ResumePreviewAnimation)
                    {
                        if (animator.CurrentPreviewAction.ProjectileFab)
                        {
                            float targetYoffset = 0;
                            if (animator.CurrentPreviewAction.Targets[0] is ECSATarget_Unit)
                            {
                                targetYoffset = 1.3f;
                            }
                            m_ActionEffectsSystem.LaunchProjectile(faction.Faction, playerVision, animator.CurrentPreviewAction.ProjectileFab, animator.ProjectileSpawnOrigin, animator.CurrentPreviewTarget, actions.ActionsList[actionRequest.ActionId], id.EntityId.Id, coord.CubeCoordinate, targetYoffset, true, animator.PlayActionSFX);
                        }
                        animator.PlayActionSFX = false;
                        unitComponentReferences.AnimatorComp.AnimationEvents.EventTriggered = true;
                        animator.AnimationEvents.EventTrigger = false;
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
