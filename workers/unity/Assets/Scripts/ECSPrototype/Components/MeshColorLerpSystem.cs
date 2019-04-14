using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;

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

        [Inject] private CircleData m_CircleData;

        public struct LineData
        {
            public readonly int Length;
            public ComponentArray<MeshGradientColor> MeshGradientColorData;
        }

        [Inject] private LineData m_LineData;

        private struct ProjectorAddedData
        {
            public readonly int Length;
            public ComponentArray<Transform> Transforms;
            public readonly ComponentArray<Projector> Projectors;
        }

        [Inject] private ProjectorAddedData m_ProjectorAddedData;


        private ComponentGroup manalithGroup;
        private ComponentGroup playerGroup;
        private ComponentGroup gameControllerGroup;

        protected override void OnCreateManager()
        {
            
            base.OnCreateManager();

            playerGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<Authoritative<Player.PlayerState.Component>>(),
                ComponentType.Create<Generic.WorldIndex.Component>()

            );

            gameControllerGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<Generic.GameState.Component>(),
                ComponentType.Create<Generic.WorldIndex.Component>(),
                ComponentType.Create<Improbable.Position.Component>()

            );

            //var Manager = World.Active.GetExistingManager<EntityManager>();
            manalithGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<Generic.FactionComponent.Component>(),
                ComponentType.Create<Cells.CircleCells.Component>(),
                ComponentType.Create<Improbable.Position.Component>()
            );

        }

        protected override void OnUpdate()
        {
            if(m_ProjectorAddedData.Length != 0 && playerGroup.GetEntityArray().Length != 0)
            {
                var playerWorldIndex = playerGroup.GetComponentDataArray<Generic.WorldIndex.Component>()[0].Value;

                for (int i = 0; i < gameControllerGroup.GetEntityArray().Length; i++)
                {
                    var gameControllerWorldIndex = gameControllerGroup.GetComponentDataArray<Generic.WorldIndex.Component>()[i].Value;
                    var position = gameControllerGroup.GetComponentDataArray<Improbable.Position.Component>()[i].Coords.ToUnityVector();

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
                    var faction = manalithGroup.GetComponentDataArray<Generic.FactionComponent.Component>()[i];
                    var position = manalithGroup.GetComponentDataArray<Improbable.Position.Component>()[i].Coords.ToUnityVector().sqrMagnitude;

                    if (position == circlePos)
                    {
                        switch (faction.Faction)
                        {
                            case 0:
                                circleColor = Color.yellow;
                                break;
                            case 1:
                                circleColor = Color.blue;
                                break;
                            case 2:
                                circleColor = Color.red;
                                break;
                        }
                        m_CircleData.MeshColorData[ci].Color = circleColor;
                    }
                }
            }

            for (int i = 0; i < m_CircleData.Length; i++)
            {
                //Debug.Log("??");
                var meshColor = m_CircleData.MeshColorData[i];

                if (meshColor.LerpColor != meshColor.Color)
                    meshColor.LerpColor = Color.Lerp(meshColor.LerpColor, meshColor.Color, 0.05f);

                if (meshColor.MeshRenderer.material.color != meshColor.LerpColor)
                    meshColor.MeshRenderer.material.color = meshColor.LerpColor;

                ParticleSystem pPs = meshColor.ParticleSystem;
                var mainModule = pPs.main;

                if (mainModule.startColor.color != meshColor.LerpColor)
                    mainModule.startColor = meshColor.LerpColor;
            }

            for (int i = 0; i < m_LineData.Length; i++)
            {
                var meshGradientColor = m_LineData.MeshGradientColorData[i];


                if (meshGradientColor.ManalithColor.LerpColor != meshGradientColor.ManalithColor.Color || meshGradientColor.ConnectedManalithColor.LerpColor != meshGradientColor.ConnectedManalithColor.Color)
                {
                    // Instead if vertex.y we use uv.x
                    for (int li = 0; li < meshGradientColor.uv.Length; li++)
                    {
                        meshGradientColor.colors[li] = Color.Lerp(meshGradientColor.ManalithColor.LerpColor, meshGradientColor.ConnectedManalithColor.LerpColor, meshGradientColor.uv[li].x / meshGradientColor.uv[meshGradientColor.uv.Length - 1].x);
                    }

                    meshGradientColor.mesh.colors = meshGradientColor.colors;
                }
            }
        }
    }
}