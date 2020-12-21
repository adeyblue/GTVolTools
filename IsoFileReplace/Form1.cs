using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace IsoFileReplace
{
    public partial class MainForm : Form
    {
        private string psxMode2File;
        public MainForm()
        {
            InitializeComponent();
            psxMode2File = Path.Combine(Application.StartupPath, "psx-mode2.exe");
            if (!File.Exists(psxMode2File))
            {
                MessageBox.Show("psx-mode2.exe wasn't found. This is required for this tool to work", "Iso File Replacer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(2);
            }
        }

        public static string GetOpenFileName(IWin32Window parent, string title, string filter)
        {
            string fileName = null;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.AutoUpgradeEnabled = true;
                ofd.CheckPathExists = true;
                ofd.ValidateNames = true;
                ofd.CheckFileExists = true;
                if (!String.IsNullOrEmpty(title))
                {
                    ofd.Title = title;
                }
                ofd.Filter = filter;
                ofd.FilterIndex = 0;
                if (ofd.ShowDialog(parent) == DialogResult.OK)
                {
                    fileName = ofd.FileName;
                }
            }
            return fileName;
        }

        private void CheckEnableStartButton()
        {
            startButton.Enabled = (
                (isoFileName.Text.Length > 0) &&
                (replacementFilePath.Text.Length > 0) && 
                (newFilePath.Text.Length > 0)
            );
        }

        private void selectIsoButton_Click(object sender, EventArgs e)
        {
            string iso = GetOpenFileName(this, "Select ISO to Modify", "ISO/Bin files (*.iso, *.bin)|*.iso;*.bin|All Files (*.*)|*.*");
            if (String.IsNullOrEmpty(iso))
            {
                return;
            }
            isoFileName.Text = iso;
        }

        private void newFileButton_Click(object sender, EventArgs e)
        {
            string newFile = GetOpenFileName(this, "Select File to Insert", "All Files (*.*)|*.*");
            if (String.IsNullOrEmpty(newFile))
            {
                return;
            }
            newFilePath.Text = newFile;
        }

        private void isoFileName_TextChanged(object sender, EventArgs e)
        {
            CheckEnableStartButton();
        }

        private void newFilePath_TextChanged(object sender, EventArgs e)
        {
            CheckEnableStartButton();
        }

        private void replacementFilePath_TextChanged(object sender, EventArgs e)
        {
            CheckEnableStartButton();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            string isoFile = isoFileName.Text;
            string newFile = newFilePath.Text;
            string replacementPath = replacementFilePath.Text;
            if(!(File.Exists(isoFile) && File.Exists(newFile)))
            {
                MessageBox.Show("The Iso/bin and/or the new file don't exist!", "Iso File Replacer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            outputText.Text = String.Empty;
            startButton.Enabled = false;
            PsxMode2.Run(psxMode2File, isoFile, newFile, replacementPath, this);
        }

        private void RealUpdateText(string text)
        {
            outputText.AppendText(text + Environment.NewLine);
        }

        public void UpdateText(string text)
        {
            BeginInvoke(new Action<string>(RealUpdateText), text);
        }

        public void EnableOKButton()
        {
            startButton.Enabled = true;
        }
    }
}
