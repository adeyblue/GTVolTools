using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GT2VolToolGui
{
    public partial class MainForm : Form
    {
        private WindowsFormsSynchronizationContext wfss;
        public MainForm()
        {
            InitializeComponent();
            wfss = new WindowsFormsSynchronizationContext();
        }

        private void browseVolButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.CheckFileExists = false;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = "VOL";
                ofd.Title = "Open GT2(K).VOL File...";
                ofd.Filter = "VOL Files (*.vol)|*.vol|All Files (*.*)|*.*";
                ofd.FilterIndex = 0;
                DialogResult dr = ofd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    volNameTextBox.Text = ofd.FileName;
                }
            }
        }

        private void browseDirectoryButton_Click(object sender, EventArgs e)
        {
            // the FolderBtowserDialog doesn't allow you to add the flag for the edit box
            // and I always hate it when that's missing. This adds it for Windows by P/Invoking the
            // native function
            string title = "Select the directory that (will) contain the exploded files";
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string displayName = new string('\0', 260);
                Win32.BrowseFolderInfo bfi = new Win32.BrowseFolderInfo();
                bfi.flags = (uint)(Win32.BIF_Flags.BIF_USENEWUI | Win32.BIF_Flags.BIF_VALIDATE | Win32.BIF_Flags.BIF_RETURNONLYFSDIRS);
                bfi.title = title;
                bfi.displayName = displayName;
                IntPtr pPidl = Win32.SHBrowseForFolder(bfi);
                if (pPidl.ToInt64() != 0)
                {
                    StringBuilder path = new StringBuilder(260);
                    Win32.SHGetPathFromIDList(pPidl, path);
                    dirNameTextBox.Text = path.ToString();
                    Marshal.FreeCoTaskMem(pPidl);
                }
            }
            else
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.ShowNewFolderButton = true;
                    fbd.Description = title;
                    DialogResult dr = fbd.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        dirNameTextBox.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private void TryEnableButtons()
        {
            bool enable = !(String.IsNullOrEmpty(volNameTextBox.Text) || String.IsNullOrEmpty(dirNameTextBox.Text));
            volToDirButton.Enabled = dirToVolButton.Enabled = gt2kVolToDir.Enabled = gt3VolToDir.Enabled = enable;
        }

        private void DisableButtons()
        {
            volToDirButton.Enabled = dirToVolButton.Enabled = gt2kVolToDir.Enabled = gt3VolToDir.Enabled = false;
        }

        private void volNameTextBox_TextChanged(object sender, EventArgs e)
        {
            TryEnableButtons();
        }

        private void dirNameTextBox_TextChanged(object sender, EventArgs e)
        {
            TryEnableButtons();
        }

        class ThreadArgs
        {
            public string volFile;
            public string dir;
        }

        private void volToDirButton_Click(object sender, EventArgs e)
        {
            DisableButtons();
            ThreadArgs tArgs = new ThreadArgs();
            tArgs.dir = dirNameTextBox.Text;
            tArgs.volFile = volNameTextBox.Text;
            Thread t = new Thread(new ParameterizedThreadStart(ExplodeGT2Vol));
            t.SetApartmentState(ApartmentState.MTA);
            t.Start(tArgs);
            while (!t.Join(100))
            {
                Application.DoEvents();
            }
            wfss.Send(new SendOrPostCallback(SetStatusText), "Finished");
            TryEnableButtons();
        }

        private void dirToVolButton_Click(object sender, EventArgs e)
        {
            DisableButtons();
            ThreadArgs tArgs = new ThreadArgs();
            tArgs.dir = dirNameTextBox.Text;
            tArgs.volFile = volNameTextBox.Text;
            Thread t = new Thread(new ParameterizedThreadStart(DoRebuilding));
            t.SetApartmentState(ApartmentState.MTA);
            t.Start(tArgs);
            while (!t.Join(100))
            {
                Application.DoEvents();
            }
            wfss.Send(new SendOrPostCallback(SetStatusText), "Finished");
            TryEnableButtons();
        }

        private void gt2kVolToDir_Click(object sender, EventArgs e)
        {
            DisableButtons();
            try
            {
                Explode2KVol(volNameTextBox.Text, dirNameTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Caught the following exception while exploding:\n{0}", ex.Message), "GTVolTool");
            }
            statusText.Text = "Finished";
            TryEnableButtons();
        }

        void SetStatusText(object state)
        {
            statusText.Text = (string)state;
        }

        static int gt2VolNotifyCalls = 0;
        void EmbeddedFileNotify(GT2Vol.EmbeddedFileInfo efi)
        {
            if ((Interlocked.Increment(ref gt2VolNotifyCalls) % 5) == 0)
            {
                string message = String.Format("Found {0} at offset 0x{1:X} of size {2}", efi.name, efi.fileAddress, efi.size);
                wfss.Post(new SendOrPostCallback(SetStatusText), message);
            }
        }

        void GT2ExplodeCallback(string message)
        {
            if ((Interlocked.Increment(ref gt2VolNotifyCalls) % 5) == 0)
            {
                wfss.Post(new SendOrPostCallback(SetStatusText), message);
            }
        }

        private void DoRebuilding(object o)
        {
            ThreadArgs tArgs = (ThreadArgs)o;
            try
            {
                gt2VolNotifyCalls = 0;
                GT2Vol.Rebuilder r = new GT2Vol.Rebuilder();
                statusText.Text = "Scanning directory";
                List<GT2Vol.Rebuilder.FSEntry> entries = r.ScanAndCompress(tArgs.dir, false);
                if (entries.Count == 0)
                {
                    MessageBox.Show("There are no files in the directory!", "GT2VolTool");
                    return;
                }
                statusText.Text = "Building Header";
                GT2Vol.Rebuilder.HeaderInfo hi = r.BuildHeader(entries);
                statusText.Text = "Writing new VOL";
                r.WriteNewVol(tArgs.volFile, hi, entries, new GT2Vol.VolFile.ExplodeProgressCallback(GT2ExplodeCallback));
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Caught the following exception while building:\n{0}", ex.Message), "GT2VolTool");
            }
        }

        private void gt3VolToDir_Click(object sender, EventArgs e)
        {
            DisableButtons();
            ThreadArgs tArgs = new ThreadArgs();
            tArgs.dir = dirNameTextBox.Text;
            tArgs.volFile = volNameTextBox.Text;
            Thread t = new Thread(new ParameterizedThreadStart(ExplodeGT3Vol));
            t.SetApartmentState(ApartmentState.MTA);
            t.Start(tArgs);
            while (!t.Join(100))
            {
                Application.DoEvents();
            }
            wfss.Send(new SendOrPostCallback(SetStatusText), "Finished");
            TryEnableButtons();
        }

        class GT2KVolEntry
        {
            public string name; // prefixed with a single byte of length
            public int offset; // multiples of 0x800, big endian
            public int length; // in bytes, big endian
        }

        void Explode2KVol(string volFile, string dir)
        {
            try
            {
                using (FileStream vol2k = new FileStream(volFile, FileMode.Open, FileAccess.Read))
                {
                    List<GT2KVolEntry> files = new List<GT2KVolEntry>();
                    BinaryReader br = new BinaryReader(vol2k);
                    string name = br.ReadString();
                    while (!String.IsNullOrEmpty(name))
                    {
                        GT2KVolEntry file = new GT2KVolEntry();
                        file.name = name;
                        byte[] offsetArray = br.ReadBytes(4);
                        byte[] sizeArray = br.ReadBytes(4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(offsetArray);
                            Array.Reverse(sizeArray);
                        }
                        file.offset = BitConverter.ToInt32(offsetArray, 0) * 0x800;
                        file.length = BitConverter.ToInt32(sizeArray, 0);
                        files.Add(file);
                        name = br.ReadString();
                    }
                    int num = files.Count;
                    char[] pathSeps = new char[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar };
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < num; ++i)
                    {
                        vol2k.Seek(files[i].offset, SeekOrigin.Begin);
                        byte[] fileData = br.ReadBytes(files[i].length);
                        string fileName = files[i].name;

                        string[] pathParts = fileName.Split(pathSeps);
                        sb.AppendFormat("{0}{1}", dir, Path.DirectorySeparatorChar);
                        if (pathParts.Length > 1)
                        {
                            sb.AppendFormat("{0}{1}", pathParts[0], Path.DirectorySeparatorChar);
                            Directory.CreateDirectory(sb.ToString());
                            fileName = pathParts[1];
                        }
                        sb.Append(fileName);
                        File.WriteAllBytes(sb.ToString(), fileData);
                        statusText.Text = String.Format("Extracted {0}", fileName);
                        sb.Remove(0, sb.Length);
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(String.Format("GT2K Vol file couldn't be opened.\nError {0}", e.Message), "GT2K Vol Explode Error");
            }
        }

        void ExplodeGT2Vol(object o)
        {
            ThreadArgs tArgs = (ThreadArgs)o;
            try
            {
                string file = tArgs.volFile;
                if (!File.Exists(file))
                {
                    MessageBox.Show("Selected VOL file doesn't exist!", "GTVolTool");
                    return;
                }
                using (GT2Vol.VolFile vf = new GT2Vol.VolFile(file))
                {
                    gt2VolNotifyCalls = 0;
                    if (!vf.CheckAndCacheHeaderDetails())
                    {
                        MessageBox.Show("Incorrect or inconsistent VOL header", "GTVolTool");
                        return;
                    }
                    statusText.Text = "Exploding...";
                    vf.ParseToc(null);
                    gt2VolNotifyCalls = 0;
                    vf.Explode(tArgs.dir, false, new GT2Vol.VolFile.ExplodeProgressCallback(GT2ExplodeCallback));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Caught the following exception while exploding:\n{0}", ex.Message), "GTVolTool");
            }
        }

        void ExplodeGT3Vol(object o)
        {
            ThreadArgs tArgs = (ThreadArgs)o;
            try
            {
                string file = tArgs.volFile;
                if (!File.Exists(file))
                {
                    MessageBox.Show("Selected VOL file doesn't exist!", "GTVolTool");
                    return;
                }
                GT2Vol.GT3Vol vf = new GT2Vol.GT3Vol(file);
                gt2VolNotifyCalls = 0;
                statusText.Text = "Exploding...";
                vf.Extract(tArgs.dir, false, new GT2Vol.VolFile.ExplodeProgressCallback(GT2ExplodeCallback));
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Caught the following exception while exploding:\n{0}", ex.Message), "GTVolTool");
            }
        }

        static class Win32
        {
            [Flags]
            public enum BIF_Flags : short
            {
                BIF_RETURNONLYFSDIRS = 0x1,
                BIF_EDITBOX = 0x10,
                BIF_VALIDATE = 0x20,
                BIF_NEWDIALOGSTYLE = 0x40,
                BIF_USENEWUI = BIF_NEWDIALOGSTYLE | BIF_EDITBOX
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct BrowseFolderInfo
            {
                public IntPtr hwnd;
                public IntPtr pidl;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string displayName;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string title;
                public uint flags;
                public IntPtr callback;
                public IntPtr lParam;
                public int image;
            }
            [DllImport("shell32.dll", CallingConvention=CallingConvention.Winapi, EntryPoint="SHBrowseForFolderW", CharSet=CharSet.Unicode)]
            public static extern IntPtr SHBrowseForFolder(BrowseFolderInfo bfi);

            [DllImport("Shell32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "SHGetPathFromIDListW")]
            public static extern int SHGetPathFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder path);
        }
    }
}
