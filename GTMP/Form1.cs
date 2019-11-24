using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GTMP
{
    public partial class GTMPForm : Form
    {
        string openedFile;
        public GTMPForm()
        {
            InitializeComponent();
            openedFile = null;
        }

        private bool IsFileDrop(IDataObject ido, out string file)
        {
            bool isFile = false;
            file = null;
            string stringFormat = System.Windows.Forms.DataFormats.FileDrop;
            if (ido.GetDataPresent(stringFormat))
            {
                string[] data = (string[])ido.GetData(stringFormat);
                if (File.Exists(data[0]))
                {
                    file = data[0];
                    isFile = true;
                }
            }
            return isFile;
        }

        private void GTMPForm_DragDrop(object sender, DragEventArgs e)
        {
            string file = null;
            if (IsFileDrop(e.Data, out file))
            {
                openedFile = file;
                OpenFile(openedFile);
                this.Text = "GTMP (Drag + Drop) - " + Path.GetFileName(file);
            }
        }

        private void GTMPForm_DragEnter(object sender, DragEventArgs e)
        {
            string notNeeded = null;
            e.Effect = IsFileDrop(e.Data, out notNeeded) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void picBox_DragDrop(object sender, DragEventArgs e)
        {
            GTMPForm_DragDrop(sender, e);
        }

        private void picBox_DragEnter(object sender, DragEventArgs e)
        {
            GTMPForm_DragEnter(sender, e);
        }

        private void OpenFile(string file)
        {
            //Bitmap bm = GTMPFile.Parse(file);
            //Bitmap bm = GMFile.Parse(file);
            Bitmap bm = GT3Tex.Parse(file);
            if (bm == null)
            {
                MessageBox.Show("This isn't a GTMP file!");
                return;
            }
            picBox.Image = bm;
            picBox.Refresh();
        }
    }
}
