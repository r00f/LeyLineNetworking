using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable;
using Cell;
using Generic;
using Player;
using Unity.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.UI;
using Unity.Mathematics;
using Unit;

namespace LeyLineHybridECS
{
    //[DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ManalithSystemDW : ComponentSystem
    {
        UISystem m_UISystem;
        EntityQuery m_LineData;
        EntityQuery m_CircleData;
        EntityQuery m_ManaLithData;
        EntityQuery m_ProjectorData;
        private EntityQuery m_ManalithUnitData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameControllerData;
        ComponentUpdateSystem m_ComponentUpdateSystem;

        Settings settings;
        bool initialized;

        private bool WorldsInitialized()
        {
            if (Worlds.ClientWorld != null)
            {
                if (!initialized)
                {
                    m_ComponentUpdateSystem = Worlds.ClientWorld.World.GetExistingSystem<ComponentUpdateSystem>();
                    m_UISystem = Worlds.ClientWorld.World.GetExistingSystem<UISystem>();

                    m_PlayerData = Worlds.ClientWorld.CreateEntityQuery(
                        ComponentType.ReadOnly<PlayerState.HasAuthority>(),
                        ComponentType.ReadOnly<PlayerState.Component>(),
                        ComponentType.ReadOnly<FactionComponent.Component>(),
                        ComponentType.ReadOnly<HighlightingDataComponent>(),
                        ComponentType.ReadOnly<WorldIndex.Component>()
                    );

                    m_GameControllerData = Worlds.ClientWorld.CreateEntityQuery(

                        ComponentType.ReadOnly<GameState.Component>(),
                        ComponentType.ReadOnly<WorldIndex.Component>(),
                        ComponentType.ReadOnly<Position.Component>()

                    );

                    //var Manager = World.Active.GetExistingManager<EntityManager>();
                    m_ManaLithData = Worlds.ClientWorld.CreateEntityQuery(
                        ComponentType.ReadOnly<FactionComponent.Component>(),
                        ComponentType.ReadOnly<Manalith.Component>(),
                        ComponentType.ReadOnly<Position.Component>(),
                        ComponentType.ReadOnly<SpatialEntityId>()
                    );

                    m_ManalithUnitData = Worlds.ClientWorld.CreateEntityQuery(
                        ComponentType.ReadOnly<SpatialEntityId>(),
                        ComponentType.ReadOnly<ManalithUnit.Component>(),
                        ComponentType.ReadOnly<TeamColorMeshes>()
                    );

                    initialized = true;
                }
            }
            return initialized;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            //var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName).GetComponent<Unit_BaseDataSet>();
            settings = Resources.Load<Settings>("Settings");

            //Debug.Log(settings.FactionColors[0].g);
            m_CircleData = GetEntityQuery(

                    ComponentType.ReadWrite<MeshColor>(),
                    ComponentType.ReadWrite<ManalithClientData>(),
                    ComponentType.ReadOnly<ManalithInitializer>()
                    //ComponentType.ReadWrite<StudioEventEmitter>()
            );

            m_LineData = GetEntityQuery(

                 ComponentType.ReadWrite<MeshGradientColor>(),
                 ComponentType.ReadWrite<ManalithClientData>()
            );

            m_ProjectorData = GetEntityQuery(

                   ComponentType.ReadWrite<Transform>(),
                   ComponentType.ReadWrite<Projector>()
            );



        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            Entities.With(m_CircleData).ForEach((MeshColor meshColor) =>
            {
                meshColor.MapColor = settings.FactionMapColors[0];
                meshColor.Color = settings.FactionColors[0];
            });
        }

        protected override void OnUpdate()
        {
            if(WorldsInitialized())
            {
                if (m_PlayerData.CalculateEntityCount() == 0 || m_GameControllerData.CalculateEntityCount() == 0)
                {
                    return;
                }

                var manaLithPositions = m_ManaLithData.ToComponentDataArray<Position.Component>(Allocator.TempJob);
                var manaLithFactions = m_ManaLithData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
                var manalithComps = m_ManaLithData.ToComponentDataArray<Manalith.Component>(Allocator.TempJob);
                var entityID = m_ManaLithData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);


                var manalithUnitEntittyIds = m_ManalithUnitData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);
                var manalithUnitTeamColorMeshes = m_ManalithUnitData.ToComponentArray<TeamColorMeshes>();



                var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
                var playerStates = m_PlayerData.ToComponentDataArray<PlayerState.Component>(Allocator.TempJob);
                var playerHighlightingDatas = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

                var gameStates = m_GameControllerData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

                var gameState = gameStates[0];
                var playerFaction = playerFactions[0];
                var playerState = playerStates[0];

                var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.InitializeMapEvent.Event>();

                if(initMapEvents.Count > 0)
                {
                    for (int i = 0; i < manaLithFactions.Length; i++)
                    {
                        var manalithFaction = manaLithFactions[i];
                        var manaLith = manalithComps[i];
                        var ID = entityID[i].EntityId.Id;
                        var pos = manaLithPositions[i];

                        Entities.With(m_CircleData).ForEach((Entity e, ManalithClientData clientData, MeshColor meshColor, ManalithInitializer initData/*, StudioEventEmitter eventEmitter*/) =>
                        {
                            var circlePos = meshColor.transform.position.sqrMagnitude;

                            if (pos.Coords.ToUnityVector().sqrMagnitude == circlePos)
                            {
                                if (m_UISystem.UIRef != null)
                                    {
                                        switch (initData.circleSize)
                                        {
                                            case ManalithInitializer.CircleSize.Seven:
                                                clientData.ManalithEntityID = ID;

                                                var coord1 = initData.leyLineCircleCoords[0];

                                                if (!clientData.MiniMapTileInstance)
                                                    PopulateMap(m_UISystem.UIRef.MinimapComponent.MapSize, initData, m_UISystem.UIRef.MinimapComponent.MiniMapManalithTilesPanel.transform, coord1, ref clientData, settings.FactionMapColors[0]);
                                                if (!clientData.BigMapTileInstance)
                                                    PopulateMap(m_UISystem.UIRef.BigMapComponent.MapSize, initData, m_UISystem.UIRef.BigMapComponent.MiniMapManalithTilesPanel.transform, coord1, ref clientData, settings.FactionMapColors[0]);

                                                break;
                                            case ManalithInitializer.CircleSize.Three:
                                                clientData.ManalithEntityID = ID;

                                                if (!clientData.MiniMapTileInstance)
                                                    PopulateMap(m_UISystem.UIRef.MinimapComponent.MapSize, initData, m_UISystem.UIRef.MinimapComponent.MiniMapManalithTilesPanel.transform, initData.leyLineCircleCoords, ref clientData, settings.FactionMapColors[0]);
                                                if (!clientData.BigMapTileInstance)
                                                    PopulateMap(m_UISystem.UIRef.BigMapComponent.MapSize, initData, m_UISystem.UIRef.BigMapComponent.MiniMapManalithTilesPanel.transform, initData.leyLineCircleCoords, ref clientData, settings.FactionMapColors[0]);

                                                break;
                                        }


                                    }
                            }
                        });
                    }
                }

                for (int i = 0; i < manaLithFactions.Length; i++)
                {
                    var manalithFaction = manaLithFactions[i];
                    var manaLith = manalithComps[i];
                    var ID = entityID[i].EntityId.Id;
                    var pos = manaLithPositions[i];

                    Entities.With(m_CircleData).ForEach((Entity e, ManalithClientData clientData, MeshColor meshColor, ManalithInitializer initData, StudioEventEmitter eventEmitter) =>
                    {
                        if (clientData.ManalithEntityID == ID)
                        {
                            if(!clientData.ManalithUnitTeamColorMeshes)
                            {
                                for (int y = 0; y < manalithUnitEntittyIds.Length; y++)
                                {
                                    var manalithUnitID = manalithUnitEntittyIds[y].EntityId.Id;
                                    var manalithUnitTeamColorMesh = manalithUnitTeamColorMeshes[y];

                                    if (manalithUnitID == manaLith.ManalithUnitId)
                                    {
                                        clientData.ManalithUnitTeamColorMeshes = manalithUnitTeamColorMesh;
                                    }
                                }
                            }
                            else
                            {
                                clientData.ManalithUnitTeamColorMeshes.color = meshColor.Color;
                            }

                            #region EnableDisableManalithHovered
                            bool manalithIsHovered = false;

                            if(playerState.CurrentState != PlayerStateEnum.waiting_for_target)
                            {
                                if (Vector3fext.ToUnityVector(manaLith.ManalithUnitCoordinate) == Vector3fext.ToUnityVector(playerHighlightingDatas[0].HoveredCoordinate))
                                {
                                    manalithIsHovered = true;
                                }
                            }

                            if (manalithIsHovered && gameState.CurrentState == GameStateEnum.planning)
                            {
                                m_UISystem.UIRef.SelectionOutlineMaterial.SetColor("_OuterColor", meshColor.Color);
                                foreach (GameObject g in meshColor.ManaLithObject.SelectionOutlineRenderers)
                                {
                                    g.layer = 21;
                                }
                            }
                            else
                            {
                                foreach (GameObject g in meshColor.ManaLithObject.SelectionOutlineRenderers)
                                {
                                    g.layer = 0;
                                }
                            }

                            #endregion

                            meshColor.MapColor = settings.FactionMapColors[(int) manalithFaction.Faction];
                            meshColor.Color = settings.FactionColors[(int) manalithFaction.Faction];

                            var manalithFactionChangeEvents = m_ComponentUpdateSystem.GetEventsReceived<Manalith.ManalithFactionChangeEvent.Event>();

                            for (int q = 0; q < manalithFactionChangeEvents.Count; q++)
                            {
                                var EventID = manalithFactionChangeEvents[q].EntityId.Id;

                                if (ID == EventID)
                                {
                                    //Fire Get Capture effects
                                    if (clientData.BigMapTileInstance && clientData.BigMapTileInstance.isActiveAndEnabled)
                                    {
                                        if (clientData.BigMapTileInstance.GetCapturedMapEffect)
                                        {
                                            var ping = Object.Instantiate(clientData.BigMapTileInstance.GetCapturedMapEffect, clientData.BigMapTileInstance.TileRect.position, Quaternion.identity, m_UISystem.UIRef.BigMapComponent.MiniMapEffectsPanel.transform);
                                            ParticleSystem.MainModule main = ping.ParticleSystem.main;
                                            ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                                            main.startColor = meshColor.Color;
                                            size.sizeMultiplier = m_UISystem.UIRef.BigMapComponent.ManalithCapturePingSize + clientData.BigMapTileInstance.AddPingSize;
                                            ping.ParticleSystem.Play();
                                            ping.FMODEmitter.Play();
                                            Object.Destroy(ping.gameObject, 4f);
                                        }
                                    }
                                    if (clientData.MiniMapTileInstance && clientData.MiniMapTileInstance.isActiveAndEnabled)
                                    {
                                        if (clientData.MiniMapTileInstance.GetCapturedMapEffect)
                                        {
                                            var ping = Object.Instantiate(clientData.BigMapTileInstance.GetCapturedMapEffect, clientData.MiniMapTileInstance.TileRect.position, Quaternion.identity, m_UISystem.UIRef.MinimapComponent.MiniMapEffectsPanel.transform);
                                            ParticleSystem.MainModule main = ping.ParticleSystem.main;
                                            ParticleSystem.SizeOverLifetimeModule size = ping.ParticleSystem.sizeOverLifetime;
                                            main.startColor = meshColor.Color;
                                            size.sizeMultiplier = m_UISystem.UIRef.MinimapComponent.ManalithCapturePingSize;
                                            ping.ParticleSystem.Play();
                                            Object.Destroy(ping.gameObject, 4f);
                                        }
                                    }
                                    meshColor.ManaLithObject.GainControlSoundEmitter.Play();
                                    foreach (ParticleSystem p in meshColor.ManaLithObject.OneShotParticleSystems)
                                    {
                                        var main = p.main;
                                        main.startColor = meshColor.Color;
                                        p.Play();
                                    }
                                }


                            }
                            /*
                            //Transport data from manaLith into clientData.IconPrefabref
                            if (manalithFaction.Faction == playerFaction.Faction)
                            {
                                eventEmitter.SetParameter("Pitch", manaLith.CombinedEnergyGain);
                                eventEmitter.enabled = true;
                            }
                            else
                            {
                                eventEmitter.enabled = false;
                            }
                            */
                            if (clientData.MiniMapTileInstance)
                                clientData.MiniMapTileInstance.TileImage.color = meshColor.MapLerpColor;
                            if (clientData.BigMapTileInstance)
                                clientData.BigMapTileInstance.TileImage.color = meshColor.MapLerpColor;
                        }
                    });
                }

                Entities.With(m_CircleData).ForEach((MeshColor meshColor) =>
                {

                    if (meshColor.LerpColor != meshColor.Color)
                        meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

                    if(meshColor.MapLerpColor != meshColor.MapColor)
                        meshColor.MapLerpColor = Color.Lerp(meshColor.MapLerpColor, meshColor.MapColor, 0.05f);

                    if (meshColor.MapLerpColor == meshColor.MapColor && meshColor.LerpColor == meshColor.Color)
                    {
                        meshColor.IsLerping = false;
                    }
                    else
                        meshColor.IsLerping = true;

                    meshColor.MeshRenderer.material.SetColor("_UnlitColor", meshColor.LerpColor);
                    meshColor.MeshRenderer.material.SetColor("_EmissiveColor", meshColor.LerpColor * meshColor.MeshRenderer.material.GetFloat("_EmissiveIntensity"));

                    foreach (MeshRenderer r in meshColor.ManaLithObject.EmissionColorRenderers)
                    {
                        r.material.SetColor("_EmissiveColor", meshColor.LerpColor * r.material.GetFloat("_EmissiveIntensity"));
                    }

                    foreach (Light l in meshColor.ManaLithObject.Lights)
                    {
                        l.color = meshColor.LerpColor;
                    }

                    foreach (ParticleSystem p in meshColor.ManaLithObject.ParticleSystems)
                    {
                        var mainModule = p.main;

                        if (mainModule.startColor.color != meshColor.LerpColor)
                            mainModule.startColor = meshColor.LerpColor;
                    }

                    var circlePsMain = meshColor.CirclePs.main;

                    if (circlePsMain.startColor.color != meshColor.LerpColor)
                        circlePsMain.startColor = meshColor.LerpColor;

                    for (int i = 0; i < meshColor.ManaLithObject.DetailColorRenderers.Count; i++)
                    {
                        Renderer r = meshColor.ManaLithObject.DetailColorRenderers[i];

                        r.sharedMaterial.SetColor("_BaseColor1", meshColor.LerpColor);
                    }

                });

                //only udate LineGradients while Color is Lerping
                Entities.With(m_LineData).ForEach((MeshGradientColor meshGradientColor, ManalithClientData clientData) =>
                {
                    if(meshGradientColor.ManalithColor.IsLerping || meshGradientColor.ConnectedManalithColor.IsLerping)
                    {
                        meshGradientColor.Gradient = ConstructGradient(meshGradientColor.ManalithColor.LerpColor, meshGradientColor.ConnectedManalithColor.LerpColor);
                        meshGradientColor.MapGradient = ConstructGradient(meshGradientColor.ManalithColor.MapLerpColor, meshGradientColor.ConnectedManalithColor.MapLerpColor);

                        if (clientData.MiniMapTileInstance)
                        {
                            clientData.MiniMapTileInstance.UILineRenderer.Gradient = meshGradientColor.MapGradient;
                            clientData.MiniMapTileInstance.UILineRenderer.SetVerticesDirty();
                        }

                        if (clientData.BigMapTileInstance)
                        {
                            clientData.BigMapTileInstance.UILineRenderer.Gradient = meshGradientColor.MapGradient;
                            clientData.BigMapTileInstance.UILineRenderer.SetVerticesDirty();
                        }

                        for (int li = 0; li < meshGradientColor.colors.Length; li++)
                        {
                            meshGradientColor.colors[li] = meshGradientColor.Gradient.Evaluate((float) li / meshGradientColor.colors.Length);

                            meshGradientColor.mesh.colors = meshGradientColor.colors;
                        }
                    }
                });

                manalithUnitEntittyIds.Dispose();
                manaLithPositions.Dispose();
                manaLithFactions.Dispose();
                entityID.Dispose();
                manalithComps.Dispose();
                playerFactions.Dispose();
                playerHighlightingDatas.Dispose();
                playerStates.Dispose();
                gameStates.Dispose();
            }
        }

        Gradient ConstructGradient(Color c1, Color c2)
        {
            Gradient g = new Gradient();

            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            var colorKey = new GradientColorKey[4];
            colorKey[0].color = c1;
            colorKey[0].time = 0.0f;
            colorKey[1].color = c1;
            colorKey[1].time = .25f;
            colorKey[2].color = c2;
            colorKey[2].time = .75f;
            colorKey[3].color = c2;
            colorKey[3].time = 1.0f;

            var alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 1.0f;

            g.SetKeys(colorKey, alphaKey);

            return g;
        }

        void PopulateMap(float scale, ManalithInitializer initData, Transform parent, float3 coord, ref ManalithClientData isVisibleRef, Color tileColor)
        {
            float offsetMultiplier = scale;
            float tilescale = scale / 5.8f;
            //Instantiate MiniMapTile into Map
            Vector3f c = new Vector3f(coord.x, coord.y, coord.x);
            Vector3 pos = CellGridMethods.CubeToPos(c, new Vector2f(0f, 0f));

            Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);

            if(!isVisibleRef.MiniMapTileInstance)
            {
                isVisibleRef.MiniMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, invertedPos, tileColor, initData, scale, true);
            }
            else if(!isVisibleRef.BigMapTileInstance)
            {
                isVisibleRef.BigMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, invertedPos, tileColor, initData, scale, false);
            }
        }

        void PopulateMap(float scale, ManalithInitializer initData, Transform parent, List<float3> coords, ref ManalithClientData isVisibleRef, Color tileColor)
        {
            float offsetMultiplier = scale;
            float tilescale = scale / 5.8f;

            List<Vector2> mapSpacePositions = new List<Vector2>();
            //Instantiate MiniMapTile into Map
            foreach(float3 f in coords)
            {
                Vector3f c = new Vector3f(f.x, f.y, f.x);
                Vector3 pos = CellGridMethods.CubeToPos(c, new Vector2f(0f, 0f));
                Vector2 invertedPos = new Vector2(pos.x * offsetMultiplier, pos.z * offsetMultiplier);
                mapSpacePositions.Add(invertedPos);
            }

            //calculate mapSpacePositions Center
            var center = new Vector2(0, 0);
            var i = 0;
            foreach(Vector2 v in mapSpacePositions)
            {
                center += v;
                i++;
            }

            var theCenter = center / i;

            if (!isVisibleRef.MiniMapTileInstance)
            {
                isVisibleRef.MiniMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, theCenter, tileColor, initData, scale, true);
            }
            else if (!isVisibleRef.BigMapTileInstance)
            {
                isVisibleRef.BigMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, theCenter, tileColor, initData, scale, false);
            }
        }

        MiniMapTile InstantiateMapTile(ref ManalithClientData isVisibleRef, Transform parent, float tileScale, Vector2 invertedPos, Color tileColor, ManalithInitializer initData, float scale, bool isMiniMapTile)
        {
            MiniMapTile instanciatedTile = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, parent);
            instanciatedTile.TileRect.sizeDelta = new Vector2((int)(instanciatedTile.TileRect.sizeDelta.x * tileScale), (int)(instanciatedTile.TileRect.sizeDelta.y * tileScale));
            invertedPos = new Vector2((int)invertedPos.x, (int)invertedPos.y);
            instanciatedTile.TileRect.anchoredPosition = invertedPos;
            instanciatedTile.TileColor = tileColor;

            if (instanciatedTile.UILineRenderer)
            {
                instanciatedTile.UILineRenderer.Points = new Vector2[initData.leyLinePathCoords.Count];


                if(isMiniMapTile)
                    instanciatedTile.UILineRenderer.lineThickness = 2;
                else
                    instanciatedTile.UILineRenderer.lineThickness = 8;

                if (initData.leyLinePathCoords.Count == 0)
                    Object.Destroy(instanciatedTile.UILineRenderer.gameObject);

                //populate line positions
                for (int v = 0; v < instanciatedTile.UILineRenderer.Points.Length; v++)
                {
                    var coord = new Vector3f(initData.leyLinePathCoords[v].x, initData.leyLinePathCoords[v].y, initData.leyLinePathCoords[v].z);
                    var p = CellGridMethods.CubeToPos(coord, new Vector2f(0f, 0f));
                    Vector2 invertedPos2 = new Vector2(p.x * scale, p.z * scale);
                    instanciatedTile.UILineRenderer.Points[v] = invertedPos2 - instanciatedTile.TileRect.anchoredPosition;
                }

                instanciatedTile.UILineRenderer.color = tileColor;
            }

            return instanciatedTile;
        }
    }


}
