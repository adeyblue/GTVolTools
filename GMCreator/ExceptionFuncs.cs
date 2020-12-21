using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GMCreator
{
    static class ExceptionFuncs
    {
        static internal void Initialise()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GMCreatorUnhandledException);
            Application.ThreadException += new ThreadExceptionEventHandler(GMCreatorUnhandledThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        }

        static void GMCreatorUnhandledThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ReportUnhandledException(e.Exception, "GT2 GMCreator encountered an unhandled thread error. Please report this to");
        }

        static void GMCreatorUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportUnhandledException((Exception)e.ExceptionObject, "GT2 GMCreator encountered an unhandled error. Please report this to");
        }

        static internal void ReportUnhandledException(Exception e, string titleLine)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "gmcreatorerror.txt");
            StringBuilder sb = new StringBuilder(2048);
            sb.AppendLine(titleLine);
            sb.AppendLine("adeyblue@airesoft.co.uk attaching a screen cap of this message or");
            sb.AppendFormat("the file at \"{0}\"", tempPath);
            sb.AppendLine();
            sb.AppendLine("Exception Message:");
            sb.AppendLine(e.Message);
            sb.AppendLine();
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(e.StackTrace);
            string message = sb.ToString();
            DebugLogger.Log("Program", "Caught top level exception:\n" + message);
            using (StreamWriter sw = new StreamWriter(tempPath, false, Encoding.UTF8))
            {
                sw.WriteLine(message);
                sw.WriteLine("Debug Log:");
                sw.WriteLine(DebugLogger.GetContents());
            }
            MessageBox.Show(message, "GMCreator", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
