using System;
using System.Collections.Generic;


public struct Polygon
{
    public Vector2F[] points;

    public Vector2F min;
    public Vector2F max;

    public Polygon(Vector2F[] points)
    {
        this.points = points;
        min = new Vector2F(float.MaxValue, float.MaxValue);
        max = new Vector2F(float.MinValue, float.MinValue);
    }

    public void calc_minmax()
    {
        min = new Vector2F(float.MaxValue, float.MaxValue);
        max = new Vector2F(float.MinValue, float.MinValue);
        foreach (Vector2F v in points)
        {
            if (min.X > v.X) min.X = v.X;
            if (min.Y > v.Y) min.Y = v.Y;
            if (max.X < v.X) max.X = v.X;
            if (max.Y < v.Y) max.Y = v.Y;
        }
    }

    public static Polygon load_airfoid(string filename)
    {
        string[] lines = System.IO.File.ReadAllLines(filename);
        List<Vector2F> points = new List<Vector2F>();
        float[] xy = new float[3];
        foreach (string line in System.IO.File.ReadAllLines(filename))
        {
            int cnt = 0;
            foreach (string word in line.Split(' '))
            {
                if (cnt >= 3) break;
                if (float.TryParse(word, out xy[cnt])) cnt++;
            }
            if (cnt == 2)
            {
                points.Add(new Vector2F(xy[0], xy[1]));
            }
        }
        Polygon poly = new Polygon();
        poly.points = points.ToArray();
        return poly;
    }

    public void Multiply(float mult)
    {
        for (int i = 0; i < points.Length; i++) points[i] *= mult;
    }

    public void Add(Vector2F v)
    {
        for (int i = 0; i < points.Length; i++) points[i] += v;
    }

    public Polygon Clone()
    {
        Polygon poly = new Polygon();
        poly.points = new Vector2F[points.Length];
        for (int i = 0; i < points.Length; i++) poly.points[i] = new Vector2F(points[i].X, points[i].Y);
        return poly;
    }

    //return index of nearest point to vector v
    public int NearestIndex(Vector2F v)
    {
        int imin = -1;
        float dist2min = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            if (dist2min > Vector2F.DistanceSquared(v, points[i]))
            {
                dist2min = Vector2F.DistanceSquared(v, points[i]);
                imin = i;
            }
        }
        return imin;
    }

    //return nearest point to vector v
    public Vector2F NearestVector(Vector2F v)
    {
        return points[NearestIndex(v)];
    }

    //return index of furthest point from vector v
    public int FurthestIndex(Vector2F v)
    {
        int imax = -1;
        float dist2max = float.MinValue;
        for (int i = 0; i < points.Length; i++)
        {
            if (dist2max < Vector2F.DistanceSquared(v, points[i]))
            {
                dist2max = Vector2F.DistanceSquared(v, points[i]);
                imax = i;
            }
        }
        return imax;
    }

    //return furthest point from vector v
    public Vector2F FurthestVector(Vector2F v)
    {
        return points[FurthestIndex(v)];
    }

    //return polygon with intersection points between line segment p-p2 and polygon
    public Polygon IntersectSegment(Vector2F p, Vector2F p2)
    {
        Vector2F intersection;
        List<Vector2F> intersections = new List<Vector2F>();
        for (int i = 0; i < this.points.Length - 2; i++)
        {
            if (Vector2F.IntersectSegementSegment(p, p2, this.points[i], this.points[i + 1], out intersection))
            {
                intersections.Add(intersection);
            }
        }
        return new Polygon(intersections.ToArray());
    }
}

public struct Vector3F
{
    public float X;
    public float Y;
    public float Z;
    private const float Epsilon = 1e-6f; //float has 7 digit precision allow for 1 digit numerical instability
    public readonly static Vector3F Zero = new Vector3F(0, 0, 0);
    public readonly static Vector3F One = new Vector3F(1, 1, 1);
    public readonly static Vector3F UnitX = new Vector3F(1, 0, 0);
    public readonly static Vector3F UnitY = new Vector3F(0, 1, 0);
    public readonly static Vector3F UnitZ = new Vector3F(0, 0, 1);
    public Vector3F(float x, float y, float z) { X = x; Y = y; Z = z; }
    public float LengthSquared
    {
        get
        {
            return X * X + Y * Y + Z * Z;
        }
    }
    public float Length
    {
        get
        {
            return (float)Math.Sqrt((double)LengthSquared);
        }
    }
    public static Vector3F operator -(Vector3F v, Vector3F w)
    {
        return new Vector3F(v.X - w.X, v.Y - w.Y, v.Z - w.Z);
    }
    public static Vector3F operator +(Vector3F v, Vector3F w)
    {
        return new Vector3F(v.X + w.X, v.Y + w.Y, v.Z + w.Z);
    }
    public static float operator *(Vector3F v, Vector3F w)
    {
        return v.X * w.X + v.Y * w.Y + v.Z * w.Z;
    }
    public static Vector3F operator *(Vector3F v, float mult)
    {
        return new Vector3F(v.X * mult, v.Y * mult, v.Z * mult);
    }
    public static Vector3F operator *(float mult, Vector3F v)
    {
        return new Vector3F(v.X * mult, v.Y * mult, v.Z * mult);
    }
    public static Vector3F operator /(Vector3F v, float div)
    {
        return new Vector3F(v.X / div, v.Y / div, v.Z / div);
    }
    public static bool operator ==(Vector3F v, Vector3F w)
    {
        return IsEqual(v, w);
    }
    public static bool operator !=(Vector3F v, Vector3F w)
    {
        return !IsEqual(v, w);
    }
    public override bool Equals(object obj)
    {
        return IsEqual(this, (Vector3F)obj);
    }
    private static bool IsZeroFloat(float f)
    {
        return Math.Abs(f) < Epsilon;
    }
    private static bool IsEqualFloat(float f1, float f2)
    {
        if (f1 == f2) return true; //early exit
        return Math.Abs(f1 - f2) < Epsilon * (Math.Abs(f1) + Math.Abs(f2)) / 2;
    }
    private static bool IsEqual(Vector3F v, Vector3F w)
    {
        return IsEqualFloat(w.X, v.X) && IsEqualFloat(w.Y, v.Y) && IsEqualFloat(w.Z, v.Z);
    }
    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }
    public static Vector3F Cross(Vector3F v1, Vector3F v2)
    {
        return new Vector3F(v1.Y * v2.Z - v1.Z * v2.Y, v1.Z * v2.X - v1.X * v2.Z, v1.X * v2.Y - v1.Y * v2.X);
    }
    public static float Dot(Vector3F v1, Vector3F v2)
    {
        return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
    }
    public static Vector3F Normal(Vector3F vertix0, Vector3F vertix1, Vector3F vertix2)
    {
        return Normal(vertix0 - vertix1, vertix1 - vertix2);
    }
    public static Vector3F Normal(Vector3F[] Vertices)
    {
        return Normal(Vertices[0] - Vertices[1], Vertices[1] - Vertices[2]);
    }
    public static Vector3F Normal(Vector3F v1, Vector3F v2)
    {
        Vector3F v = Vector3F.Cross(v1, v2);
        return v / v.Length;
    }
    public static float AngleBetween(Vector3F v1, Vector3F v2)
    {
        return (float)Math.Acos((v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z) / Math.Sqrt((v1.X * v1.X + v1.Y * v1.Y + v1.Z * v1.Z) * (v2.X * v2.X + v2.Y * v2.Y + v2.Z * v2.Z)));
    }
    public static float Area(Vector3F v1, Vector3F v2)
    {
        return Vector3F.Cross(v1, v2).Length / 2;
    }
    public static float Area(Vector3F v1, Vector3F v2, Vector3F v3)
    {
        return Area(v1 - v2, v1 - v3);
    }
    public static float Area(Vector3F[] v)
    {
        return Area(v[0] - v[1], v[0] - v[2]);
    }
}


public struct Vector2F
{
    public float X;
    public float Y;
    private const float Epsilon = 1e-6f; //float has 7 digit precision allow for 1 digit numerical instability
    public readonly static Vector2F Zero = new Vector2F(0, 0);
    public readonly static Vector2F One = new Vector2F(1, 1);
    public readonly static Vector2F UnitX = new Vector2F(1, 0);
    public readonly static Vector2F UnitY = new Vector2F(0, 1);
    public Vector2F(float x, float y) { X = x; Y = y; }
    public Vector2F(Vector2F v) { X = v.X; Y = v.Y; }
    public float LengthSquared
    {
        get
        {
            return X * X + Y * Y;
        }
    }
    public float Length
    {
        get
        {
            return (float)Math.Sqrt((double)LengthSquared);
        }
    }
    public static float PointLineDistanceSquared(Vector2F point, Vector2F line1, Vector2F line2)
    {
        Vector2F line = line2 - line1;
        float a = line.Y * point.X - line.X * point.Y + line2.X * line1.Y - line2.Y * line1.X;
        return a * a / (line.Y * line.Y + line.X * line.X);
    }
    public static float PointLineDistance(Vector2F point, Vector2F line1, Vector2F line2)
    {
        return (float)Math.Sqrt((double)PointLineDistanceSquared(point, line1, line2));
    }
    public static Vector2F operator -(Vector2F v, Vector2F w)
    {
        return new Vector2F(v.X - w.X, v.Y - w.Y);
    }
    public static Vector2F operator +(Vector2F v, Vector2F w)
    {
        return new Vector2F(v.X + w.X, v.Y + w.Y);
    }
    public static float operator *(Vector2F v, Vector2F w)
    {
        return v.X * w.X + v.Y * w.Y;
    }
    public static Vector2F operator *(Vector2F v, float mult)
    {
        return new Vector2F(v.X * mult, v.Y * mult);
    }
    public static Vector2F operator *(float mult, Vector2F v)
    {
        return new Vector2F(v.X * mult, v.Y * mult);
    }
    public static Vector2F operator /(Vector2F v, float div)
    {
        return new Vector2F(v.X / div, v.Y / div);
    }
    public static bool operator ==(Vector2F v, Vector2F w)
    {
        return IsEqual(v,w);
    }
    public static bool operator !=(Vector2F v, Vector2F w)
    {
        return !IsEqual(v, w);
    }
    public override bool Equals(object obj)
    {
        return IsEqual(this, (Vector2F)obj);
    }
    private static bool IsZeroFloat(float f)
    {
        return Math.Abs(f) < Epsilon;
    }
    private static bool IsEqualFloat(float f1, float f2)
    {
        if (f1 == f2) return true; //early exit
        return Math.Abs(f1 - f2) < Epsilon * (Math.Abs(f1) + Math.Abs(f2)) / 2;
    }
    private static bool IsEqual(Vector2F v, Vector2F w)
    {
        return IsEqualFloat(w.X, v.X) && IsEqualFloat(w.Y, v.Y);
    }
    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }
    public static float Dot(Vector2F v1, Vector2F v2)
    {
        return v1.X * v2.X + v1.Y * v2.Y;
    }
    public float Cross(Vector2F v)
    {
        return X * v.Y - Y * v.X;
    }
    public static Vector2F Normal(Vector2F v)
    {
        return (new Vector2F(-v.Y, v.X)) / v.Length;
    }
    public static float DistanceSquared(Vector2F a, Vector2F b)
    {
        return (a - b).LengthSquared;
    }
    public static float Distance(Vector2F a, Vector2F b)
    {
        return (a - b).Length;
    }

    // Test whether two line segments intersect. If so, calculate the intersection point.
    // p Vector to the start point of p.</param>
    // p2 Vector to the end point of p.</param>
    // q Vector to the start point of q.</param>
    // q2 Vector to the end point of q.</param>
    // intersection The point of intersection, if any.
    // considerOverlapAsIntersect Do we consider overlapping lines as intersecting?
    // Returns True if an intersection point was found.
    public static bool IntersectSegementSegment(Vector2F p, Vector2F p2, Vector2F q, Vector2F q2, out Vector2F intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector2F();

        Vector2F r = p2 - p;
        Vector2F s = q2 - q;
        float rxs = r.Cross(s);
        float qpxr = (q - p).Cross(r);

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (IsZeroFloat(rxs) && IsZeroFloat(qpxr))
        {
            // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
            // then the two lines are overlapping,
            if (considerCollinearOverlapAsIntersect)
                if ((0 <= (q - p) * r && (q - p) * r <= r * r) || (0 <= (p - q) * s && (p - q) * s <= s * s))
                    return true;

            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (IsZeroFloat(rxs) && !IsZeroFloat(qpxr))
            return false;

        // t = (q - p) x s / (r x s)
        var t = (q - p).Cross(s) / rxs;

        // u = (q - p) x r / (r x s)

        var u = (q - p).Cross(r) / rxs;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!IsZeroFloat(rxs) && (0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = p + t * r;

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }

}

