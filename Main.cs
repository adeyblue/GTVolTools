using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;

namespace GT2Vol
{
    class Program
    {
        static int i = 0;
        static void EmbeddedFileNotify(EmbeddedFileInfo efi)
        {
            Console.WriteLine("{3:x}: Found {0} at offset 0x{1:X} of size {2}", efi.name, efi.fileAddress, efi.size, i++);
        }

        static void PrintUsage()
        {
            // rebuilding GT3 doesn't work (yet?)
            // GTVolTool.exe -r3(ebuild GT3) C:\\Path\\To\\explodedVol NewVolFile <-d> OR

            Console.Error.WriteLine(
@"Usage: GTVolTool.exe -e2(xplode GT2) C:\\Path\\To\\GT2.Vol OutputDir <-u> OR
       GTVolTool.exe -l2(ist GT2) C:\\Path\\To\\GT2.vol OR
       GTVolTool.exe -r2(ebuild GT2) C:\\Path\\To\\explodedVol NewVolFile <-r> OR
       GTVolTool.exe -e3(xplode GT3) C:\\Path\\To\\GT3.vol OutputDir <-u> OR
       GTVolTool.exe -l3(list GT3) C:\\Path\\To\\GT3.Vol OR
       GTVolTool.exe -e2k(xplode GT2000) C:\\Path\\To\\GT2K.Vol OutputDir OR
       GTVolTool.exe -l2k(list GT2K) C:\\Path\\To\\GT2K.vol
For -e2 and -e3
The last argument is optional and can be anything. If specified,
any gzip archives (those files ending in .gz) will be decompressed
into a '" + VolFile.DecompDir + @"' folder within the folder where the .gz file is.
For -r2
The last argument is optional and can be anything. If specified,
files in any '" + VolFile.DecompDir + @"' child directories will be compressed and have .gz
added to their filename before being inserted
-l2
Outputs a list of the contents of a GT2 vol file and their filesize & location
in the VOL. Helpful if you only want to replace one file in a vol without
extracting and rebuilding.
-e2k
This explodes GT2K.VOL format files. Tested on PAPX-90203 & SCUS-97115. 
It does not support GT3.
-l2k
This outputs a list of the entries in a GT2K.vol and their filesize/offset.
This is primarily useful if you just want to replace one file without
going through the entire explode - replace - rebuild process
-l3
This outputs a list of the entries in a GT3.vol and their filesize/offset.
This is primarily useful if you just want to replace one file without
going through the entire explode - replace - rebuild process");
/*
 * For -r3
The last argument is optional and can be anything. If specified,
the tool will rebuild a VOL compatible with the demos rather than the final game */
        }

        static void WriteToConsole(string progress)
        {
            Console.Error.WriteLine(progress);
        }

        static void ExplodeVol(string[] args)
        {
            bool decompGZ = (args.Length >= 4);
            if (!File.Exists(args[1]))
            {
                Console.Error.WriteLine("VOL file '{0}' doesn't exist!", args[1]);
                return;
            }
            using (VolFile theVol = new VolFile(args[1]))
            {
                if (!theVol.CheckAndCacheHeaderDetails())
                {
                    Console.WriteLine("File is not a GT2.vol (incorrect GTFS signature, not enough entries, or total entries less than files)");
                    return;
                }
                theVol.ParseToc(new VolFile.TocFileNotify(EmbeddedFileNotify));
                theVol.Explode(args[2], decompGZ, new VolFile.ExplodeProgressCallback(WriteToConsole));
            }
        }

        static void DumpGT2Toc(string[] args)
        {
            if (!File.Exists(args[1]))
            {
                Console.Error.WriteLine("VOL file '{0}' doesn't exist!", args[1]);
                return;
            }
            using (VolFile theVol = new VolFile(args[1]))
            {
                if (!theVol.CheckAndCacheHeaderDetails())
                {
                    Console.WriteLine("File is not a GT2.vol (incorrect GTFS signature, not enough entries, or total entries less than files)");
                    return;
                }
                theVol.ParseToc(new VolFile.TocFileNotify(EmbeddedFileNotify));
            }
        }

        [MTAThread]
        static void Main(string[] args)
        {
            Console.Error.WriteLine("Gran Turismo 2/2K/3 VOL File Exploder/Rebuilder - http;//www.airesoft.co.uk\n");
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }
            string lowerArg = args[0].ToLower();
            switch (lowerArg)
            {
                case "-e2":
                    {
                        ExplodeVol(args);
                    }
                    break;
                case "-r2":
                    {
                        Rebuilder.RebuildGT2(args);
                    }
                    break;
                case "-l2":
                    {
                        DumpGT2Toc(args);
                    }
                    break;
                case "-e2k":
                    {
                        GT2K.Explode2KVol(args);
                    }
                    break;
                case "-l2k":
                    {
                        GT2K.List2KVol(args);
                    }
                    break;
                case "-e3":
                    {
                        GT3Vol.Explode(args, new VolFile.ExplodeProgressCallback(WriteToConsole));
                    }
                    break;
                //case "-r3":
                //    {
                //        GT3Rebuild.Rebuild(args);
                //    }
                //    break;
                case "-l3":
                    {
                        GT3Vol.List(args);
                    }
                    break;
                default:
                    {
                        PrintUsage();
                    }
                    break;
            }
        }
    }
}
