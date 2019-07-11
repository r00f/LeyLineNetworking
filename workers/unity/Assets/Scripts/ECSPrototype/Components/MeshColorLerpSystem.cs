using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable;
using Cell;
using Generic;
using Player;
using Improbable.Gdk.ReactiveComponents;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    [DisableAutoCreation]
    public class MeshColorLerpSystem : ComponentSystem
    {
        public struct CircleData
        {
            public readonly int Length;
            public ComponentArray<MeshColor> MeshColorData;
        }

        [Inject] CircleData m_CircleData;

        public struct LineData
        {
            public readonly int Length;
            public ComponentArray<MeshGradientColor> MeshGradientColorData;
        }

        [Inject] LineData m_LineData;

        private struct ProjectorAddedData
        {
            public readonly int Length;
            public ComponentArray<Transform> Transforms;
            public readonly ComponentArray<Projector> Projectors;
        }

        [Inject] ProjectorAddedData m_ProjectorAddedData;

        ComponentGroup manalithGroup;
        ComponentGroup playerGroup;
        ComponentGroup gameControllerGroup;

        Settings settings;

        protected override void OnCreateManager()
        {
            
            base.OnCreateManager();

            //var Stats = Resources.Load<GameObject>("Prefabs/UnityClient/" + unitToSpawn.UnitName).GetComponent<Unit_BaseDataSet>();
            settings = Resources.Load<Settings>("Settings");

            playerGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<Authoritative<PlayerState.Component>>(),
                ComponentType.Create<WorldIndex.Component>()

            );

            gameControllerGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<GameState.Component>(),
                ComponentType.Create<WorldIndex.Component>(),
                ComponentType.Create<Position.Component>()

            );

            //var Manager = World.Active.GetExistingManager<EntityManager>();
            manalithGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<FactionComponent.Component>(),
                ComponentType.Create<CircleCells.Component>(),
                ComponentType.Create<Position.Component>()
            );

        }

        protected override void OnUpdate()
        {
            if(m_ProjectorAddedData.Length != 0 && playerGroup.GetEntityArray().Length != 0)
            {
                var playerWorldIndex = playerGroup.GetComponentDataArray<WorldIndex.Component>()[0].Value;

                for (int i = 0; i < gameControllerGroup.GetEntityArray().Length; i++)
                {
                    var gameControllerWorldIndex = gameControllerGroup.GetComponentDataArray<WorldIndex.Component>()[i].Value;
                    var position = gameControllerGroup.GetComponentDataArray<Position.Component>()[i].Coords.ToUnityVector();

                    if (gameControllerWorldIndex == playerWorldIndex)
                    {

                        if (m_ProjectorAddedData.Transforms[0].position != position + new Vector3(0, 10, 50))
                        {
                            //Debug.Log("SetProjectorPosition");
                            m_ProjectorAddedData.Transforms[0].position = position + new Vector3(0, 10, 50);
                        }

                    }
                }
            }


            for (int ci = 0; ci < m_CircleData.Length; ci++)
            {
                var circlePos = m_CircleData.MeshColorData[ci].transform.position.sqrMagnitude;
                var circleColor = m_CircleData.MeshColorData[ci].Color;

                for (int i = 0; i < manalithGroup.GetEntityArray().Length; i++)
                {
                    var faction = manalithGroup.GetComponentDataArray<FactionComponent.Component>()[i];
                    var position = manalithGroup.GetComponentDataArray<Position.Component>()[i].Coords.ToUnityVector().sqrMagnitude;

                    if (position == circlePos)
                    {
                        circleColor = settings.FactionColors[(int)faction.Faction];

                        m_CircleData.MeshColorData[ci].Color = circleColor;
                    }
                }
            }

            for (int i = 0; i < m_CircleData.Length; i++)
            {
                var meshColor = m_CircleData.MeshColorData[i];

                if (meshColor.LerpColor != meshColor.Color)
                    meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

                Color emissionColor = meshColor.LerpColor * meshColor.EmissionMultiplier;
                meshColor.MeshRenderer.material.color = emissionColor;

                ParticleSystem pPs = meshColor.ParticleSystem;
                var mainModule = pPs.main;

                if (mainModule.startColor.color != meshColor.LerpColor)
                    mainModule.startColor = meshColor.LerpColor;
            }

            for (int i = 0; i < m_LineData.Length; i++)
            {
                var meshGradientColor = m_LineData.MeshGradientColorData[i];

                Color emissiveColor1 = meshGradientColor.ManalithColor.LerpColor * meshGradientColor.EmissionMultiplier;
                Color emissiveColor2 = meshGradientColor.ConnectedManalithColor.LerpColor * meshGradientColor.EmissionMultiplier;

                for (int li = 0; li < meshGradientColor.colors.Length; li++)
                {

                    if(li < 2)
                    {
                        meshGradientColor.colors[li] = emissiveColor1;
                    }
                    else if(li > meshGradientColor.colors.Length - 3)
                    {
                        meshGradientColor.colors[li] = emissiveColor2;
                    }
                    else
                    {
                        if (li % 2 == 0)
                        {
                            meshGradientColor.colors[li] = Color.Lerp(emissiveColor1, emissiveColor2, (float)li / (meshGradientColor.colors.Length - 3));
                        }
                        else
                        {
                            meshGradientColor.colors[li] = meshGradientColor.colors[li - 1];
                        }

                    }

                }

                meshGradientColor.mesh.colors = meshGradientColor.colors;
                //}
            }
        }
    }
}