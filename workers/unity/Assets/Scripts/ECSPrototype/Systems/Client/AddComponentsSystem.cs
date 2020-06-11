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

            cam.PlayerMapTileInstance = Object.Instantiate(cam.PlayerMapTilePrefab, Vector3.zero, Quaternion.identity, m_UIReferences.MiniMapPlayerTilePanel.transform);
            PostUpdateCommands.AddComponent(entity, highlightingData);
            m_UIReferences.MinimapComponent.h_Transform = htrans;

            PostUpdateCommands.AddComponent(entity, new WorldIndexStateData { WorldIndexState = pWorldIndex });
        });

        Entities.With(m_CellAddedData).ForEach((Entity entity, ref WorldIndex.Component cellWorldIndex, ref CellAttributesComponent.Component cellAtt) =>
        {
            //Debug.Log(authPlayerWorldIndex);

            //if (cellWorldIndex.Value == authPlayerWorldIndex)
            //{
            //Debug.Log("AddCellComponents");
            //UnityEngine.Rendering.Hybrid

            /*
            RenderMesh meshTest = new RenderMesh
            {


            };
            */
            //only add components if this cell is not water

            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);
            int colorIndex = cellAtt.CellAttributes.CellMapColorIndex;
           
            if (cellAtt.CellAttributes.CellMapColorIndex != 5)
            {
                PopulateMap(m_UIReferences.MiniMapCellTilesPanel.transform, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex]);

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
                PopulateMap(m_UIReferences.MiniMapCellTilesPanel.transform, cellAtt.CellAttributes.Cell.CubeCoordinate, ref isVisibleRef, settings.MapCellColors[colorIndex], true);
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

            PopulateMap(m_UIReferences.MiniMapUnitTilesPanel.transform, coord.CubeCoordinate, ref isVisibleRef, settings.FactionMapColors[(int)faction.Faction], true);

            if(isVisibleRef.MiniMapTileInstance.UnitBecomeVisiblePingPS)
            {
                ParticleSystem.MainModule main = isVisibleRef.MiniMapTileInstance.UnitBecomeVisiblePingPS.main;
                main.startColor = settings.FactionMapColors[(int)faction.Faction];
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

    void PopulateMap(Transform parent, Vector3f coord, ref IsVisibleReferences isVisibleRef, Color tileColor, bool initColored = false)
    {
        float offsetMultiplier = 5.8f;
        //Instantiate MiniMapTile into Map
        Vector3 pos = CellGridMethods.CubeToPos(coord, new Vector2f(0f, 0f));
        Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);
        isVisibleRef.MiniMapTileInstance = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, parent);
        isVisibleRef.MiniMapTileInstance.TileRect.anchoredPosition = invertedPos;

        isVisibleRef.MiniMapTileInstance.TileColor = tileColor;
        

        //init gray if not water
        if (!initColored)
            isVisibleRef.MiniMapTileInstance.TileImage.color = isVisibleRef.MiniMapTilePrefab.TileInvisibleColor;
        //init blue if water
        else
            isVisibleRef.MiniMapTileInstance.TileImage.color = isVisibleRef.MiniMapTileInstance.TileColor;
    }


}
