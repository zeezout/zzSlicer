using System;
using System.Collections.Generic;
using System.Drawing;


public class Visualize
{
    public int w;
    public int h;
    public float sc;
    public float x0;
    public float y0;
    public Image img;
    public Graphics g;

    public Color[] colors = new Color[] { Color.Red, Color.Green, Color.Blue, Color.Cyan, Color.Brown, Color.Magenta };


    public Visualize(int w, int h)
    {
        this.w = w;
        this.h = h;
        sc = 1;
        x0 = w / 2;
        y0 = h / 2;

        img = new Bitmap(w, h);
        g = Graphics.FromImage(img);
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
        g.Clear(Color.White);
    }

    ~Visualize()
    {
        g.Dispose();
    }

    public Image show_Slices(Slices slices, int xdiv, int ydiv)
    {
        int xstep = w / xdiv;
        int ystep = h / ydiv;
        x0 = xstep / 2;
        y0 = ystep / 2;

        //set scale
        float wmodel = slices.mesh.xmax - slices.mesh.xmin;
        float hmodel = slices.mesh.ymax - slices.mesh.ymin;
        float xscale = (float)xstep * 0.8f / wmodel;
        float yscale = (float)ystep * 0.8f / hmodel;
        sc = (xscale < yscale ? xscale : yscale);

        //set center
        float xcmodel = (slices.mesh.xmax + slices.mesh.xmin) / 2;
        float ycmodel = (slices.mesh.ymax + slices.mesh.ymin) / 2;
        x0 -= (int)(sc * xcmodel);
        y0 += (int)(sc * ycmodel);

        //last tool pos in pixel coord
        float xlast = 0;
        float ylast = 0;
        Pen pen_transfer = new Pen(Color.LightGray);

        bool suppress_tool_transfer = false;
        foreach (Slice slice in slices.slices)
        {
            //show tool transfer between layers
            if (slice.paths.Count > 0)
            {
                float xfirst = ImgX(slice.paths[0].p.First.Value.X);
                float yfirst = ImgY(slice.paths[0].p.First.Value.Y);
                if (!suppress_tool_transfer) g.DrawLine(pen_transfer, xlast, ylast, xfirst, yfirst);
                xlast = ImgX(slice.paths[slice.paths.Count - 1].p.Last.Value.X);
                ylast = ImgY(slice.paths[slice.paths.Count - 1].p.Last.Value.Y);
                suppress_tool_transfer = false;
            }
            //show slice
            show_Slice(slice);
            //move to next screen tile
            x0 += xstep;
            if (x0 > w - xstep / 2)
            {
                x0 -= w;
                y0 += ystep;
                suppress_tool_transfer = true;
            }
        }
        return img;
    }

    public void show_Slice(Slice slice)
    {
        //draw tool transfers first
        Vector2F lastpos = new Vector2F(float.NaN, float.NaN);
        Pen pen_transfer = new Pen(Color.LightGray);
        //DrawMarker(pen_transfer, lastpos.X, lastpos.Y);
        foreach (SegmentPath s in slice.paths)
        {
            if (!float.IsNaN(lastpos.X))
            {
                DrawLine(pen_transfer, lastpos.X, lastpos.Y, s.p.First.Value.X, s.p.First.Value.Y);
            }
            lastpos = s.p.Last.Value;
        }
        //DrawLine(pen_transfer, lastpos.X, lastpos.Y, 5, -10);

        //draw paths
        int color_index = 0;
        foreach (SegmentPath s in slice.paths)
        {
            Pen p = new Pen(colors[color_index]);
            color_index++;
            if (color_index >= colors.Length) color_index = 0;
            LinkedListNode<Vector2F> vn = s.p.First;
            DrawMarker(p, vn.Value.X, vn.Value.Y);
            while (vn.Next != null)
            {
                DrawLine(p, vn.Value.X, vn.Value.Y, vn.Next.Value.X, vn.Next.Value.Y);
                vn = vn.Next;
            }
        }
    }

    public void show_segments(LinkedList<Segment> segments)
    {
        Pen p = new Pen(Color.Red);
        Pen p2 = new Pen(Color.LightGray);
        Pen p3 = new Pen(Color.Black);
        for (int x = -10; x <= 10; x++) DrawLine((x == 0 ? p3 : p2), x, -10, x, 10);
        for (int y = -10; y <= 10; y++) DrawLine((y == 0 ? p3 : p2), -10, y, 10, y);
        int cnt = 0;
        foreach (Segment s in segments)
        {
            DrawLine(p, s.p[0].X, s.p[0].Y, s.p[1].X, s.p[1].Y);
            Console.WriteLine(++cnt);
        }
    }

    //get image coordinates
    private float ImgX(float x)
    {
        return x0 + sc * x;
    }
    private float ImgY(float y)
    {
        return y0 - sc * y;
    }

    private void DrawLine(Pen p, float x1, float y1, float x2, float y2)
    {
        g.DrawLine(p, x0 + sc * x1, y0 - sc * y1, x0 + sc * x2, y0 - sc * y2);
    }

    private void DrawMarker(Pen p, float x1, float y1)
    {
        g.DrawRectangle(p, x0 + sc * x1 - 1, y0 - sc * y1 - 1, 3, 3);
    }
}


