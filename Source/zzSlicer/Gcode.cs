using System;
using System.Text;


class Gcode
{
    //float extruder_diameter = 0.4f;
    private float filament_diameter = 1.75f;
    public float FilamentDiameter
    {
        get { return filament_diameter; }
        set { filament_diameter = value; _init_e(); }
    }
    private float z_step = 0.2f;
    public float ZStep
    {
        get { return z_step; }
        set { z_step = value; _init_e(); }
    }
    private float wall_thickness = 0.50f;
    public float WallThickness
    {
        get { return wall_thickness; }
        set { wall_thickness = value; _init_e(); }
    }
    private float e_per_mm; //length of filament needed to make 1mm of wall
    private void _init_e()
    {
        e_per_mm = (z_step * wall_thickness) / ((filament_diameter / 2) * (filament_diameter / 2) * (float)Math.PI);
    }

    //Kossel specs: move 150mm/sec = 9000mm/min, print 20-80mm/sec = 1200-4800mm/min
    public float f0 = 9000; //speed for G0 commands in mm/min
    public float f1 = 1200; //speed for G1 commands in mm/min

    public string header = @";==============================================================
; START header
;==============================================================
M104 S200                 ; extrusion temperature (no wait)
M190 S60                  ; Wait for bed temperature to reach target temp
M109 S200                 ; extrusion temperature and wait
M117 Homing...
G21                       ; metric values
G90                       ; absolute positioning
M107                      ; start with the fan off
G28                       ; move to endstops
G92 E0                    ; zero the extruded length
G1 F200 E3                ; extrude 3mm of feed stock
G92 E0                    ; zero the extruded length again
G1 F9000 Z1               ; move print head down fast

M82                       ; Set extruder to absolute mode
;M83                       ; Set extruder to relative mode

M117 Printing...
;==============================================================
; END header
;==============================================================";

    public string footer = @";==============================================================
;START footer
;==============================================================
M104 S0                       ;set extruder temperature (no wait)
M140 S0                       ;set bed temperature (no wait)
G91                           ;relative positioning
M83                           ;Set extruder to relative mode
G0 E-1 F300                   ;retract the filament a bit before lifting the nozzle, to release some of the pressure
G0 Z+0.5 E-5 X-20 Y-20 F9000  ;move Z up a bit and retract filament even more
G28                           ;move to endstops
M84                           ;steppers off
G90                           ;absolute positioning
M107                          ;Fan Off 
;==============================================================
;END footer
;==============================================================";
    public float x = 0;
    public float y = 0;
    public float z = 0;
    public float e = 0;

    StringBuilder gcode = new StringBuilder(1000000);

    public float FilamentUsageCc
    {
        get { return e * ((filament_diameter / 2) * (filament_diameter / 2) * (float)Math.PI) / 1000; }
    }

    public Gcode()
    {
        _init_e();
    }

    public void Append(Slices slices)
    {
        //perimeter
        tz(0);
        txy(new Vector2F(slices.mesh.xmin - 6, slices.mesh.ymin - 6));
        mxy(new Vector2F(slices.mesh.xmax + 6, slices.mesh.ymin - 6));
        mxy(new Vector2F(slices.mesh.xmax + 6, slices.mesh.ymax + 6));
        mxy(new Vector2F(slices.mesh.xmin - 6, slices.mesh.ymax + 6));
        mxy(new Vector2F(slices.mesh.xmin - 6, slices.mesh.ymin - 6));
        foreach (Slice s in slices.slices) Append(s);
    }

    public void Append(Slice slice)
    {
        mz(slice.z);
        foreach (SegmentPath p in slice.paths) Append(p);
    }

    public void Append(SegmentPath path)
    {
        txy(path.p.First.Value);
        foreach (Vector2F v in path.p)
        {
            mxy(v);
        }
    }

    public void save(string filename)
    {
        string info = string.Format(";Filament usage: {0:0.00} m, {1:0.0} cc, {2:0.0} gram PLA, {3:0.0} gram PLA\n", e / 1000, FilamentUsageCc, FilamentUsageCc * 1.27, FilamentUsageCc * 1.05);
        info += string.Format(";Filament diameter: {0} mm\n", filament_diameter);
        info += string.Format(";Wall thickness: {0} mm\n", wall_thickness);
        info += string.Format(";Feed speed: {0}\n", f1);
        info += string.Format(";Created on: {0}\n", DateTime.Now);
        System.IO.File.WriteAllText(filename, info + "\n" + header + "\n" + gcode + footer + "\n");
    }

    #region Low level gcode output
    public void txy(float new_x, float new_y)
    {
        x = new_x;
        y = new_y;
        g("G0 X{0:0.###} Y{1:0.###} F{2:0.###}", x, y, f0);
    }

    public void mxy(float new_x, float new_y)
    {
        e += e_per_mm * (float)Math.Sqrt((x - new_x) * (x - new_x) + (y - new_y) * (y - new_y));
        x = new_x;
        y = new_y;
        g("G1 X{0:0.###} Y{1:0.###} E{2:0.#####} F{3:0.###}", x, y, e, f1);
    }

    public void mz(float new_z)
    {
        z = new_z;
        g("G1 Z{0:0.###} F{1:0.###}", z, f1);
    }

    public void tz(float new_z)
    {
        z = new_z;
        g("G0 Z{0:0.###} F{1:0.###}", z, f0);
    }

    public void txy(Vector2F v)
    {
        txy(v.X, v.Y);
    }

    public void mxy(Vector2F v)
    {
        mxy(v.X, v.Y);
    }

    public void g(string msg, params object[] args)
    {
        //Console.WriteLine(msg, args);
        gcode.Append(string.Format(msg, args) + "\n");
    }

    public void comment(string msg, params object[] args)
    {
        g(";" + msg, args);
    }
    #endregion

    public enum PolyOptions
    {
        None = 0,
        Reverse = 1,
        Closed = 2
    }

    public void Polygon(Polygon poly, int start_index = 0, PolyOptions options = PolyOptions.None)
    {
        int cnt = poly.points.Length + ((options & PolyOptions.Closed) == PolyOptions.Closed ? 1 : 0);
        int pos = start_index;
        int step = ((options & PolyOptions.Reverse) == PolyOptions.Reverse ? -1 : 1);
        while (cnt > 0)
        {
            mxy(poly.points[pos].X, poly.points[pos].Y);
            pos = (pos + step + poly.points.Length) % poly.points.Length; // NOTE: % operator gives negative results on negative input
            cnt--;
        }
    }

}

