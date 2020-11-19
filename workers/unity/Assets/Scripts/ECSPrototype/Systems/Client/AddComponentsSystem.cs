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

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class AddComponentsSystem : ComponentSystem
{
    public struct WorldIndexStateData : ISystemStateComponentData
    {
        public WorldIndex.Component WorldIndexState;
    }

    EntityQuery m_PlayerStateData;
    EntityQuery m_PlayerAddedData;
    EntityQuery m_ProjectileAddedData;
    EntityQuery m_UnitAddedData;
    EntityQuery m_CellAddedData;
    EntityManager em;

    UIReferences m_UIReferences;

    Settings settings;

    protected override void OnStartRunning()
    {
        m_UIReferences = Object.FindObjectOfType<UIReferences>();
        base.OnStartRunning();
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        em = World.EntityManager;
        settings = Resources.Load<Settings>("Settings");

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
                ComponentType.ReadOnly<Health.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<UnitEffects>(),
                ComponentType.ReadOnly<AnimatorComponent>()

            }
        };

        m_UnitAddedData = GetEntityQuery(unitAddedDesc);

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

        m_PlayerStateData = GetEntityQuery(
            ComponentType.ReadOnly<WorldIndex.Component>(),
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>()
            );

    }

    protected override void OnUpdate()
    {
        if (m_PlayerStateData.CalculateEntityCount() == 0)
            return;

        var authPlayerWorldIndexes = m_PlayerStateData.ToComponentDataArray<WorldIndex.Component>(Allocator.TempJob);
        var playerFactions = m_PlayerStateData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

        var playerFaction = playerFactions[0];
        var authPlayerWorldIndex = authPlayerWorldIndexes[0].Value;

        Entities.With(m_PlayerAddedData).ForEach((Entity entity, HeroTransform htrans, ref WorldIndex.Component pWorldIndex) =>
        {
            HighlightingDataComponent highlightingData = new HighlightingDataComponent
            {
                ShowIngameUI = true

            };

            var cam = EntityManager.GetComponentObject<Moba_Camera>(entity);

            cam.PlayerMapTileInstance = Object.Instantiate(cam.PlayerMapTilePrefab, Vector3.zero, Quaternion.identity, m_UIReferences.MinimapComponent.MiniMapPlayerTilePanel.transform);
            PostUpdateCommands.AddComponent(entity, highlightingData);
            m_UIReferences.MinimapComponent.h_Transform = htrans;

            PostUpdateCommands.AddComponent(entity, new WorldIndexStateData { WorldIndexState = pWorldIndex });
        });

        Entities.With(m_CellAddedData).ForEach((Entity entity, ref WorldIndex.Component cellWorldIndex, ref CellAttributesComponent.Component cellAtt) =>
        {
            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);
            int colorIndex = cellAtt.CellAttributes.CellMapColorIndex;
           
            if (cellAtt.CellAttributes.CellMapColorIndex != 5)
            {
                PopulateMap(m_UIReferences.MinimapComponent.MapSize, m_UIReferences.MinimapComponent.MiniMapCellTilesPanel.transform, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex]);
                PopulateMap(m_UIReferences.BigMapComponent.MapSize, m_UIReferences.BigMapComponent.MiniMapCellTilesPanel.transform, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex]);

                IsVisible isVisible = new IsVisible
                {
                    Value = 0,
                    RequireUpdate = 1,
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


                PostUpdateCommands.AddComponent(entity, mouseVars);
                PostUpdateCommands.AddComponent(entity, mouseState);
                PostUpdateCommands.AddComponent(entity, markerState);
                PostUpdateCommands.AddComponent(entity, isVisible);
                PostUpdateCommands.AddComponent(entity, requireMarkerUpdate);
            }
            else
            {
                PopulateMap(m_UIReferences.MinimapComponent.MapSize, m_UIReferences.MinimapComponent.MiniMapCellTilesPanel.transform, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex], true);
                PopulateMap(m_UIReferences.BigMapComponent.MapSize, m_UIReferences.BigMapComponent.MiniMapCellTilesPanel.transform, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex], true);
            }


            /*
            //PostUpdateCommands.AddComponent(entity, new NonUniformScale());
            //PostUpdateCommands.AddComponent(entity, new Translation());
            //PostUpdateCommands.AddComponent(entity, new LocalToWorld());
            //PostUpdateCommands.AddComponent(entity, new WorldRenderBounds());
            //PostUpdateCommands.AddComponent(entity, new ChunkWorldRenderBounds());

            PostUpdateCommands.AddSharedComponent(entity, new RenderMesh
            {
                mesh = settings.TestMesh,
                material = settings.TestMat

            });
            */

            PostUpdateCommands.AddComponent(entity, new WorldIndexStateData { WorldIndexState = cellWorldIndex });

        });

        Entities.With(m_UnitAddedData).ForEach((Entity entity, AnimatorComponent anim, ref WorldIndex.Component unitWorldIndex, ref FactionComponent.Component faction, ref CubeCoordinate.Component coord) =>
        {

            var unitEffects = EntityManager.GetComponentObject<UnitEffects>(entity);
            unitEffects.LastStationaryCoordinate = coord.CubeCoordinate;
            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);

            PopulateMap(m_UIReferences.MinimapComponent.MapSize, m_UIReferences.MinimapComponent.MiniMapUnitTilesPanel.transform, coord.CubeCoordinate, ref isVisibleRef, settings.FactionMapColors[(int)faction.Faction], true);
            PopulateMap(m_UIReferences.BigMapComponent.MapSize, m_UIReferences.BigMapComponent.MiniMapUnitTilesPanel.transform, coord.CubeCoordinate, ref isVisibleRef, settings.FactionMapColors[(int)faction.Faction], true);

            if (isVisibleRef.MiniMapTileInstance.UnitBecomeVisiblePingPS)
            {
                ParticleSystem.MainModule main = isVisibleRef.MiniMapTileInstance.UnitBecomeVisiblePingPS.main;
                ParticleSystem.SizeOverLifetimeModule size = isVisibleRef.MiniMapTileInstance.UnitBecomeVisiblePingPS.sizeOverLifetime;
                main.startColor = settings.FactionMapColors[(int)faction.Faction];
                size.sizeMultiplier = isVisibleRef.MiniMapTileInstance.SmallTileScale * 32;
            }
            if (isVisibleRef.BigMapTileInstance.UnitBecomeVisiblePingPS)
            {
                ParticleSystem.MainModule main = isVisibleRef.BigMapTileInstance.UnitBecomeVisiblePingPS.main;
                ParticleSystem.SizeOverLifetimeModule size = isVisibleRef.BigMapTileInstance.UnitBecomeVisiblePingPS.sizeOverLifetime;
                main.startColor = settings.FactionMapColors[(int)faction.Faction];
                size.sizeMultiplier = isVisibleRef.BigMapTileInstance.BigTileScale * 32;
            }
            //if (unitWorldIndex.Value == authPlayerWorldIndex)
            //{
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
                {
                    isVisible.Value = 1;
                    isVisible.RequireUpdate = 1;
                }
                else
                {
                    isVisible.Value = 0;
                    isVisible.RequireUpdate = 1;
                }

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

                PostUpdateCommands.AddComponent(entity, mouseVars);
                PostUpdateCommands.AddComponent(entity, markerState);
                PostUpdateCommands.AddComponent(entity, mouseState);
                PostUpdateCommands.AddComponent(entity, isVisible);
            //}

            PostUpdateCommands.AddComponent(entity, new WorldIndexStateData { WorldIndexState = unitWorldIndex });
        });

        /*
        for(int i = 0; i < m_ProjectileAddedData.Length; i++)
        {
            var projectileWorldIndex = m_ProjectileAddedData.WorldIndexData[i];
            var entity = m_ProjectileAddedData.Entities[i];

            if (projectileWorldIndex.Value == authPlayerWorldIndex)
            {

            }
        }
        */

        authPlayerWorldIndexes.Dispose();
        playerFactions.Dispose();
    }

    void PopulateMap(float scale, Transform parent, Vector3f coord, ref IsVisibleReferences isVisibleRef, Color tileColor, bool initColored = false)
    {
        float offsetMultiplier = scale;
        float tilescale = scale / 5.8f;
        //Instantiate MiniMapTile into Map
        Vector3 pos = CellGridMethods.CubeToPos(coord, new Vector2f(0f, 0f));
        Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);


        if (!isVisibleRef.MiniMapTileInstance)
        {
            isVisibleRef.MiniMapTileInstance = InstantiateMapTile(isVisibleRef, parent, tilescale, invertedPos, tileColor, initColored, true);
        }
        else if (!isVisibleRef.BigMapTileInstance)
        {
            isVisibleRef.BigMapTileInstance = InstantiateMapTile(isVisibleRef, parent, tilescale, invertedPos, tileColor, initColored, false);
        }
    }

    MiniMapTile InstantiateMapTile(IsVisibleReferences isVisibleRef, Transform parent, float tileScale, Vector2 invertedPos, Color tileColor, bool initColored, bool isSmallTile)
    {
        MiniMapTile instanciatedTile = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, parent);
        instanciatedTile.TileRect.sizeDelta = new Vector2((int)(instanciatedTile.TileRect.sizeDelta.x * tileScale), (int)(instanciatedTile.TileRect.sizeDelta.y * tileScale));
        //invertedPos = new Vector2((int)invertedPos.x, (int)invertedPos.y);
        instanciatedTile.TileRect.anchoredPosition = invertedPos;
        instanciatedTile.TileColor = tileColor;

        if(instanciatedTile.UnitPlayerColorSprite)
        {
            instanciatedTile.UnitPlayerColorSprite.offsetMin = new Vector2((int)(instanciatedTile.UnitPlayerColorSprite.offsetMin.x * tileScale), (int)(instanciatedTile.UnitPlayerColorSprite.offsetMin.y * tileScale));
            instanciatedTile.UnitPlayerColorSprite.offsetMax = new Vector2((int)(instanciatedTile.UnitPlayerColorSprite.offsetMax.x * tileScale),(int)(instanciatedTile.UnitPlayerColorSprite.offsetMax.y * tileScale));

        }

        if (isSmallTile)
        {
            instanciatedTile.SmallTileScale = tileScale;
        }
        else
        {
            instanciatedTile.BigTileScale = tileScale;
        }

        //init gray if not water
        if (!initColored)
            instanciatedTile.TileImage.color = isVisibleRef.MiniMapTilePrefab.TileInvisibleColor;
        //init blue if water
        else
            instanciatedTile.TileImage.color = instanciatedTile.TileColor;

        return instanciatedTile;
    }



}
