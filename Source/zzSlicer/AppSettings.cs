using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

//[DefaultProperty("XXX")]
public class AppSettings
{
    public AppSettings()
    {
        IsDirtyClearFlag();
    }

    private AppSettings last_saved_version;

    public bool IsDirty()
    {
        if (last_saved_version == null) return true;
        foreach(PropertyInfo pi in GetType().GetProperties())
        {
            //Console.WriteLine(pi.Name + " = " + pi.GetValue(this) + " last=" + pi.GetValue(last_saved_version));
            if (pi.GetValue(this) != pi.GetValue(last_saved_version)) return true;
        }
        return false;   
    }

    public void IsDirtyClearFlag()
    {
        this.last_saved_version = null;
        this.last_saved_version = (AppSettings)this.MemberwiseClone();
    }



    [Category("1. STL"),
    DisplayName("Scale"),
    Description("Scale the model with this factor"),
    DefaultValue(1.0f)]
    public float stl_scale { get; set; } = 1.0f;



    [Category("2. Slicer"),
    DisplayName("Layer Thickness (mm)"),
    Description(""),
    DefaultValue(0.2f)]
    public float zstep { get; set; } = 0.2f;

    [Category("2. Slicer"),
    DisplayName("Tolerance"),
    Description(""),
    DefaultValue(1e-4f),
    Browsable(false)]
    public float slice_tol { get; set; } = 1e-4f;

    [Category("2. Slicer"),
    DisplayName("Z Angle (deg)"),
    Description("Print two lines when angle between normal and z-axis is smaller than this value. Set to 0 to disable."),
    DefaultValue(45f)]
    public float z_angle { get; set; } = 45f;



    

    [Category("3. G-code"),
    DisplayName("Print Perimeter"),
    Description(""),
    DefaultValue(true)]
    public bool PrintPerimeter { get; set; } = true;

    [Category("3. G-code"),
    DisplayName("Filament Diameter (mm)"),
    Description(""),
    DefaultValue(1.75f)]
    public float FilamentDiameter { get; set; } = 1.75f;

    [Category("3. G-code"),
    DisplayName("Wall Thickness (mm)"),
    Description(""),
    DefaultValue(0.5f)]
    public float WallThickness { get; set; } = 0.5f;

    [Category("3. G-code"),
    DisplayName("Print Speed (mm/s)"),
    Description("Print Speed (mm/s)"),
    DefaultValue(30f)]
    public float PrintSpeed { get; set; } = 30f;

    [Category("3. G-code"),
    DisplayName("Header"),
    Description("Header"),
    DefaultValue(""),
    Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
    public string gcode_header { get; set; } = @";==============================================================
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

    [Category("3. G-code"),
    DisplayName("Footer"),
    Description("Footer"),
    DefaultValue(""),
    Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
    public string gcode_footer { get; set; } = @";==============================================================
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

}

