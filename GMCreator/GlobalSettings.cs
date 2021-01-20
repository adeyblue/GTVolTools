using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace GMCreator
{
    [Serializable()]
    class AppSettings
    {
        public int CentralAnchorWidth = 10;
        public int CentralAnchorHeight = 10;
        public int ResizeAnchorWidth = 5;
        public int ResizeAnchorHeight = 5;
        public Color AnchorColour = Color.Gray;
        public Color DefaultOutlineColour = Color.Yellow;
        public Color SelectedOutlineColour = Color.White;
        public Color TransparentBackgroundAreasColour = Color.Magenta;
        public int CompressionLevel = 9;
        public bool ShowInnerContent = true;
        public IconImgType GT2Version = IconImgType.Invalid;

        public Rectangle WindowLocation = Rectangle.Empty;

        [NonSerialized()]
        public Brush AnchorBrush = null;
        [NonSerialized()]
        public Pen DefaultOutlinePen = null;

        public event EventHandler SettingsChanged;

        public void FireSettingsChanged()
        {
            if (SettingsChanged != null)
            {
                SettingsChanged(this, EventArgs.Empty);
            }
        }

        public void RefreshNonSerialised()
        {
            SolidBrush newAnchorBrush = new SolidBrush(AnchorColour);
            Brush currentAnchorBrush = AnchorBrush;
            AnchorBrush = newAnchorBrush;
            if (currentAnchorBrush != null)
            {
                currentAnchorBrush.Dispose();
            }
            Pen newDefaultPen = new Pen(DefaultOutlineColour);
            Pen currentDefaultPen = DefaultOutlinePen;
            DefaultOutlinePen = newDefaultPen;
            if (currentDefaultPen != null)
            {
                currentDefaultPen.Dispose();
            }
        }
    }

    static class Globals
    {
        static internal AppSettings App
        {
            get;
            private set;
        }

        private const string SETTINGS_FILE = "settings.json";

        private static int g_fgImageCounter = 0;
        private static string g_debugSaveDir;

        static public void Load(string fileDir)
        {
            string filePath = Path.Combine(fileDir, SETTINGS_FILE);
            try
            {
                string json = File.ReadAllText(filePath);
                App = Json.Parse<AppSettings>(json);
            }
            catch (Exception e)
            {
                DebugLogger.Log("Settings", "Caught exception {0} loading settings file from {1}", e.Message, filePath);
                App = new AppSettings();
            }
            App.RefreshNonSerialised();
        }

        static public void Save(string fileDir, Rectangle windowBounds)
        {
            string filePath = Path.Combine(fileDir, SETTINGS_FILE);
            try
            {
                App.WindowLocation = windowBounds;
                string json = Json.Serialize(App);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                DebugLogger.Log("Settings", "Caught exception {0} saving settings file to {1}", e.Message, filePath);
            }
        }

        internal static void RestartLoggingDir(string dir)
        {
            g_debugSaveDir = dir;
            try
            {
                Directory.Delete(dir, true);
            }
            // won't exist the first time
            catch (Exception)
            { }
            Directory.CreateDirectory(dir);
        }

        internal static string MakeDebugSaveName(bool incrementNumber, string pattern, params object[] objs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(g_debugSaveDir);
            sb.Append(Path.DirectorySeparatorChar);
            if (incrementNumber)
            {
                ++g_fgImageCounter;
            }
            sb.AppendFormat("{0}-", g_fgImageCounter);
            sb.AppendFormat(pattern, objs);
            return sb.ToString();
        }
    }
}
