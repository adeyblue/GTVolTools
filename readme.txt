GTVolTools
----------

There is code for four tools in this repo regarding the Gran Turismo 2, 2000, 3, and 4(-ish) games. This file is for te benefit of github, if you've downloaed the tools from my site, you don't need to read this.

GTVolToolGui.exe - This allows you to explode GT2, GT2000 & GT3 VOL files, including GT Concept (including demos of all these games) and rebuild GT 2 ones with a relatively simple Windows interface. It uses C#

GTVolTool.exe - This is a command line version of the above. It does the same but can also list the file size and locations of the constituent files of GT2 and GT3 vols. These lists can help when you only want to replace single files without having to explode and rebuild an entire VOL. It also uses C#

GT2DataExploder - This is also a command line tool that extracts data from an extracted GT2 VOL (final game only, doesn't work on demos) and transforms most of the car and race data into tabbed text files. Most of the useful extracted data is already on my GT2 website at http://gt2.airesoft.co.uk. Most people will not need to use this tool, but the code shows the layout of the database files. It uses C++

GTMP - See the readme in the GTMP directory. This can do multiple things like displaying GT2 GM & GTMP files (final game only), most GT3/4 & Concept Tex1 images (inc demos), and to parse out info from the db files from the database folder in GT3 VOLs.