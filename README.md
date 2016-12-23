#zzSlicer
Slices STL to Gcode for 3D printers. Prints the STL file as it is, without additions or ommisions. Designed for printing thin vertical walls, for example to print the wings of a model airplane.

##Features
* Reads ASCII or Binary STL files
* Slices facets in the STL file into line segments
* Combines the line segments to segment paths
* Rearranges segment paths to optimize print head travel
* Surface with more than 45 degree angle are printed as a double layer (can be swiched off).
* Outputs the paths as Gcode file
* Shows the 2D paths on screen

##What is missing
* Horizontal surfaces are not printed at all

##Uses
* QuantumConcepts.Formats.StereoLithography STLdotNET library to read STL files