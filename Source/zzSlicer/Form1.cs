using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace STLtest
{
    public partial class frmSlicer : Form
    {
        public frmSlicer()
        {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            //string file = "die.";
            //string file = "fuselage_crude";
            //string file = "house1";
            //string file = "servo";
            //string file = "wing_sd7037-1_6vertical";
            string file = "sd7037-1";

            string dir = Application.StartupPath + @"\..\..\..\..\Models\";
            string stl_file = dir + file + ".stl";
            string gcode_file = dir + file + ".gcode";

            int w = 2400;
            int h = 1600;
            int xdiv = 12;
            int ydiv = 8;

            //slice
            Mesh mesh = new Mesh(stl_file);
            mesh.ShiftCenter();
            //mesh.Scale(0.5f);
            Console.WriteLine(mesh);
            //float zstep = (mesh.zmax - mesh.zmin) / (xdiv * ydiv);
            float zstep = 0.2f;
            Slices slices = new Slices(mesh, zstep, 1e-4f);

            //gcode
            Gcode gcode = new Gcode();
            gcode.Append(slices);
            gcode.save(gcode_file);

            //visualize
            Visualize vis = new Visualize(w, h);
            pictureBox1.Image = vis.show_Slices(slices, xdiv, ydiv);
        }
    }
}

//if (segments.Count==0) return NULL;		// nothing found at this Z

/*

            // find left most segment to start...
            segment s1 = null;
            float x = 0, y = 0;
            foreach(segment s in segments)
            {
                if (s.p[0].Y != s.p[1].Y)
                {
                    int e;
                    for (e = 0; e < 2; e++)
                    { 
                        if (s1 == null || s.p[e].X < x)
                        {
                            s1 = s;
                            x = s.p[e].X;
                            y = s.p[e].Y;
                        }
                    }
                }
            }
            // work out clockwise direction
            dir = 0;
            if (s1.p[0].Y > s1.p[1].Y) dir = 1;

            // Construct paths (and free segments)
            polygon_t* outline = poly_new();


            while (segments.Count>0)
            {
//#ifdef DEBUG
//      fprintf(stderr, "Starting %s", dimout (x));
//      fprintf(stderr, ",%s\n", dimout (y));
//#endif
                poly_start(outline);
                b = segments;		// pick a point
                while (1)
                {
                    // unlink
                    if (b->next)
                         b->next->prev = b->prev;

                    * b->prev = b->next;

                    poly_add(outline, b->point[dir].X, b->point[dir].Y, 0);
                    float x = b->point[1 - dir].X;
                    float y = b->point[1 - dir].Y;

                     free(b);

                    // find closest connected point
                    float bestd = 0;
                    b = NULL;
                    for (s = segments; s; s = s->next)
                    {
                          float d = (s.p[dir].X - x) * (s.p[dir].X - x) + (s.p[dir].Y - y) * (s.p[dir].Y - y);
                          if (!b || d<bestd)
                            {
                              b = s;
                              bestd = d;
                            }
                    }
                    if (!b || bestd > tolerance2) break;
//#ifdef DEBUG
//	  fprintf(stderr, "Best %s\n", dimout (bestd));
//#endif
                }
            }

          int seglost = 0;
          if (segments)
            {
              while (segments)
            {
              seglost++;
//#if 0
//	  fprintf (stderr, "Segment %s", dimout (segments.p[0].X));
//	  fprintf (stderr, ",%s", dimout (segments.p[0].Y));
//	  fprintf (stderr, " %s", dimout (segments.p[1].X));
//	  fprintf (stderr, ",%s\n", dimout (segments.p[1].Y));
//#endif
              segment_t* s = segments->next;

              free(segments);
        segments = s;
            }
            }
          if (debug)
            fprintf(stderr, "Slicing at %s made %d segments\n", dimout (z), segcount);
          slice_t* slice = mymalloc(sizeof(*slice));
          slice->z = z;
          poly_tidy(outline, tolerance / 10);
        slice->outline = poly_clip(POLY_UNION, 1, outline);
          poly_free(outline);
          return slice;
          */




