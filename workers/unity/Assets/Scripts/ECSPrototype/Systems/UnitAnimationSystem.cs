using UnityEngine;
using Unity.Entities;
using Unit;
using Generic;
using Improbable;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using System.Collections.Generic;
using Unity.Collections;
using Cell;
using Player;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class UnitAnimationSystem : ComponentSystem
{
    ActionEffectsSystem m_ActionEffectsSystem;
    UISystem m_UISystem;
    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    EntityQuery m_TransformData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadOnly<CellAttributesComponent.Component>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>()
        );

        m_TransformData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<Transform>()
        );

        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<WorldIndex.Component>(),
        ComponentType.ReadOnly<IsVisible>(),
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<Energy.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<Unit_BaseDataSet>(),
        ComponentType.ReadOnly<UnitEffects>(),
        ComponentType.ReadWrite<AnimatorComponent>(),
        ComponentType.ReadWrite<Transform>(),
        ComponentType.ReadOnly<MovementVariables.Component>()
        );

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<Vision.Component>(),
        ComponentType.ReadOnly<HeroTransform>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>(),
        ComponentType.ReadOnly<HighlightingDataComponent>(),
        ComponentType.ReadOnly<FactionComponent.Component>()
        );

        settings = Resources.Load<Settings>("Settings");
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_UISystem = World.GetExistingSystem<UISystem>();
        m_ActionEffectsSystem = World.GetExistingSystem<ActionEffectsSystem>();

    }

    protected override void OnUpdate()
    {
        if (m_GameStateData.CalculateEntityCount() == 0 || m_PlayerData.CalculateEntityCount() == 0)
            return;

        var playerHighs = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);
        var playerVisionData = m_PlayerData.ToComponentDataArray<Vision.Component>(Allocator.TempJob);
        var playerHeroTransforms = m_PlayerData.ToComponentArray<HeroTransform>();
        var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

        var playerVision = playerVisionData[0];
        var playerHeroTransform = playerHeroTransforms[0];
        var playerHigh = playerHighs[0];

        Entities.With(m_UnitData).ForEach((Entity e, AnimatorComponent animatorComponent, ref SpatialEntityId id, ref WorldIndex.Component worldIndex, ref Actions.Component actions, ref Energy.Component energy, ref Health.Component health) =>
        {
            var faction = EntityManager.GetComponentData<FactionComponent.Component>(e);
            var serverPosition = EntityManager.GetComponentData<Position.Component>(e);
            var moveVars = EntityManager.GetComponentData<MovementVariables.Component>(e);
            var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);
            var teamColorMeshes = EntityManager.GetComponentObject<TeamColorMeshes>(e);
            var unitComponentReferences = EntityManager.GetComponentObject<UnitComponentReferences>(e);
            var visible = EntityManager.GetComponentData<IsVisible>(e);

            //if this caracter has a filled voice emitter field and a voice event is triggered
            if (animatorComponent.VoiceEmitter && animatorComponent.AnimationEvents.VoiceTrigger)
            {
                animatorComponent.VoiceEmitter.Play();
                animatorComponent.AnimationEvents.VoiceTrigger = false;
            }

            //if this caracter has a filled footStep emitter field and a footstep event is triggered
            if (animatorComponent.FootStempEmitter && animatorComponent.AnimationEvents.FootStepTrigger)
            {
                animatorComponent.FootStempEmitter.Play();
                animatorComponent.AnimationEvents.FootStepTrigger = false;
            }

            //outgoing effects (launch projectiles usw.)
            if (actions.LockedAction.Index != -3)
            {
                if (!animatorComponent.CurrentLockedAction)
                {
                    Unit_BaseDataSet basedata = EntityManager.GetComponentObject<Unit_BaseDataSet>(e);

                    if (actions.LockedAction.Index >= 0)
                    {
                        if (actions.LockedAction.Index < basedata.Actions.Count)
                        {
                            animatorComponent.CurrentLockedAction = basedata.Actions[actions.LockedAction.Index];
                        }
                        else
                        {
                            animatorComponent.CurrentLockedAction = basedata.SpawnActions[actions.LockedAction.Index - basedata.Actions.Count];
                        }
                    }
                    else
                    {
                        switch (actions.LockedAction.Index)
                        {
                            case -3:
                                break;
                            /*case -2:
                                animatorComponent.CurrentLockedAction = basedata.BasicMove;
                                break;
                            case -1:
                                animatorComponent.CurrentLockedAction = basedata.BasicAttack;
                                break;*/
                        }
                    }
                }

                else if (gameStates[0].CurrentState != GameStateEnum.planning)
                {
                    if(gameStates[0].CurrentState == GameStateEnum.interrupt)
                    {
                        animatorComponent.RotationTarget = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, gameStates[0].MapCenter); //&CoordinateToWorldPosition(worldIndex.Value, actions.LockedAction.Targets[0].TargetCoordinate);
                    }


                    if (animatorComponent.AnimationEvents)
                    {
                        /*
                        if (animatorComponent.AnimationEvents.EffectGameObjectIndex > -1)
                        {
                            animatorComponent.CharacterEffects[animatorComponent.AnimationEvents.EffectGameObjectIndex].SetActive(!animatorComponent.CharacterEffects[animatorComponent.AnimationEvents.EffectGameObjectIndex].activeSelf);
                            animatorComponent.AnimationEvents.EffectGameObjectIndex = -1;
                        }
                        */

                        foreach (AnimStateEffectHandler a in animatorComponent.AnimStateEffectHandlers)
                        {
                            if(a.IsActiveState)
                            {
                                for (int i = 0; i < a.CurrentEffectOnTimestamps.Count; i++)
                                {
                                    if (a.CurrentEffectOnTimestamps[i].x <= 0)
                                    {
                                        animatorComponent.CharacterEffects[(int) a.CurrentEffectOnTimestamps[i].y].SetActive(true);
                                    }
                                }

                                for (int i = 0; i < a.CurrentEffectOffTimestamps.Count; i++)
                                {
                                    if (a.CurrentEffectOffTimestamps[i].x <= 0)
                                    {
                                        animatorComponent.CharacterEffects[(int) a.CurrentEffectOffTimestamps[i].y].SetActive(false);
                                    }
                                }
                            }
                        }



                        //event triggered from animation
                        if (animatorComponent.AnimationEvents.EventTrigger)
                        {
                            if (animatorComponent.CurrentLockedAction.ProjectileFab)
                            {
                                Vector3 targetPos = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, gameStates[0].MapCenter);
                                float targetYoffset = 0;
                                if (animatorComponent.CurrentLockedAction.Targets[0] is ECSATarget_Unit)
                                {
                                    targetYoffset = 1.3f;
                                }
                                m_ActionEffectsSystem.LaunchProjectile(playerVision, animatorComponent.CurrentLockedAction.ProjectileFab, animatorComponent.ProjectileSpawnOrigin, targetPos, actions.LockedAction, id.EntityId.Id, coord.CubeCoordinate, targetYoffset);
                            }
                            else
                            {
                                //Debug.Log("Trigger Action Effect: " + animatorComponent.Animator.GetInteger("ActionIndexInt"));
                                m_ActionEffectsSystem.TriggerActionEffect(playerVision, actions.LockedAction, id.EntityId.Id, animatorComponent.WeaponTransform);
                            }

                            animatorComponent.AnimationEvents.EventTriggered = true;
                            animatorComponent.AnimationEvents.EventTrigger = false;
                        }
                    }

                    if (!animatorComponent.ExecuteTriggerSet)
                    {
                        ExecuteActionAnimation(actions, animatorComponent, gameStates[0], worldIndex.Value);
                    }
                    else
                    {
                        //constantly rotate towards serverposition if moving
                        if (gameStates[0].CurrentState == GameStateEnum.move)
                        {
                            if (animatorComponent.RotationTarget != serverPosition.Coords.ToUnityVector())
                                animatorComponent.RotationTarget = serverPosition.Coords.ToUnityVector();
                        }
                    }


                    //rotate animatorComponent.RotateTransform towards targetDirection
                    Vector3 targetDirection = RotateTowardsDirection(animatorComponent.RotateTransform, animatorComponent.RotationTarget, animatorComponent.RotationSpeed);
                    animatorComponent.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);

                }
            }

            if (animatorComponent.Visuals.Count != 0)
            {
                if (animatorComponent.EnableVisualsDelay >= 0)
                {
                    //initially rotate visuals AWAY from hero(so leech makes more sense)
                    if(playerHeroTransform.Transform && moveVars.StartRotation == 0)
                    {
                        Vector3 dir = animatorComponent.RotateTransform.position - playerHeroTransform.Transform.position;
                        dir.y = 0;
                        animatorComponent.RotateTransform.rotation = Quaternion.LookRotation(dir);
                    }

                    //animatorComponent.RotateTransform.rotation
                    foreach (GameObject g in animatorComponent.Visuals)
                    {
                        if(g.activeSelf)
                            g.SetActive(false);
                    }
                    animatorComponent.EnableVisualsDelay -= Time.DeltaTime;
                }
                else if (!animatorComponent.Dead)
                {
                    foreach (GameObject g in animatorComponent.Visuals)
                    {
                        g.SetActive(true);
                    }
                }
            }

            //reset in planning SHOULD BE ONLY ONE TIME
            if (actions.LockedAction.Index == -3 && gameStates[0].CurrentState == GameStateEnum.planning)
            {
                animatorComponent.CurrentLockedAction = null;

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
                if (animatorComponent.AnimationEvents.EventTriggered)
                    animatorComponent.AnimationEvents.EventTriggered = false;
            }

            #region Set Animator Variables

            if(gameStates[0].CurrentState == GameStateEnum.planning)
            {
                if(visible.Value == 1 && !playerVision.RevealVision)
                {
                    unitComponentReferences.SelectionCircleGO.SetActive(true);

                    if (Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(playerHigh.HoveredCoordinate))
                    {
                        unitComponentReferences.SelectionMeshRenderer.material.SetColor("_EmissiveColor", teamColorMeshes.color * 30000);
                    }
                    else
                    {
                        unitComponentReferences.SelectionMeshRenderer.material.SetColor("_EmissiveColor", Color.black);
                    }
                }
                else
                    unitComponentReferences.SelectionCircleGO.SetActive(false);

                animatorComponent.Animator.SetBool("Executed", false);
                animatorComponent.Animator.SetBool("Planning", true);
                animatorComponent.Animator.ResetTrigger("Execute");
            }
            else
            {
                unitComponentReferences.SelectionCircleGO.SetActive(false);
                animatorComponent.Animator.SetBool("Planning", false);
            }

            foreach (Renderer r in teamColorMeshes.HarvestingEmissionColorMeshes)
            {
                if (energy.Harvesting)
                {
                    teamColorMeshes.EmissionLerpColor = Color.Lerp(teamColorMeshes.EmissionLerpColor, teamColorMeshes.color * teamColorMeshes.EmissionIntensity, Time.DeltaTime * teamColorMeshes.EmissionLerpTime);
                }
                else
                {
                    teamColorMeshes.EmissionLerpColor = Color.Lerp(teamColorMeshes.EmissionLerpColor, Color.black, Time.DeltaTime * teamColorMeshes.EmissionLerpTime);
                }

                r.materials[r.materials.Length - 1].SetColor("_EmissiveColor", teamColorMeshes.EmissionLerpColor);
            }

            if (animatorComponent.CurrentLockedAction)
                animatorComponent.Animator.SetBool("HasWindup", animatorComponent.CurrentLockedAction.HasWindup);
            else
                animatorComponent.Animator.SetBool("HasWindup", false);

            animatorComponent.Animator.SetBool("Harvesting", energy.Harvesting);

            if (animatorComponent.Animator.GetInteger("ActionIndexInt") != actions.LockedAction.Index)
                animatorComponent.Animator.SetInteger("ActionIndexInt", actions.LockedAction.Index);

            if (animatorComponent.Animator.GetFloat("ActionIndex") != actions.LockedAction.Index)
                animatorComponent.Animator.SetFloat("ActionIndex", actions.LockedAction.Index);

            #endregion

            #region Movement

            if (animatorComponent.transform.position != serverPosition.Coords.ToUnityVector())
            {
                float step;
                if (actions.LockedAction.Index != -3)
                    step = (Time.DeltaTime / actions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell) * 1.732f;
                else
                    step = Time.DeltaTime * 1.732f;

                //move
                animatorComponent.transform.position = Vector3.MoveTowards(animatorComponent.transform.position, serverPosition.Coords.ToUnityVector(), step);
            }

            Vector2 posXZ = new Vector2(animatorComponent.transform.position.x, animatorComponent.transform.position.z);

            if (posXZ == animatorComponent.DestinationPosition)
            {
                if (!animatorComponent.DestinationReachTriggerSet)
                {
                    //Debug.Log("DestinationReached");
                    animatorComponent.Animator.SetTrigger("DestinationReached");
                    unitEffects.LastStationaryCoordinate = coord.CubeCoordinate;
                    animatorComponent.DestinationPosition = Vector3.zero;
                    animatorComponent.DestinationReachTriggerSet = true;
                }
            }

            #endregion
        });

        playerVisionData.Dispose();
        playerHighs.Dispose();
        gameStates.Dispose();
    }

    public void ExecuteActionAnimation(Actions.Component actions, AnimatorComponent animatorComponent, GameState.Component gameState, uint worldIndex)
    {
        if ((int)actions.LockedAction.ActionExecuteStep == (int)gameState.CurrentState - 2)
        {
            if (!animatorComponent.InitialValuesSet)
            {
                var pos = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, gameState.MapCenter);
                animatorComponent.DestinationPosition = new Vector2(pos.x, pos.z);
                animatorComponent.InitialValuesSet = true;
            }
            animatorComponent.Animator.SetTrigger("Execute");
            animatorComponent.Animator.SetBool("Executed", true);
            animatorComponent.ExecuteTriggerSet = true;
        }
    }

    public Vector3 RotateTowardsDirection(Transform originTransform, Vector3 targetPosition, float rotationSpeed)
    {
        Vector3 targetDir = targetPosition - originTransform.position;
        targetDir.y = 0;
        float rotSpeed = Time.DeltaTime * rotationSpeed;
        Vector3 direction = Vector3.RotateTowards(originTransform.forward, targetDir, rotSpeed, 0.0f);
        
        return direction;
    }

}
