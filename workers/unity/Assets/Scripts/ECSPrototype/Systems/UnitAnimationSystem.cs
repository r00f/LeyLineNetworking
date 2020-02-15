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

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class UnitAnimationSystem : ComponentSystem
{
    ActionEffectsSystem m_ActionEffectsSystem;
    UISystem m_UISystem;
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
        ComponentType.ReadOnly <WorldIndex.Component>(),
        ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<Energy.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<Unit_BaseDataSet>(),
        ComponentType.ReadOnly<UnitEffects>(),
        ComponentType.ReadWrite<AnimatorComponent>(),
        ComponentType.ReadWrite<Transform>()
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
        if (m_GameStateData.CalculateEntityCount() == 0)
            return;

        var gameStates = m_GameStateData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

        Entities.With(m_UnitData).ForEach((Entity e, AnimatorComponent animatorComponent, ref SpatialEntityId id, ref WorldIndex.Component worldIndex, ref Actions.Component actions, ref Energy.Component energy, ref Health.Component health) =>
        {
            var faction = EntityManager.GetComponentData<FactionComponent.Component>(e);
            var serverPosition = EntityManager.GetComponentData<Position.Component>(e);
            var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(e);
            var teamColorMeshes = EntityManager.GetComponentObject<TeamColorMeshes>(e);

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
                            case -2:
                                animatorComponent.CurrentLockedAction = basedata.BasicMove;
                                break;
                            case -1:
                                animatorComponent.CurrentLockedAction = basedata.BasicAttack;
                                break;
                        }
                    }
                }

                else if (gameStates[0].CurrentState != GameStateEnum.planning)
                {
                    if(gameStates[0].CurrentState == GameStateEnum.interrupt)
                    {
                        animatorComponent.RotationTarget = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, new Vector2(28f, 28f)); //&CoordinateToWorldPosition(worldIndex.Value, actions.LockedAction.Targets[0].TargetCoordinate);
                    }

                    if (animatorComponent.AnimationEvents)
                    {
                        if (animatorComponent.AnimationEvents.EffectGameObjectIndex > -1)
                        {
                            animatorComponent.CharacterEffects[animatorComponent.AnimationEvents.EffectGameObjectIndex].SetActive(!animatorComponent.CharacterEffects[animatorComponent.AnimationEvents.EffectGameObjectIndex].activeSelf);
                            animatorComponent.AnimationEvents.EffectGameObjectIndex = -1;
                        }

                        //event triggered from animation
                        if (animatorComponent.AnimationEvents.EventTrigger)
                        {
                            if (animatorComponent.CurrentLockedAction.ProjectileFab)
                            {
                                //THIS METHOD SUCKS
                                Vector3 targetPos = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, new Vector2(28f, 28f)); //CoordinateToWorldPosition(worldIndex.Value, actions.LockedAction.Targets[0].TargetCoordinate);
                                float targetYoffset = 0;
                                if (animatorComponent.CurrentLockedAction.Targets[0] is ECSATarget_Unit)
                                {
                                    targetYoffset = 1.3f;
                                }
                                m_ActionEffectsSystem.LaunchProjectile(animatorComponent.CurrentLockedAction.ProjectileFab, animatorComponent.ProjectileSpawnOrigin, targetPos, actions.LockedAction, id.EntityId.Id, targetYoffset);
                            }
                            else
                            {
                                Debug.Log("Trigger Action Effect: " + animatorComponent.Animator.GetInteger("ActionIndexInt"));
                                m_ActionEffectsSystem.TriggerActionEffect(actions.LockedAction, id.EntityId.Id, animatorComponent.WeaponTransform);
                            }

                            animatorComponent.AnimationEvents.EventTrigger = false;
                        }
                    }

                    if (!animatorComponent.ExecuteTriggerSet)
                    {
                        ExecuteActionAnimation(actions, animatorComponent, gameStates[0].CurrentState, worldIndex.Value);
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
                    animatorComponent.EnableVisualsDelay -= Time.deltaTime;
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
            }

            #region Set Animator Variables

            if(gameStates[0].CurrentState == GameStateEnum.planning)
            {
                animatorComponent.Animator.SetBool("Executed", false);
                animatorComponent.Animator.SetBool("Planning", true);
            }
            else
            {
                animatorComponent.Animator.SetBool("Planning", false);
            }

            //if this unit has emissionColorMeshes set intensity to 0 / 100 when harvesting
            if(teamColorMeshes.EmissionColorMeshes.Count != 0)
            {
                Color color = settings.FactionColors[(int)faction.TeamColor + 1];
                foreach(Renderer r in teamColorMeshes.EmissionColorMeshes)
                {
                    if (energy.Harvesting)
                    {
                        teamColorMeshes.EmissionLerpColor = Color.Lerp(teamColorMeshes.EmissionLerpColor, color * teamColorMeshes.EmissionIntensity, Time.deltaTime * teamColorMeshes.EmissionLerpTime);
                        //r.material.SetColor("_EmissiveColor", color * 100);
                    }
                    else
                    {
                        teamColorMeshes.EmissionLerpColor = Color.Lerp(teamColorMeshes.EmissionLerpColor, Color.black, Time.deltaTime * teamColorMeshes.EmissionLerpTime);
                    }

                    r.material.SetColor("_EmissiveColor", teamColorMeshes.EmissionLerpColor);
                }
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
                    step = (Time.deltaTime / actions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell) * 1.732f;
                else
                    step = Time.deltaTime * 1.732f;

                //move
                animatorComponent.transform.position = Vector3.MoveTowards(animatorComponent.transform.position, serverPosition.Coords.ToUnityVector(), step);
            }

            Vector3 posZeroY = new Vector3(animatorComponent.transform.position.x, 5, animatorComponent.transform.position.z);

            if (posZeroY == animatorComponent.DestinationPosition)
            {
                if (!animatorComponent.DestinationReachTriggerSet)
                {
                    Debug.Log("DestinationReached");
                    unitEffects.LastStationaryCoordinate = coord.CubeCoordinate;
                    animatorComponent.Animator.SetTrigger("DestinationReached");
                    animatorComponent.DestinationPosition = Vector3.zero;
                    animatorComponent.DestinationReachTriggerSet = true;
                }
            }

            #endregion
        });

        gameStates.Dispose();
    }

    public void ExecuteActionAnimation(Actions.Component actions, AnimatorComponent animatorComponent, GameStateEnum gameState, uint worldIndex)
    {
        if ((int)actions.LockedAction.ActionExecuteStep == (int)gameState - 2)
        {
            if (!animatorComponent.InitialValuesSet)
            {
                animatorComponent.DestinationPosition = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, new Vector2(28f, 28f));
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
        float rotSpeed = Time.deltaTime * rotationSpeed;
        Vector3 direction = Vector3.RotateTowards(originTransform.forward, targetDir, rotSpeed, 0.0f);
        
        return direction;
    }

}
