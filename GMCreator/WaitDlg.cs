using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GMCreator
{
    public partial class WaitDlg : Form
    {
        private string newLine;
        public WaitDlg(string title)
        {
            InitializeComponent();
            this.Text = title;
            newLine = Environment.NewLine;
        }

        private void RealUpdateStatus(string text)
        {
            messageText.AppendText(text + newLine);
        }

        private void RealAllowClose(string finalStatus)
        {
            okButton.Enabled = true;
            if (!String.IsNullOrEmpty(finalStatus))
            {
                RealUpdateStatus(finalStatus);
            }
        }

        public void UpdateStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(RealUpdateStatus), text);
            }
            else
            {
                RealUpdateStatus(text);
            }
        }

        public void UpdateStatus(string format, params object[] args)
        {
            UpdateStatus(String.Format(format, args));
        }

        public void AllowClose(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(RealAllowClose), text);
            }
            else
            {
                RealAllowClose(text);
            }
        }

        public void AllowClose(string format, params object[] args)
        {
            AllowClose(String.Format(format, args));
        }

        private void WaitDlg_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !okButton.Enabled;
        }
    }
}
