using UnityEngine;
using System.Collections;
using Unity.Entities;
using Improbable.Gdk.Core;

public class InitializeUnitsSystem : ComponentSystem
{

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<NewlyAddedSpatialOSEntity> NewEntity;
        public readonly ComponentDataArray<Generic.FactionComponent.Component> FactionData;
        public ComponentArray<TeamColorMeshes> TeamColorMeshesData;
    }

    [Inject] private UnitData m_UnitData;

    protected override void OnUpdate()
    {
        for(int i = 0; i < m_UnitData.Length; i++)
        {
            var factionComp = m_UnitData.FactionData[i];
            var teamColorMeshes = m_UnitData.TeamColorMeshesData[i];

            foreach(MeshRenderer r in teamColorMeshes.meshRenderers)
            {
                switch(factionComp.TeamColor)
                {
                    case Generic.TeamColorEnum.blue:
                        r.material.color = Color.blue;
                        break;
                    case Generic.TeamColorEnum.red:
                        r.material.color = Color.red;
                        break;
                }
            }
        }
    }
}
