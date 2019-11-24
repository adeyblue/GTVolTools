This tool can display images from various GT2 & 3 formats. It doesn't auto detect and as it currently stands, must be recompiled to handle a different type. To change which type, go to the bottom of Form1.cs and change which of the three supported types is uncommented.

As the code stands, it only works via drag and drop.

By commenting and uncommenting things in program.cs though, it can be made to:
Parse the output of 'bgrep 1f8b08' and decompress all the gzip archives found
Dump rather than display a directory full of (one of) the image types above
Dump the contents of Gran Turismo 3 / Concept 2001 / Concept 2002 car database files to text files (the ones found in the database folder in the VOL). This won't completely work with demos as the structure is different

Again, currently it can only do one of those things at a time with a recompile needed to change.

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

GT3/4/Tourist Trophy - Tex1
----------
These are everywhere, but mostly .img files
Tex1 files that are embedded (for instance in the .imgs archives) will need to be exploded out before being accepted. (E.g. if the first four bytes of the dragged file aren't Tex1, it won't be opened)

The Tex1 support isn't 100%, some files may crash it or display minor or total garbage.