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

namespace LeyLineHybridECS
{
    //[DisableAutoCreation]
    public class ManalithSystemDW : ComponentSystem
    {
        UISystem m_UISystem;
        EntityQuery m_LineData;
        EntityQuery m_CircleData;
        EntityQuery m_ManaLithData;
        EntityQuery m_ProjectorData;
        EntityQuery m_PlayerData;
        EntityQuery m_GameControllerData;

        Settings settings;
        bool initialized;

        private bool WorldsInitialized()
        {
            if (Worlds.ClientWorld != null)
            {
                if (!initialized)
                {
                    m_UISystem = Worlds.ClientWorld.World.GetExistingSystem<UISystem>();
                    m_PlayerData = Worlds.ClientWorld.CreateEntityQuery(

                        //ComponentType.ReadOnly<Authoritative<PlayerState.Component>>(),
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

                 ComponentType.ReadWrite<MeshGradientColor>()
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
                meshColor.Color = settings.FactionColors[0];
            });
        }

        protected override void OnUpdate()
        {
            if(WorldsInitialized())
            {
                var manaLithPositions = m_ManaLithData.ToComponentDataArray<Position.Component>(Allocator.TempJob);
                var manaLithFactions = m_ManaLithData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);
                var manalithComps = m_ManaLithData.ToComponentDataArray<Manalith.Component>(Allocator.TempJob);
                var entityID = m_ManaLithData.ToComponentDataArray<SpatialEntityId>(Allocator.TempJob);

                for (int i = 0; i < manaLithFactions.Length; i++)
                {

                    //Entities.With(m_ManaLithData).ForEach((ref SpatialEntityId id, ref Manalith.Component m, ref FactionComponent.Component fact, ref Position.Component inPos) =>
                    //{
                    var manalithFaction = manaLithFactions[i];
                    var manaLith = manalithComps[i];
                    var ID = entityID[i].EntityId.Id;
                    var pos = manaLithPositions[i];

                        Entities.With(m_CircleData).ForEach((ManalithClientData clientData, MeshColor meshColor, ManalithInitializer initData, StudioEventEmitter eventEmitter) =>
                        {

                            if (clientData.ManalithEntityID == 0)
                            {

                                var circlePos = meshColor.transform.position.sqrMagnitude;
                                if (pos.Coords.ToUnityVector().sqrMagnitude == circlePos)
                                {
                                    if (m_UISystem.UIRef != null)
                                    {
                                        switch (initData.circleSize) {
                                            case ManalithInitializer.CircleSize.Seven:
                                                clientData.WorldPos = meshColor.transform.position + new Vector3(0, initData.iconHightoffset, 0);
                                                clientData.IngameIconRef = Object.Instantiate(m_UISystem.UIRef.ManalithIconPrefab, m_UISystem.WorldToUISpace(m_UISystem.UIRef.Canvas, clientData.WorldPos), Quaternion.identity, m_UISystem.UIRef.ManalithInfoPanel.transform);
                                                clientData.ManalithEntityID = ID;
                                                clientData.IngameIconRef.InfoButton.onClick.AddListener(delegate { SwitchTooltip(clientData); });
                                                break;
                                            case ManalithInitializer.CircleSize.Three:
                                                clientData.WorldPos = meshColor.transform.position + new Vector3(1, initData.iconHightoffset, 0);
                                                clientData.IngameIconRef = Object.Instantiate(m_UISystem.UIRef.ManalithIconPrefab, m_UISystem.WorldToUISpace(m_UISystem.UIRef.Canvas, clientData.WorldPos), Quaternion.identity, m_UISystem.UIRef.ManalithInfoPanel.transform);
                                                clientData.ManalithEntityID = ID;
                                                clientData.IngameIconRef.InfoButton.onClick.AddListener(delegate { SwitchTooltip(clientData); });
                                                break;
                                        }
                                        

                                    }
                                }
                            }

                            if (clientData.ManalithEntityID == ID)
                            {
                                if (clientData.IngameIconRef)
                                {
                                    clientData.IngameIconRef.transform.position = m_UISystem.WorldToUISpace(m_UISystem.UIRef.Canvas, clientData.WorldPos);
                                    if(ID == m_UISystem.UIRef.ManalithToolTipFab.ActiveManalithID)
                                    {
                                        Vector2 Screenpos = new Vector2(0f, 0f);
                                        Vector2 Anchor = clientData.IngameIconRef.RectTrans.anchoredPosition;
                                        Debug.Log("xdist: " + (Anchor.x - Screenpos.x) + "yDist: " + (Anchor.y - Screenpos.y));
                                        Debug.DrawLine(Screenpos, Anchor);

                                        
                                        if (((Anchor.x - Screenpos.x) > Screen.width*0.9f || (Anchor.x - Screenpos.x) < -Screen.width * 0.9f) || ((Anchor.y - Screenpos.y) > Screen.height * 0.9f || (Anchor.y - Screenpos.y) < -Screen.height * 0.9f))
                                        {
                                            TurnOffToolTip();
                                        }
                                        //check if distance to middle of screen is bigger than screensize/height /2, if yes call turnoff manalithtooltip, if no updatepos
                                        else {
                                            UpdateLeyLineTooltipPosition(Anchor);
                                        }
                                    }
                                }

                                meshColor.Color = settings.FactionColors[(int)manalithFaction.Faction];
                            //Transport data from manaLith into clientData.IconPrefabref
                            if (manalithFaction.Faction != 0)
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
                            //if fact.Faction != 0 display gain, else dont
                            //if color of the image isnt lerped to meshcolor, lerp there
                        }
                        });
                    //});
                }
                manaLithPositions.Dispose();
                manaLithFactions.Dispose();
                entityID.Dispose();
                manalithComps.Dispose();

                Entities.With(m_CircleData).ForEach((MeshColor meshColor) =>
                {

                    if (meshColor.LerpColor != meshColor.Color)
                        meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

                    Color emissionColor = meshColor.LerpColor * meshColor.EmissionMultiplier;
                    meshColor.MeshRenderer.material.SetColor("_UnlitColor", emissionColor);
                    meshColor.MeshRenderer.material.SetColor("_EmissiveColor", emissionColor);

                    foreach (MeshRenderer r in meshColor.EmissionColorRenderers)
                    {   
                        r.material.SetColor("_EmissiveColor", emissionColor);
                    }

                    foreach (Light l in meshColor.Lights)
                    {
                        l.color = meshColor.LerpColor;
                    }

                    foreach (ParticleSystem p in meshColor.ParticleSystems)
                    {
                        var mainModule = p.main;

                        if (mainModule.startColor.color != meshColor.LerpColor)
                            mainModule.startColor = meshColor.LerpColor;
                    }

                });

                Entities.With(m_LineData).ForEach((MeshGradientColor meshGradientColor) =>
                {

                    Color c1 = meshGradientColor.ManalithColor.LerpColor;
                    Color c2 = meshGradientColor.ConnectedManalithColor.LerpColor;


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

                    meshGradientColor.Gradient.SetKeys(colorKey, alphaKey);


                    for (int li = 0; li < meshGradientColor.colors.Length; li++)
                    {
                        meshGradientColor.colors[li] = meshGradientColor.Gradient.Evaluate((float)li / meshGradientColor.colors.Length) /** meshGradientColor.EmissionMultiplier*/;

                        meshGradientColor.mesh.colors = meshGradientColor.colors;
                    }
                });


            }
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
                        Tooltip.NodeImage.color = settings.UIEnergyIncomeColor;
                        Tooltip.ManalithBaseEnergyGain.color = settings.UIEnergyIncomeColor;
                    }

                    for(int i = 0; i<Tooltip.ManalithUISlots.Count; i++)
                    {
                        if (i < manalith.Manalithslots.Count)
                        {
                            Tooltip.ManalithUISlots[i].gameObject.SetActive(true);
                            if (manalith.Manalithslots[i].OccupyingFaction != 0)
                            {
                                Tooltip.ManalithUISlots[i].HexFill.color = settings.FactionColors[(int)manalith.Manalithslots[i].OccupyingFaction];
                                if (manalith.Manalithslots[i].EnergyGained > 0)
                                {
                                    Tooltip.ManalithUISlots[i].HexOutline.color = settings.UIEnergyIncomeColor;
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

            //set positione
            UpdateLeyLineTooltipPosition(clientData.IngameIconRef.RectTrans.anchoredPosition);

           
            //Tooltip.RectTrans.anchoredPosition = ButtonScreenpos;


        }

        protected void UpdateLeyLineTooltipPosition(Vector2 inButtonAnchoredPosition)
        {
            Vector2 ScreenCenter = new Vector2(0f, 0f);
            Vector2 Dir = ScreenCenter - inButtonAnchoredPosition;
            Debug.Log("ButtonV3: " + inButtonAnchoredPosition + " ScreenCenterV3: " + ScreenCenter + " DirNormalized: " + Dir.normalized);
            m_UISystem.UIRef.ManalithToolTipFab.RectTrans.anchoredPosition = inButtonAnchoredPosition + new Vector2(m_UISystem.UIRef.ManalithToolTipFab.RectTrans.rect.width / 1.8f * Dir.normalized.x, m_UISystem.UIRef.ManalithToolTipFab.RectTrans.rect.height / 1.8f * Dir.normalized.y);
        }

        protected void TurnOffToolTip()
        {
            m_UISystem.UIRef.ManalithToolTipFab.ActiveManalithID = 0;
            m_UISystem.UIRef.ManalithToolTipFab.gameObject.SetActive(false);
        }
    }
}