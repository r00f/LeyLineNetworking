using UnityEngine;
using System.Collections;
using Generic;
using Unity.Mathematics;

public static class Vector3fext
{
    /// <summary>
    ///     Converts a Unity vector to a Spatial Vector3f.
    /// </summary>
    public static Vector3f FromUnityVector(Vector3 unityVector)
    {
        return new Vector3f(unityVector.x, unityVector.y, unityVector.z);
    }

    /// <summary>
    ///     Converts the Vector3f to a Unity Vector3.
    /// </summary>
    public static Vector3 ToUnityVector(Vector3f inVector)
    {
        return new Vector3(inVector.X, inVector.Y, inVector.Z);
    }

    public static Vector3f ToVector3f(float3 inVector)
    {
        return new Vector3f(inVector.x, inVector.y, inVector.z);
    }

    public static float3 ToFloat3(Vector3f inVector)
    {
        return new float3(inVector.X, inVector.Y, inVector.Z);
    }

}



