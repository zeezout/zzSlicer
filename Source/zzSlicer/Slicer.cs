using System;
using System.Collections.Generic;
using System.Linq;
using QuantumConcepts.Formats.StereoLithography;
using System.Diagnostics;

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

    public float zmin()
    {
        float zmin = Vertices[0].Z;
        if (zmin > Vertices[1].Z) zmin = Vertices[1].Z;
        if (zmin > Vertices[2].Z) zmin = Vertices[2].Z;
        return zmin;
    }

    public float zmax()
    {
        float zmax = Vertices[0].Z;
        if (zmax < Vertices[1].Z) zmax = Vertices[1].Z;
        if (zmax < Vertices[2].Z) zmax = Vertices[2].Z;
        return zmax;
    }

    //intersect facet with horizontal plane at z
    public bool IntersectHorizontalPlane(float z, ref Segment s)
    { 
            int a, b, c;
            for (a = 0; a< 3 && Vertices[a].Z> z; a++) ;
            if (a == 3) return false;  // all below
            for (b = 0; b< 3 && Vertices[b].Z <= z; b++) ;
            if (b == 3) return false;  // all above

            s = new Segment();
            // Line from vertex a->b is one facet... find point. Note a<=z and b>z
            s.p[0].X = Vertices[a].X + (Vertices[b].X - Vertices[a].X) * (z - Vertices[a].Z) / (Vertices[b].Z - Vertices[a].Z);
            s.p[0].Y = Vertices[a].Y + (Vertices[b].Y - Vertices[a].Y) * (z - Vertices[a].Z) / (Vertices[b].Z - Vertices[a].Z);

            // Line a->c or c->b is another facet... find point
            for (c = 0; c == a || c == b; c++) ;
            if (Vertices[c].Z <= z) a = c; else b = c;
            s.p[1].X = Vertices[a].X + (Vertices[b].X - Vertices[a].X) * (z - Vertices[a].Z) / (Vertices[b].Z - Vertices[a].Z);
            s.p[1].Y = Vertices[a].Y + (Vertices[b].Y - Vertices[a].Y) * (z - Vertices[a].Z) / (Vertices[b].Z - Vertices[a].Z);

            if (s.p[0] == s.p[1]) return false; //ignore single points
        return true;
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

    public SegmentPath(Vector2F p0, Vector2F p1)
    {
        p.AddLast(p0);
        p.AddLast(p1);
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
    public float z;
    public List<Segment> segments;
    public List<SegmentPath> paths;

    public Slice( float z)
    {
        this.z = z;
        segments = new List<Segment>();
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
    public float z_angle = (float)Math.PI / 4; //print two lines when angle between normal and z-axis is smaller than z_angle

    public Slices(Mesh mesh, float zstep, float tol, float z_angle)
    {
        this.mesh = mesh;
        this.zstep = zstep;
        this.tol = tol;
        this.z_angle = z_angle;
        Slicer();
    }

    public int LineSegmentCount()
    {
        int cnt = 0;
        foreach (Slice s in slices) foreach (SegmentPath p in s.paths) cnt += p.p.Count - 1;
        return cnt;
    }

    public void Slicer()
    {
        Stopwatch sw = Stopwatch.StartNew();
        slices = new List<Slice>();
        for (float z = mesh.zmin; z <= mesh.zmax; z += zstep)
        {
            slices.Add(new Slice(z));
        }
        Segment seg = null;
        foreach (Facet f in mesh.Facets)
        {
            int layer_start = (int)((f.zmin()-mesh.zmin) / zstep);
            int layer_end = (int)((f.zmax() - mesh.zmin) / zstep);
            for (int layer = layer_start; layer <= layer_end; layer++)
            {
                float z = mesh.zmin + zstep * layer;
                if (f.IntersectHorizontalPlane(z,ref seg))
                {
                    slices[layer].segments.Add(seg);
                }
            }
        }
        Console.WriteLine("slicer: {0} ms", sw.ElapsedMilliseconds); sw.Restart();

        foreach (Slice slc in slices)
        {
            slc.paths = segments_to_paths__sorted_list(slc.segments, tol); //combine segments to paths
        }
        Console.WriteLine("segments_to_paths__sorted_list: {0} ms", sw.ElapsedMilliseconds); sw.Restart();

        //foreach (Slice slc in slices)
        //{
        //    slc.paths = segments_to_paths__tiles(slc.segments, tol); //combine segments to paths
        //}
        //Console.WriteLine("segments_to_paths__tiles: {0} ms", sw.ElapsedMilliseconds); sw.Restart();

        //foreach (Slice slc in slices)
        //{
        //    slc.paths = segments_to_paths__tiles2(slc.segments, tol); //combine segments to paths
        //}
        //Console.WriteLine("segments_to_paths__tiles2: {0} ms", sw.ElapsedMilliseconds); sw.Restart();

        foreach (Slice slc in slices)
        {
            foreach (SegmentPath path in slc.paths) path.Reduce(tol); //remove redundant points on path
        }
        Console.WriteLine("path.Reduce: {0} ms", sw.ElapsedMilliseconds); sw.Restart();

        Vector2F current_head_pos = new Vector2F(0, 0);
        OptimizePaths(ref current_head_pos);
        Console.WriteLine("optimize paths: {0} ms", sw.ElapsedMilliseconds); sw.Restart();
    }

    #region Segment to Paths using Sorted List Algorithm
    private class SegmentPoint:IComparable<SegmentPoint>
    {
        public Vector2F point;
        public SegmentPoint otherSegmentPoint;
        public SegmentPoint next;
        public SegmentPoint prev;
        public bool alive = true;
        public float X;

        public SegmentPoint(Vector2F point, SegmentPoint otherSegmentPoint)
        {
            this.point = point;
            this.otherSegmentPoint = otherSegmentPoint;
            this.X = point.X;
        }

        public int CompareTo(SegmentPoint other)
        {
            return Math.Sign(this.point.X - other.point.X);
        }
    }

    public static List<SegmentPath> segments_to_paths__sorted_list(List<Segment> segments, float distMax = 1e-10f)
    {
        List<SegmentPath> paths = new List<SegmentPath>();

        //create a linked list of points sorted by x coordinate
        List<SegmentPoint> segmentPoints = new List<SegmentPoint>();
        foreach (Segment s in segments)
        {
            SegmentPoint sp0 = new SegmentPoint(s.p[0],null);
            SegmentPoint sp1 = new SegmentPoint(s.p[1], sp0);
            sp0.otherSegmentPoint = sp1;
            segmentPoints.Add(sp0);
            segmentPoints.Add(sp1);
        }
        segmentPoints.Sort();
        for (int i=0;i<segmentPoints.Count-1;i++)
        {
            segmentPoints[i].next = segmentPoints[i + 1];
        }
        for (int i = 1; i < segmentPoints.Count; i++)
        {
            segmentPoints[i].prev = segmentPoints[i - 1];
        }

        //process each segment
        foreach (SegmentPoint spMainLoop in segmentPoints)
        {
            //only process segments that are alive
            if (!spMainLoop.alive) continue;

            //start a new path with this segment
            spMainLoop.alive = false;
            spMainLoop.otherSegmentPoint.alive = false;
            SegmentPath path = new SegmentPath(spMainLoop.point,spMainLoop.otherSegmentPoint.point);
            paths.Add(path);

            //append/prepend points to path
            SegmentPoint spPath; //the current path point to append/prepend to
            SegmentPoint spTry; //the point that is examined to be appended/prepended
            bool found;
            //find points to APPEND to path     
            spPath = spMainLoop.otherSegmentPoint; //APPEND (last point of path)
            do
            {
                found = false;
                //go FORWARD through the sorted list
                spTry = spPath.next; //FORWARD
                while (spTry != null && spTry.X - spPath.X <= distMax) //FORWARD try.x >= path.x
                {
                    if (spTry.alive && Vector2F.DistanceManhattan(spPath.point, spTry.point) <= distMax)
                    {
                        spTry.alive = false;
                        spTry.otherSegmentPoint.alive = false;
                        spPath = spTry.otherSegmentPoint;
                        path.p.AddLast(spPath.point); //APPEND                    
                        found = true;
                        break;
                    }
                    else
                    {
                        spTry = spTry.next; //FORWARD
                    }
                }
                //if nothing found, then go BACKWARD through the sorted list
                if (!found)
                {
                    spTry = spPath.prev; //BACKWARD 
                    while (spTry != null && spPath.X - spTry.X <= distMax) //BACKWARD path.x >= try.x
                    {
                        if (spTry.alive && Vector2F.DistanceManhattan(spPath.point, spTry.point) <= distMax)
                        {
                            spTry.alive = false;
                            spTry.otherSegmentPoint.alive = false;
                            spPath = spTry.otherSegmentPoint;
                            path.p.AddLast(spPath.point); //APPEND
                            found = true;
                            break;
                        }
                        else
                        {
                            spTry = spTry.prev; //BACKWARD
                        }
                    }
                }
            } while (found);

            //find points to PREPEND to path
            spPath = spMainLoop; //PREPEND (first point of path)
            do
            {
                found = false;
                //go FORWARD through the sorted list
                spTry = spPath.next; //FORWARD
                while (spTry != null && spTry.X - spPath.X <= distMax) //FORWARD try.x >= path.x
                {
                    if (spTry.alive && Vector2F.DistanceManhattan(spPath.point, spTry.point) <= distMax)
                    {
                        spTry.alive = false;
                        spTry.otherSegmentPoint.alive = false;
                        spPath = spTry.otherSegmentPoint;
                        path.p.AddFirst(spPath.point); //PREPEND
                        found = true;
                        break;
                    }
                    else
                    {
                        spTry = spTry.next; //FORWARD
                    }
                }
                //if nothing found, then go BACKWARD through the sorted list
                if (!found)
                {
                    spTry = spPath.prev; //BACKWARD
                    while (spTry != null && spPath.X - spTry.X <= distMax) //BACKWARD path.x >= try.x
                    {
                        if (spTry.alive && Vector2F.DistanceManhattan(spPath.point, spTry.point) <= distMax)
                        {
                            spTry.alive = false;
                            spTry.otherSegmentPoint.alive = false;
                            spPath = spTry.otherSegmentPoint;
                            path.p.AddFirst(spPath.point); //PREPEND
                            found = true;
                            break;
                        }
                        else
                        {
                            spTry = spTry.prev; //BACKWARD
                        }
                    }
                }
            } while (found);
        }

        return paths;
    }
    #endregion
    #region Segment to Paths using Tiled Algorithm
    private class SegmentTilePoint
    {
        public int seg_i;
        public Vector2F p;
        public SegmentTilePoint othersegidx;
        public bool alive = true;
        public SegmentTilePoint(int seg_i, Vector2F p, SegmentTilePoint othersegidx)
        {
            this.seg_i = seg_i;
            this.p = p;
            this.othersegidx = othersegidx;
        }
    }

    //combine segments to paths
    //algorithm: make a tiled index with the endpoints of the segments
    //for each segment search in the tile and neighboring tiles for matches
    public static List<SegmentPath> segments_to_paths__tiles(List<Segment> segments, float distMax = 1e-10f)
    {
        List<SegmentPath> paths = new List<SegmentPath>();

        //create SegmentTiles
        int tileLen = 30; //number of tiles per row/column
        Segment[] segs = segments.ToArray();
        float xmin = float.MaxValue;
        float ymin = float.MaxValue;
        float xmax = float.MinValue;
        float ymax = float.MinValue;
        foreach (Segment s in segs)
        {
            foreach (Vector2F v in s.p)
            {
                if (xmin > v.X) xmin = v.X;
                if (xmax < v.X) xmax = v.X;
                if (ymin > v.Y) ymin = v.Y;
                if (ymax < v.Y) ymax = v.Y;
            }
        }
        float xstep = (xmax - xmin)/ tileLen;
        float ystep = (ymax - ymin)/ tileLen;  
        List<SegmentTilePoint>[] segTiles = new List<SegmentTilePoint>[(tileLen+2)* (tileLen+2)]; //add extra row/column around perimeter
        for (int i = 0; i < (tileLen + 2) * (tileLen + 2); i++) segTiles[i] = new List<SegmentTilePoint>();
        SegmentTilePoint[] sidx = new SegmentTilePoint[2];
        for (int seg_i= 0;seg_i< segs.Length;seg_i++)
        {
            Segment s = segs[seg_i];
            for (int j = 0; j < 2; j++)
            {
                int idx_x = (int)((s.p[j].X - xmin) / xstep);
                int idx_y = (int)((s.p[j].Y - ymin) / ystep);
                int idx = (idx_y + 1) * tileLen + (idx_x + 1);
                sidx[j] = new SegmentTilePoint(seg_i, s.p[j], null);
                segTiles[idx].Add(sidx[j]);
            }
            sidx[0].othersegidx = sidx[1];
            sidx[1].othersegidx = sidx[0];
        }

        //array near defines the tile offsets to: current tile, left-above, above, right-above, left, right, left-below, below, right-below
        int[] near = new int[] { 0, -tileLen - 1, -tileLen, -tileLen + 1, -1, 1, tileLen - 1, tileLen, tileLen + 1 };

        //process each segment
        for(int seg_i = 0; seg_i < segs.Length; seg_i++)
        {
            Segment seg = segs[seg_i];
            if (seg == null) continue;
            segs[seg_i] = null;
            SegmentPath path = new SegmentPath(seg.p);
            paths.Add(path);
            //find poins that match the end of the path
            bool pointadded;
            do
            {
                pointadded = false;
                Vector2F pathendpoint = path.p.Last.Value;
                int idx_x = (int)((pathendpoint.X - xmin) / xstep);
                int idx_y = (int)((pathendpoint.Y - ymin) / ystep);
                int idx = (idx_y + 1) * tileLen + (idx_x + 1);
                for (int near_i = 0; !pointadded && near_i < 9; near_i++)
                {
                    List<SegmentTilePoint> sgidx = segTiles[idx + near[near_i]];
                    foreach (SegmentTilePoint si in sgidx)
                    {
                        if (si.alive && Vector2F.DistanceManhattan(pathendpoint, si.p) < distMax)
                        {
                            path.p.AddLast(si.othersegidx.p);
                            si.alive = false;
                            si.othersegidx.alive = false;
                            segs[si.seg_i] = null;
                            pointadded = true;
                            break;
                        }
                    }
                }
            } while (pointadded);
            //find poins that match the beginning of the path
            do
            {
                pointadded = false;
                Vector2F pathbeginpoint = path.p.First.Value;
                int idx_x = (int)((pathbeginpoint.X - xmin) / xstep);
                int idx_y = (int)((pathbeginpoint.Y - ymin) / ystep);
                int idx = (idx_y + 1) * tileLen + (idx_x + 1);
                for (int near_i = 0; !pointadded && near_i < 9; near_i++)
                {
                    List<SegmentTilePoint> sgidx = segTiles[idx + near[near_i]];
                    foreach (SegmentTilePoint si in sgidx)
                    {
                        if (si.alive && Vector2F.DistanceManhattan(pathbeginpoint, si.p) < distMax)
                        {
                            path.p.AddFirst(si.othersegidx.p);
                            si.alive = false;
                            si.othersegidx.alive = false;
                            segs[si.seg_i] = null;
                            pointadded = true;
                            break;
                        }
                    }
                }
            } while (pointadded);
        }
        return paths;
    }


    //combine segments to paths
    //algorithm: make a tiled index with the endpoints of the segments
    //for each segment search in the tile and neighboring tiles for matches
    //only examine neighboring tiles if the tile is within distMax from current point
    public static List<SegmentPath> segments_to_paths__tiles2(List<Segment> segments, float distMax = 1e-10f)
    {
        List<SegmentPath> paths = new List<SegmentPath>();

        //create SegmentTiles
        int tileLen = 30; //number of tiles per row/column
        Segment[] segs = segments.ToArray();
        float xmin = float.MaxValue;
        float ymin = float.MaxValue;
        float xmax = float.MinValue;
        float ymax = float.MinValue;
        foreach (Segment s in segs)
        {
            foreach (Vector2F v in s.p)
            {
                if (xmin > v.X) xmin = v.X;
                if (xmax < v.X) xmax = v.X;
                if (ymin > v.Y) ymin = v.Y;
                if (ymax < v.Y) ymax = v.Y;
            }
        }
        float xstep = (xmax - xmin) / tileLen;
        float ystep = (ymax - ymin) / tileLen;
        List<SegmentTilePoint>[] segTiles = new List<SegmentTilePoint>[(tileLen + 2) * (tileLen + 2)]; //add extra row/column around perimeter
        for (int i = 0; i < (tileLen + 2) * (tileLen + 2); i++) segTiles[i] = new List<SegmentTilePoint>();
        SegmentTilePoint[] sidx = new SegmentTilePoint[2];
        for (int seg_i = 0; seg_i < segs.Length; seg_i++)
        {
            Segment s = segs[seg_i];
            for (int j = 0; j < 2; j++)
            {
                int idx_x = (int)((s.p[j].X - xmin) / xstep);
                int idx_y = (int)((s.p[j].Y - ymin) / ystep);
                int idx = (idx_y + 1) * tileLen + (idx_x + 1);
                sidx[j] = new SegmentTilePoint(seg_i, s.p[j], null);
                segTiles[idx].Add(sidx[j]);
            }
            sidx[0].othersegidx = sidx[1];
            sidx[1].othersegidx = sidx[0];
        }

        //array near defines the tile offsets to: current tile, left-above, above, right-above, left, right, left-below, below, right-below
        int[] near = new int[] { 0, -tileLen - 1, -tileLen, -tileLen + 1, -1, 1, tileLen - 1, tileLen, tileLen + 1 };

        //process each segment
        for (int seg_i = 0; seg_i < segs.Length; seg_i++)
        {
            Segment seg = segs[seg_i];
            if (seg == null) continue;
            segs[seg_i] = null;
            SegmentPath path = new SegmentPath(seg.p);
            paths.Add(path);
            //find poins that match the end of the path
            bool pointadded;
            do
            {
                pointadded = false;
                Vector2F pathendpoint = path.p.Last.Value;
                int idx_x = (int)((pathendpoint.X - xmin) / xstep);
                int idx_y = (int)((pathendpoint.Y - ymin) / ystep);
                int idx = (idx_y + 1) * tileLen + (idx_x + 1);
                float x0 = xmin + idx_x * xstep;
                float x1 = xmin + (idx_x+1) * xstep;
                float y0 = ymin + idx_y * ystep;
                float y1 = xmin + (idx_y+1) * ystep;
                for (int near_i = 0; !pointadded && near_i < 9; near_i++)
                {
                    //check if need to examine tile for possible matches
                    switch(near_i)
                    {
                        case 0:break;
                        case 1: if (pathendpoint.X - distMax > x0 && pathendpoint.Y - distMax > y0) continue; break; //left-above
                        case 2: if (                                 pathendpoint.Y - distMax > y0) continue; break; //above
                        case 3: if (pathendpoint.X + distMax < x1 && pathendpoint.Y - distMax > y0) continue; break; //right-above
                        case 4: if (pathendpoint.X - distMax > x0                                 ) continue; break; //left
                        case 5: if (pathendpoint.X + distMax < x1                                 ) continue; break; //right
                        case 6: if (pathendpoint.X - distMax > x0 && pathendpoint.Y + distMax < y1) continue; break; //left-below
                        case 7: if (                                 pathendpoint.Y + distMax < y1) continue; break; //below
                        case 8: if (pathendpoint.X + distMax < x1 && pathendpoint.Y + distMax < y1) continue; break; //right-below
                    }
                    List<SegmentTilePoint> sgidx = segTiles[idx + near[near_i]];
                    foreach (SegmentTilePoint si in sgidx)
                    {
                        if (si.alive && Vector2F.DistanceManhattan(pathendpoint, si.p) < distMax)
                        {
                            path.p.AddLast(si.othersegidx.p);
                            si.alive = false;
                            si.othersegidx.alive = false;
                            segs[si.seg_i] = null;
                            pointadded = true;
                            break;
                        }
                    }
                }
            } while (pointadded);
            //find poins that match the beginning of the path
            do
            {
                pointadded = false;
                Vector2F pathbeginpoint = path.p.First.Value;
                int idx_x = (int)((pathbeginpoint.X - xmin) / xstep);
                int idx_y = (int)((pathbeginpoint.Y - ymin) / ystep);
                int idx = (idx_y + 1) * tileLen + (idx_x + 1);
                float x0 = xmin + idx_x * xstep;
                float x1 = xmin + (idx_x + 1) * xstep;
                float y0 = ymin + idx_y * ystep;
                float y1 = xmin + (idx_y + 1) * ystep;
                for (int near_i = 0; !pointadded && near_i < 9; near_i++)
                {
                    //check if need to examine tile for possible matches
                    switch (near_i)
                    {
                        case 0: break;
                        case 1: if (pathbeginpoint.X - distMax > x0 && pathbeginpoint.Y - distMax > y0) continue; break; //left-above
                        case 2: if (                                   pathbeginpoint.Y - distMax > y0) continue; break; //above
                        case 3: if (pathbeginpoint.X + distMax < x1 && pathbeginpoint.Y - distMax > y0) continue; break; //right-above
                        case 4: if (pathbeginpoint.X - distMax > x0                                   ) continue; break; //left
                        case 5: if (pathbeginpoint.X + distMax < x1                                   ) continue; break; //right
                        case 6: if (pathbeginpoint.X - distMax > x0 && pathbeginpoint.Y + distMax < y1) continue; break; //left-below
                        case 7: if (                                   pathbeginpoint.Y + distMax < y1) continue; break; //below
                        case 8: if (pathbeginpoint.X + distMax < x1 && pathbeginpoint.Y + distMax < y1) continue; break; //right-below
                    }
                    List<SegmentTilePoint> sgidx = segTiles[idx + near[near_i]];
                    foreach (SegmentTilePoint si in sgidx)
                    {
                        if (si.alive && Vector2F.DistanceManhattan(pathbeginpoint, si.p) < distMax)
                        {
                            path.p.AddFirst(si.othersegidx.p);
                            si.alive = false;
                            si.othersegidx.alive = false;
                            segs[si.seg_i] = null;
                            pointadded = true;
                            break;
                        }
                    }
                }
            } while (pointadded);
        }
        return paths;
    }


    #endregion


    //Optimize print head travel by reordering/reversing paths
    public void OptimizePaths(ref Vector2F current_head_pos)
    {
        foreach (Slice s in slices) s.OptimizePaths(ref current_head_pos);
    }
}


