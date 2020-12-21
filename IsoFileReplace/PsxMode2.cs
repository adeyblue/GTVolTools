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
            Process proc = new Process();
            proc.SynchronizingObject = statusWindow;
            proc.StartInfo = psi;
            proc.Exited += new EventHandler(PsxMode2Exited);
            proc.EnableRaisingEvents = true;
            UIUpdater updater = new UIUpdater(statusWindow);
            proc.ErrorDataReceived += new DataReceivedEventHandler(updater.LineReceived);
            proc.OutputDataReceived += new DataReceivedEventHandler(updater.LineReceived);
            statusWindow.UpdateText("Process startng... (this may take a while)");
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
        }

        static private void PsxMode2Exited(object sender, EventArgs e)
        {
            Process p = (Process)sender;
            MainForm mf = (MainForm)p.SynchronizingObject;
            mf.EnableOKButton();
            p.Dispose();
        }
    }

    class UIUpdater
    {
        private MainForm ui;
        public UIUpdater(MainForm mainForm)
        {
            ui = mainForm;
        }

        public void LineReceived(object sender, DataReceivedEventArgs dataRecv)
        {
            ui.UpdateText(dataRecv.Data);
        }
    }
}
