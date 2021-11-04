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

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class UnitAnimationSystem : JobComponentSystem
{
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



        /*
                m_TransformData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<Transform>()
        );

                m_CellData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadOnly<CellAttributesComponent.Component>()
        );


        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<IsVisible>(),
        //ComponentType.ReadOnly<Health.Component>(),
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<Energy.Component>(),
        ComponentType.ReadOnly<Position.Component>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<UnitDataSet>(),
        ComponentType.ReadOnly<UnitEffects>(),
        ComponentType.ReadWrite<unitComponentReferences.AnimatorComp>(),
        ComponentType.ReadWrite<Transform>(),
        ComponentType.ReadOnly<MovementVariables.Component>()
        );
        */

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
            Entities.ForEach((Entity e, UnitComponentReferences unitComponentReferences, ref SpatialEntityId id, ref Actions.Component actions, ref Energy.Component energy, in FactionComponent.Component faction) =>
            {
                var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);

                if (unitComponentReferences.SelectionMeshRenderer.material.HasProperty("_UnlitColor"))
                    unitComponentReferences.SelectionMeshRenderer.material.SetColor("_UnlitColor", settings.FactionColors[(int)faction.Faction]);

                unitComponentReferences.AnimatorComp.DestinationPosition = Vector2.zero;
                unitComponentReferences.UnitEffectsComp.OriginCoordinate = coord.CubeCoordinate;
                unitComponentReferences.AnimatorComp.DestinationReachTriggerSet = false;
                unitComponentReferences.AnimatorComp.InitialValuesSet = false;
                unitComponentReferences.AnimatorComp.ExecuteTriggerSet = false;

                if(unitComponentReferences.AnimatorComp.AnimationEvents)
                    unitComponentReferences.AnimatorComp.AnimationEvents.EventTriggered = false;

                if (unitComponentReferences.AnimatorComp.Animator)
                {
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

        Entities.ForEach((Entity e, UnitComponentReferences unitComponentReferences, ref SpatialEntityId id, ref Position.Component serverPosition, ref Actions.Component actions, ref Energy.Component energy, in FactionComponent.Component faction) =>
        {
            var moveVars = EntityManager.GetComponentData<MovementVariables.Component>(e);
            var coord = EntityManager.GetComponentData<CubeCoordinate.Component>(e);
            var visible = EntityManager.GetComponentData<IsVisible>(e);

            if (gameState.CurrentState != GameStateEnum.planning)
            {
                HandleAnimStateEffects(unitComponentReferences, visible);
            }

            HandleEnableVisualsDelay(unitComponentReferences, moveVars, playerHeroTransform.Transform);

            HandleSoundTriggers(unitComponentReferences, gameState.CurrentState);

            HandleLockedAction(unitComponentReferences, actions, faction, gameState, serverPosition, id, playerVision, coord);

            HandleHarverstingVisuals(unitComponentReferences, actions, visible, energy, id, coord, gameState.CurrentState, faction, playerFaction, playerState, playerHigh);

            if (unitComponentReferences.AnimatorComp.Animator)
            {
                if(gameState.CurrentState == GameStateEnum.planning)
                {
                    if (visible.Value == 1 && !playerVision.RevealVision)
                    {
                        SetHoveredOutlineColor(unitComponentReferences, coord.CubeCoordinate, playerHigh.HoveredCoordinate, playerState.CurrentState);
                    }
                    else
                        unitComponentReferences.SelectionCircleGO.SetActive(false);
                }

                SetAnimatorVariables(unitComponentReferences, gameState.CurrentState, energy, actions);

                SetHarvestingEmissiveColorMeshes(unitComponentReferences, energy);

                MoveUnit(unitComponentReferences, serverPosition, actions);
            }
            else if (gameState.CurrentState == GameStateEnum.planning)
            {
                SetHoveredOutlineColor(unitComponentReferences, coord.CubeCoordinate, playerHigh.HoveredCoordinate, playerState.CurrentState);
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }

    public void HandleHarverstingVisuals(UnitComponentReferences unitComponentReferences, Actions.Component actions, IsVisible visible, Energy.Component energy, SpatialEntityId id, CubeCoordinate.Component coord, GameStateEnum currentGameState, FactionComponent.Component unitFaction, FactionComponent.Component playerFaction, PlayerState.Component playerState, HighlightingDataComponent playerHigh)
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
            foreach (GameObject g in unitComponentReferences.AnimatorComp.CharacterEffects)
            {
                g.SetActive(false);
            }

            if (unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem)
            {
                var harvestingEmission = unitComponentReferences.UnitEffectsComp.HarvestingEnergyParticleSystem.emission;
                harvestingEmission.enabled = false;
            }
        }
    }

    public void HandleLockedAction(UnitComponentReferences unitComponentReferences, Actions.Component actions, FactionComponent.Component faction, GameState.Component gameState, Position.Component serverPosition, SpatialEntityId id, Vision.Component playerVision, CubeCoordinate.Component coord)
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
                else
                {
                    //constantly rotate towards serverposition if moving
                    if (gameState.CurrentState == GameStateEnum.move)
                    {
                        if (unitComponentReferences.AnimatorComp.RotationTarget != serverPosition.Coords.ToUnityVector())
                            unitComponentReferences.AnimatorComp.RotationTarget = serverPosition.Coords.ToUnityVector();
                    }
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

    public void HandleEnableVisualsDelay(UnitComponentReferences unitComponentReferences, MovementVariables.Component moveVars, Transform playerHeroTransform)
    {
        if (unitComponentReferences.AnimatorComp.Visuals.Count != 0)
        {
            if (unitComponentReferences.AnimatorComp.EnableVisualsDelay >= 0)
            {
                //initially rotate visuals AWAY from hero(so leech makes more sense)
                if (playerHeroTransform && moveVars.StartRotation == 0)
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
                            unitComponentReferences.AnimatorComp.CharacterEffects[(int) a.CurrentEffectOnTimestamps[i].y].SetActive(true);
                        a.CurrentEffectOnTimestamps.Remove(a.CurrentEffectOnTimestamps[i]);
                    }
                }

                for (int i = 0; i < a.CurrentEffectOffTimestamps.Count; i++)
                {
                    if (a.CurrentEffectOffTimestamps[i].x <= 0)
                    {
                        if (visible.Value == 1)
                            unitComponentReferences.AnimatorComp.CharacterEffects[(int) a.CurrentEffectOffTimestamps[i].y].SetActive(false);
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

    public void MoveUnit(UnitComponentReferences unitComponentReferences, Position.Component serverPosition, Actions.Component actions)
    {
        if (unitComponentReferences.AnimatorComp.transform.position != serverPosition.Coords.ToUnityVector())
        {
            float step;
            if (actions.LockedAction.Index != -3)
                step = (Time.DeltaTime / actions.LockedAction.Effects[0].MoveAlongPathNested.TimePerCell) * 1.732f;
            else
                step = Time.DeltaTime * 1.732f;

            unitComponentReferences.AnimatorComp.IsMoving = true;
            unitComponentReferences.AnimatorComp.transform.position = Vector3.MoveTowards(unitComponentReferences.AnimatorComp.transform.position, serverPosition.Coords.ToUnityVector(), step);
        }
        else
        {
            unitComponentReferences.AnimatorComp.IsMoving = false;
        }

        Vector2 posXZ = new Vector2(unitComponentReferences.AnimatorComp.transform.position.x, unitComponentReferences.AnimatorComp.transform.position.z);

        if (Vector2.Distance(posXZ, unitComponentReferences.AnimatorComp.DestinationPosition) <= 0.2f)
        {
            if (!unitComponentReferences.AnimatorComp.DestinationReachTriggerSet)
            {
                unitComponentReferences.AnimatorComp.Animator.SetTrigger("DestinationReached");
                unitComponentReferences.AnimatorComp.DestinationReachTriggerSet = true;
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

        if (unitComponentReferences.AnimatorComp.Animator.GetInteger("ActionIndexInt") != actions.LockedAction.Index)
            unitComponentReferences.AnimatorComp.Animator.SetInteger("ActionIndexInt", actions.LockedAction.Index);
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

    public void SetHoveredOutlineColor(UnitComponentReferences unitComponentReferences, Vector3f unitCoord, Vector3f playerHoveredCoord, PlayerStateEnum currentPlayerState)
    {
        if (Vector3fext.ToUnityVector(unitCoord) == Vector3fext.ToUnityVector(playerHoveredCoord) && currentPlayerState != PlayerStateEnum.waiting_for_target)
        {
            m_UISystem.UIRef.SelectionOutlineMaterial.SetColor("_OuterColor", unitComponentReferences.UnitEffectsComp.PlayerColor);

            foreach (GameObject g in unitComponentReferences.SelectionGameObjects)
                g.layer = 21;
        }
        else
        {
            foreach (GameObject g in unitComponentReferences.SelectionGameObjects)
                g.layer = 11;
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
