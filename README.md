# AutoBench
Automated benchmarking script for Vegas Pro Video

These folders will help installing and using this script

When run sucessfully, this script will create a .CSV table file in the output directory chosen for renders.  This file can be loaded into Excel.


Render Templates

     The script looks for sub-strings of the Render Template name, enclosed in parentheses in the exaple templates.

     These tokens distinguish RedCar templates from SampleProject.

     Also distinguish 4K templates from HD

     And also distinguish MainConcept (MC), Nvidia (Nvenc), AND (VCE), and Intel (Qsv) encoders

     If you copy the contents of "Render Templates" to "C:\Users\<User Name>\AppData\Roaming\VEGAS\Render Templates" you'll have the ones recognised.

     Note that I only use Magix AVC templates (in the AVC sub-folder under Roaming) for benchmarking but you can make your own for other codecs with Vegas.

     Once there under Roaming, they will show up in all Vesions of Vegas that you have installed.

     Beware, however, that there were some bugs in older versions of Vegas regarding some of the render modes (vbr vrs cbr). 

     So you might want to look at them for versions before vp19

     Or just follow the name convention and make them yourself with your version of Vegas.

     Suggest you only copy the templates for the hardware you have to avoid confusing the script or Vegas.
     
     The script is designed to run 3 different hardware encoders for both RedCar and SampleProject rendered to both HD and 4k. But you can uncheck encoder templates in the script's form if you want to run fewer.
     
     Note that the script runs each render twice with a cooling-off period between renders and records the best of the 2 times in the output file.  I don't have this behavior adjustable in this version.
     

Vegas Script Menu

     Copy this folder to create "Vegas Script Menu" in your Documents folder if it's not there already.

     That's the preferred place to put custom Vegas scripts.

     If the folder is already there, just add the contents to the custom scripts already in it.


     AutoBench.DLL is the compiled script, in DLL form to include Microsoft Visual Studio 19 library code not in Vegas.

     This enables the script to probe the os for hardware and use optional methods Vegas itself does not provide.

     The C# source code is Form1.cs and the graphical design of the Form and it's fields is Form1.Designer.cs.

     If you alter the script with Visual Studio 19 and recompile your solution, it will appear in the "AutoBench/bin/Release" folder

     I have a batch file in there that should also copy it automatically down a few levels for convenience.

     In any event, manually copy the newly generated DLL to your "Vegas Script Menu" folder after testing it. 


     AutoBench.txt is an optional config file to initialize variables like the GPU, Decoder, and Sleep time.

     The script looks for this file in the same folder AutoBench.DLL is run from.

     If you put AutoBench.DLL in the Script Menu folder of the Program directory (not recommended) put the text file there too.
 
     All these variables can be edited in the script's form but I find putting them in a config file speeds up repeated use.

     Note that the GPU and Decoder variables are informational only for the output text file.

     To change GPU and Decoder actually used by Vegas, you need to do it in Vegas followed by a restart.


     AutoBench.png is an icon that will appear alongside the script if you run it manually in Vegas.

     It can also be made to appear on the Vegas toolbar if you customize it.

     Vegas looks for the icon file in the same folder that AutoBench.DLL is run from.

     If you put AutoBench.DLL in the Script Menu folder of the Program directory (not recommended) put the icon there too.

 
 The render code and icon presented here was inspired by JetDV tutorials (http://www.jetdv.com/)... highly recommended.
