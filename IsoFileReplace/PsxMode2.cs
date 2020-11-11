using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace IsoFileReplace
{
    static class PsxMode2
    {
        class RunThreadArgs
        {
            public ProcessStartInfo psi;
            public MainForm statusWindow;
        }

        static internal void Run(
            string psxMode2Exe,
            string isoFile, 
            string newFile, 
            string replacePath, 
            MainForm statusWindow
        )
        {
            // this needs to start with a slash
            if (replacePath[0] != '\\')
            {
                replacePath = '\\' + replacePath;
            }
            // path separating forward slashes need to be replaced with backslashes
            // which is what psx-mode2.exe expects
            replacePath = replacePath.Replace('/', '\\');
            string arguments = String.Format("\"{0}\" \"{1}\" \"{2}\"", Path.GetFullPath(isoFile), replacePath, Path.GetFullPath(newFile));
            ProcessStartInfo psi = new ProcessStartInfo(psxMode2Exe, arguments);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            RunThreadArgs rta = new RunThreadArgs();
            rta.psi = psi;
            rta.statusWindow = statusWindow;
            ThreadPool.QueueUserWorkItem(new WaitCallback(RunProcess), rta);
        }

        static private void RunProcess(object o)
        {
            RunThreadArgs rta = (RunThreadArgs)o;
            ProcessStartInfo psi = rta.psi;
            MainForm statusWindow = rta.statusWindow;
            ProcessWithOutput.HandleRedirect outputRedirect = new ProcessWithOutput.HandleRedirect(AppendStatusLine);
            statusWindow.UpdateText("Starting process...");
            using (ProcessWithOutput pwo = new ProcessWithOutput(outputRedirect, outputRedirect))
            {
                pwo.StartInfo = psi;
                pwo.Tag = statusWindow;
                WaitHandle mre = pwo.GetExitEvent();
                pwo.Start();
                pwo.StartAsyncOps();
                mre.WaitOne();
            }
            statusWindow.UpdateText("Finished");
        }

        static private void AppendStatusLine(ProcessWithOutput sender, string line)
        {
            MainForm statusWindow = (MainForm)sender.Tag;
            statusWindow.UpdateText(line);
        }
    }

    class ProcessWithOutput : Process
    {
        public delegate void HandleRedirect(ProcessWithOutput sender, string line);

        public object Tag { get; set; }

        private HandleRedirect outputRedirect;
        private HandleRedirect errorRedirect;
        private ManualResetEvent exitEvent;

        public ProcessWithOutput()
            : base()
        {
            Tag = null;
            this.EnableRaisingEvents = true;
            this.Exited += new EventHandler(ProcessWithOutput_Exited);
            exitEvent = new ManualResetEvent(false);
            this.ErrorDataReceived += new DataReceivedEventHandler(ErrorLineReceived);
            this.OutputDataReceived += new DataReceivedEventHandler(OutputLineReceived);
        }

        public ProcessWithOutput(HandleRedirect outputRedirector, HandleRedirect errorRedirector)
            : this()
        {
            outputRedirect = outputRedirector;
            errorRedirect = errorRedirector;
        }

        public void StartAsyncOps()
        {
            BeginOutputReadLine();
            BeginErrorReadLine();
        }

        public WaitHandle GetExitEvent()
        {
            return exitEvent;
        }

        private void ForwardLine(HandleRedirect redirFunc, DataReceivedEventArgs args)
        {
            if (args.Data != null)
            {
                string line = args.Data.TrimEnd();
                if (line.Length > 0)
                {
                    if (redirFunc != null)
                    {
                        redirFunc(this, line);
                    }
                }
            }
        }

        private void OutputLineReceived(object sender, DataReceivedEventArgs args)
        {
            ForwardLine(outputRedirect, args);
        }

        private void ErrorLineReceived(object sender, DataReceivedEventArgs args)
        {
            ForwardLine(errorRedirect, args);
        }

        private void ProcessWithOutput_Exited(object sender, EventArgs e)
        {
            exitEvent.Set();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (exitEvent != null)
            {
                exitEvent.Close();
                exitEvent = null;
            }
        }
    }
}
