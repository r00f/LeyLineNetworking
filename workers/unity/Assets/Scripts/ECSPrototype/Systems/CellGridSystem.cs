using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace LeyLineHybridECS
{

    public class CellGridSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentArray<Cell> CellData;
            public readonly ComponentDataArray<GridCoordinates> OffsetCoordinateData;
        }

        [Inject] Data m_Data;

        protected override void OnUpdate()
        {
            //throw new NotImplementedException();
        }

        public readonly float3[] _directions =  {
        new float3(+1, -1, 0), new float3(+1, 0, -1), new float3(0, +1, -1),
        new float3(-1, +1, 0), new float3(-1, 0, +1), new float3(0, -1, +1)};


        public int GetDistance(float3 originCubeCoordinate, float3 otherCubeCoordinate)
        {
            int distance = (int)(Mathf.Abs(originCubeCoordinate.x - otherCubeCoordinate.x) + Mathf.Abs(originCubeCoordinate.y - otherCubeCoordinate.y) + Mathf.Abs(originCubeCoordinate.z - otherCubeCoordinate.z)) / 2;
            return distance;
        }//Distance is given using Manhattan Norm.


        public List<Cell> GetRadius(float3 originCellCubeCoordinate, int radius)
        {
            var neighbours = new List<Cell>();

            for(int i = 0; i < m_Data.Length; i++)
            {
                var cell = m_Data.CellData[i];

                float3 cubeCoordinate = m_Data.OffsetCoordinateData[i].CubeCoordinate;

                if (GetDistance(originCellCubeCoordinate, cubeCoordinate) < radius)
                    neighbours.Add(cell);
            }

            return neighbours;
        }

        public float GetAngles(Cell origin, Cell target)
        {
            Vector3 originPos = origin.GetComponent<Position3DDataComponent>().Value.Value;
            Vector3 targetPos = target.GetComponent<Position3DDataComponent>().Value.Value;
            Vector3 dir = targetPos - originPos;
            //dir = Target.transform.InverseTransformDirection(dir);
            float Angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            //Debug.Log(Angle);
            return Angle;
        }

    }


}

