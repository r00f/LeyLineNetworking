using UnityEngine;
using System.Collections;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using LeyLineHybridECS;
using Unity.Entities;
using Cell;
using Generic;
using Unit;
using System.Collections.Generic;

public class ActionEffectsSystem : ComponentSystem
{
    public struct CellData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public readonly ComponentDataArray<MarkerState> MarkerStateData;
        public ComponentArray<MarkerGameObjects> MarkerGameObjects;
    }

    [Inject] CellData m_CellData;

    public struct UnitData
    {
        public readonly int Length;
        public readonly ComponentDataArray<SpatialEntityId> EntityIds;
        public readonly ComponentDataArray<WorldIndex.Component> WorldIndexData;
        public readonly ComponentDataArray<Actions.Component> ActionsData;
        public readonly ComponentArray<Unit_BaseDataSet> BaseDataSets;
        public readonly ComponentDataArray<CubeCoordinate.Component> Coords;
        public ComponentArray<AnimatorComponent> AnimatorComponents;
    }

    [Inject] UnitData m_UnitData;

    protected override void OnUpdate()
    {


        
    }

    public void TriggerActionEffect(EffectTypeEnum inEffectType, HashSet<Vector3f> inCubeCoordinates)
    {

        foreach (Vector3f v in inCubeCoordinates)
        {
            Debug.Log("Activate Cells with Effect: " + v + ", " + inEffectType);
        }


        for (int i = 0; i < m_UnitData.Length; i++)
        {
            var coord = m_UnitData.Coords[i].CubeCoordinate;
            var animatorComp = m_UnitData.AnimatorComponents[i];

            if (inCubeCoordinates.Contains(animatorComp.LastStationaryCoordinate))
            {
                animatorComp.ActionEffectTrigger = true;
            }
        }

        //set effectType / instanciate particle effect on cell if action requires it
        for(int i = 0; i < m_CellData.Length; i++)
        {
            var coord = m_CellData.Coords[i].CubeCoordinate;
            var references = m_CellData.MarkerGameObjects[i];

            if(inCubeCoordinates.Contains(coord))
            {
                references.EffectType = inEffectType;
            }
        }

    }

    public void LaunchProjectile()
    {


    }

}
