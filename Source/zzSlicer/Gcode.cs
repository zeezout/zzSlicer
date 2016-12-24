using System;
using System.Text;


class Gcode
{
    private float filament_diameter = 1.75f;
    public float f0 = 9000; //speed for G0 commands in mm/min
    public float f1 = 1200; //speed for G1 commands in mm/min
    public float printtime_minutes; //total print time in minutes
    public float distance_g1; //total print distance in mm
    public float distance_g0; //total transfer distance in mm
    public bool PrintPerimeter = true; //print perimeter around object
    public Vector2F pos = new Vector2F(0, 0); //current xy position in mm
    public float z = 0; //current z position in mm
    public float e = 0; //filament length feeded in mm

    StringBuilder gcode = new StringBuilder(8000000); //generated gcode

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



    public float PrintSpeed
    {
        get { return f1 / 60; }
        set { f1 = value * 60; }
    }

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

        tz(0);
        if (PrintPerimeter)
        { 
            txy(new Vector2F(slices.mesh.xmin - 6, slices.mesh.ymin - 6));
            mxy(new Vector2F(slices.mesh.xmax + 6, slices.mesh.ymin - 6));
            mxy(new Vector2F(slices.mesh.xmax + 6, slices.mesh.ymax + 6));
            mxy(new Vector2F(slices.mesh.xmin - 6, slices.mesh.ymax + 6));
            mxy(new Vector2F(slices.mesh.xmin - 6, slices.mesh.ymin - 6));
        }
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

    public string info()
    {
        string s = "";
        s += string.Format(";Filament usage {0:0.00} m\n", e / 1000);
        s += string.Format(";Filament PLA   {0:0.0} gr\n",  FilamentUsageCc * 1.27);
        s += string.Format(";Filament ABS   {0:0.0} gr\n",  FilamentUsageCc * 1.05);
        s += string.Format(";Print time     {0:0.0} min\n", printtime_minutes);
        s += string.Format(";Movement G1    {0:0.0} m\n", distance_g1/1000);
        s += string.Format(";Movement G0    {0:0.0} m\n", distance_g0 / 1000);
        s += string.Format(";Filament diam. {0:0.00} mm\n", filament_diameter);
        s += string.Format(";Wall thickness {0:0.00} mm\n", wall_thickness);
        s += string.Format(";Print speed    {0:0.} mm/sec\n", f1/60);
        s += string.Format(";Created on     {0}\n", DateTime.Now);
        return s;
    }

    public void save(string filename)
    {        
        System.IO.File.WriteAllText(filename, info() + "\n" + header + "\n" + gcode + footer + "\n");
    }

    #region Low level gcode output
    //transfer to xy
    public void txy(Vector2F v)
    {
        float d = Vector2F.Distance(pos, v);
        distance_g0 += d;
        printtime_minutes += d / f0;
        pos = v;
        g("G0 X{0:0.###} Y{1:0.###} F{2:0.###}", pos.X, pos.Y, f0);
    }

    //transfer to xy
    public void txy(float new_x, float new_y)
    {
        txy(new Vector2F(new_x, new_y));
    }

    //move to xy
    public void mxy(Vector2F v)
    {
        float d = Vector2F.Distance(pos, v);
        distance_g1 += d;
        printtime_minutes += d / f1;
        pos = v;
        e += e_per_mm * d;
        g("G1 X{0:0.###} Y{1:0.###} E{2:0.#####} F{3:0.###}", pos.X, pos.Y, e, f1);
        
    }

    //move to xy
    public void mxy(float new_x, float new_y)
    {
        mxy(new Vector2F(new_x, new_y));
    }

    //move to z
    public void mz(float new_z)
    {
        z = new_z;
        float d = (float)Math.Abs(new_z-z);
        distance_g1 += d;
        printtime_minutes += d / f1;
        g("G1 Z{0:0.###} F{1:0.###}", z, f1);
    }

    //transfer to z
    public void tz(float new_z)
    {
        z = new_z;
        float d = (float)Math.Abs(new_z - z);
        distance_g0 += d;
        printtime_minutes += d / f0;
        g("G0 Z{0:0.###} F{1:0.###}", z, f0);
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

