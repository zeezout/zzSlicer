#zzSlicer
Slices STL to Gcode for 3D printers. Prints the STL file as it is, without additions or ommisions. Designed for printing thin vertical walls, for example to print the wings of a model airplane.

##Features
* Reads ASCII or Binary STL files
* Slices facets in the STL file into line segments
* Combines the line segments to segment paths
* Rearranges the paths to optimize print head travel
* Outputs the paths as Gcode file
* Shows the 2D paths on screen

##What is missing
* An userinterface. You'll need to change the constants in the file to change for example the STL file that is opened.
* Horizontal surfaces are not printed at all
* Surface with more than approx 45 degree angle are not printed as a surface, need to add double or more layers in your STL model to get these surfaces to print correctly.

##Uses
* QuantumConcepts.Formats.StereoLithography STLdotNET library to read STL files