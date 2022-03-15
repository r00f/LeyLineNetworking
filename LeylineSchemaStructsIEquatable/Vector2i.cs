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
    public struct Vector2i : IEquatable<Vector2i>
    {
        public int X;
        public int Y;

        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) =>
            (obj is Vector2i metrics) && Equals(metrics);

        public bool Equals(Vector2i other) =>
            (X, Y) == (other.X, other.Y);

        public override int GetHashCode() =>
             (X, Y).GetHashCode();


        public static class Serialization
        {
            public static void Serialize(Vector2i instance, global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                {
                    obj.AddSint32(1, instance.X);
                }

                {
                    obj.AddSint32(2, instance.Y);
                }
            }

            public static Vector2i Deserialize(global::Improbable.Worker.CInterop.SchemaObject obj)
            {
                var instance = new Vector2i();

                {
                    instance.X = obj.GetSint32(1);
                }

                {
                    instance.Y = obj.GetSint32(2);
                }

                return instance;
            }
        }
    }
}
