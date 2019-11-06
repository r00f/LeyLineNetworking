using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable;
using Cell;
using Generic;
using Player;
using Unity.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    //[DisableAutoCreation]
    public class MeshColorLerpSystem : ComponentSystem
    {
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
                        ComponentType.ReadOnly<CircleCells.Component>(),
                        ComponentType.ReadOnly<Position.Component>()
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

                    ComponentType.ReadWrite<MeshColor>()
            );


            m_LineData = GetEntityQuery(

                 ComponentType.ReadWrite<MeshGradientColor>()
            );


            m_ProjectorData = GetEntityQuery(

                   ComponentType.ReadWrite<Transform>(),
                   ComponentType.ReadWrite<Projector>()
            );


        }

        protected override void OnUpdate()
        {
            if(WorldsInitialized())
            {
                var manaLithPositions = m_ManaLithData.ToComponentDataArray<Position.Component>(Allocator.TempJob);
                var manaLithFactions = m_ManaLithData.ToComponentDataArray<FactionComponent.Component>(Allocator.TempJob);

                Entities.With(m_CircleData).ForEach((MeshColor meshColor) =>
                {
                    var circlePos = meshColor.transform.position.sqrMagnitude;

                    for (int i = 0; i < manaLithFactions.Length; i++)
                    {
                        var pos = manaLithPositions[i];
                        var faction = manaLithFactions[i];

                        if (pos.Coords.ToUnityVector().sqrMagnitude == circlePos)
                        {
                            //Debug.Log("SetColor");
                            meshColor.Color = settings.FactionColors[(int)faction.Faction];
                        }
                    }
                });

                manaLithPositions.Dispose();
                manaLithFactions.Dispose();

                //Entities.With() does not seem to work with entities from other worlds
                /*
                Entities.With(m_ManaLithData).ForEach((ref CircleCells.Component c, ref FactionComponent.Component faction, ref Position.Component position) =>
                {
                    Debug.Log("ManaLithData: " + m_ManaLithData.CalculateEntityCount());

                });
                */
                Entities.With(m_CircleData).ForEach((MeshColor meshColor) =>
                {

                    if (meshColor.LerpColor != meshColor.Color)
                        meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

                    Color emissionColor = meshColor.LerpColor * meshColor.EmissionMultiplier;
                    meshColor.MeshRenderer.material.color = emissionColor;

                    ParticleSystem pPs = meshColor.ParticleSystem;
                    var mainModule = pPs.main;

                    if (mainModule.startColor.color != meshColor.LerpColor)
                        mainModule.startColor = meshColor.LerpColor;
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
                        //Debug.Log((float)li / meshGradientColor.colors.Length);
                        //meshGradientColor.colors[li] = Color.Lerp(emissiveColor1, emissiveColor2, (float)li / meshGradientColor.colors.Length);
                        meshGradientColor.colors[li] = meshGradientColor.Gradient.Evaluate((float)li / meshGradientColor.colors.Length) * meshGradientColor.EmissionMultiplier;
                        /*
                        if (li < 3)
                        {
                            meshGradientColor.colors[li] = emissiveColor1;
                        }
                        else if(li > meshGradientColor.colors.Length - 4)
                        {
                            meshGradientColor.colors[li] = emissiveColor2;
                        }
                        else
                        {
                            if (li % 2 == 0)
                            {
                                meshGradientColor.colors[li] = Color.Lerp(emissiveColor1, emissiveColor2, (float)li);
                            }
                            else
                            {
                                meshGradientColor.colors[li] = meshGradientColor.colors[li - 1];
                            }

                        }
                                        */
                    }

                    meshGradientColor.mesh.colors = meshGradientColor.colors;
                    //}
                });


            }
        }
    }
}