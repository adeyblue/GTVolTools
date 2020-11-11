using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;

namespace GTMP
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //GT3DB.DumpDBIDsAndNames(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\tokyoconcept2001\", GT3DB.ISNT_DEMO);
            //GT3DB.DumpDBIDsAndNames(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\SCUS-97115\", GT3DB.IS_DEMO);
            //GT3DB.DumpDBIDsAndNames(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\JP1.0Vol\", GT3DB.ISNT_DEMO);
            //GT3DB.DumpDBIDsAndNames(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\US1.0Vol\", GT3DB.ISNT_DEMO);
            //GT3DB.DumpDBIDsAndNames(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\PalVol\", GT3DB.ISNT_DEMO);
            //ParseEmbeddedGZipFiles();
            //return;
            //GT3Tex.DumpTexDir(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\palvol\menu\US\arcade\unzip", @"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\palvol\menu\US\arcade\imgs");
            //GT3Tex.DumpTexDir(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\palvol\menu\US\arcade", @"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\palvol\menu\US\arcade");
            //GT3Tex.DumpTexDir(@"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\tokyoconcept2002Seoul\menu\TW\gzout", @"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\tokyoconcept2002Seoul\menu\TW\gzout\pics");
            //GTMPFile.DumpGTMPDir(@"T:\PalVolEngBG", @"T:\PalVolEngBG\pics");
            //GTMPFile.DumpGTMPDir(@"C:\Users\Adrian\Downloads\GT2\Jap1.0Vol\gtmenu\decomp", @"C:\Users\Adrian\Downloads\GT2\Jap1.0Vol\gtmenu\decomp\pics");
            //GMFile.DumpGMDir(@"C:\Users\Adrian\Downloads\GT2\PalVol\gtmenu\eng\decomp", @"C:\Users\Adrian\Downloads\GT2\PalVol\gtmenu\eng\decomp\lines");
            //GMFile.DumpGMDir(@"V:\US1.0VolSpa\decomp", @"V:\US1.0VolSpa\decomp\pics");
            //using (Bitmap bm = GMFile.Parse(@"C:\Users\Adrian\Downloads\GT2\Jap1.0Vol\gtmenu\eng\exploded\decomp\727"))
            //{
            //    bm.Save(@"C:\Users\Adrian\Downloads\GT2\Jap1.0Vol\gtmenu\eng\exploded\decomp\727.png", System.Drawing.Imaging.ImageFormat.Png);
            //}
            string dir = args[0];
            //GMFile.DumpGMFile(Path.Combine(dir, "2942"), dir + "\\png");
            //GTMPFile.DumpGTMPFile("T:\\gt2\\commonpic\\105.gtm", "T:\\build");
            dir = @"C:\Users\Adrian\Downloads\A-GT2\us12-gm";
            //GMFile.DumpGMFile(Path.Combine(dir, "2942.gm.decomp"), dir + "\\png");
            GMFile.DumpGMDir(dir, dir + "\\png");
            //GTMPFile.ExplodeCommonPic(@"C:\Users\Adrian\Downloads\GT2\US1.0\gtmenu\commonpic.dat", "T:\\gt2\\commonpic");
            //GTMPFile.DumpGTMPDir("T:\\gt2\\commonpic", "T:\\gt2\\commonpic\\png");
            //GT3Tex.DumpTexDir(dir, dir + "\\pics");
            return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GTMPForm());
        }

        // gzipFiles.txt must be output from "bgrep 1f8b08"
        // This parses such a file and decompresses all the embedded GZip archives into a gzout folder
        // in the same directory as the file containing the embedded archive
        static void ParseEmbeddedGZipFiles()
        {
            string baseDir = @"C:\Users\Adrian\Downloads\GT2\GT3\gt3vol\SCUS-97115\menu";
            string gzipFile = baseDir + "\\" + "gzipFiles.txt";
            string[] lines = File.ReadAllLines(gzipFile);
            char[] delims = new char[] { ':', ' ' };
            byte[] buffer = new byte[0x10000];
            int num = 0;
            foreach (string line in lines)
            {
                string[] parts = line.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                string file = baseDir + parts[0];
                Console.WriteLine("Processing {0}:{1}", parts[0], parts[1]);
                int offset = 0;
                Int32.TryParse(parts[1], System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.CurrentCulture, out offset);
                using (FileStream fsIn = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    fsIn.Seek(offset, SeekOrigin.Begin);
                    //MemoryStream ms = new MemoryStream();
                    //StreamUtils.Copy(fsIn, ms, buffer);
                    //ms.Seek(0, SeekOrigin.Begin);
                    GZipStream gzIn = new GZipStream(fsIn, CompressionMode.Decompress);
                    //GZipInputStream gzin = new GZipInputStream(ms);
                    string fileDir = Path.GetDirectoryName(file);
                    string gzOutDir = Path.Combine(fileDir, "gzout");
                    Directory.CreateDirectory(gzOutDir);
                    string gzOutFileBase = Path.Combine(gzOutDir, Path.GetFileName(file));
                    string gzOutFile = gzOutFileBase;
                    while (File.Exists(gzOutFile))
                    {
                        ++num;
                        gzOutFile = String.Format("{0}.{1}", gzOutFileBase, num);
                    }
                    using (FileStream fsOut = new FileStream(gzOutFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        try
                        {
                            StreamUtils.Copy(gzIn, fsOut, buffer);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Caught exception {0}", e.Message);
                            fsOut.Close();
                            File.Delete(gzOutFile);
                        }
                    }
                }
                num = 0;
            }
        }
    }
}
