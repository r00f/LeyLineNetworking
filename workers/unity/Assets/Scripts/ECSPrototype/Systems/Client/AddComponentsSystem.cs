using Cell;
using Generic;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Player;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;
using Improbable;

[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class AddComponentsSystem : JobComponentSystem
{
    public struct ComponentsAddedIdentifier : ISystemStateComponentData
    {
    }

    public struct MapPopulatedIdentifiier : IComponentData
    {
    }

    ILogDispatcher logger;
    EntityQuery m_UnitMapPopulatedData;
    EntityQuery m_PlayerStateData;
    EntityQuery m_PlayerAddedData;
    EntityQuery m_ProjectileAddedData;
    EntityQuery m_UnitAddedData;
    EntityQuery m_UnitRemovedData;
    EntityQuery m_CellAddedData;
    EntityQuery m_GameStateData;
    EntityQuery m_InitializedGameStateData;

    UIReferences UIRef;
    Settings settings;
    MapVisuals map;

    bool clientPositionSet;

    protected override void OnCreate()
    {
        base.OnCreate();
        settings = Resources.Load<Settings>("Settings");

        m_PlayerStateData = GetEntityQuery(
            ComponentType.ReadOnly<FactionComponent.Component>(),
            ComponentType.ReadOnly<PlayerState.HasAuthority>(),
            ComponentType.ReadOnly<SpatialEntityId>()
            );

        m_GameStateData = GetEntityQuery(
            ComponentType.ReadOnly<GameState.Component>(),
            ComponentType.ReadOnly<MapData.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.ReadOnly<SpatialEntityId>()
            );
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        map = Object.FindObjectOfType<MapVisuals>();
        UIRef = Object.FindObjectOfType<UIReferences>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PlayerStateData.CalculateEntityCount() != 1 || m_GameStateData.CalculateEntityCount() != 1)
            return inputDeps;

        var gameStatePosition = m_GameStateData.GetSingleton<Position.Component>();
        var playerFaction = m_PlayerStateData.GetSingleton<FactionComponent.Component>();
        var playerId = m_PlayerStateData.GetSingleton<SpatialEntityId>();

        if (map && !clientPositionSet)
        {
            map.transform.position = new Vector3((float) gameStatePosition.Coords.X, (float) gameStatePosition.Coords.Y, (float) gameStatePosition.Coords.Z);
            map.Terrain.Flush();
            clientPositionSet = true;
        }

        Entities.WithNone<ComponentsAddedIdentifier>().ForEach((Entity entity, in MapData.Component mapData) =>
        {
            foreach(MapCell c in mapData.CoordinateCellDictionary.Values)
            {
                Vector3 pos = CellGridMethods.CubeToPos(CellGridMethods.AxialToCube(c.AxialCoordinate), new Vector2f(0, 0));
                Vector2 invertedPos = new Vector2(pos.x * UIRef.MinimapComponent.MapSize, pos.z * UIRef.MinimapComponent.MapSize);
                InstanciateMapCellTileFromSettings(UIRef.MinimapComponent, invertedPos, settings.MapCellColors[(int)c.MapCellColorIndex]);
            }

            var curMapState = new CurrentMapState { CoordinateCellDictionary = mapData.CoordinateCellDictionary };
            EntityManager.AddComponentObject(entity, curMapState);
            EntityManager.AddComponentData(entity, new ComponentsAddedIdentifier { });
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<ComponentsAddedIdentifier>().ForEach((Entity entity, HeroTransform htrans) =>
        {
            HighlightingDataComponent highlightingData = new HighlightingDataComponent
            {
                ShowIngameUI = true
            };

            var cam = EntityManager.GetComponentObject<Moba_Camera>(entity);

            cam.PlayerMapTileInstance = Object.Instantiate(cam.PlayerMapTilePrefab, Vector3.zero, Quaternion.identity, UIRef.MinimapComponent.MiniMapPlayerTilePanel.transform);
            EntityManager.AddComponentData(entity, highlightingData);
            UIRef.MinimapComponent.h_Transform = htrans;

            EntityManager.AddComponentData(entity, new ComponentsAddedIdentifier {});
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        Entities.WithNone<ComponentsAddedIdentifier>().ForEach((Entity entity, UnitComponentReferences unitComponentReferences, in FactionComponent.Component faction, in CubeCoordinate.Component coord, in SpatialEntityId id) =>
        {
            var isVisibleRef = EntityManager.GetComponentObject<IsVisibleReferences>(entity);

            if (EntityManager.HasComponent<ManalithInitializer>(entity))
            {
                var manalith = EntityManager.GetComponentData<Manalith.Component>(entity);
                var manalithObject = EntityManager.GetComponentObject<ManalithObject>(entity);
                var manalithInit = EntityManager.GetComponentObject<ManalithInitializer>(entity);

                var em = manalithObject.ChargedPS.emission;
                em.rateOverTime = manalith.Bounty;

                for (int i = 0; i < manalithInit.leyLinePathRenderer.positionCount; i++)
                {
                    manalithInit.leyLinePathRenderer.SetPosition(i, manalithInit.leyLinePathRenderer.GetPosition(i) + new Vector3((float) gameStatePosition.Coords.X, (float) gameStatePosition.Coords.Y, (float) gameStatePosition.Coords.Z));
                }
            }

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

            EntityManager.AddComponents(entity, new ComponentTypes(typeof(MouseVariables), typeof(MouseState), typeof(MarkerState), typeof(IsVisible), typeof(ComponentsAddedIdentifier)));

            EntityManager.SetComponentData(entity, mouseVars);
            EntityManager.SetComponentData(entity, markerState);
            EntityManager.SetComponentData(entity, mouseState);
            EntityManager.SetComponentData(entity, isVisible);
        })
        .WithStructuralChanges()
        .WithoutBurst()
        .Run();

        if(UIRef)
        {
            Entities.WithNone<MapPopulatedIdentifiier>().ForEach((Entity entity, UnitComponentReferences unitCompRef, ref CubeCoordinate.Component coord, ref IsVisible isVisible, in FactionComponent.Component faction) =>
            {
                unitCompRef.UnitEffectsComp.OriginCoordinate = coord.CubeCoordinate;
                unitCompRef.UnitEffectsComp.DestinationCoordinate = coord.CubeCoordinate;

                if (unitCompRef.AnimatorComp.EnableVisualsDelay <= 0)
                {
                    PopulateMap(UIRef.MinimapComponent, isVisible.Value, coord.CubeCoordinate, ref unitCompRef.IsVisibleRefComp, settings.FactionMapColors[(int) faction.Faction], true);
                    PopulateMap(UIRef.BigMapComponent, isVisible.Value, coord.CubeCoordinate, ref unitCompRef.IsVisibleRefComp, settings.FactionMapColors[(int) faction.Faction], true);
                    EntityManager.AddComponentData(entity, new MapPopulatedIdentifiier { });
                }
            })
            .WithStructuralChanges()
            .WithoutBurst()
            .Run();
        }
        return inputDeps;
    }

    void PopulateMap(MinimapScript miniMap, byte isVisible, Vector3f coord, ref IsVisibleReferences isVisibleRef, Color tileColor, bool isUnitTile = false)
    {
        Vector3 pos = CellGridMethods.CubeToPos(coord, new Vector2f(0, 0));
        Vector2 invertedPos = new Vector2(pos.x * miniMap.MapSize, pos.z * miniMap.MapSize);

        if (!isVisibleRef.MiniMapTileInstance)
        {
            isVisibleRef.MiniMapTileInstance = InstantiateMapTile(miniMap, isVisible, isVisibleRef, invertedPos, tileColor, isUnitTile);
        }
        else if (!isVisibleRef.BigMapTileInstance)
        {
            isVisibleRef.BigMapTileInstance = InstantiateMapTile(miniMap, isVisible, isVisibleRef, invertedPos, tileColor, isUnitTile);
        }
    }

    void InstanciateMapCellTileFromSettings(MinimapScript miniMap, Vector2 invertedPos, Color tileColor)
    {
        MiniMapTile instanciatedTile = Object.Instantiate(settings.MapCellTile, Vector3.zero, Quaternion.identity);
        instanciatedTile.TileColor = tileColor;
        instanciatedTile.TileImage.color = instanciatedTile.TileColor;
        instanciatedTile.transform.SetParent(miniMap.MiniMapCellTilesPanel.transform, false);
        instanciatedTile.TileRect.anchoredPosition = invertedPos;
        instanciatedTile.TileRect.sizeDelta = miniMap.MapCellPixelSize;
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
            instanciatedTile.transform.SetParent(miniMap.MiniMapCellTilesPanel.transform, false);
            instanciatedTile.TileRect.anchoredPosition = invertedPos;
            instanciatedTile.TileRect.sizeDelta = miniMap.MapCellPixelSize;
        }
        else
        {
            instanciatedTile.transform.SetParent(miniMap.MiniMapUnitTilesPanel.transform, false);
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
        {
            /*
            if(isUnitTile)
            {
                logger.HandleLog(LogType.Warning,
                new LogEvent("Set unit tile active")
                .WithField("Index", isVisibleRef.transform.name));
            }
            */
            instanciatedTile.gameObject.SetActive(true);
        }
        else
        {
            /*
            if(isUnitTile)
            {
                logger.HandleLog(LogType.Warning,
                new LogEvent("Set unit tile not active")
                .WithField("Index", isVisibleRef.transform.name));
            }
            */
            instanciatedTile.gameObject.SetActive(false);
        }


        return instanciatedTile;
    }

}
