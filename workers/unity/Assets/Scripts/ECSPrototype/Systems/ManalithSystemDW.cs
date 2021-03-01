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

                    //Debug.Log("init: " + m_ManaLithData.CalculateEntityCount());
                    //Debug.Log(m_CircleData.CalculateEntityCount());
                    
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
                    ComponentType.ReadOnly<ManalithInitializer>(),
                    ComponentType.ReadWrite<StudioEventEmitter>()
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
                    //Debug.Log("AuthPlayerCount is 0");
                    return;
                }

                var manaLithPositions = m_ManaLithData.ToComponentDataArray<Position.Component>(Allocator.TempJob);
                var manaLithFactions = m_ManaLithData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
                var manalithComps = m_ManaLithData.ToComponentDataArray<Manalith.Component>(Allocator.TempJob);
                var entityID = m_ManaLithData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);

                var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
                var playerHighlightingDatas = m_PlayerData.ToComponentDataArray<HighlightingDataComponent>(Allocator.TempJob);

                var gameStates = m_GameControllerData.ToComponentDataArray<GameState.Component>(Allocator.TempJob);

                var gameState = gameStates[0];
                var playerFaction = playerFactions[0];


                var initMapEvents = m_ComponentUpdateSystem.GetEventsReceived<GameState.InitializeMapEvent.Event>();

                if(initMapEvents.Count > 0)
                {
                    for (int i = 0; i < manaLithFactions.Length; i++)
                    {
                        var manalithFaction = manaLithFactions[i];
                        var manaLith = manalithComps[i];
                        var ID = entityID[i].EntityId.Id;
                        var pos = manaLithPositions[i];


                        Entities.With(m_CircleData).ForEach((Entity e, ManalithClientData clientData, MeshColor meshColor, ManalithInitializer initData, StudioEventEmitter eventEmitter) =>
                        {
                                var circlePos = meshColor.transform.position.sqrMagnitude;
                                if (pos.Coords.ToUnityVector().sqrMagnitude == circlePos)
                                {
                                    if (m_UISystem.UIRef != null)
                                    {
                                        switch (initData.circleSize)
                                        {
                                            case ManalithInitializer.CircleSize.Seven:
                                                clientData.WorldPos = clientData.UIElementTransform.position;
                                                clientData.IngameIconRef = Object.Instantiate(m_UISystem.UIRef.ManalithIconPrefab, Vector3.zero, Quaternion.identity, m_UISystem.UIRef.ManalithInfoPanel.transform);
                                                clientData.ManalithEntityID = ID;
                                                clientData.IngameIconRef.InfoButton.onClick.AddListener(delegate { SwitchTooltip(clientData); });
                                                var coord1 = initData.leyLineCircleCoords[0];

                                                if (!clientData.MiniMapTileInstance)
                                                    PopulateMap(m_UISystem.UIRef.MinimapComponent.MapSize, initData, m_UISystem.UIRef.MinimapComponent.MiniMapManalithTilesPanel.transform, coord1, ref clientData, settings.FactionMapColors[0]);
                                                if (!clientData.BigMapTileInstance)
                                                    PopulateMap(m_UISystem.UIRef.BigMapComponent.MapSize, initData, m_UISystem.UIRef.BigMapComponent.MiniMapManalithTilesPanel.transform, coord1, ref clientData, settings.FactionMapColors[0]);


                                                break;
                                            case ManalithInitializer.CircleSize.Three:
                                                clientData.WorldPos = clientData.UIElementTransform.position;
                                                clientData.IngameIconRef = Object.Instantiate(m_UISystem.UIRef.ManalithIconPrefab, Vector3.zero, Quaternion.identity, m_UISystem.UIRef.ManalithInfoPanel.transform);
                                                clientData.ManalithEntityID = ID;
                                                clientData.IngameIconRef.InfoButton.onClick.AddListener(delegate { SwitchTooltip(clientData); });


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
                            #region EnableDisableManalithHovered
                            bool manalithIsHovered = false;
                            foreach (CellAttribute c in manaLith.CircleAttributeList.CellAttributes)
                            {
                                if (Vector3fext.ToUnityVector(playerHighlightingDatas[0].HoveredCoordinate) == Vector3fext.ToUnityVector(c.CubeCoordinate))
                                    manalithIsHovered = true;
                            }

                            if (manalithIsHovered && gameState.CurrentState == GameStateEnum.planning)
                            {
                                clientData.ManalithHoveredMesh.enabled = true;
                                clientData.ManalithHoveredMesh.material.SetColor("_UnlitColor", meshColor.LerpColor);
                            }
                            else
                            {
                                clientData.ManalithHoveredMesh.enabled = false;
                            }

                            #endregion

                            if (clientData.IngameIconRef && m_UISystem.UIRef)
                            {
                                clientData.IngameIconRef.transform.localPosition = m_UISystem.RoundVector3(m_UISystem.WorldToUISpace(m_UISystem.UIRef.Canvas, clientData.WorldPos));
                                if (ID == m_UISystem.UIRef.ManalithToolTipFab.ActiveManalithID)
                                {
                                    Vector2 Screenpos = new Vector2(0f, 0f);
                                    Vector2 Anchor = clientData.IngameIconRef.RectTrans.anchoredPosition;

                                    if (((Anchor.x - Screenpos.x) > Screen.width * 0.9f || (Anchor.x - Screenpos.x) < -Screen.width * 0.9f) || ((Anchor.y - Screenpos.y) > Screen.height * 0.9f || (Anchor.y - Screenpos.y) < -Screen.height * 0.9f))
                                    {
                                        TurnOffToolTip();
                                    }
                                    //check if distance to middle of screen is bigger than screensize/height /2, if yes call turnoff manalithtooltip, if no updatepos
                                    else
                                    {
                                        UpdateLeyLineTooltipPosition(Anchor);
                                    }
                                }
                            }

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
                                        p.startColor = meshColor.Color;
                                        p.Play();
                                    }
                                }


                            }
                            //Transport data from manaLith into clientData.IconPrefabref
                            if (manalithFaction.Faction == playerFaction.Faction)
                            {
                                clientData.IngameIconRef.EnergyText.text = manaLith.CombinedEnergyGain.ToString();
                                eventEmitter.SetParameter("Pitch", manaLith.CombinedEnergyGain);
                                eventEmitter.enabled = true;
                            }
                            else
                            {
                                clientData.IngameIconRef.EnergyText.text = "";
                                eventEmitter.enabled = false;
                            }

                            clientData.IngameIconRef.PlayerColorImage.color = meshColor.LerpColor;
                            clientData.IngameIconRef.EnergyText.color = meshColor.LerpColor;

                            if (clientData.MiniMapTileInstance)
                                clientData.MiniMapTileInstance.TileImage.color = meshColor.MapLerpColor;
                            if (clientData.BigMapTileInstance)
                                clientData.BigMapTileInstance.TileImage.color = meshColor.MapLerpColor;
                            //if fact.Faction != 0 display gain, else dont
                            //if color of the image isnt lerped to meshcolor, lerp there
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


                    //Color emissionColor = meshColor.LerpColor * meshColor.EmissionMultiplier;
                    meshColor.MeshRenderer.material.SetColor("_UnlitColor", meshColor.LerpColor);
                    //meshColor.MeshRenderer.material.EnableKeyword("_EMISSION");
                    meshColor.MeshRenderer.material.SetColor("_EmissiveColor", meshColor.LerpColor * meshColor.MeshRenderer.material.GetFloat("_EmissiveIntensity"));
                    //meshColor.MeshRenderer.UpdateGIMaterials();

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

                manaLithPositions.Dispose();
                manaLithFactions.Dispose();
                entityID.Dispose();
                manalithComps.Dispose();
                playerFactions.Dispose();
                playerHighlightingDatas.Dispose();
                gameStates.Dispose();
            }
        }

        Gradient ConstructGradient(Color c1, Color c2)
        {
            //Color c1 = meshGradientColor.ManalithColor.LerpColor;
            //Color c2 = meshGradientColor.ConnectedManalithColor.LerpColor;
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

        protected void SwitchTooltip(ManalithClientData clientData)
        {
            ManalithTooltip Tooltip = m_UISystem.UIRef.ManalithToolTipFab;
            if (clientData.ManalithEntityID == Tooltip.ActiveManalithID)
            {
                TurnOffToolTip();
                return;
            }

            Tooltip.gameObject.SetActive(true);
            Tooltip.ActiveManalithID = clientData.ManalithEntityID;
            Tooltip.ManalithName.text = clientData.NodeName;
            //Debug.Log(m_ManaLithData.CalculateEntityCount());

            var playerFactions = m_PlayerData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            var playerFaction = playerFactions[0];

            var facts = m_ManaLithData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
            var manaliths = m_ManaLithData.ToComponentDataArray<Manalith.Component>(Allocator.TempJob);
            var IDs = m_ManaLithData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);

            for(int j = 0; j < IDs.Length; j++) {
                var ID = IDs[j];
                var fact = facts[j];
                var manalith = manaliths[j];

                //Debug.Log(ID.EntityId.Id);
                if (ID.EntityId.Id == Tooltip.ActiveManalithID)
                {
                    //Debug.Log("Arrived at right Manalith");
                    Tooltip.ManalithBaseEnergyGain.text = "+" + manalith.BaseIncome;
                    if(fact.Faction != 0)
                    {
                        Tooltip.NodeImage.color = settings.FactionColors[(int)fact.Faction];
                        Tooltip.ManalithBaseEnergyGain.color = settings.FactionColors[(int)fact.Faction];
                    }
                    else
                    {
                        Tooltip.NodeImage.color = settings.UINeutralColor;
                        Tooltip.ManalithBaseEnergyGain.color = settings.UINeutralColor;
                    }

                    for(int i = 0; i<Tooltip.ManalithUISlots.Count; i++)
                    {
                        if (i < manalith.Manalithslots.Count)
                        {
                            Tooltip.ManalithUISlots[i].gameObject.SetActive(true);

                            if (manalith.Manalithslots[i].OccupyingFaction != 0)
                            {
                                Tooltip.ManalithUISlots[i].HexFill.color = settings.FactionColors[(int)manalith.Manalithslots[i].OccupyingFaction];

                                if (manalith.Manalithslots[i].EnergyGained > 0 && manalith.Manalithslots[i].OccupyingFaction == playerFaction.Faction)
                                {
                                    Tooltip.ManalithUISlots[i].HexOutline.color = settings.UINeutralColor;
                                    Tooltip.ManalithUISlots[i].EnergyGainText.text = manalith.Manalithslots[i].EnergyGained.ToString();
                                }
                                else
                                {
                                    Tooltip.ManalithUISlots[i].HexOutline.color = Color.black;
                                    Tooltip.ManalithUISlots[i].EnergyGainText.text = "";
                                }
                            }
                            else {
                                Tooltip.ManalithUISlots[i].HexFill.color = Color.gray;
                                Tooltip.ManalithUISlots[i].HexOutline.color = Color.black;
                                Tooltip.ManalithUISlots[i].EnergyGainText.text = "";
                            }
                        }
                        else
                        {
                            Tooltip.ManalithUISlots[i].gameObject.SetActive(false);
                        }
                    }
                }
                
            }
            facts.Dispose();
            IDs.Dispose();
            manaliths.Dispose();
            playerFactions.Dispose();

            //set positione
            UpdateLeyLineTooltipPosition(clientData.IngameIconRef.RectTrans.anchoredPosition);


        }

        protected void UpdateLeyLineTooltipPosition(Vector2 inButtonAnchoredPosition)
        {
            Vector2 ScreenCenter = new Vector2(0f, 0f);
            Vector2 Dir = ScreenCenter - inButtonAnchoredPosition;
            //Debug.Log("ButtonV3: " + inButtonAnchoredPosition + " ScreenCenterV3: " + ScreenCenter + " DirNormalized: " + Dir.normalized);
            m_UISystem.UIRef.ManalithToolTipFab.RectTrans.anchoredPosition = inButtonAnchoredPosition + new Vector2(m_UISystem.UIRef.ManalithToolTipFab.RectTrans.rect.width / 1.5f * Dir.normalized.x, m_UISystem.UIRef.ManalithToolTipFab.RectTrans.rect.height / 1.5f * Dir.normalized.y);
        }

        protected void TurnOffToolTip()
        {
            m_UISystem.UIRef.ManalithToolTipFab.ActiveManalithID = 0;
            m_UISystem.UIRef.ManalithToolTipFab.gameObject.SetActive(false);
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
                isVisibleRef.MiniMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, invertedPos, tileColor, false);
            }
            else if(!isVisibleRef.BigMapTileInstance)
            {
                isVisibleRef.BigMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, invertedPos, tileColor, true);
            }
        }

        MiniMapTile InstantiateMapTile(ref ManalithClientData isVisibleRef, Transform parent, float tileScale, Vector2 invertedPos, Color tileColor, bool isBigMapTile)
        {
            MiniMapTile instanciatedTile = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, parent);
            instanciatedTile.TileRect.sizeDelta = new Vector2((int)(instanciatedTile.TileRect.sizeDelta.x * tileScale), (int)(instanciatedTile.TileRect.sizeDelta.y * tileScale));
            invertedPos = new Vector2((int)invertedPos.x, (int)invertedPos.y);
            instanciatedTile.TileRect.anchoredPosition = invertedPos;
            instanciatedTile.TileColor = tileColor;

            if (isBigMapTile)
                instanciatedTile.EmitSoundEffect = true;


            return instanciatedTile;
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
                isVisibleRef.MiniMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, theCenter, tileColor, coords, initData, scale, true);
            }
            else if (!isVisibleRef.BigMapTileInstance)
            {
                isVisibleRef.BigMapTileInstance = InstantiateMapTile(ref isVisibleRef, parent, tilescale, theCenter, tileColor, coords, initData, scale, false);
            }
        }

        MiniMapTile InstantiateMapTile(ref ManalithClientData isVisibleRef, Transform parent, float tileScale, Vector2 invertedPos, Color tileColor, List<float3> coords, ManalithInitializer initData, float scale, bool isMiniMapTile)
        {
            MiniMapTile instanciatedTile = Object.Instantiate(isVisibleRef.MiniMapTilePrefab, Vector3.zero, Quaternion.identity, parent);
            instanciatedTile.TileRect.sizeDelta = new Vector2((int)(instanciatedTile.TileRect.sizeDelta.x * tileScale), (int)(instanciatedTile.TileRect.sizeDelta.y * tileScale));
            invertedPos = new Vector2((int)invertedPos.x, (int)invertedPos.y);
            instanciatedTile.TileRect.anchoredPosition = invertedPos;
            instanciatedTile.TileColor = tileColor;

            if (instanciatedTile.UILineRenderer)
            {
                //Debug.Log("Populate UI Line Renderer Positions");
                //isVisibleRef.MiniMapTileInstance.UILineRenderer.set
                instanciatedTile.UILineRenderer.Points = new Vector2[initData.leyLinePathCoords.Count];
                if(isMiniMapTile)
                    instanciatedTile.UILineRenderer.lineThickness = 2;
                else
                    instanciatedTile.UILineRenderer.lineThickness = 8;
                //populate line positions
                for (int v = 0; v < instanciatedTile.UILineRenderer.Points.Length; v++)
                {
                    var coord = new Vector3f(initData.leyLinePathCoords[v].x, initData.leyLinePathCoords[v].y, initData.leyLinePathCoords[v].z);
                    var p = CellGridMethods.CubeToPos(coord, new Vector2f(0f, 0f));
                    Vector2 invertedPos2 = new Vector2(p.x * scale, p.z * scale);
                    instanciatedTile.UILineRenderer.Points[v] = invertedPos2 - instanciatedTile.TileRect.anchoredPosition;
                }

                instanciatedTile.UILineRenderer.color = tileColor;
                //isVisibleRef.MiniMapTileInstance.UILineRendererRect.anchoredPosition
            }

            return instanciatedTile;
        }
    }


}
