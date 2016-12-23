using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace STLtest
{
    public partial class frmSlicer : Form
    {
        AppSettings appset = new AppSettings();
        string stl_file;
        Slices slices;
        Gcode gcode;

        public frmSlicer()
        {
            InitializeComponent();
        }

        private void frmSlicer_Load(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = appset;
        }

        private void frmSlicer_Shown(object sender, EventArgs e)
        {
            string file = "die";
            //string file = "fuselage_crude";
            //string file = "house1";
            //string file = "servo";
            //string file = "wing_sd7037-1_6vertical";
            //string file = "sd7037-1";

            string dir = Application.StartupPath + @"\..\..\..\..\Models\";
            string stl_file = dir + file + ".stl";
            string gcode_file = dir + file + ".gcode";
            if (LoadStlFile(stl_file)) SaveGcodeFile(gcode_file);
        }

        private bool LoadStlFile(string filename)
        {
            this.stl_file = filename;

            if (!File.Exists(filename))
            { 
                pictureBox1.Image = null;
                return false;
            }

            int w = 2400;
            int h = 1600;
            int xdiv = 12;
            int ydiv = 8;

            //slice
            Mesh mesh = new Mesh(filename);
            mesh.ShiftCenter();
            mesh.Scale(appset.stl_scale);
            //float zstep = (mesh.zmax - mesh.zmin) / (xdiv * ydiv);
            float zstep = appset.zstep;
            slices = new Slices(mesh, zstep, appset.slice_tol, appset.z_angle*(float)Math.PI/180f);

            //visualize
            Visualize vis = new Visualize(w, h);
            pictureBox1.Image = vis.show_Slices(slices, xdiv, ydiv);

            //gcode
            gcode = new Gcode();
            gcode.ZStep = appset.zstep;
            gcode.header = appset.gcode_header;
            gcode.footer = appset.gcode_footer;
            gcode.WallThickness = appset.WallThickness;
            gcode.FilamentDiameter = appset.FilamentDiameter;
            gcode.PrintSpeed = appset.PrintSpeed;
            gcode.PrintPerimeter = appset.PrintPerimeter;
            gcode.Append(slices);

            //show info
            txtInfo.Text =
                gcode.info().Replace(";", "").Replace("\n", "\r\n") +
                mesh.ToString() + "\r\n" +
                string.Format("Mesh Facets    {0:0.}\r\n", mesh.Facets.Length) +
                string.Format("Layers         {0:0.}\r\n", slices.slices.Count) +
                string.Format("Line Segments  {0:0.}\r\n", slices.LineSegmentCount());

            //clear settings isdirty flag
            appset.IsDirtyClearFlag();
            return true;
        }

        private bool SaveGcodeFile(string filename)
        {
            if(appset.IsDirty())
            {           
                if (!LoadStlFile(stl_file)) return false;
            }            
            gcode.save(filename);
            return true;
        }


#region menu and button event handlers
        private void btnApplySettings_Click(object sender, EventArgs e)
        {
            if(appset.IsDirty()) LoadStlFile(this.stl_file);
        }

        private void loadSTLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Title = "Load STL File";
            d.Filter = "STL Files|*.stl|All Files|*.*";
            d.CheckFileExists = true;
            if (DialogResult.Cancel == d.ShowDialog()) return;
            LoadStlFile(d.FileName);
        }

        private void saveGcodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Path.GetFullPath(Path.ChangeExtension(stl_file, ".gcode"));
            SaveFileDialog d = new SaveFileDialog();
            d.Title = "Save G-code File";
            d.Filter = "G-code Files|*.gcode|All Files|*.*";
            d.OverwritePrompt = true;
            d.InitialDirectory = Path.GetDirectoryName(path);
            d.FileName = Path.GetFileName(path);
            if (DialogResult.Cancel == d.ShowDialog()) return;
            SaveGcodeFile(d.FileName);
        }
    }
#endregion
}

