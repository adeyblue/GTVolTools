using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace GMCreator
{
    static class Tools
    {
        public enum ConvertType
        {
            GTMP,
            GM
        }

        public static bool ConvertImageTo(string inputFile, string outputFile, ConvertType type)
        {
            string converterExe = Path.Combine(Application.StartupPath, "GT2ImageConverter.exe");
            if(!File.Exists(converterExe))
            {
                MainForm.DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Error, "Converter tool {0} wasn't found!", converterExe);
                return false;
            }
            ProcessStartInfo psi = new ProcessStartInfo(converterExe, String.Format("\"{0}\" \"{1}\" {2}", inputFile, outputFile, type.ToString()));
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            bool success = false;
            DebugLogger.Log("Tools", "Converting to {0} with command line {1}", type.ToString(), psi.Arguments);
            try
            {
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (!(success = (p.ExitCode == 0)))
                    {
                        MainForm.DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Error, "Conversion failed. GT2ImagePConverter output was:{0}{1}", Environment.NewLine, output);
                    }
                }
            }
            catch (Exception e)
            {
                MainForm.DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Error, "Caught exception {0} trying to run GT2ImageConverter", e.Message);
            }
            return success;
        }

        public static byte[] CheckConvertImageTo(Bitmap bm, ConvertType type)
        {
            string tempIn = Path.GetTempFileName();
            string tempOut = Path.GetTempFileName();
            bm.Save(tempIn, System.Drawing.Imaging.ImageFormat.Png);
            bool converted = ConvertImageTo(tempIn, tempOut, type);
            GMProject.DeleteFileLoop(tempIn);
            byte[] gmllData = null;
            if (converted)
            {
                gmllData = File.ReadAllBytes(tempOut);
            }
            GMProject.DeleteFileLoop(tempOut);
            return gmllData;
        }

        public static void ConvertImageToGTMP(IWin32Window parent)
        {
            string inputFileName = MainForm.GetOpenFileName(parent, "Open Image To Convert", "Image Files (*.bmp, *.png, *.jpg)|*.bmp;*.png;*.jpg", null);
            if(String.IsNullOrEmpty(inputFileName))
            {
                return;
            }
            string outputFileName = MainForm.GetSaveFileName(parent, "Save Converted Image As...", "GT2 Background (*.gtmp)|*.gtmp|All Files (*.*)|*.*", Path.GetDirectoryName(inputFileName));
            if (String.IsNullOrEmpty(outputFileName))
            {
                return;
            }
            DebugLogger.Log("Tools", "Converting {0} to GTMP {1}", inputFileName, outputFileName);
            ConvertImageTo(inputFileName, outputFileName, ConvertType.GTMP);
        }

        private delegate void FileSplitter();

        public static void SplitCommonPic(IWin32Window parent)
        {
            string inputFileName = MainForm.GetOpenFileName(parent, "Open CommonPic.dat", "CommonPic.dat (*.dat)|commonpic*.*|All Files (*.*)|*.*", null);
            if (String.IsNullOrEmpty(inputFileName))
            {
                return;
            }
            string outputFolder = MainForm.PickFolder(parent, "Pick directory to split to...", Path.GetDirectoryName(inputFileName));
            if (String.IsNullOrEmpty(outputFolder))
            {
                return;
            }
            DebugLogger.Log("Tools", "Splitting and decomping CommonPic {0} to {1}", inputFileName, outputFolder);
            FileSplitter fs = () => {
                GTMP.GTMPFile.ExplodeCommonPic(inputFileName, outputFolder, GTMP.GTMPFile.SplitCommonPicArgs.OutputPngPicture);
            };
            SplitFiles(fs, "Splitting " + Path.GetFileName(inputFileName));
        }

        public static void SplitGTMenuDat(IWin32Window parent)
        {
            string inputFileName = MainForm.GetOpenFileName(parent, "Open GTMenuDat.dat", "GTMenuDat.dat (*.dat)|gtmenudat*.*|All Files (*.*)|*.*", null);
            if (String.IsNullOrEmpty(inputFileName))
            {
                return;
            }
            string outputFolder = MainForm.PickFolder(parent, "Pick directory to split to...", Path.GetDirectoryName(inputFileName));
            if (String.IsNullOrEmpty(outputFolder))
            {
                return;
            }
            DebugLogger.Log("Tools", "Splitting and decomping GTMenuDat {0} to {1}", inputFileName, outputFolder);
            FileSplitter fs = () =>
            {
                const GTMP.GMFile.SplitGTMenuFlags flags = GTMP.GMFile.SplitGTMenuFlags.OutputPngPicture;
                GTMP.GMFile.SplitGTMenuDat(inputFileName, outputFolder, flags);
            };
            SplitFiles(fs, "Splitting " + Path.GetFileName(inputFileName));
        }

        private class LogicalComparer : IComparer<string>
        {
            Regex partNumReg;
            int stringStartPoint;
            public LogicalComparer(int startPoint)
            {
                partNumReg = new Regex("([0-9]+)");
                stringStartPoint = startPoint;
            }

            public int Compare(string x, string y)
            {
                Match xMatch = partNumReg.Match(x, stringStartPoint);
                Match yMatch = partNumReg.Match(y, stringStartPoint);
                // if either doesn't have numbers
                if (!(xMatch.Success && yMatch.Success))
                {
                    // then if either does have numbers
                    if (xMatch.Success || yMatch.Success)
                    {
                        return xMatch.Success ? -1 : 1;
                    }
                    else
                    {
                        // otherwise neither have numbers
                        return x.CompareTo(y);
                    }
                }
                int xNum = 0, yNum = 0;
                Int32.TryParse(xMatch.Groups[1].Value, out xNum);
                Int32.TryParse(yMatch.Groups[1].Value, out yNum);
                return xNum - yNum;
            }
        }

        public static void MakeCommonPic(IWin32Window parent)
        {
            string inputFolder = MainForm.PickFolder(parent, "Pick directory to create CommonPic.dat from...", null);
            if (String.IsNullOrEmpty(inputFolder))
            {
                return;
            }
            string outputFileName = MainForm.GetSaveFileName(parent, "Save CommonPic.dat", "CommonPic.dat (*.dat)|commonpic.dat|All Files (*.*)|*.*", Path.GetDirectoryName(inputFolder));
            if (String.IsNullOrEmpty(outputFileName))
            {
                return;
            }
            DebugLogger.Log("Tools", "Creating Commonpic.dat from {0} to {1}", inputFolder, outputFileName);
            string[] files = Directory.GetFiles(inputFolder, "*.gtmp");
            if (files.Length < 10)
            {
                string[] moreFiles = Directory.GetFiles(inputFolder);
                if (moreFiles.Length > files.Length)
                {
                    files = moreFiles;
                }
            }
            MergeFiles(new List<string>(files), outputFileName, "Making CommonPic.dat", 0, 0, parent);
        }

        public static void MakeGTMenuDat(IWin32Window parent)
        {
            string inputFolder = MainForm.PickFolder(parent, "Pick directory to create GTMenuDat.dat from...", null);
            if (String.IsNullOrEmpty(inputFolder))
            {
                return;
            }
            string outputFileName = MainForm.GetSaveFileName(parent, "Save GTMenuDat.dat", "GTMenuDat.dat (*.dat)|gtmenudat.dat|All Files (*.*)|*.*", Path.GetDirectoryName(inputFolder));
            if (String.IsNullOrEmpty(outputFileName))
            {
                return;
            }
            DebugLogger.Log("Tools", "Creating GTMenuDat.dat from {0} to {1}", inputFolder, outputFileName);
            string[] files = Directory.GetFiles(inputFolder, "*.gm");
            if (files.Length < 10)
            {
                string[] moreFiles = Directory.GetFiles(inputFolder);
                if (moreFiles.Length > files.Length)
                {
                    files = moreFiles;
                }
            }
            MergeFiles(new List<string>(files), outputFileName, "Making GTMenuDat.dat", Globals.App.CompressionLevel, 4, parent);
        }

        private class MergeThreadParams
        {
            public WaitDlg dlg;
            public List<string> files;
            public string outFile;
            public int compression;
            public int fileAlignment;

            public MergeThreadParams(
                WaitDlg dialog, 
                List<string> inFiles, 
                string outputFile, 
                int compressLevel,
                int alignment)
            {
                dlg = dialog;
                files = inFiles;
                outFile = outputFile;
                compression = compressLevel;
                fileAlignment = alignment;
            }
        }

        private static void MergeThread(object o)
        {
#if TEST_AS_FRENCH
            Tools.SetThreadToFrench();
#endif
            MergeThreadParams mtp = (MergeThreadParams)o;
            WaitDlg dlg = mtp.dlg;
            List<string> inFiles = mtp.files;
            string fileDir = Path.GetDirectoryName(inFiles[0]);
            inFiles.Sort(new LogicalComparer(fileDir.Length));
            try
            {
                Stopwatch sw = new Stopwatch();
                using (Archiver archive = new Archiver(mtp.outFile, mtp.fileAlignment))
                {
                    sw.Start();
                    dlg.UpdateStatus("Adding {0} files", inFiles.Count);
                    archive.AddFiles(inFiles, mtp.compression);
                    dlg.UpdateStatus("Waiting for completion");
                    archive.Finish();
                    sw.Stop();
                }
                dlg.AllowClose("Completed in {0:F2} seconds", sw.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                string exception = String.Format("Failed with exception {0} during merge of {1} files", e.Message, inFiles.Count);
                dlg.AllowClose(exception);
                DebugLogger.Log("Merge", exception + "{0}{1}", Environment.NewLine, e.StackTrace);
            }
            GC.Collect();
        }

        private static void MergeFiles(List<string> inFiles, string outFile, string operation, int compression, int fileAlignment, IWin32Window parent)
        {
            if (inFiles.Count <= 1)
            {
                MainForm.DisplayMsgBox(MessageBoxButtons.OK, MessageBoxIcon.Error, "Directory contains only one or no files to merge");
                return;
            }
            using (WaitDlg dlg = new WaitDlg(operation))
            {
                dlg.UpdateStatus("Please wait while files are being merged and compressed");
                Thread mergeThread = new Thread(new ParameterizedThreadStart(MergeThread));
                mergeThread.SetApartmentState(ApartmentState.STA);
                MergeThreadParams mtp = new MergeThreadParams(dlg, inFiles, outFile, compression, fileAlignment);
                mergeThread.Start(mtp);
                dlg.ShowDialog(parent);
            }
        }

        private class SplitThreadParams
        {
            public FileSplitter splitter;
            public WaitDlg dlg;
            public SplitThreadParams(FileSplitter fs, WaitDlg waitDlg)
            {
                splitter = fs;
                dlg = waitDlg;
            }
        }

        private static void SplitThread(object o)
        {
#if TEST_AS_FRENCH
            Tools.SetThreadToFrench();
#endif
            SplitThreadParams stp = (SplitThreadParams)o;
            WaitDlg dlg = stp.dlg;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                stp.splitter();
                sw.Stop();
                dlg.AllowClose("Completed in {0:F2} seconds", sw.Elapsed.TotalSeconds);
            }
            catch (Exception e)
            {
                string exception = String.Format("Failed with exception {0} during split ", e.Message);
                dlg.AllowClose(exception);
                DebugLogger.Log("Split", exception + "{0}{1}", Environment.NewLine, e.StackTrace);
            }
            sw.Stop();
            GC.Collect();
        }

        private static void SplitFiles(FileSplitter fs, string operation)
        {
            Thread t = new Thread(new ParameterizedThreadStart(SplitThread));
            t.SetApartmentState(ApartmentState.STA);
            using (WaitDlg dlg = new WaitDlg(operation))
            {
                dlg.UpdateStatus("Please wait while files are being extracted and imaged");
                SplitThreadParams stp = new SplitThreadParams(fs, dlg);
                t.Start(stp);
                dlg.ShowDialog();
            }
        }

#if TEST_AS_FRENCH
        internal static void SetThreadToFrench()
        {
            Thread curThread = Thread.CurrentThread;
            System.Globalization.CultureInfo fr = System.Globalization.CultureInfo.GetCultureInfo("FR-FR");
            curThread.CurrentCulture = curThread.CurrentUICulture = fr;
        }
#endif
    }
}
