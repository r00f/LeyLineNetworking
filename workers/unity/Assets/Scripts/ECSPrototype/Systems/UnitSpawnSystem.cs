using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace LeyLineHybridECS
{
    public class UnitSpawnData
    {
        public GameObject UnitToSpawn;
        public float3 Position;
        public Cell Cell;
    }

    public static class UnitSpawnSystem
    {
        static Transform unitParent;


        public static void SpawnUnit(UnitSpawnData data)
        {
            if (!unitParent)
                unitParent = GameObject.FindGameObjectWithTag("UnitsParent").transform;

            var newUnit = Object.Instantiate(data.UnitToSpawn, unitParent);
            newUnit.GetComponent<OccupiedCell>().Cell = data.Cell;
            data.Cell.GetComponent<UnitOnCell>().Value = newUnit.GetComponent<Unit>();
            newUnit.GetComponent<Position3DDataComponent>().Value = new Position3D
            {
                Value = data.Position
            };
            data.Cell.GetComponent<IsTaken>().Value = true;
            foreach(Renderer r in newUnit.GetComponent<PlayerColor>().PlayerColorMeshes)
            {
                Color playerColor = newUnit.GetComponent<PlayerColor>().Color;
                r.material.color = playerColor;
            }
        }
    }
}
