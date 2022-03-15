// =====================================================
// DO NOT EDIT - this file is automatically regenerated.
// =====================================================

using System;
using System.Linq;
using Improbable.Gdk.Core;
using UnityEngine;


namespace Generic
{
    [global::System.Serializable]
    public struct MapCell: IEquatable<MapCell>
    {
        public global::Generic.Vector2i AxialCoordinate;
        public global::Generic.Vector3f Position;
        public uint MovementCost;
        public bool IsTaken;
        public long UnitOnCellId;
        public uint MapCellColorIndex;

        public MapCell(global::Generic.Vector2i axialCoordinate, global::Generic.Vector3f position, uint movementCost, bool isTaken, long unitOnCellId, uint mapCellColorIndex)
        {
            AxialCoordinate = axialCoordinate;
            Position = position;
            MovementCost = movementCost;
            IsTaken = isTaken;
            UnitOnCellId = unitOnCellId;
            MapCellColorIndex = mapCellColorIndex;
        }

        public override bool Equals(object obj) =>
            (obj is MapCell metrics) && Equals(metrics);

        public bool Equals(MapCell other) =>
            (AxialCoordinate.X, AxialCoordinate.Y) == (other.AxialCoordinate.X, other.AxialCoordinate.Y);

        public override int GetHashCode() =>
             (AxialCoordinate.X, AxialCoordinate.Y).GetHashCode();

        public static class Serialization
        {
            public static void Serialize(MapCell instance, global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                {
                    global::Generic.Vector2i.Serialization.Serialize(instance.AxialCoordinate, obj.AddObject(1));
                }

                {
                    global::Generic.Vector3f.Serialization.Serialize(instance.Position, obj.AddObject(2));
                }

                {
                    obj.AddUint32(3, instance.MovementCost);
                }

                {
                    obj.AddBool(4, instance.IsTaken);
                }

                {
                    obj.AddInt64(5, instance.UnitOnCellId);
                }

                {
                    obj.AddUint32(6, instance.MapCellColorIndex);
                }
            }

            public static MapCell Deserialize(global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                var instance = new MapCell();

                {
                    instance.AxialCoordinate = global::Generic.Vector2i.Serialization.Deserialize(obj.GetObject(1));
                }

                {
                    instance.Position = global::Generic.Vector3f.Serialization.Deserialize(obj.GetObject(2));
                }

                {
                    instance.MovementCost = obj.GetUint32(3);
                }

                {
                    instance.IsTaken = obj.GetBool(4);
                }

                {
                    instance.UnitOnCellId = obj.GetInt64(5);
                }

                {
                    instance.MapCellColorIndex = obj.GetUint32(6);
                }

                return instance;
            }
        }
    }
}
