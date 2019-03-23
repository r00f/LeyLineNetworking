﻿using UnityEngine;
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

        private ComponentGroup manalithGroup;

        protected override void OnCreateManager()
        {
            
            base.OnCreateManager();

            //var Manager = World.Active.GetExistingManager<EntityManager>();
            manalithGroup = Worlds.ClientWorld.CreateComponentGroup(

                ComponentType.Create<Generic.FactionComponent.Component>(),
                ComponentType.Create<Cells.CircleCells.Component>(),
                ComponentType.Create<Improbable.Position.Component>()
            );


        }

        protected override void OnUpdate()
        {
            for (int ci = 0; ci < m_CircleData.Length; ci++)
            {
                for (int i = 0; i < manalithGroup.GetEntityArray().Length; i++)
                {
                    var faction = manalithGroup.GetComponentDataArray<Generic.FactionComponent.Component>()[i];
                    var position = manalithGroup.GetComponentDataArray<Improbable.Position.Component>()[i];

                    var circlePos = m_CircleData.MeshColorData[ci].transform.position;

                    if (position.Coords.ToUnityVector() == circlePos)
                    {
                        var circleColor = m_CircleData.MeshColorData[ci].Color;

                        if(faction.Faction == 0)
                        {
                            circleColor = Color.yellow;
                        }
                        else
                        {
                            switch (faction.TeamColor)
                            {
                                case Generic.TeamColorEnum.blue:
                                    circleColor = Color.blue;
                                    break;
                                case Generic.TeamColorEnum.red:
                                    circleColor = Color.red;
                                    break;
                            }

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