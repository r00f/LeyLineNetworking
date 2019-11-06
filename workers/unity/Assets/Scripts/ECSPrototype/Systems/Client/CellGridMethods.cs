using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Generic;

public static class CellGridMethods
{
    static Vector3f[] DirectionsArray = new Vector3f[]{
          new Vector3f(+1, -1, 0), new Vector3f(+1, 0, -1), new Vector3f(0, +1, -1),
            new Vector3f(-1, +1, 0), new Vector3f(-1, 0, +1), new Vector3f(0, -1, +1)
    };

    static Vector3f CubeDirection(uint direction)
    {
        if (direction < 6)
            return DirectionsArray[direction];
        else
            return new Vector3f();
    }

    static int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }

    static Vector3f CubeRound(Vector3f cubeFloat)
    {

        var rx = Mathf.Round(cubeFloat.X);
        var ry = Mathf.Round(cubeFloat.Y);
        var rz = Mathf.Round(cubeFloat.Z);

        var x_diff = Mathf.Abs(rx - cubeFloat.X);
        var y_diff = Mathf.Abs(ry - cubeFloat.Y);
        var z_diff = Mathf.Abs(rz - cubeFloat.Z);

        if (x_diff > y_diff && x_diff > z_diff)
        {
            rx = -ry - rz;
        }
        else if (y_diff > z_diff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new Vector3f(rx, ry, rz);
    }

    static Vector3f CubeLerp(Vector3f a, Vector3f b, float t)
    {
        return CubeRound(new Vector3f(LineLerp(a.X, b.X, t), LineLerp(a.Y, b.Y, t), LineLerp(a.Z, b.Z, t)));
    }

    static float LineLerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    static Vector3f CubeNeighbour(Vector3f origin, uint direction)
    {
        var cubeDirection = CubeDirection(direction);
        return new Vector3f(origin.X + cubeDirection.X, origin.Y + cubeDirection.Y, origin.Z + cubeDirection.Z);
    }

    static Vector3f CubeScale(Vector3f direction, uint scale)
    {
        return new Vector3f(direction.X * scale, direction.Y * scale, direction.Z * scale);
    }

    public static List<Vector3f> LineDraw(Vector3f origin, Vector3f destination)
    {
        List<Vector3f> line = new List<Vector3f>();
        var n = GetDistance(origin, destination);

        //nudge destination
        //destination = new Vector3f(destination.X + 1e - 6f, destination.Y + 2e-6f, destination.Z + 3e-6f);

        for (int i = 0; i <= n; i++)
        {
            line.Add(CubeLerp(origin, destination, 1f / n * i));
        }

        return line;
    }

    public static List<Vector3f> CircleDraw(Vector3f originCellCubeCoordinate, uint radius)
    {
        var results = new List<Vector3f>();
        results.Add(originCellCubeCoordinate);

        for (int x = (int)(-radius); x <= radius; x++)
        {
            for (int y = (int)(Mathf.Max(-(float)radius, -x - (float)radius)); y <= (int)(Mathf.Min((float)radius, -x + radius)); y++)
            {
                var z = -x - y;
                results.Add(new Vector3f(originCellCubeCoordinate.X + x, originCellCubeCoordinate.Y + y, originCellCubeCoordinate.Z + z));
            }
        }
        return results;
    }

    public static List<Vector3f> RingDraw(Vector3f origin, uint radius)
    {
        var ring = new List<Vector3f>();
        var cubeScale = CubeScale(DirectionsArray[4], radius);
        //Debug.Log("OriginCoord = " + origin.X + ", " + origin.Y + ", " + origin.Z);
        //Debug.Log("CubeScale = " + cubeScale.X + ", " + cubeScale.Y + ", " + cubeScale.Z);
        var coord = new Vector3f(origin.X + cubeScale.X, origin.Y + cubeScale.Y, origin.Z + cubeScale.Z);

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                //Debug.Log("RingCoord = " + coord.X + coord.Y + coord.Z);
                ring.Add(coord);
                coord = CubeNeighbour(coord, (uint)i);
            }
        }

        //Debug.Log("RingCount = " + ring.Count);
        return ring;
    }

}
