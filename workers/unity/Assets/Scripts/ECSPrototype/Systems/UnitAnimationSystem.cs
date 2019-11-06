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
        public readonly ComponentDataArray<Energy.Component> EnergyData;
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

    ComponentGroup GarbageCollectionGroup;

    protected override void OnCreateManager()
    {
        base.OnCreateManager();

    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        GarbageCollection = Object.FindObjectOfType<GarbageCollectorComponent>().gameObject;
        GarbageCollectionGroup = Worlds.DefaultWorld.CreateComponentGroup(
        ComponentType.Create<GarbageCollectorComponent>()
        );
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
            var energy = m_UnitData.EnergyData[i];
            var healthComponent = m_UnitData.HealthData[i];
            var unitId = m_UnitData.EntityIds[i].EntityId.Id;
            var worldIndex = m_UnitData.WorldIndexData[i].Value;
            var coord = m_UnitData.Coordinates[i].CubeCoordinate;
            var basedata = m_UnitData.BaseDataSets[i];


            if (actions.LockedAction.Index != -3)
            {
                if (!animatorComponent.CurrentLockedAction)
                {
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
                
                else if (m_GameStateData.GameState[0].CurrentState != GameStateEnum.planning)
                {
                    var actionEffectType = actions.LockedAction.Effects[0].EffectType;
                    HashSet<Vector3f> coordsToTrigger = new HashSet<Vector3f> { actions.LockedAction.Targets[0].TargetCoordinate };

                    if (animatorComponent.AnimationEvents)
                    {
                        
                        if (animatorComponent.AnimationEvents.EffectGameObjectIndex > -1)
                        {
                            animatorComponent.CharacterEffects[animatorComponent.AnimationEvents.EffectGameObjectIndex].SetActive(!animatorComponent.CharacterEffects[animatorComponent.AnimationEvents.EffectGameObjectIndex].activeSelf);
                            animatorComponent.AnimationEvents.EffectGameObjectIndex = -1;
                        }
                        

                        if (animatorComponent.AnimationEvents.EventTrigger)
                        {
                            if (actions.LockedAction.Targets[0].Mods.Count != 0)
                            {
                                foreach (CoordinatePositionPair p in actions.LockedAction.Targets[0].Mods[0].CoordinatePositionPairs)
                                {
                                    coordsToTrigger.Add(p.CubeCoordinate);
                                }
                            }

                            if (animatorComponent.CurrentLockedAction.ProjectileFab)
                            {
                                //THIS METHOD SUCKS
                                Vector3 targetPos = m_CellGridSystem.CoordinateToWorldPosition(worldIndex, actions.LockedAction.Targets[0].TargetCoordinate);
                                float targetYoffset = 0;
                                if (animatorComponent.CurrentLockedAction.Targets[0] is ECSATarget_Unit)
                                {
                                    targetYoffset = 1.3f;
                                }
                                m_ActionEffectsSystem.LaunchProjectile(animatorComponent.CurrentLockedAction.ProjectileFab, actionEffectType, coordsToTrigger, animatorComponent.ProjectileSpawnOrigin, targetPos, targetYoffset);
                            }
                            else
                            {
                                m_ActionEffectsSystem.TriggerActionEffect(actions.LockedAction.Effects[0].EffectType, coordsToTrigger);
                            }
                            animatorComponent.AnimationEvents.EventTrigger = false;
                        }
                    }
                    
                    if (animatorComponent.LastHealth != 0)
                    {
                        //Debug.Log("TriggerHatchActionEffect");
                        m_ActionEffectsSystem.TriggerActionEffect(actions.LockedAction.Effects[0].EffectType, coordsToTrigger);
                    }
                    
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


            if (!animatorComponent.Animator)
            {
                Debug.Log("Animator reference not set!");
                return;
            }
            //check if action has the current step and set executeTrigger if true
            if (actions.LockedAction.Index != -3 && m_GameStateData.GameState[0].CurrentState != GameStateEnum.planning)
            {
                //Debug.Log("Execute");
                //set initial anim Values if in the right step and they are not set yet
                if (!animatorComponent.ExecuteTriggerSet)
                {
                    ExecuteActionAnimation(actions, animatorComponent, m_GameStateData.GameState[0].CurrentState, worldIndex);
                }
                else
                {
                    //constantly rotate towards serverposition if moving
                    if (m_GameStateData.GameState[0].CurrentState == GameStateEnum.move)
                    {
                        if (animatorComponent.RotationTarget != serverPosition.Coords.ToUnityVector())
                            animatorComponent.RotationTarget = serverPosition.Coords.ToUnityVector();
                    }
                    //rotate animatorComponent.RotateTransform towards targetDirection
                    Vector3 targetDirection = RotateTowardsDirection(animatorComponent.RotateTransform, animatorComponent.RotationTarget, animatorComponent.RotationSpeed);
                    animatorComponent.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
            else
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

            //damage feedback

            //FeedBack to incoming Attacks / Heals
            if (animatorComponent.ActionEffectTrigger)
            {
                if (animatorComponent.LastHealth != healthComponent.CurrentHealth)
                {
                    if (healthComponent.CurrentHealth == 0)
                    {
                        Death(animatorComponent);
                    }
                    else if (animatorComponent.LastHealth > healthComponent.CurrentHealth)
                    {
                        m_UISystem.SetHealthFloatText(unitId, animatorComponent.LastHealth - healthComponent.CurrentHealth);
                        animatorComponent.Animator.SetTrigger("GetHit");
                    }
                    else
                    {
                        //healing feedback
                    }
                    animatorComponent.LastHealth = healthComponent.CurrentHealth;
                }

                animatorComponent.ActionEffectTrigger = false;
            }

            animatorComponent.Animator.SetBool("Harvesting", energy.Harvesting);

            if (animatorComponent.Animator.GetInteger("ActionIndexInt") != actions.LockedAction.Index)
                animatorComponent.Animator.SetInteger("ActionIndexInt", actions.LockedAction.Index);

            if (animatorComponent.Animator.GetFloat("ActionIndex") != actions.LockedAction.Index)
                animatorComponent.Animator.SetFloat("ActionIndex", actions.LockedAction.Index);
            
            if (transform.position != serverPosition.Coords.ToUnityVector())
            {
                float step;
                if (actions.LockedAction.Index != -3)
                    step = (Time.deltaTime / actions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell) * 1.732f;
                else
                    step = Time.deltaTime * 1.732f;
                //move
                transform.position = Vector3.MoveTowards(transform.position, serverPosition.Coords.ToUnityVector(), step);
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

    public void ExecuteActionAnimation(Actions.Component actions, AnimatorComponent animatorComponent, GameStateEnum gameState, uint worldIndex)
    {
        if ((int)actions.LockedAction.ActionExecuteStep == (int)gameState - 2)
        {
            if (!animatorComponent.InitialValuesSet)
            {
                //Debug.Log("SetInitialValues");
                if (gameState != GameStateEnum.move)
                    animatorComponent.RotationTarget = m_CellGridSystem.CoordinateToWorldPosition(worldIndex, actions.LockedAction.Targets[0].TargetCoordinate);
                animatorComponent.DestinationPosition = m_CellGridSystem.CoordinateToWorldPosition(worldIndex, actions.LockedAction.Targets[0].TargetCoordinate);
                animatorComponent.InitialValuesSet = true;
            }
            animatorComponent.Animator.SetTrigger("Execute");
            animatorComponent.ExecuteTriggerSet = true;
        }
    }

    public void Death(AnimatorComponent animatorComponent)
    {
        var garbageCollector = GarbageCollectionGroup.GetComponentArray<GarbageCollectorComponent>()[0];

        //Debug.Log("Death");

        animatorComponent.Dead = true;

        if (animatorComponent.DeathParticleSystem)
        {
            ParticleSystem ps = animatorComponent.DeathParticleSystem;
            ps.Emit(animatorComponent.DeathParticlesCount);
        }

        foreach(GameObject go in animatorComponent.ObjectsToDisable)
        {
            go.SetActive(false);
        }

        foreach (Transform t in animatorComponent.Props)
        {
            t.parent = animatorComponent.Animator.transform;
        }

        animatorComponent.Animator.transform.parent = GarbageCollection.transform;
        animatorComponent.Animator.enabled = false;
        garbageCollector.GarbageObjects.Add(animatorComponent.Animator.gameObject);

        foreach (Rigidbody r in animatorComponent.RagdollRigidBodies)
        {
            garbageCollector.GarbageRigidbodies.Add(r);
            r.isKinematic = false;
        }

        if(animatorComponent.DeathExplosionPos)
        {
            foreach (Rigidbody r in animatorComponent.RagdollRigidBodies)
            {
                r.AddExplosionForce(animatorComponent.DeathExplosionForce, animatorComponent.DeathExplosionPos.position, animatorComponent.DeathExplosionRadius);
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
