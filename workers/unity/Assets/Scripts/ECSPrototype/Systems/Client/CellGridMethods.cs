using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Generic;

public static class CellGridMethods
{
    public static Vector3f PosToCube(Vector2 point)
    {
        var q = (2f / 3 * point.x);
        var r = (-1f / 3 * point.x + Mathf.Sqrt(3) / 3 * point.y);
        return CubeRound(AxialToCube(new Vector2(q, r)));
    }

    public static Vector3 CubeToPos(Vector3f cubeCoord, Vector2f mapCenter)
    {
        Vector2 axial = CubeToAxial(cubeCoord);

        var x = (3f / 2 * axial.x);
        var z = (Mathf.Sqrt(3) / 2 * axial.x + Mathf.Sqrt(3) * axial.y);

        //factor in mapCenter before returning
        return new Vector3(mapCenter.X + x, 5, mapCenter.Y + z);
    }

    public static Vector3f[] DirectionsArray = new Vector3f[]{
          new Vector3f(+1, -1, 0), new Vector3f(+1, 0, -1), new Vector3f(0, +1, -1),
            new Vector3f(-1, +1, 0), new Vector3f(-1, 0, +1), new Vector3f(0, -1, +1)
    };

    public static Vector2 CubeToAxial(Vector3f cube)
    {
        return new Vector2(cube.X, cube.Y);
    }

    public static Vector3f AxialToCube(Vector2 axial)
    {
        return new Vector3f(axial.x, axial.y, -axial.x - axial.y);
    }

    //size equals width of a hexagon / 2
    /*
    public Vector2 CubeCoordToXZ(Vector3f coord)
    {
        Vector2 axial = CubeToAxial(coord);
        var x = 1.5f * (3 / 2 * axial.x);
        var y = 1.73f * ((axial.x * 0.5f) + axial.y);

        //center cell + coordinate offset = XZ coordinate in world space - offset X by (worldindex - 1) * 100?
        return new Vector2(50, 55.22f) + new Vector2(x, y);
    }
    */

    public static Vector3f CubeDirection(uint direction)
    {
        if (direction < 6)
            return DirectionsArray[direction];
        else
            return new Vector3f();
    }

    public static Vector3f CubeNeighbour(Vector3f origin, uint direction)
    {
        var cubeDirection = CubeDirection(direction);
        return new Vector3f(origin.X + cubeDirection.X, origin.Y + cubeDirection.Y, origin.Z + cubeDirection.Z);
    }

    public static Vector3f CubeScale(Vector3f direction, uint scale)
    {
        return new Vector3f(direction.X * scale, direction.Y * scale, direction.Z * scale);
    }

    public static Vector3f CoordinateDirection(Vector3f origin, Vector3f destination)
    {
        return new Vector3f(destination.X - origin.X, destination.Y - origin.Y, destination.Z - origin.Z);
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

    public static int GetDistance(Vector3f originCubeCoordinate, Vector3f otherCubeCoordinate)
    {
        int distance = (int)(Mathf.Abs(originCubeCoordinate.X - otherCubeCoordinate.X) + Mathf.Abs(originCubeCoordinate.Y - otherCubeCoordinate.Y) + Mathf.Abs(originCubeCoordinate.Z - otherCubeCoordinate.Z)) / 2;
        return distance;
    }

    public static float GetAngle(Vector3f originPos, Vector3f targetPos)
    {
        Vector3f dir = CoordinateDirection(originPos, targetPos);
        float Angle = Mathf.Atan2(dir.X, dir.Z) * Mathf.Rad2Deg;
        return Angle;
    }

    public static float LineLerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public static Vector3f CubeLerp(Vector3f a, Vector3f b, float t)
    {
        return CubeRound(new Vector3f(LineLerp(a.X, b.X, t), LineLerp(a.Y, b.Y, t), LineLerp(a.Z, b.Z, t)));
    }

    public static List<Vector3f> LineDraw(Vector3f origin, Vector3f destination)
    {
        List<Vector3f> line = new List<Vector3f>();
        var n = GetDistance(origin, destination);

        //nudge destination
        destination = new Vector3f(destination.X + 0.000001f, destination.Y + 0.000002f, destination.Z + 0.000003f);

        for (int i = 0; i <= n; i++)
        {
            line.Add(CubeLerp(origin, destination, 1f / n * i));
        }

        return line;
    }

    public static Vector3f CubeRound(Vector3f cubeFloat)
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

}
