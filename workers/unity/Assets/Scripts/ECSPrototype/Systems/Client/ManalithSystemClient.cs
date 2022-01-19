using Unity.Entities;
using Improbable.Gdk.Core;
using LeyLineHybridECS;
using Generic;
using Cell;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using FMODUnity;

//Update after playerState selected unit has been set
[DisableAutoCreation, UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class ManalithSystemClient : JobComponentSystem
{
    //HighlightingSystem m_HighlightingSystem;
    //EntityQuery m_PlayerData;
    EntityQuery m_GameStateData;
    //EntityQuery m_CellData;
    EntityQuery m_ManalithData;
    //CommandSystem m_CommandSystem;
    UISystem m_UISystem;
    //EventSystem eventSystem;
    ILogDispatcher logger;
    ComponentUpdateSystem m_ComponentUpdateSystem;
    Settings settings;

    protected override void OnCreate()
    {
        base.OnCreate();

        //eventSystem = Object.FindObjectOfType<EventSystem>();
        settings = Resources.Load<Settings>("Settings");

        /*
        m_UnitData = GetEntityQuery(
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<MouseState>(),
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>()
        );

        m_PlayerData = GetEntityQuery(
        ComponentType.ReadOnly<PlayerEnergy.Component>(),
        ComponentType.ReadOnly<PlayerState.HasAuthority>(),
        ComponentType.ReadWrite<PlayerState.Component>(),
        ComponentType.ReadWrite<PlayerPathing.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadWrite<HighlightingDataComponent>()
        );

        m_RightClickedUnitData = GetEntityQuery(
        ComponentType.ReadOnly<Actions.Component>(),
        ComponentType.ReadOnly<FactionComponent.Component>(),
        ComponentType.ReadOnly<SpatialEntityId>(),
        ComponentType.ReadOnly<CubeCoordinate.Component>(),
        ComponentType.ReadOnly<RightClickEvent>(),
        ComponentType.ReadWrite<AnimatorComponent>()
        );


        */

        //var Manager = World.Active.GetExistingManager<EntityManager>();
        m_ManalithData = GetEntityQuery(
            ComponentType.ReadOnly<Manalith.Component>(),
            ComponentType.ReadWrite<MeshGradientColor>()
        );

        m_GameStateData = GetEntityQuery(
        ComponentType.ReadOnly<GameState.Component>()
        );

    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        m_ComponentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        logger = World.GetExistingSystem<WorkerSystem>().LogDispatcher;
        m_UISystem = World.GetExistingSystem<UISystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<ClientWorkerIds.MapInitializedEvent.Event>();

        if (initMapEvents.Count > 0)
        {
            var manalithComps = m_ManalithData.ToComponentDataArray<Manalith.Component>(Allocator.TempJob);
            var meshGradientColors = m_ManalithData.ToComponentArray<MeshGradientColor>();

            //innitially connect MeshGradientColors with connected meshcolor to be able to construct gradients for the leylines
            for (int i = 0; i < manalithComps.Length; i++)
            {
                var manalith = manalithComps[i];
                var meshGradientColor = meshGradientColors[i];

                Entities.ForEach((MeshColor meshColor, in CubeCoordinate.Component coord) =>
                {
                    if(Vector3fext.ToUnityVector(coord.CubeCoordinate) == Vector3fext.ToUnityVector(manalith.ConnectedManalithCoordinate))
                    {
                        meshGradientColor.ConnectedManalithColor = meshColor;
                    }
                })
                .WithoutBurst()
                .Run();
            }

            manalithComps.Dispose();

            Entities.ForEach((Entity e, ManalithObject manalithObject, MeshColor meshColor, ManalithInitializer initData, in Manalith.Component manalith, in FactionComponent.Component faction) =>
            {
                meshColor.MapColor = settings.FactionMapColors[(int)faction.Faction];
                meshColor.Color = settings.FactionColors[(int)faction.Faction];

                if (m_UISystem.UIRef != null)
                {
                    switch (initData.circleSize)
                    {
                        case ManalithInitializer.CircleSize.Seven:
                            if (!manalithObject.MiniMapTileInstance)
                                PopulateMap(m_UISystem.UIRef.MinimapComponent.MapSize, m_UISystem.UIRef.MinimapComponent.MiniMapManalithTilesPanel.transform, manalith.CircleCoordinatesList[0], manalith.PathCoordinatesList, ref manalithObject, settings.FactionMapColors[0]);
                            if (!manalithObject.BigMapTileInstance)
                                PopulateMap(m_UISystem.UIRef.BigMapComponent.MapSize, m_UISystem.UIRef.BigMapComponent.MiniMapManalithTilesPanel.transform, manalith.CircleCoordinatesList[0], manalith.PathCoordinatesList, ref manalithObject, settings.FactionMapColors[0]);
                            break;
                        case ManalithInitializer.CircleSize.Three:
                            if (!manalithObject.MiniMapTileInstance)
                                PopulateMap(m_UISystem.UIRef.MinimapComponent.MapSize, m_UISystem.UIRef.MinimapComponent.MiniMapManalithTilesPanel.transform, manalith.CircleCoordinatesList, manalith.PathCoordinatesList, ref manalithObject, settings.FactionMapColors[0]);
                            if (!manalithObject.BigMapTileInstance)
                                PopulateMap(m_UISystem.UIRef.BigMapComponent.MapSize, m_UISystem.UIRef.BigMapComponent.MiniMapManalithTilesPanel.transform, manalith.CircleCoordinatesList, manalith.PathCoordinatesList, ref manalithObject, settings.FactionMapColors[0]);
                            break;
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        var manalithFactionChangeEvents = m_ComponentUpdateSystem.GetEventsReceived<Manalith.ManalithFactionChangeEvent.Event>();

        for (int q = 0; q < manalithFactionChangeEvents.Count; q++)
        {
            var EventID = manalithFactionChangeEvents[q].EntityId.Id;

            Entities.ForEach((Entity e, MeshColor meshColor, ManalithObject manalithObject, StudioEventEmitter eventEmitter, in SpatialEntityId id, in FactionComponent.Component faction) =>
            {
                if (id.EntityId.Id == EventID)
                {
                    //Fire Get Capture effects
                    if (manalithObject.BigMapTileInstance && manalithObject.BigMapTileInstance.isActiveAndEnabled)
                    {
                        if (manalithObject.BigMapTileInstance.GetCapturedMapEffect)
                        {
                            var ping = Object.Instantiate(manalithObject.BigMapTileInstance.GetCapturedMapEffect, manalithObject.BigMapTileInstance.TileRect.position, Quaternion.identity, m_UISystem.UIRef.BigMapComponent.MiniMapEffectsPanel.transform);
                            ParticleSystem.MainModule main = ping.ParticleSystem.main;
                            ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                            main.startColor = meshColor.Color;
                            size.sizeMultiplier = m_UISystem.UIRef.BigMapComponent.ManalithCapturePingSize + manalithObject.BigMapTileInstance.AddPingSize;
                            ping.ParticleSystem.Play();
                            ping.FMODEmitter.Play();
                            Object.Destroy(ping.gameObject, 4f);
                        }
                    }
                    if (manalithObject.MiniMapTileInstance && manalithObject.MiniMapTileInstance.isActiveAndEnabled)
                    {
                        if (manalithObject.MiniMapTileInstance.GetCapturedMapEffect)
                        {
                            var ping = Object.Instantiate(manalithObject.BigMapTileInstance.GetCapturedMapEffect, manalithObject.MiniMapTileInstance.TileRect.position, Quaternion.identity, m_UISystem.UIRef.MinimapComponent.MiniMapEffectsPanel.transform);
                            ParticleSystem.MainModule main = ping.ParticleSystem.main;
                            ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                            main.startColor = meshColor.Color;
                            size.sizeMultiplier = m_UISystem.UIRef.MinimapComponent.ManalithCapturePingSize;
                            ping.ParticleSystem.Play();
                            Object.Destroy(ping.gameObject, 4f);
                        }
                    }

                    meshColor.MapColor = settings.FactionMapColors[(int) faction.Faction];
                    meshColor.Color = settings.FactionColors[(int) faction.Faction];

                    manalithObject.GainControlSoundEmitter.Play();
                    foreach (ParticleSystem p in manalithObject.OneShotParticleSystems)
                    {
                        var main = p.main;
                        main.startColor = meshColor.Color;
                        p.Play();
                    }
                }
            })
            .WithoutBurst()
            .Run();
        }

        Entities.ForEach((MeshColor meshColor, ManalithObject manalithObject, MeshGradientColor meshGradientColor, UnitEffects unitEffects) =>
        {
            if (meshColor.LerpColor != meshColor.Color)
                meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

            if (meshColor.MapLerpColor != meshColor.MapColor)
                meshColor.MapLerpColor = Color.Lerp(meshColor.MapLerpColor, meshColor.MapColor, 0.05f);

            if (meshColor.MapLerpColor == meshColor.MapColor && meshColor.LerpColor == meshColor.Color)
            {
                meshColor.IsLerping = false;
            }
            else
                meshColor.IsLerping = true;

            unitEffects.PlayerColor = meshColor.LerpColor;

            meshColor.MeshRenderer.material.SetColor("_UnlitColor", meshColor.LerpColor);
            meshColor.MeshRenderer.material.SetColor("_EmissiveColor", meshColor.LerpColor * meshColor.MeshRenderer.material.GetFloat("_EmissiveIntensity"));

            if (manalithObject.MiniMapTileInstance)
                manalithObject.MiniMapTileInstance.TileImage.color = meshColor.MapLerpColor;
            if (manalithObject.BigMapTileInstance)
                manalithObject.BigMapTileInstance.TileImage.color = meshColor.MapLerpColor;

            foreach (MeshRenderer r in manalithObject.EmissionColorRenderers)
            {
                r.material.SetColor("_EmissiveColor", meshColor.LerpColor * r.material.GetFloat("_EmissiveIntensity"));
            }

            foreach (Light l in manalithObject.Lights)
            {
                l.color = meshColor.LerpColor;
            }

            foreach (ParticleSystem p in manalithObject.ParticleSystems)
            {
                var mainModule = p.main;

                if (mainModule.startColor.color != meshColor.LerpColor)
                    mainModule.startColor = meshColor.LerpColor;
            }

            for (int i = 0; i < manalithObject.DetailColorRenderers.Count; i++)
            {
                manalithObject.DetailColorRenderers[i].sharedMaterial.SetColor("_BaseColor1", meshColor.LerpColor);
            }

            if (meshGradientColor.ConnectedManalithColor && (meshColor.IsLerping || meshGradientColor.ConnectedManalithColor.IsLerping))
            {
                ConstructGradient(meshGradientColor.colorKeys, meshGradientColor.alphaKeys, ref meshGradientColor.Gradient, meshColor.LerpColor, meshGradientColor.ConnectedManalithColor.LerpColor);
                ConstructGradient(meshGradientColor.colorKeys, meshGradientColor.alphaKeys, ref meshGradientColor.MapGradient, meshColor.MapLerpColor, meshGradientColor.ConnectedManalithColor.MapLerpColor);

                if (manalithObject.MiniMapTileInstance)
                {
                    manalithObject.MiniMapTileInstance.UILineRenderer.Gradient = meshGradientColor.MapGradient;
                    manalithObject.MiniMapTileInstance.UILineRenderer.SetVerticesDirty();
                }
                if (manalithObject.BigMapTileInstance)
                {
                    manalithObject.BigMapTileInstance.UILineRenderer.Gradient = meshGradientColor.MapGradient;
                    manalithObject.BigMapTileInstance.UILineRenderer.SetVerticesDirty();
                }

                meshGradientColor.LeylineRenderer.colorGradient = meshGradientColor.Gradient;
            }
        })
        .WithoutBurst()
        .Run();

        return inputDeps;
    }

    void ConstructGradient(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys, ref Gradient gradient, Color c1, Color c2)
    {
        // Populate the color keys at the relative time 0 and 1 (0 and 100%)
        colorKeys[0].color = c1;
        colorKeys[0].time = 0.0f;
        colorKeys[1].color = c1;
        colorKeys[1].time = .25f;
        colorKeys[2].color = c2;
        colorKeys[2].time = .75f;
        colorKeys[3].color = c2;
        colorKeys[3].time = 1.0f;

        alphaKeys[0].alpha = 1.0f;
        alphaKeys[0].time = 0.0f;
        alphaKeys[1].alpha = 1.0f;
        alphaKeys[1].time = 1.0f;

        gradient.SetKeys(colorKeys, alphaKeys);
    }

    void PopulateMap(float scale, Transform parent, Vector3f coord, List<Vector3f> pathCoords, ref ManalithObject manalithObject, Color tileColor)
    {
        float offsetMultiplier = scale;
        float tilescale = scale / 5.8f;
        //Instantiate MiniMapTile into Map
        Vector3 pos = CellGridMethods.CubeToPos(coord, new Vector2f(0, 0));

        Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);

        if (!manalithObject.MiniMapTileInstance)
        {
            manalithObject.MiniMapTileInstance = InstantiateMapTile(ref manalithObject, parent, tilescale, invertedPos, tileColor, pathCoords, scale, true);
        }
        else if (!manalithObject.BigMapTileInstance)
        {
            manalithObject.BigMapTileInstance = InstantiateMapTile(ref manalithObject, parent, tilescale, invertedPos, tileColor, pathCoords, scale, false);
        }
    }

    void PopulateMap(float scale, Transform parent, List<Vector3f> circleCoords, List<Vector3f> pathCoords, ref ManalithObject manalithObject, Color tileColor)
    {
        float offsetMultiplier = scale;
        float tilescale = scale / 5.8f;

        List<Vector2> mapSpacePositions = new List<Vector2>();
        //Instantiate MiniMapTile into Map
        foreach (Vector3f f in circleCoords)
        {
            Vector3 pos = CellGridMethods.CubeToPos(f, new Vector2f(0, 0));
            Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);
            mapSpacePositions.Add(invertedPos);
        }

        //calculate mapSpacePositions Center
        var center = new Vector2(0, 0);
        var i = 0;
        foreach (Vector2 v in mapSpacePositions)
        {
            center += v;
            i++;
        }

        var theCenter = center / i;

        if (!manalithObject.MiniMapTileInstance)
        {
            manalithObject.MiniMapTileInstance = InstantiateMapTile(ref manalithObject, parent, tilescale, theCenter, tileColor, pathCoords, scale, true);
        }
        else if (!manalithObject.BigMapTileInstance)
        {
            manalithObject.BigMapTileInstance = InstantiateMapTile(ref manalithObject, parent, tilescale, theCenter, tileColor, pathCoords, scale, false);
        }
    }

    MiniMapTile InstantiateMapTile(ref ManalithObject manalithObject, Transform parent, float tileScale, Vector2 invertedPos, Color tileColor, List<Vector3f> pathCoords, float scale, bool isMiniMapTile)
    {
        MiniMapTile instanciatedTile = Object.Instantiate(manalithObject.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, parent);
        instanciatedTile.TileRect.sizeDelta = new Vector2((int) (instanciatedTile.TileRect.sizeDelta.x * tileScale), (int) (instanciatedTile.TileRect.sizeDelta.y * tileScale));
        invertedPos = new Vector2((int) invertedPos.x, (int) invertedPos.y);
        instanciatedTile.TileRect.anchoredPosition = invertedPos;
        instanciatedTile.TileColor = tileColor;

        if (instanciatedTile.UILineRenderer)
        {
            instanciatedTile.UILineRenderer.Points = new Vector2[pathCoords.Count];

            if (isMiniMapTile)
                instanciatedTile.UILineRenderer.lineThickness = 2;
            else
                instanciatedTile.UILineRenderer.lineThickness = 8;

            if (pathCoords.Count == 0)
                Object.Destroy(instanciatedTile.UILineRenderer.gameObject);

            //populate line positions
            for (int v = 0; v < instanciatedTile.UILineRenderer.Points.Length; v++)
            {
                var coord = new Vector3f(pathCoords[v].X, pathCoords[v].Y, pathCoords[v].Z);
                var p = CellGridMethods.CubeToPos(coord, new Vector2f(0, 0));
                Vector2 invertedPos2 = new Vector2(p.x * scale, p.z * scale);
                instanciatedTile.UILineRenderer.Points[v] = invertedPos2 - instanciatedTile.TileRect.anchoredPosition;
            }

            instanciatedTile.UILineRenderer.color = tileColor;
        }
        return instanciatedTile;
    }
}
