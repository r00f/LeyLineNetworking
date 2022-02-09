// =====================================================
// DO NOT EDIT - this file is automatically regenerated.
// =====================================================

using System;
using System.Linq;
using Cell;
using Improbable.Gdk.Core;
using UnityEngine;

namespace Generic
{
    [global::System.Serializable]
    public struct Vector3f : IEquatable<Vector3f>
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override bool Equals(object obj) =>
            (obj is Vector3f metrics) && Equals(metrics);

        public bool Equals(Vector3f other) =>
            (X, Y, Z) == (other.X, other.Y, other.Z);

        public override int GetHashCode() =>
             (X,Y, Z).GetHashCode();


        public static class Serialization
        {
            public static void Serialize(Vector3f instance, global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                {
                    obj.AddFloat(1, instance.X);
                }

                {
                    obj.AddFloat(2, instance.Y);
                }

                {
                    obj.AddFloat(3, instance.Z);
                }
            }

            public static Vector3f Deserialize(global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                var instance = new Vector3f();

                {
                    instance.X = obj.GetFloat(1);
                }

                {
                    instance.Y = obj.GetFloat(2);
                }

                {
                    instance.Z = obj.GetFloat(3);
                }

                return instance;
            }
        }
    }
}
