using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;

public class InitializeUnitsSystem : ComponentSystem
{

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentArray<Transform> Transforms;
        public readonly ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
        public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        public ComponentArray<TeamColorMeshes> TeamColorMeshesData;
    }

    [Inject] private UnitData m_UnitData;

    public struct PlayerData
    {
        public readonly int Length;
        public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        public ComponentArray<HeroTransform> HeroTransforms;
    }

    [Inject] private PlayerData m_PlayerData;

    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var unitFactionComp = m_UnitData.FactionData[i];
            var teamColorMeshes = m_UnitData.TeamColorMeshesData[i];
            var unitTransform = m_UnitData.Transforms[i];

            foreach(MeshRenderer r in teamColorMeshes.meshRenderers)
            {
                switch(unitFactionComp.TeamColor)
                {
                    case Generic.TeamColorEnum.blue:
                        r.material.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, r.material.color.a);
                        break;
                    case Generic.TeamColorEnum.red:
                        r.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, r.material.color.a);
                        break;
                }
            }

            //Debug.Log(m_PlayerData.Length);

            for(int pi = 0; pi < m_PlayerData.Length; pi++)
            {

                var playerFactionComp = m_PlayerData.FactionData[pi];
                var heroTransform = m_PlayerData.HeroTransforms[pi];

                if(playerFactionComp.Faction == unitFactionComp.Faction && heroTransform.Transform == null)
                {
                    heroTransform.Transform = unitTransform;
                }

            }
        }
    }
}
