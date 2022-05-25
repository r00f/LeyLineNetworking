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
using Unity.Jobs;
using FMODUnity;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class UnitAnimationSystem : JobComponentSystem
{
    ILogDispatcher logger;
    ActionEffectsSystem m_ActionEffectsSystem;
    UISystem m_UISystem;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    EntityQuery m_TransformData;
    EntityQuery m_UnitData;
    EntityQuery m_CellData;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();


        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>()
        );

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<Vision.Component>(),
        ComponentType.ReadOnly<HeroTransform>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>(),
        ComponentType.ReadOnly<PlayerState.Component>(),
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
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_GameStateData.CalculateEntityCount() != 1 || m_PlayerData.CalculateEntityCount() == 0)
            return inputDeps;

        var playerEntity = m_PlayerData.GetSingletonEntity();
        var playerHeroTransform = EntityManager.GetComponentObject<HeroTransform>(playerEntity);

        var gameState = m_GameStateData.GetSingleton<GameState.Component>();
        var playerFaction = m_PlayerData.GetSingleton<FactionComponent.Component>();
        var playerState = m_PlayerData.GetSingleton<PlayerState.Component>();
        var playerVision = m_PlayerData.GetSingleton<Vision.Component>();
        var playerHigh = m_PlayerData.GetSingleton<HighlightingDataComponent>();

        var cleanUpStateEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.CleanupStateEvent.Event>();

        if (cleanUpStateEvents.Count > 0)
        {
            Entities.ForEach((Entity e, UnitComponentReferences unitComponentReferences, in Actions.Component actions, in Energy.Component energy, in FactionComponent.Component faction, in SpatialEntityId id) =>
            {
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);

                if (unitComponentReferences.SelectionMeshRenderer.material.HasProperty("_UnlitColor"))
                    unitComponentReferences.SelectionMeshRenderer.material.SetColor("_UnlitColor", settings.FactionColors[(int)faction.Faction]);

                unitComponentReferences.CurrentMoveIndex = -1;
                unitComponentReferences.CurrentMoveTime = 0;
                unitComponentReferences.LastStationaryPosition = unitComponentReferences.transform.position;
                unitComponentReferences.AnimatorComp.DestinationPosition = Vector2.zero;
                unitComponentReferences.UnitEffectsComp.OriginCoordinate = coord.CubeCoordinate;
                unitComponentReferences.UnitEffectsComp.DestinationCoordinate = coord.CubeCoordinate;
                unitComponentReferences.AnimatorComp.DestinationReachTriggerSet = false;
                unitComponentReferences.AnimatorComp.InitialValuesSet = false;
                unitComponentReferences.AnimatorComp.ExecuteTriggerSet = false;
                unitComponentReferences.AnimatorComp.CurrentLockedAction = null;

                if (unitComponentReferences.AnimatorComp.AnimationEvents)
                    unitComponentReferences.AnimatorComp.AnimationEvents.EventTriggered = false;

                if (unitComponentReferences.AnimatorComp.Animator)
                {
                    unitComponentReferences.AnimatorComp.Animator.SetInteger("ActionIndexInt", -3);
                    unitComponentReferences.AnimatorComp.Animator.SetInteger("Armor", 0);
                    unitComponentReferences.AnimatorComp.Animator.ResetTrigger("Execute");
                    unitComponentReferences.AnimatorComp.Animator.SetBool("Harvesting", energy.Harvesting);
                    unitComponentReferences.AnimatorComp.Animator.SetBool("Executed", false);
                    unitComponentReferences.AnimatorComp.Animator.ResetTrigger("DestinationReached");
                }
            })
            .WithoutBurst()
            .Run();
        }

        Entities.ForEach((Entity e, UnitComponentReferences unitComponentReferences, in SpatialEntityId id, in Actions.Component actions, in Energy.Component energy, in CubeCoordinate.Component coord, in FactionComponent.Component faction, in IncomingActionEffects.Component incomingActionEffects) =>
        {
            var startRotation = EntityManager.GetComponentData<StartRotation.Component>(e);
            var visible = EntityManager.GetComponentData<IsVisible>(e);

            HandleEnableVisualsDelay(unitComponentReferences, startRotation, playerHeroTransform.Transform);
            HandleSoundTriggers(unitComponentReferences, gameState.CurrentState);
            HandleLockedAction(unitComponentReferences, playerVision, actions, faction, gameState, id, coord);
            HandleHarverstingVisuals(unitComponentReferences, visible, energy);
            SetAnimatorVariables(unitComponentReferences, gameState.CurrentState, energy, actions);
            SetHarvestingEmissiveColorMeshes(unitComponentReferences, energy);
            HandleAnimStateEffects(unitComponentReferences, visible);

            if (gameState.CurrentState == GameStateEnum.planning)
            {
                if (visible.Value == 1 && !playerVision.RevealVision)
                {
                    //SetHoveredOutlineColor(unitComponentReferences, coord.CubeCoordinate, playerHigh.HoveredCoordinate, playerState.CurrentState);
                }
                else
                    unitComponentReferences.SelectionCircleGO.SetActive(false);
            }
            else
            {
                if (incomingActionEffects.MoveEffects.Count != 0)
                {
                    if (incomingActionEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs.Count > unitComponentReferences.CurrentMoveIndex)
                    {
                        if (unitComponentReferences.CurrentMoveIndex >= 0)
                            unitComponentReferences.AnimatorComp.RotationTarget = Vector3fext.ToUnityVector(incomingActionEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[unitComponentReferences.CurrentMoveIndex].WorldPosition);

                        if (unitComponentReferences.CurrentMoveTime > 0)
                        {
                            unitComponentReferences.CurrentMoveTime -= Time.DeltaTime;
                            float normalizedTime = 1f - (unitComponentReferences.CurrentMoveTime / incomingActionEffects.MoveEffects[0].MoveAlongPathNested.TimePerCell);

                            if (unitComponentReferences.CurrentMoveIndex == 0)
                                unitComponentReferences.transform.position = Vector3.Lerp(unitComponentReferences.LastStationaryPosition, Vector3fext.ToUnityVector(incomingActionEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[0].WorldPosition), normalizedTime);
                            else
                                unitComponentReferences.transform.position = Vector3.Lerp(Vector3fext.ToUnityVector(incomingActionEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[unitComponentReferences.CurrentMoveIndex - 1].WorldPosition), Vector3fext.ToUnityVector(incomingActionEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[unitComponentReferences.CurrentMoveIndex].WorldPosition), normalizedTime);
                        }
                        else
                        {
                            unitComponentReferences.CurrentMoveIndex++;
                            unitComponentReferences.CurrentMoveTime = incomingActionEffects.MoveEffects[0].MoveAlongPathNested.TimePerCell;
                        }
                    }
                    else if (!unitComponentReferences.AnimatorComp.DestinationReachTriggerSet)
                    {
                        unitComponentReferences.AnimatorComp.Animator.SetTrigger("DestinationReached");
                        unitComponentReferences.AnimatorComp.DestinationReachTriggerSet = true;
                    }
                }
            }
        })
        .WithoutBurst()
        .Run();

        Entities.WithAll<Manalith.Component>().ForEach((Entity e, UnitComponentReferences unitComponentReferences, in IsVisible visible, in SpatialEntityId id, in Actions.Component actions, in CubeCoordinate.Component coord, in FactionComponent.Component faction) =>
        {
            if (gameState.CurrentState != GameStateEnum.planning)
            {
                HandleAnimStateEffects(unitComponentReferences, visible);
            }

            HandleSoundTriggers(unitComponentReferences, gameState.CurrentState);
            HandleLockedAction(unitComponentReferences, playerVision, actions, faction, gameState, id, coord);
            
        })
        .WithoutBurst()
        .Run();
        if (m_UISystem != null && !m_UISystem.UIRef.DollyPathCameraActive)
        {
            Entities.WithAll<HoveredState>().ForEach((Entity e, UnitComponentReferences unitComponentReferences) =>
        {

            SetHoveredOutlineColor(unitComponentReferences, true);
        })
        .WithoutBurst()
        .Run();

            Entities.WithNone<HoveredState>().ForEach((Entity e, UnitComponentReferences unitComponentReferences) =>
            {
                SetHoveredOutlineColor(unitComponentReferences, false);
            })
            .WithoutBurst()
            .Run();
        }
        return inputDeps;
    }

    public void HandleHarverstingVisuals(UnitComponentReferences unitComponentReferences, IsVisible visible, Energy.Component energy)
    {
        if (visible.Value == 1)
        {
            if (energy.Harvesting && !unitComponentReferences.AnimatorComp.IsMoving && unitComponentReferences.UnitEffectsComp.CurrentHealth > 0)
            {
                if (unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem)
                {
                    var harvestingEmission = unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem.emission;
                    harvestingEmission.enabled = true;
                }
            }
            else
            {
                if (unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem)
                {
                    var harvestingEmission = unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem.emission;
                    harvestingEmission.enabled = false;
                }
            }
        }
        else
        {
            foreach (StudioEventEmitter g in unitComponentReferences.AnimatorComp.CharacterEffects)
            {
                g.gameObject.SetActive(false);
            }

            if (unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem)
            {
                var harvestingEmission = unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem.emission;
                harvestingEmission.enabled = false;
            }
        }
    }

    public void HandleLockedAction(UnitComponentReferences unitComponentReferences, Vision.Component playerVision, Actions.Component actions, FactionComponent.Component faction, GameState.Component gameState, SpatialEntityId id, CubeCoordinate.Component coord)
    {
        if (actions.LockedAction.Index != -3)
        {
            //Debug.Log("LockedActionNotNull");
            if (!unitComponentReferences.AnimatorComp.CurrentLockedAction)
            {
                if (actions.LockedAction.Index >= 0)
                {
                    if (actions.LockedAction.Index < unitComponentReferences.BaseDataSetComp.Actions.Count)
                    {
                        unitComponentReferences.AnimatorComp.CurrentLockedAction = unitComponentReferences.BaseDataSetComp.Actions[actions.LockedAction.Index];
                    }
                    else
                    {
                        unitComponentReferences.AnimatorComp.CurrentLockedAction = unitComponentReferences.BaseDataSetComp.SpawnActions[actions.LockedAction.Index - unitComponentReferences.BaseDataSetComp.Actions.Count];
                    }
                }
            }
            else if (gameState.CurrentState != GameStateEnum.planning)
            {
                //Debug.Log("LockedActionNotNullExecute");
                if (gameState.CurrentState == GameStateEnum.interrupt)
                {
                    unitComponentReferences.AnimatorComp.RotationTarget = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, gameState.MapCenter); //&CoordinateToWorldPosition(worldIndex.Value, actions.LockedAction.Targets[0].TargetCoordinate);
                }

                if (unitComponentReferences.AnimatorComp.AnimationEvents)
                {
                    //Debug.Log("AnimationEventsNotNullExecute");
                    //event triggered from animation
                    if (unitComponentReferences.AnimatorComp.AnimationEvents.EventTrigger)
                    {
                        //Debug.Log("EventTrigger from unit animation system");

                        if (unitComponentReferences.AnimatorComp.CurrentLockedAction.ProjectileFab)
                        {
                            Debug.Log("Launch Projectile from unit animation system");

                            Vector3 targetPos = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, gameState.MapCenter);
                            float targetYoffset = 0;
                            if (unitComponentReferences.AnimatorComp.CurrentLockedAction.Targets[0] is ECSATarget_Unit)
                            {
                                targetYoffset = 1.3f;
                            }
                            m_ActionEffectsSystem.LaunchProjectile(faction.Faction, playerVision, unitComponentReferences.AnimatorComp.CurrentLockedAction.ProjectileFab, unitComponentReferences.AnimatorComp.ProjectileSpawnOrigin, targetPos, actions.LockedAction, id.EntityId.Id, coord.CubeCoordinate, targetYoffset);
                        }
                        else
                        {
                            //Debug.Log("TriggerActionEffect from unit animation system");
                            m_ActionEffectsSystem.TriggerActionEffect(faction.Faction, actions.LockedAction, id.EntityId.Id, unitComponentReferences.AnimatorComp.WeaponTransform, gameState);
                        }

                        unitComponentReferences.AnimatorComp.AnimationEvents.EventTriggered = true;
                        unitComponentReferences.AnimatorComp.AnimationEvents.EventTrigger = false;
                    }
                }

                if (!unitComponentReferences.AnimatorComp.ExecuteTriggerSet)
                {
                    ExecuteActionAnimation(unitComponentReferences, gameState, actions);
                }

                if(unitComponentReferences.AnimatorComp.RotateTransform)
                {
                    //rotate unitComponentReferences.AnimatorComp.RotateTransform towards targetDirection
                    Vector3 targetDirection = RotateTowardsDirection(unitComponentReferences.AnimatorComp.RotateTransform, unitComponentReferences.AnimatorComp.RotationTarget, unitComponentReferences.AnimatorComp.RotationSpeed);
                    unitComponentReferences.AnimatorComp.RotateTransform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
        }
        else if (unitComponentReferences.AnimatorComp.CurrentLockedAction)
        {
            unitComponentReferences.AnimatorComp.CurrentLockedAction = null;
        }
    }

    public void HandleEnableVisualsDelay(UnitComponentReferences unitComponentReferences, StartRotation.Component startRotation, Transform playerHeroTransform)
    {
        if (unitComponentReferences.AnimatorComp.Visuals.Count != 0)
        {
            if (unitComponentReferences.AnimatorComp.EnableVisualsDelay >= 0)
            {
                //initially rotate visuals AWAY from hero(so leech makes more sense)
                if (playerHeroTransform && startRotation.Value == 0)
                {
                    Vector3 dir = unitComponentReferences.AnimatorComp.RotateTransform.position - playerHeroTransform.position;
                    dir.y = 0;
                    unitComponentReferences.AnimatorComp.RotateTransform.rotation = Quaternion.LookRotation(dir);
                }

                //unitComponentReferences.AnimatorComp.RotateTransform.rotation
                foreach (GameObject g in unitComponentReferences.AnimatorComp.Visuals)
                {
                    if (g.activeSelf)
                        g.SetActive(false);
                }
                unitComponentReferences.AnimatorComp.EnableVisualsDelay -= Time.DeltaTime;
            }
            else if (!unitComponentReferences.AnimatorComp.Dead)
            {
                foreach (GameObject g in unitComponentReferences.AnimatorComp.Visuals)
                {
                    g.SetActive(true);
                }
            }
        }
    }

    public void HandleAnimStateEffects(UnitComponentReferences unitComponentReferences, IsVisible visible)
    {
        foreach (AnimStateEffectHandler a in unitComponentReferences.AnimatorComp.AnimStateEffectHandlers)
        {
            if (a.IsActiveState)
            {
                for (int i = 0; i < a.CurrentEffectOnTimestamps.Count; i++)
                {
                    if (a.CurrentEffectOnTimestamps[i].x <= 0)
                    {
                        if (visible.Value == 1)
                        {
                            unitComponentReferences.AnimatorComp.CharacterEffects[(int) a.CurrentEffectOnTimestamps[i].y].gameObject.SetActive(true);
                            if(unitComponentReferences.AnimatorComp.PlayActionSFX)
                                unitComponentReferences.AnimatorComp.CharacterEffects[(int) a.CurrentEffectOnTimestamps[i].y].Play();
                        }

                        a.CurrentEffectOnTimestamps.Remove(a.CurrentEffectOnTimestamps[i]);
                    }
                }

                for (int i = 0; i < a.CurrentEffectOffTimestamps.Count; i++)
                {
                    if (a.CurrentEffectOffTimestamps[i].x <= 0)
                    {
                        if (visible.Value == 1)
                            unitComponentReferences.AnimatorComp.CharacterEffects[(int) a.CurrentEffectOffTimestamps[i].y].gameObject.SetActive(false);
                        a.CurrentEffectOffTimestamps.Remove(a.CurrentEffectOffTimestamps[i]);
                    }
                }
            }
        }
    }

    public void HandleSoundTriggers(UnitComponentReferences unitComponentReferences, GameStateEnum currentGameState)
    {
        //if this character has a filled voice emitter field and a voice event is triggered
        if (unitComponentReferences.AnimatorComp.VoiceEmitter && unitComponentReferences.AnimatorComp.AnimationEvents.VoiceTrigger)
        {
            unitComponentReferences.AnimatorComp.VoiceEmitter.Play();
            unitComponentReferences.AnimatorComp.AnimationEvents.VoiceTrigger = false;
        }

        if (currentGameState != GameStateEnum.planning)
        {
            //if this character has a filled footStep emitter field and a footstep event is triggered
            if (unitComponentReferences.AnimatorComp.FootStempEmitter && unitComponentReferences.AnimatorComp.AnimationEvents.FootStepTrigger)
            {
                unitComponentReferences.AnimatorComp.FootStempEmitter.Play();
                unitComponentReferences.AnimatorComp.AnimationEvents.FootStepTrigger = false;
            }
        }
    }

    public void MoveUnit(UnitComponentReferences unitComponentReferences, IncomingActionEffects.Component incomingEffects)
    {
        if (incomingEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs.Count > unitComponentReferences.CurrentMoveIndex)
        {
            if(unitComponentReferences.CurrentMoveIndex >= 0)
                unitComponentReferences.AnimatorComp.RotationTarget = Vector3fext.ToUnityVector(incomingEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[unitComponentReferences.CurrentMoveIndex].WorldPosition);

            if (unitComponentReferences.CurrentMoveTime > 0)
            {
                /*
                logger.HandleLog(LogType.Warning,
                new LogEvent("Moving Unit")
                .WithField("NextPathIndex", unitComponentReferences.CurrentMoveIndex));
                */

                unitComponentReferences.CurrentMoveTime -= Time.DeltaTime;
                float normalizedTime = 1f - (unitComponentReferences.CurrentMoveTime / incomingEffects.MoveEffects[0].MoveAlongPathNested.TimePerCell);

                if(unitComponentReferences.CurrentMoveIndex == 0)
                    unitComponentReferences.transform.position = Vector3.Lerp(unitComponentReferences.LastStationaryPosition, Vector3fext.ToUnityVector(incomingEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[0].WorldPosition), normalizedTime);
                else
                    unitComponentReferences.transform.position = Vector3.Lerp(Vector3fext.ToUnityVector(incomingEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[unitComponentReferences.CurrentMoveIndex -1].WorldPosition), Vector3fext.ToUnityVector(incomingEffects.MoveEffects[0].MoveAlongPathNested.CoordinatePositionPairs[unitComponentReferences.CurrentMoveIndex].WorldPosition), normalizedTime);
            }
            else
            {
                unitComponentReferences.CurrentMoveIndex++;
                unitComponentReferences.CurrentMoveTime = incomingEffects.MoveEffects[0].MoveAlongPathNested.TimePerCell;
            }
        }
        else if (!unitComponentReferences.AnimatorComp.DestinationReachTriggerSet)
        {
            unitComponentReferences.AnimatorComp.Animator.SetTrigger("DestinationReached");
            unitComponentReferences.AnimatorComp.DestinationReachTriggerSet = true;
        }
    }

    public void MoveObjectAlongPath(MeshMaterialComponent objectToMove, Vector3[] pathPositions, float timePerCell, float rotationSpeed, bool loop = false)
    {
        if (objectToMove.CurrentMoveTime > 0)
        {
            objectToMove.CurrentMoveTime -= Time.DeltaTime;
            float normalizedTime = 1f - (objectToMove.CurrentMoveTime / timePerCell);

            Vector3 targetDirection = RotateTowardsDirection(objectToMove.transform, pathPositions[objectToMove.CurrentMoveIndex], rotationSpeed);
            objectToMove.transform.rotation = Quaternion.LookRotation(targetDirection);

            objectToMove.transform.position = Vector3.Lerp(pathPositions[objectToMove.CurrentMoveIndex - 1], pathPositions[objectToMove.CurrentMoveIndex], normalizedTime);
        }
        else if(objectToMove.CurrentMoveIndex < pathPositions.Length - 1)
        {
            objectToMove.CurrentMoveIndex++;
            objectToMove.CurrentMoveTime = timePerCell;
        }
        else
        {
            if (loop)
            {
                objectToMove.transform.position = pathPositions[0];
                objectToMove.CurrentMoveIndex = 0;
            }
            else
            {
                objectToMove.Animator.SetTrigger("DestinationReached");
            }
        }
    }

    public void SetAnimatorVariables(UnitComponentReferences unitComponentReferences, GameStateEnum currentGameState, Energy.Component energy, Actions.Component actions)
    {
        if (currentGameState == GameStateEnum.planning)
        {
            unitComponentReferences.AnimatorComp.Animator.SetBool("Planning", true);
        }
        else
        {
            if (unitComponentReferences.AnimatorComp.Animator.GetInteger("ActionIndexInt") != actions.LockedAction.Index)
                unitComponentReferences.AnimatorComp.Animator.SetInteger("ActionIndexInt", actions.LockedAction.Index);

            foreach (GameObject g in unitComponentReferences.SelectionGameObjects)
                g.layer = 11;

            unitComponentReferences.SelectionCircleGO.SetActive(false);
            unitComponentReferences.AnimatorComp.Animator.SetBool("Planning", false);
        }

        if (unitComponentReferences.AnimatorComp.CurrentLockedAction)
            unitComponentReferences.AnimatorComp.Animator.SetBool("HasWindup", unitComponentReferences.AnimatorComp.CurrentLockedAction.HasWindup);
        else
            unitComponentReferences.AnimatorComp.Animator.SetBool("HasWindup", false);

        unitComponentReferences.AnimatorComp.Animator.SetBool("Harvesting", energy.Harvesting);

    }

    public void SetHarvestingEmissiveColorMeshes(UnitComponentReferences unitComponentReferences, Energy.Component energy)
    {
        if (energy.Harvesting)
        {
            unitComponentReferences.TeamColorMeshesComp.EmissionLerpColor = Color.Lerp(unitComponentReferences.TeamColorMeshesComp.EmissionLerpColor, unitComponentReferences.TeamColorMeshesComp.color * unitComponentReferences.TeamColorMeshesComp.EmissionIntensity, Time.DeltaTime * unitComponentReferences.TeamColorMeshesComp.EmissionLerpTime);
        }
        else
        {
            unitComponentReferences.TeamColorMeshesComp.EmissionLerpColor = Color.Lerp(unitComponentReferences.TeamColorMeshesComp.EmissionLerpColor, Color.black, Time.DeltaTime * unitComponentReferences.TeamColorMeshesComp.EmissionLerpTime);
        }

        foreach (Material m in unitComponentReferences.TeamColorMeshesComp.HarvestingEmissionColorMaterials)
        {
            m.SetColor("_EmissiveColor", unitComponentReferences.TeamColorMeshesComp.EmissionLerpColor);
        }
    }

    public void SetHoveredOutlineColor(UnitComponentReferences unitComponentReferences, bool enableOutline)
    {
        if(enableOutline)
        {
            m_UISystem.UIRef.SelectionOutlineMaterial.SetColor("_OuterColor", unitComponentReferences.UnitEffectsComp.PlayerColor);

            foreach (GameObject g in unitComponentReferences.SelectionGameObjects)
            {
                g.layer = 21;
            }
        }
        else
        {
            foreach (GameObject g in unitComponentReferences.SelectionGameObjects)
            {
                g.layer = 11;
            }
        }
    }

    public void ExecuteActionAnimation(UnitComponentReferences unitComponentReferences, GameState.Component gameState, Actions.Component actions)
    {
        if ((int)actions.LockedAction.ActionExecuteStep == (int)gameState.CurrentState - 3)
        {
            if (!unitComponentReferences.AnimatorComp.InitialValuesSet)
            {
                if(actions.LockedAction.ActionExecuteStep == ExecuteStepEnum.move)
                {
                    unitComponentReferences.UnitEffectsComp.DestinationCoordinate = actions.LockedAction.Targets[0].TargetCoordinate;
                    var pos = CellGridMethods.CubeToPos(actions.LockedAction.Targets[0].TargetCoordinate, gameState.MapCenter);
                    unitComponentReferences.AnimatorComp.DestinationPosition = new Vector2(pos.x, pos.z);
                }
                unitComponentReferences.AnimatorComp.InitialValuesSet = true;
            }

            if(unitComponentReferences.AnimatorComp.Animator)
            {
                unitComponentReferences.AnimatorComp.Animator.SetTrigger("Execute");
                unitComponentReferences.AnimatorComp.Animator.SetBool("Executed", true);
            }

            unitComponentReferences.AnimatorComp.ExecuteTriggerSet = true;
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
