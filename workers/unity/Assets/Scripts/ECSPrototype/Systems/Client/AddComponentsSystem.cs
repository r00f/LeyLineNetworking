using Cell;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unit;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;


[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class AddComponentsSystem : JobComponentSystem
{
    public struct WorldIndexStateData : ISystemStateComponentData
    {
        public WorldIndex.Component WorldIndexState;
    }

    public struct MapPopulatedIdentifyier : IComponentData
    {

    }

    EntityQuery m_UnitMapPopulatedData;
    EntityQuery m_PlayerStateData;
    EntityQuery m_PlayerAddedData;
    EntityQuery m_ProjectileAddedData;
    EntityQuery m_UnitAddedData;
    EntityQuery m_UnitRemovedData;
    EntityQuery m_CellAddedData;

    UIReferences UIRef;
    Settings settings;

    protected override void OnStartRunning()
    {
        UIRef = Object.FindObjectOfType<UIReferences>();
        base.OnStartRunning();
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        settings = Resources.Load<Settings>("Settings");

        /*
        var projectileAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(WorldIndexStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<Projectile>()
            }
        };

        m_ProjectileAddedData = GetEntityQuery(projectileAddedDesc);

        var unitAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(WorldIndexStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<UnitEffects>(),
                ComponentType.ReadOnly<AnimatorComponent>()
            }
        };

        m_UnitAddedData = GetEntityQuery(unitAddedDesc);

        var unitMapPopulatedDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
        {
        typeof(MapPopulatedIdentifyier)
        },
            All = new ComponentType[]
        {
                typeof(WorldIndexStateData),
                ComponentType.ReadOnly<FactionComponent.Component>(),
                ComponentType.ReadOnly<IsVisible>(),
                ComponentType.ReadOnly<CubeCoordinate.Component>(),
                ComponentType.ReadOnly<AnimatorComponent>()
        }
        };

        m_UnitMapPopulatedData = GetEntityQuery(unitMapPopulatedDesc);

        var cellAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[]
            {
                typeof(WorldIndexStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<CellAttributesComponent.Component>()
            }
        };

        m_CellAddedData = GetEntityQuery(cellAddedDesc);

        var playerAddedDesc = new EntityQueryDesc
        {
            None = new ComponentType[] 
            {
                typeof(WorldIndexStateData)
            },
            All = new ComponentType[]
            {
                ComponentType.ReadOnly<WorldIndex.Component>(),
                ComponentType.ReadOnly<PlayerState.Component>(),
                ComponentType.ReadOnly<HeroTransform>(),
                ComponentType.ReadWrite<Moba_Camera>()
            }
        };

        m_PlayerAddedData = GetEntityQuery(playerAddedDesc);
        */

        m_PlayerStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>()
            );
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PlayerStateData.CalculateEntityCount() == 0)
            return inputDeps;

        var playerFaction = m_PlayerStateData.GetSingleton<FactionComponent.Component>();
        var authPlayerWorldIndex = m_PlayerStateData.GetSingleton<WorldIndex.Component>();

        Entities.WithNone<WorldIndexStateData>().ForEach((Entity entity, HeroTransform htrans, ref WorldIndex.Component pWorldIndex) =>
        {
            HighlightingDataComponent highlightingData = new HighlightingDataComponent
            {
                ShowIngameUI = true
            };

            var cam = EntityManager.GetComponentObject<Moba_Camera>(entity);

            cam.PlayerMapTileInstance = Object.Instantiate(cam.PlayerMapTilePrefab, Vector3.zero, Quaternion.identity, UIRef.MinimapComponent.MiniMapPlayerTilePanel.transform);
            EntityManager.AddComponentData(entity, highlightingData);
            UIRef.MinimapComponent.h_Transform = htrans;

            EntityManager.AddComponentData(entity, new WorldIndexStateData { WorldIndexState = pWorldIndex });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<WorldIndexStateData>().ForEach((Entity entity, ref WorldIndex.Component cellWorldIndex, ref CellAttributesComponent.Component cellAtt) =>
        {
            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);
            int colorIndex = cellAtt.CellAttributes.CellMapColorIndex;

            PopulateMap(UIRef.MinimapComponent, 1, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex]);
            PopulateMap(UIRef.BigMapComponent, 1, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex]);

            //if this cell is not water, Add all components
            if (cellAtt.CellAttributes.CellMapColorIndex != 5)
            {
                IsVisible isVisible = new IsVisible
                {
                    Value = 0,
                    LerpSpeed = 0.5f,
                };

                MouseState mouseState = new MouseState
                {
                    CurrentState = MouseState.State.Neutral,
                };

                MouseVariables mouseVars = new MouseVariables
                {
                    Distance = 0.865f
                };

                MarkerState markerState = new MarkerState
                {
                    CurrentTargetType = MarkerState.TargetType.Neutral,
                    IsSet = 0,
                    TargetTypeSet = 0,
                    CurrentState = MarkerState.State.Neutral,
                    IsUnit = 0
                };

                RequireMarkerUpdate requireMarkerUpdate = new RequireMarkerUpdate();


                EntityManager.AddComponentData(entity, mouseVars);
                EntityManager.AddComponentData(entity, mouseState);
                EntityManager.AddComponentData(entity, markerState);
                EntityManager.AddComponentData(entity, isVisible);
                EntityManager.AddComponentData(entity, requireMarkerUpdate);
            }
            //if it is water, only add visibility component to exclude water from highlighting / mouse behaviour
            else
            {

                IsVisible isVisible = new IsVisible
                {
                    Value = 0,
                    LerpSpeed = 0.5f,
                };

                RequireMarkerUpdate requireMarkerUpdate = new RequireMarkerUpdate();

                EntityManager.AddComponentData(entity, isVisible);
                EntityManager.AddComponentData(entity, requireMarkerUpdate);

            }

            EntityManager.AddComponentData(entity, new WorldIndexStateData { WorldIndexState = cellWorldIndex });

        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<WorldIndexStateData>().ForEach((Entity entity, UnitComponentReferences unitComponentReferences, ref WorldIndex.Component unitWorldIndex, ref FactionComponent.Component faction) =>
        {
            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);

            foreach (Renderer r in unitComponentReferences.TeamColorMeshesComp.HarvestingEmissionColorMeshes)
            {
                //instantiate material instances on initialization to prevent GC alloc in Update later on
                unitComponentReferences.TeamColorMeshesComp.HarvestingEmissionColorMaterials.Add(r.materials[r.materials.Length - 1]);
            }

            MouseState mouseState = new MouseState
            {
                CurrentState = MouseState.State.Neutral,
                ClickEvent = 0
            };

            MouseVariables mouseVars = new MouseVariables
            {
                yOffset = 1f,
                Distance = 1.2f
            };

            IsVisible isVisible = new IsVisible();

            if (faction.Faction == playerFaction.Faction)
                isVisible.Value = 1;
            else
                isVisible.Value = 0;

            if (isVisible.Value == 1)
            {
                foreach (GameObject g in isVisibleRef.GameObjects)
                    g.SetActive(true);
            }

            MarkerState markerState = new MarkerState
            {
                CurrentTargetType = MarkerState.TargetType.Neutral,
                IsSet = 0,
                TargetTypeSet = 0,
                CurrentState = MarkerState.State.Neutral,
                IsUnit = 1
            };


            EntityManager.AddComponentData(entity, mouseVars);
            EntityManager.AddComponentData(entity, markerState);
            EntityManager.AddComponentData(entity, mouseState);
            EntityManager.AddComponentData(entity, isVisible);
            EntityManager.AddComponentData(entity, new WorldIndexStateData { WorldIndexState = unitWorldIndex });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<MapPopulatedIdentifyier>().ForEach((Entity entity, AnimatorComponent anim, ref FactionComponent.Component faction, ref CubeCoordinate.Component coord, ref IsVisible isVisible) =>
        {
            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(entity);
            unitEffects.OriginCoordinate = coord.CubeCoordinate;
            unitEffects.DestinationCoordinate = coord.CubeCoordinate;
            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);

            if (anim.EnableVisualsDelay <= 0)
            {
                //Debug.Log(isVisible.Value);
                PopulateMap(UIRef.MinimapComponent, isVisible.Value, coord.CubeCoordinate, ref isVisibleRef, settings.FactionMapColors[(int)faction.Faction], true);
                PopulateMap(UIRef.BigMapComponent, isVisible.Value, coord.CubeCoordinate, ref isVisibleRef, settings.FactionMapColors[(int)faction.Faction], true);
                EntityManager.AddComponentData(entity, new MapPopulatedIdentifyier{ });
            }
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        return inputDeps;
    }

    void PopulateMap(MinimapScript miniMap, byte isVisible, Vector3f coord, ref IsVisibleReferences isVisibleRef, Color tileColor, bool isUnitTile = false)
    {
        float offsetMultiplier = miniMap.MapSize;
        //Instantiate MiniMapTile into Map
        Vector3 pos = CellGridMethods.CubeToPos(coord, new Vector2f(0f, 0f));
        Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);

        //Debug.Log("IsVisible: " + isVisible);

        if (!isVisibleRef.MiniMapTileInstance)
        {
            isVisibleRef.MiniMapTileInstance = InstantiateMapTile(miniMap, isVisible, isVisibleRef, invertedPos, tileColor, isUnitTile);
        }
        else if (!isVisibleRef.BigMapTileInstance)
        {
            isVisibleRef.BigMapTileInstance = InstantiateMapTile(miniMap, isVisible, isVisibleRef, invertedPos, tileColor, isUnitTile);
        }
    }

    MiniMapTile InstantiateMapTile(MinimapScript miniMap, byte isVisible, IsVisibleReferences isVisibleRef, Vector2 invertedPos, Color tileColor, bool isUnitTile)
    {
        if (!isVisibleRef.MiniMapTilePrefab)
            return null;

        MiniMapTile instanciatedTile = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity);
        instanciatedTile.TileColor = tileColor;
        instanciatedTile.TileImage.color = instanciatedTile.TileColor;
        instanciatedTile.EmitSoundEffect = miniMap.IsFullscreenMap;

        if (!isUnitTile)
        {
            instanciatedTile.transform.parent = miniMap.MiniMapCellTilesPanel.transform;
            instanciatedTile.TileRect.anchoredPosition = invertedPos;
            instanciatedTile.TileRect.sizeDelta = miniMap.MapCellPixelSize;
            instanciatedTile.DarknessTile.sizeDelta = miniMap.MapCellDarknessPixelSize;
            instanciatedTile.DarknessTile.transform.parent = miniMap.MiniMapDarknessTilesPanel.transform;
            instanciatedTile.DarknessTile.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            instanciatedTile.transform.parent = miniMap.MiniMapUnitTilesPanel.transform;
            instanciatedTile.TileRect.anchoredPosition = invertedPos;
            instanciatedTile.TileRect.sizeDelta = miniMap.MapUnitPixelSize;

            if(instanciatedTile.EvenOutlineOffset)
            {
                instanciatedTile.UnitPlayerColorSprite.offsetMin = new Vector2(miniMap.UnitColorOffsetMin.x, miniMap.UnitColorOffsetMin.x);
                instanciatedTile.UnitPlayerColorSprite.offsetMax = new Vector2(-miniMap.UnitColorOffsetMin.x, -miniMap.UnitColorOffsetMin.x);
            }
            else
            {
                instanciatedTile.UnitPlayerColorSprite.offsetMin = miniMap.UnitColorOffsetMin;
                instanciatedTile.UnitPlayerColorSprite.offsetMax = miniMap.UnitColorOffsetMax * -1;
            }

            instanciatedTile.DeathCrossSize = new Vector2(miniMap.DeathPingSize, miniMap.DeathPingSize);

            //Check if is visible and if minimap that is being populated is active
            if (instanciatedTile.BecomeVisibleMapEffect && isVisible == 1 && miniMap.isActiveAndEnabled)
            {
                var ping = Object.Instantiate(instanciatedTile.BecomeVisibleMapEffect, instanciatedTile.TileRect.position, Quaternion.identity, miniMap.MiniMapEffectsPanel.transform);
                ParticleSystem.MainModule main = ping.ParticleSystem.main;
                ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                main.startColor = tileColor;
                size.sizeMultiplier = miniMap.BecomeVisiblePingSize;
                ping.ParticleSystem.Play();

                if (instanciatedTile.EmitSoundEffect && instanciatedTile.isActiveAndEnabled)
                {
                    ping.FMODEmitter.Play();
                }

                Object.Destroy(ping.gameObject, 2f);
            }
        }

        instanciatedTile.TileRect.localScale = new Vector3(1, 1, 1);

        if (isVisible == 1)
            instanciatedTile.gameObject.SetActive(true);
        else
            instanciatedTile.gameObject.SetActive(false);

        return instanciatedTile;
    }

}
