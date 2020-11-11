using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;

namespace GMCreator
{
    static class DebugLogger
    {
        private static void DumpEnvironment()
        {
            OperatingSystem os = Environment.OSVersion;
            string message = String.Format(
                "Logging started at {0}{7}OS: {1}{7}.Net Framework version: {2}{7}UI Culture: {3}{7}Culture: {4}{7}Command Line: {5}{7}Startup Path: {6}",
                DateTime.Now.ToString("R"),
                os.VersionString,
                Environment.Version.ToString(4),
                Thread.CurrentThread.CurrentUICulture.EnglishName,
                Thread.CurrentThread.CurrentCulture.EnglishName,
                Environment.CommandLine,
                System.Windows.Forms.Application.StartupPath,
                Environment.NewLine
            );
            Log("Init", message);
        }

        static object g_lock = null;
        static StringBuilder g_logStringStream;

        internal static void Initialise()
        {
#if FILE_LOGGING
            string extraLog = Path.Combine(Path.GetTempPath(), "gmcreatorlog.txt");
            TextWriterTraceListener twtl = new TextWriterTraceListener(new StreamWriter(extraLog, false, Encoding.UTF8), "GMCreatorFileLog");
            Debug.Listeners.Add(twtl);
#endif
            if (g_lock == null)
            {
                g_lock = new object();
            }
            g_logStringStream = new StringBuilder(1024 * 1024);
            DumpEnvironment();
        }

        internal static string GetContents()
        {
            string contents;
            lock (g_lock)
            {
                contents = g_logStringStream.ToString();
            }
            return contents;
        }

        internal static void Reset()
        {
            lock (g_lock)
            {
                Initialise();
            }
        }

        internal static void Log(string who, string message, params object[] args)
        {
            Log(who, String.Format(message, args));
        }

        internal static void Log(string who, string message)
        {
            lock (g_lock)
            {
                g_logStringStream.AppendFormat("{0}: {1}", who, message);
                g_logStringStream.AppendLine();
            }
#if FILE_LOGGING || DEBUG
            Debug.WriteLine(message, who);
#endif
        }
    }
}
