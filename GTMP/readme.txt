This tool can display images from various GT2 & 3 formats. It doesn't auto detect and as it currently stands, must be recompiled to handle a different type. To change which type, go to the bottom of Form1.cs and change which of the three supported types is uncommented.

It only works via drag and drop.

Supported formats:
GT2 - GM (final game only)
--------------------------
A decompressed GM "menu" GT2 image file. 
These are the ones in the \gtmenu\<lang>\gtmenudat.dat.
That file needs exploding into its constituent gzip archives and the individual archives need decompressing before the files can be dragged into the program.

GT2 - GTMP
----------
Backgrounds in GT2
These are the ones in \gtmenu\commonpic.dat. That file needs exploding into its constituent GTMP files before the individal files can be dragged here.

GT3 - Tex1
----------
These are everywhere, but mostly .img files
Tex1 files that are embedded (for instance in the .imgs archives) will need to be exploded out before being accepted. (E.g. if the first four bytes of the dragged file aren't Tex1, it won't be opened)

The Tex1 support isn't 100%, some files may crash it or display minor or total garbage.