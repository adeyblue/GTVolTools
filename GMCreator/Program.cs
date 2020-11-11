using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GMCreator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            DebugLogger.Initialise();
            ExceptionFuncs.Initialise();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception e)
            {
                ExceptionFuncs.ReportUnhandledException(e, "GT2 GMCreator encountered a fatal error. Please report this to");
            }
        }
    }
}
