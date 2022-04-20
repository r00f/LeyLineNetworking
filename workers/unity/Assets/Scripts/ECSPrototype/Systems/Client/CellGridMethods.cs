using UnityEngine;
using System.Collections.Generic;
using Generic;
using Unity.Collections;
using Unity.Mathematics;

public static class CellGridMethods
{
    public static Vector3f PosToCube(Vector2 point)
    {
        var q = 2f / 3 * point.x;
        var r = -1f / 3 * point.x + Mathf.Sqrt(3) / 3 * point.y;

        if (IsCoordinateWithinMapBounds(CubeRound(AxialToCube(new Vector2f(q, r)))))
            return CubeRound(AxialToCube(new Vector2f(q, r)));
        else
            return new Vector3f(0, 0, 0);
    }

    public static Vector3f PosToCorner(Vector3f center, int i, float yOffset = 0)
    {
        var angle_deg = 60 * i;
        var angle_rad = Mathf.PI / 180 * angle_deg;
        return new Vector3f(center.X + Mathf.Cos(angle_rad), center.Y + yOffset, center.Z + Mathf.Sin(angle_rad));
    }

    public static Vector3f PosToCorner(Vector3 center, int i, float yOffset = 0)
    {
        var angle_deg = 60 * i;
        var angle_rad = Mathf.PI / 180 * angle_deg;
        return new Vector3f(center.x + Mathf.Cos(angle_rad), center.y + yOffset, center.z + Mathf.Sin(angle_rad));
    }

    public static Vector3 CubeToPos(Vector3f cubeCoord, Vector2f mapCenter)
    {
        Vector2i axial = CubeToAxial(cubeCoord);

        var x = (3f / 2 * axial.X);
        var z = (Mathf.Sqrt(3) / 2 * axial.X + Mathf.Sqrt(3) * axial.Y);

        //factor in mapCenter before returning
        return new Vector3(mapCenter.X + x, 3.1f, mapCenter.Y + z);
    }

    public static Vector3f[] DirectionsArray = new Vector3f[]{
          new Vector3f(+1, -1, 0), new Vector3f(+1, 0, -1), new Vector3f(0, +1, -1),
            new Vector3f(-1, +1, 0), new Vector3f(-1, 0, +1), new Vector3f(0, -1, +1)
    };

    public static Vector2i CubeToAxial(Vector3f cube)
    {
        return new Vector2i((int)cube.X, (int)cube.Y);
    }

    public static Vector2i CubeToAxial(float3 cube)
    {
        return new Vector2i((int) cube.x, (int) cube.y);
    }

    public static Vector3f AxialToCube(Vector2f axial)
    {
        return new Vector3f(axial.X, axial.Y, -axial.X - axial.Y);
    }

    public static Vector3f AxialToCube(Vector2i axial)
    {
        return new Vector3f(axial.X, axial.Y, -axial.X - axial.Y);
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

    public static KeyValuePair<Vector3f, Vector3f> CircleDegreeToCoords(Vector3f origin, uint degree)
    {
        var coords = new KeyValuePair<Vector3f, Vector3f>();

        switch (degree)
        {
            case (30):
                coords = new KeyValuePair<Vector3f, Vector3f>(CubeNeighbour(origin, 1), CubeNeighbour(origin, 2));
                break;
            case (90):
                coords = new KeyValuePair<Vector3f, Vector3f>(CubeNeighbour(origin, 0), CubeNeighbour(origin, 1));
                break;
            case (150):
                coords = new KeyValuePair<Vector3f, Vector3f>(CubeNeighbour(origin, 5), CubeNeighbour(origin, 0));
                break;
            case (210):
                coords = new KeyValuePair<Vector3f, Vector3f>(CubeNeighbour(origin, 4), CubeNeighbour(origin, 5));
                break;
            case (270):
                coords = new KeyValuePair<Vector3f, Vector3f>(CubeNeighbour(origin, 3), CubeNeighbour(origin, 4));
                break;
            case (330):
                coords = new KeyValuePair<Vector3f, Vector3f>(CubeNeighbour(origin, 2), CubeNeighbour(origin, 3));
                break;
        }

        return coords;
    }

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
        var coord = CubeAdd(origin, cubeScale);

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                ring.Add(coord);
                coord = CubeNeighbour(coord, (uint)i);
            }
        }

        return ring;
    }

    public static FixedList128<Vector3f> RingDrawFixed(Vector3f origin, uint radius)
    {
        var ring = new FixedList128<Vector3f>();
        var cubeScale = CubeScale(DirectionsArray[4], radius);
        var coord = CubeAdd(origin, cubeScale);

        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < radius; j++)
            {
                ring.Add(coord);
                coord = CubeNeighbour(coord, (uint) i);
            }
        }

        return ring;
    }

    public static Vector3f RotateRight(Vector3f origin)
    {
        return new Vector3f(-origin.Z, -origin.X, -origin.Y);
    }

    public static Vector3f RotateLeft(Vector3f origin)
    {
        return new Vector3f(-origin.Y, -origin.Z, -origin.X);
    }

    public static List<Vector3f> ConeDraw(Vector3f center, Vector3f target, uint radius, uint extent)
    {
        var cone = new List<Vector3f>();
        var coord = target;
        var halfExtent = (extent - 1) / 2;

        for (int i = 0; i <= halfExtent; i++)
        {
            cone.Add(coord);
            var direction = CoordinateDirection(center, coord);
            var rotatedDirection = RotateRight(direction);
            var rotatedCoord = CubeAdd(rotatedDirection, center);
            coord = rotatedCoord;
        }

        coord = target;

        for (int i = 0; i <= halfExtent; i++)
        {
            if(i != 0)
                cone.Add(coord);
            var direction = CoordinateDirection(center, coord);
            var rotatedDirectionL = RotateLeft(direction);
            var rotatedCoordL = CubeAdd(rotatedDirectionL, center);
            coord = rotatedCoordL;
        }

        return cone;
    }

    public static FixedList128<Vector3f> ConeDrawFixed(Vector3f center, Vector3f target, uint radius, uint extent)
    {

        var cone = new FixedList128<Vector3f>();
        var coord = target;
        var halfExtent = (extent - 1) / 2;

        for (int i = 0; i <= halfExtent; i++)
        {
            cone.Add(coord);
            var direction = CoordinateDirection(center, coord);
            var rotatedDirection = RotateRight(direction);
            var rotatedCoord = CubeAdd(rotatedDirection, center);
            coord = rotatedCoord;
        }

        coord = target;

        for (int i = 0; i <= halfExtent; i++)
        {
            if (i != 0)
                cone.Add(coord);
            var direction = CoordinateDirection(center, coord);
            var rotatedDirectionL = RotateLeft(direction);
            var rotatedCoordL = CubeAdd(rotatedDirectionL, center);
            coord = rotatedCoordL;
        }

        return cone;
    }

    public static Vector3f CubeAdd(Vector3f a, Vector3f b)
    {
        return new Vector3f(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
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

    public static List<Vector3f> LineDraw(List<Vector3f> line, Vector3f origin, Vector3f destination)
    {
        var n = GetDistance(origin, destination);

        //nudge destination
        destination = new Vector3f(destination.X + 0.000001f, destination.Y + 0.000002f, destination.Z + 0.000003f);

        for (int i = 0; i <= n; i++)
        {
            var resultingCoordinate = CubeLerp(origin, destination, 1f / n * i);

            if (IsCoordinateWithinMapBounds(resultingCoordinate))
                line.Add(resultingCoordinate);
        }

        return line;
    }

    public static FixedList128<Vector3f> LineDrawFixed(Vector3f origin, Vector3f destination)
    {
        var line = new FixedList128<Vector3f>();

        var n = GetDistance(origin, destination);

        //nudge destination
        destination = new Vector3f(destination.X + 0.000001f, destination.Y + 0.000002f, destination.Z + 0.000003f);

        for (int i = 0; i <= n; i++)
        {
            var resultingCoordinate = CubeLerp(origin, destination, 1f / n * i);

            if (IsCoordinateWithinMapBounds(resultingCoordinate))
                line.Add(resultingCoordinate);
        }

        return line;
    }

    public static List<Vector3f> LineDrawWhitoutOrigin(List<Vector3f> line, Vector3f origin, Vector3f destination)
    {
        var n = GetDistance(origin, destination);

        //nudge destination
        destination = new Vector3f(destination.X + 0.000001f, destination.Y + 0.000002f, destination.Z + 0.000003f);

        for (int i = 1; i <= n; i++)
        {
            var resultingCoordinate = CubeLerp(origin, destination, 1f / n * i);

            if (IsCoordinateWithinMapBounds(resultingCoordinate))
                line.Add(resultingCoordinate);
        }

        return line;
    }

    public static List<Vector3f> LineDrawWhithLength(List<Vector3f> line, Vector3f origin, Vector3f destination, uint length)
    {
        var dir = CoordinateDirection(origin, destination);
        var cubeScale = CubeScale(dir, length);
        var destinationCoord = CubeAdd(origin, cubeScale);

        var n = GetDistance(origin, destinationCoord);

        destinationCoord = new Vector3f(destinationCoord.X + 0.000001f, destinationCoord.Y + 0.000002f, destinationCoord.Z + 0.000003f);

        for (int i = 1; i <= n; i++)
        {
            var resultingCoordinate = CubeLerp(origin, destinationCoord, 1f / n * i);

            if (IsCoordinateWithinMapBounds(resultingCoordinate))
                line.Add(resultingCoordinate);
        }

        return line;
    }

    public static bool IsCoordinateWithinMapBounds(Vector3f coord)
    {
        if (Mathf.Abs(coord.X) <= 14 && Mathf.Abs(coord.Y) <= 14 && Mathf.Abs(coord.Z) <= 14)
            return true;
        else
            return false;
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
        var results = new List<Vector3f>
        {
            originCellCubeCoordinate
        };

        for (int x = (int)-radius; x <= radius; x++)
        {
            for (int y = (int)Mathf.Max(-radius, -x - (float)radius); y <= (int)Mathf.Min(radius, -x + radius); y++)
            {
                var z = -x - y;
                var resultingCoordinate = new Vector3f(originCellCubeCoordinate.X + x, originCellCubeCoordinate.Y + y, originCellCubeCoordinate.Z + z);

                if (IsCoordinateWithinMapBounds(resultingCoordinate))
                    results.Add(resultingCoordinate);
            }
        }
        return results;
    }

    public static FixedList128<Vector3f> CircleDrawFixed(Vector3f originCellCubeCoordinate, uint radius)
    {
        var results = new FixedList128<Vector3f>
        {
            originCellCubeCoordinate
        };

        for (int x = (int)-radius; x <= radius; x++)
        {
            for (int y = (int)Mathf.Max(-radius, -x - (float)radius); y <= (int)Mathf.Min(radius, -x + radius); y++)
            {
                var z = -x - y;
                var resultingCoordinate = new Vector3f(originCellCubeCoordinate.X + x, originCellCubeCoordinate.Y + y, originCellCubeCoordinate.Z + z);

                if (IsCoordinateWithinMapBounds(resultingCoordinate))
                    results.Add(resultingCoordinate);
            }
        }
        return results;
    }

    public static HashSet<Vector3f> CircleDrawHash(Vector3f originCellCubeCoordinate, uint radius)
    {
        var results = new HashSet<Vector3f>
        {
            originCellCubeCoordinate
        };

        for (int x = (int) -radius; x <= radius; x++)
        {
            for (int y = (int) Mathf.Max(-radius, -x - (float) radius); y <= (int) Mathf.Min(radius, -x + radius); y++)
            {
                var z = -x - y;
                if (Mathf.Abs(originCellCubeCoordinate.X + x) <= 14 && Mathf.Abs(originCellCubeCoordinate.Y + y) <= 14 && Mathf.Abs(originCellCubeCoordinate.Z + z) <= 14)
                    results.Add(new Vector3f(originCellCubeCoordinate.X + x, originCellCubeCoordinate.Y + y, originCellCubeCoordinate.Z + z));
            }
        }
        return results;
    }

    public static List<HexEdgePositionPair> SortEdgeByDistance(ref List<HexEdgePositionPair> edgeList)
    {
        List<HexEdgePositionPair> output = new List<HexEdgePositionPair>
            {
                edgeList[NearestEdge(new Vector3(), edgeList)]
            };

        edgeList.Remove(output[0]);

        int x = 0;
        for (int i = 0; i < edgeList.Count + x; i++)
        {
            if (i >= 5)
            {
                double closestRemainingDistance = NearestEdgeDist(output[output.Count - 1].B, edgeList);
                double firstLastAddedDistance = Vector2.Distance(new Vector2(output[0].B.x, output[0].B.z), new Vector2(output[output.Count - 1].A.x, output[output.Count - 1].A.z));

                //offset firstLastDistance so closing shapes is prefered when closestRemaining and firstLastAddedDist are the same
                if (closestRemainingDistance > firstLastAddedDistance -.01)
                {
                    return output;
                }
            }

            output.Add(edgeList[NearestEdge(output[output.Count - 1].B, edgeList)]);
            edgeList.Remove(output[output.Count - 1]);
            x++;
        }

        return output;
    }

    static int NearestEdge(Vector3 srcPos, List<HexEdgePositionPair> lookIn)
    {
        KeyValuePair<double, int> distanceListIndex = new KeyValuePair<double, int>();
        for (int i = 0; i < lookIn.Count; i++)
        {
            double distance = Vector2.Distance(new Vector2(srcPos.x, srcPos.z), new Vector2(lookIn[i].A.x, lookIn[i].A.z));
            if (i == 0)
            {
                distanceListIndex = new KeyValuePair<double, int>(distance, i);
            }
            else
            {
                if (distance < distanceListIndex.Key)
                {
                    distanceListIndex = new KeyValuePair<double, int>(distance, i);
                }
            }
        }
        return distanceListIndex.Value;
    }

    static double NearestEdgeDist(Vector3 srcEdge, List<HexEdgePositionPair> lookIn)
    {
        KeyValuePair<double, int> distanceListIndex = new KeyValuePair<double, int>();
        for (int i = 0; i < lookIn.Count; i++)
        {
            double distance = Vector2.Distance(new Vector2(srcEdge.x, srcEdge.z), new Vector2(lookIn[i].A.x, lookIn[i].A.z));
            if (i == 0)
            {
                distanceListIndex = new KeyValuePair<double, int>(distance, i);
            }
            else
            {
                if (distance < distanceListIndex.Key)
                {
                    distanceListIndex = new KeyValuePair<double, int>(distance, i);
                }
            }
        }
        return distanceListIndex.Key;
    }
}
