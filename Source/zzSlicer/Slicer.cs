using System;
using System.Collections.Generic;
using System.Linq;
using QuantumConcepts.Formats.StereoLithography;

public struct Facet
{
    public Vector3F[] Vertices;
    public Vector3F Normal;
    public float z_angle; //between 0 and pi/4 (0 to 90 degrees)

    public Facet(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, float v3x, float v3y, float v3z)
    {
        Vertices = new Vector3F[] { new Vector3F(v1x, v1y, v1z), new Vector3F(v2x, v2y, v2z), new Vector3F(v3x, v3y, v3z) };
        Normal = Vector3F.Normal(Vertices);
        z_angle = Vector3F.AngleBetween(Vector3F.UnitZ, Normal);
        if (z_angle > (float)Math.PI / 2) z_angle = (float)Math.PI - z_angle;
    }
}

public class Mesh
{
    public Facet[] Facets;
    public float xmin;
    public float xmax;
    public float ymin;
    public float ymax;
    public float zmin;
    public float zmax;

    public Mesh(string stl_file)
    {
        STLDocument stldoc = STLDocument.Open(stl_file);

        Facets = new Facet[stldoc.Facets.Count];
        for (int i = 0; i < stldoc.Facets.Count; i++)
        {
            Facets[i] = new Facet(stldoc.Facets[i].Vertices[0].X, stldoc.Facets[i].Vertices[0].Y, stldoc.Facets[i].Vertices[0].Z
                , stldoc.Facets[i].Vertices[1].X, stldoc.Facets[i].Vertices[1].Y, stldoc.Facets[i].Vertices[1].Z
                , stldoc.Facets[i].Vertices[2].X, stldoc.Facets[i].Vertices[2].Y, stldoc.Facets[i].Vertices[2].Z);
        }

        find_minmax();
    }

    private void find_minmax()
    {
        xmin = float.MaxValue;
        xmax = float.MinValue;
        ymin = float.MaxValue;
        ymax = float.MinValue;
        zmin = float.MaxValue;
        zmax = float.MinValue;
        foreach (Facet f in Facets)
        {
            foreach (Vector3F v in f.Vertices)
            {
                if (xmin > v.X) xmin = v.X;
                if (xmax < v.X) xmax = v.X;
                if (ymin > v.Y) ymin = v.Y;
                if (ymax < v.Y) ymax = v.Y;
                if (zmin > v.Z) zmin = v.Z;
                if (zmax < v.Z) zmax = v.Z;
            }
        }
    }

    public void Scale(float scale)
    {
        foreach (Facet f in Facets)
        {
            for (int i = 0; i < 3; i++)
            {
                f.Vertices[i].X *= scale;
                f.Vertices[i].Y *= scale;
                f.Vertices[i].Z *= scale;
            }
        }
        find_minmax();
    }

    public void Shift(float x, float y, float z)
    {
        foreach (Facet f in Facets)
        {
            for (int i = 0; i < 3; i++)
            {
                f.Vertices[i].X += x;
                f.Vertices[i].Y += y;
                f.Vertices[i].Z += z;
            }
        }
        find_minmax();
    }

    public void ShiftCenter()
    {
        Shift(-(xmin + xmax) / 2, -(ymin + ymax) / 2, -zmin);
    }

    public override string ToString()
    {
        return String.Format("Mesh dimensions X:{0:0.00}mm Y:{1:0.00}mm Z:{2:0.00}mm", xmax - xmin, ymax - ymin, zmax - zmin);
    }

}


public class Segment
{
    public Vector2F[] p;
    public Segment()
    {
        p = new Vector2F[2];
    }
    public Segment(Vector2F v1,Vector2F v2)
    {
        p = new Vector2F[] { v1, v2 };
    }
}

public class SegmentPath
{
    public LinkedList<Vector2F> p = new LinkedList<Vector2F>();

    public SegmentPath(Vector2F[] pnew)
    {
        foreach (Vector2F pn in pnew) p.AddLast(pn);
    }

    //reduce the number of point by removing points which are less than tol distance from line defined by point-1 and point+1
    public void Reduce(float tol)
    {
        float tol2 = tol * tol;

        LinkedListNode<Vector2F> vn = p.First.Next; //2nd node
        while (vn.Next != null) //not the last node
        {
            if (Vector2F.PointLineDistanceSquared(vn.Value, vn.Previous.Value, vn.Next.Value) < tol2)
            {
                LinkedListNode<Vector2F> vn_next_saved = vn.Next;
                p.Remove(vn);
                vn = vn_next_saved;
            }
            else
            {
                vn = vn.Next;
            }
        }

    }

    public void Reverse()
    {
        p = new LinkedList<Vector2F>(p.Reverse());
    }
}

public class Slice
{
    public List<SegmentPath> paths;
    public float z;

    public Slice(IList<Facet> facets, float z, float tol)
    {
        this.z = z;
        LinkedList<Segment> segments = slice_facets_to_segments(facets, z); //find segments for slice
        paths = slice_segments_to_paths(segments, tol); //combine segments to paths
        foreach (SegmentPath path in paths) path.Reduce(tol); //remove redundant points on path
    }

    //find segments for slice
    private LinkedList<Segment> slice_facets_to_segments(IList<Facet> facets, float z)
    {
        LinkedList<Segment> segments = new LinkedList<Segment>();
        foreach (Facet f in facets)
        {
            int a, b, c;
            for (a = 0; a < 3 && f.Vertices[a].Z > z; a++) ;
            if (a == 3) continue;  // all below
            for (b = 0; b < 3 && f.Vertices[b].Z <= z; b++) ;
            if (b == 3) continue;  // all above

            Segment s = new Segment();
            // Line from vertex a->b is one facet... find point. Note a<=z and b>z
            s.p[0].X = f.Vertices[a].X + (f.Vertices[b].X - f.Vertices[a].X) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);
            s.p[0].Y = f.Vertices[a].Y + (f.Vertices[b].Y - f.Vertices[a].Y) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);

            // Line a->c or c->b is another facet... find point
            for (c = 0; c == a || c == b; c++) ;
            if (f.Vertices[c].Z <= z) a = c; else b = c;
            s.p[1].X = f.Vertices[a].X + (f.Vertices[b].X - f.Vertices[a].X) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);
            s.p[1].Y = f.Vertices[a].Y + (f.Vertices[b].Y - f.Vertices[a].Y) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);

            if (s.p[0] == s.p[1]) continue; //ignore single points

            //QQQ
            //segments.AddLast(s);

            if (f.z_angle < (float)Math.PI / 4)
            {
                Vector2F Normal = Vector2F.Normal(s.p[0] - s.p[1]);
                float separation = 0.5f;
                Segment s1 = new Segment(new Vector2F(s.p[0] + (separation / 2) * Normal), new Vector2F(s.p[1] + (separation / 2) * Normal));
                segments.AddLast(s1);
                if(float.IsNaN(s1.p[0].X) || float.IsNaN(s.p[1].X))
                {
                    Console.WriteLine("EE");
                }
                Segment s2 = new Segment(new Vector2F(s.p[0] - (separation / 2) * Normal), new Vector2F(s.p[1] - (separation / 2) * Normal));
                segments.AddLast(s2);
            }
            else
            {
                segments.AddLast(s);
            }

        //QQQ

    }
        return segments;
    }

    //find segments for slice (Version 1)
    // - does not handle at all: horizontal planes, don't get printed at all
    // - does not handle well: 'nearly' horizontal planes, gets printed too thin
    private LinkedList<Segment> slice_facets_to_segments_v1(IList<Facet> facets, float z)
    {
        LinkedList<Segment> segments = new LinkedList<Segment>();
        foreach (Facet f in facets)
        {
            int a, b, c;
            for (a = 0; a < 3 && f.Vertices[a].Z > z; a++) ;
            if (a == 3) continue;  // all below
            for (b = 0; b < 3 && f.Vertices[b].Z <= z; b++) ;
            if (b == 3) continue;  // all above

            Segment s = new Segment();
            // Line from vertex a->b is one facet... find point. Note a<=z and b>z
            s.p[0].X = f.Vertices[a].X + (f.Vertices[b].X - f.Vertices[a].X) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);
            s.p[0].Y = f.Vertices[a].Y + (f.Vertices[b].Y - f.Vertices[a].Y) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);

            // Line a->c or c->b is another facet... find point
            for (c = 0; c == a || c == b; c++) ;
            if (f.Vertices[c].Z <= z) a = c; else b = c;
            s.p[1].X = f.Vertices[a].X + (f.Vertices[b].X - f.Vertices[a].X) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);
            s.p[1].Y = f.Vertices[a].Y + (f.Vertices[b].Y - f.Vertices[a].Y) * (z - f.Vertices[a].Z) / (f.Vertices[b].Z - f.Vertices[a].Z);

            if (s.p[0] == s.p[1]) continue; //ignore single points

            segments.AddLast(s);
        }
        return segments;
    }

    //combine segments to paths
    private List<SegmentPath> slice_segments_to_paths(LinkedList<Segment> segments, float tol = 1e-10f)
    {
        //combine segments into polygons
        float tol2 = tol * tol;

        List<SegmentPath> pols = new List<SegmentPath>();

        while (segments.Count > 0)
        {
            SegmentPath pol = new SegmentPath(segments.First.Value.p);
            pols.Add(pol);
            segments.Remove(segments.First);
            Vector2F v1 = pol.p.First.Value;
            Vector2F v2 = pol.p.Last.Value;
            LinkedListNode<Segment> sn = segments.First;
            while (sn != null)
            {
                if (Vector2F.DistanceSquared(v1, sn.Value.p[0]) < tol2)
                {
                    pol.p.AddFirst(sn.Value.p[1]);
                    segments.Remove(sn);
                    v1 = pol.p.First.Value;
                    sn = segments.First;
                    continue;
                }
                if (Vector2F.DistanceSquared(v1, sn.Value.p[1]) < tol2)
                {
                    pol.p.AddFirst(sn.Value.p[0]);
                    segments.Remove(sn);
                    v1 = pol.p.First.Value;
                    sn = segments.First;
                    continue;
                }
                if (Vector2F.DistanceSquared(v2, sn.Value.p[0]) < tol2)
                {
                    pol.p.AddLast(sn.Value.p[1]);
                    segments.Remove(sn);
                    v2 = pol.p.Last.Value;
                    sn = segments.First;
                    continue;
                }
                if (Vector2F.DistanceSquared(v2, sn.Value.p[1]) < tol2)
                {
                    pol.p.AddLast(sn.Value.p[0]);
                    segments.Remove(sn);
                    v2 = pol.p.Last.Value;
                    sn = segments.First;
                    continue;
                }
                sn = sn.Next;
            }
        }
        return pols;
    }

    //find closest endpoint to point
    public void find_closest_endpoint(Vector2F point, int start_idx, out int best_idx, out bool best_reverse)
    {
        float best_val = float.MaxValue;
        best_idx = 0;
        best_reverse = false;
        for (int i = start_idx; i < paths.Count; i++)
        {
            if (best_val > Vector2F.DistanceSquared(point, paths[i].p.First.Value))
            {
                best_val = Vector2F.DistanceSquared(point, paths[i].p.First.Value);
                best_idx = i;
                best_reverse = false;
            }
            if (best_val > Vector2F.DistanceSquared(point, paths[i].p.Last.Value))
            {
                best_val = Vector2F.DistanceSquared(point, paths[i].p.Last.Value);
                best_idx = i;
                best_reverse = true;
            }
        }
    }

    //Optimize print head travel by reordering/reversing paths
    public void OptimizePaths(ref Vector2F current_head_pos)
    {
        int best_i;
        bool best_reverse;
        for (int i = 0; i < paths.Count; i++)
        {
            find_closest_endpoint(current_head_pos, i, out best_i, out best_reverse);
            SegmentPath best_p = paths[best_i];
            if (best_reverse) best_p.Reverse();
            paths[best_i] = paths[i];
            paths[i] = best_p;
            current_head_pos = best_p.p.Last.Value;
        }
    }
}

public class Slices
{
    public List<Slice> slices;
    public Mesh mesh;
    public float zstep;
    public float tol;

    public Slices(Mesh mesh, float zstep, float tol)
    {
        this.mesh = mesh;
        Slice(zstep, tol);
    }

    public void Slice(float zstep, float tol)
    {
        this.zstep = zstep;
        this.tol = tol;
        slices = new List<Slice>();
        for (float z = mesh.zmin; z <= mesh.zmax; z += zstep)
        {
            slices.Add(new Slice(mesh.Facets, z, tol));
        }
        Vector2F current_head_pos = new Vector2F(0, 0);
        OptimizePaths(ref current_head_pos);
    }

    //Optimize print head travel by reordering/reversing paths
    public void OptimizePaths(ref Vector2F current_head_pos)
    {
        foreach (Slice s in slices) s.OptimizePaths(ref current_head_pos);
    }
}


