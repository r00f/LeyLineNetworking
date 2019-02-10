using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Improbable.Gdk;
using Improbable;

namespace LeyLineHybridECS
{

    public class CellGridSystem : ComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public readonly ComponentArray<Cell> CellData;
           // public readonly ComponentDataArray<Cells.CellAttributes.Component> CellData;
        }

        [Inject] Data m_Data;

        protected override void OnUpdate()
        {
            //throw new NotImplementedException();
        }

        public readonly float3[] _directions =  {
        new float3(+1, -1, 0), new float3(+1, 0, -1), new float3(0, +1, -1),
        new float3(-1, +1, 0), new float3(-1, 0, +1), new float3(0, -1, +1)};


        public int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
        {
            int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
            return distance;
        }//Distance is given using Manhattan Norm.

        /*
        public List<Vector3f> GetRadius(Vector3f originCellCubeCoordinate, int radius)
        {
            //returns a list of offsetCoordinates
            var coordsInRadius = new List<Vector3f>();

            for(int i = 0; i < m_Data.Length; i++)
            {
                var cell = m_Data.CellData[i];

                Vector3f cubeCoordinate = m_Data.CoordinateData[i].CubeCoordinate;

                if (GetDistance(originCellCubeCoordinate, cubeCoordinate) < radius)
                    coordsInRadius.Add(m_Data.CoordinateData[i].CubeCoordinate);
            }

            return coordsInRadius;
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
        */
    }


}

