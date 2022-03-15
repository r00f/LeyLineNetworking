// =====================================================
// DO NOT EDIT - this file is automatically regenerated.
// =====================================================

using System.Linq;
using Improbable.Gdk.Core;
using UnityEngine;
using System;

namespace Generic
{
    [global::System.Serializable]
    public struct Vector2f : IEquatable<Vector2f>
    {
        public float X;
        public float Y;

        public Vector2f(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) =>
            (obj is Vector2f metrics) && Equals(metrics);

        public bool Equals(Vector2f other) =>
            (X, Y) == (other.X, other.Y);

        public override int GetHashCode() =>
             (X, Y).GetHashCode();

        public static class Serialization
        {
            public static void Serialize(Vector2f instance, global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                {
                    obj.AddFloat(1, instance.X);
                }

                {
                    obj.AddFloat(2, instance.Y);
                }
            }

            public static Vector2f Deserialize(global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                var instance = new Vector2f();

                {
                    instance.X = obj.GetFloat(1);
                }

                {
                    instance.Y = obj.GetFloat(2);
                }

                return instance;
            }
        }
    }
}
