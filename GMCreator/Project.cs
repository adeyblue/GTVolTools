using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace GMCreator
{
    class GMSerializedProject
    {
        // base64 encoded
        public string foreground;
        public string background;

        public List<IconImgBox> iconBoxes;
        public List<Box> boxes;
        public GTMP.GMFile.GMMetadata fileMetadata;
        public IconImgType gt2BoxVersion;
    }

    static class GMProject
    {
        public static void SeparateBoxes(List<IBox> boxes, out List<Box> bigBoxes, out List<IconImgBox> iconBoxes)
        {
            bigBoxes = new List<Box>(boxes.Count);
            iconBoxes = new List<IconImgBox>(boxes.Count);
            foreach (IBox ib in boxes)
            {
                Box b = ib as Box;
                if (b != null)
                {
                    bigBoxes.Add(b);
                }
                else
                {
                    iconBoxes.Add((IconImgBox)ib);
                }
            }
        }
        public static void Save(
            string fileName, 
            Image bg, 
            byte[] fgGMLL, 
            List<IBox> boxes, 
            GTMP.GMFile.GMMetadata metadata,
            IconImgType fileVersion
        )
        {
            Debug.Assert(fileVersion != IconImgType.Invalid);
            DebugLogger.Log("Project", "Saving project to {0}", fileName);
            GMSerializedProject gmp = new GMSerializedProject();
            gmp.foreground = (fgGMLL != null) ? Convert.ToBase64String(fgGMLL) : String.Empty;
            byte[] imageData = Images.GetBytes(bg, System.Drawing.Imaging.ImageFormat.Png);
            gmp.background = Convert.ToBase64String(imageData);
            gmp.fileMetadata = metadata;
            gmp.gt2BoxVersion = fileVersion;
            SeparateBoxes(boxes, out gmp.boxes, out gmp.iconBoxes);
            string projectFile = Json.Serialize(gmp);
#if DEBUG
            File.WriteAllText(Path.Combine(Globals.DebugOutputPath, "gmpproj.txt"), projectFile);
#endif
            MemoryStream compProj = Compress.ZipCompressString(projectFile);
            File.WriteAllBytes(fileName, compProj.ToArray());
            if (DebugLogger.DoDebugActions())
            {
                string projectCopy = Globals.MakeDebugSaveName(true, Path.GetFileName(fileName));
                File.WriteAllBytes(projectCopy, compProj.ToArray());
            }
        }

        public static bool Load(string fileName, out Bitmap bg, out Bitmap fg, out byte[] fgGMLL, out List<IBox> boxes, out GTMP.GMFile.GMMetadata metadata)
        {
            fg = bg = null;
            fgGMLL = null;
            boxes = null;
            metadata = null;
            DebugLogger.Log("Project", "Loading file at {0}", fileName);
            if (DebugLogger.DoDebugActions())
            {
                string projectCopy = Globals.MakeDebugSaveName(true, Path.GetFileName(fileName));
                File.Copy(fileName, projectCopy, true);
            }
            byte[] projectBytes;
            using(FileStream fs = File.OpenRead(fileName))
            {
                projectBytes = Compress.DecompressStream(fs).ToArray();
            }
            string jsonText = Encoding.UTF8.GetString(projectBytes);
            GMSerializedProject projectData = Json.Parse<GMSerializedProject>(jsonText);
            IconImgType projType = projectData.gt2BoxVersion;
            IconImgType currentType = Globals.App.GT2Version;

            Debug.Assert(projType != IconImgType.Invalid);
            if (projType != currentType)
            {
                System.Windows.Forms.DialogResult res = MainForm.DisplayMsgBox(
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                    System.Windows.Forms.MessageBoxIcon.Question,
                    "{0} was saved with GT2 Version {1} which is different from the current version {2}. Change the current version?",
                    fileName,
                    projType,
                    currentType
                );
                if (res != System.Windows.Forms.DialogResult.Yes)
                {
                    return false;
                }
                Globals.App.GT2Version = projType;
                Hardcoded.Refresh(System.Windows.Forms.Application.StartupPath);
            }
            byte[] imageData;
            if (!String.IsNullOrEmpty(projectData.background))
            {
                imageData = Convert.FromBase64String(projectData.background);
                bg = Images.FromBytes(imageData);
            }
            else bg = null;
            if (!String.IsNullOrEmpty(projectData.foreground))
            {
                fgGMLL = Convert.FromBase64String(projectData.foreground);
                fg = Images.FromBytes(fgGMLL);
            }
            metadata = projectData.fileMetadata;
            boxes = new List<IBox>();
            foreach (IconImgBox img in projectData.iconBoxes)
            {
                boxes.Add(img);
            }
            foreach (Box box in projectData.boxes)
            {
                boxes.Add(box);
            }
            return true;
        }

        internal static void DeleteFileLoop(string fileName)
        {
            // this should be necessary, but anti-virus on demand scanning
            // is a thing, and they sometimes don't let go of the file for a little while
            // and trying to delete causes an exception because its still 'in use',
            // even though we've stopped using it.
            // so we have to loop on it
            for (int i = 0; i < 3; ++i)
            {
                try
                {
                    File.Delete(fileName);
                    break;
                }
                catch (FileNotFoundException)
                {
                    break;
                }
                catch (Exception e)
                {
                    DebugLogger.Log("Project", "Caught exception {0} deleting {1}", e.Message, fileName);
                    System.Threading.Thread.Sleep(300);
                }
            }
        }

        public static void ExportGM(
            string fileName, 
            List<IBox> boxes, 
            GTMP.GMFile.GMMetadata metadata,
            byte[] gmllData
        )
        {
            List<Box> bigBoxes;
            List<IconImgBox> icons;
            SeparateBoxes(boxes, out bigBoxes, out icons);
            DebugLogger.Log(
                "Project", 
                "Exporting as GMFile to {0} with {1} boxes, {2} item boxes, metadata ({3}), and gmllData of {4} bytes",
                fileName, 
                bigBoxes.Count, 
                icons.Count,
                metadata.ToString(),
                (gmllData == null) ? -1 : gmllData.Length
            );
            // this can be possible is the user just starts drawing boxes and wants to export that
            // without loading an associated image
            if (gmllData == null)
            {
                // 4 - "GMLL"
                // 4 - int for number of positions
                // 0 - position data length
                // 32 * 16 * 2 = 32 palettes of 16 2-byte colour
                // 4 - int for number of tiles
                // 0 - bytes of tile pixel data
                gmllData = new byte[4 + 4 + 0 + (32 * 16 * 2) + 4 + 0];
                Array.Copy(Encoding.ASCII.GetBytes("GMLL"), gmllData, 4);
            }
            try
            {
                using (MemoryStream ms = new MemoryStream(15000))
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    int numGroups = boxes.Max(box => box.Group) + 1;
                    bw.Write(Encoding.ASCII.GetBytes("GM\x3\x0"));
                    bw.Write(numGroups); // 0x4

                    if (icons.Any() || bigBoxes.Any())
                    {
                        for (int i = 0; i < numGroups; i++)
                        {
                            ExportBoxGroup(bw, icons, bigBoxes, i);
                        }
                        ExportUngroupedBoxes(bw, icons, bigBoxes);
                    }

                    // if any box is a CarDisplay, set flag 2 in the metadata flags
                    // check for more than one box with the default cursor position attribute
                    // don't care of there aren't any
                    int numCars = 0;
                    int defaultCursors = 0;
                    foreach (Box b in bigBoxes)
                    {
                        if (b.Contents == GTMP.GMFile.BoxItem.CarDisplay)
                        {
                            if (++numCars > 1)
                            {
                                throw new InvalidBoxStateException("The game will not render more than 1 CarDisplay box", b);
                            }
                        }
                        if ((b.BehaviourAttributes & GTMP.GMFile.InfoBoxAttributes.DefaultCursorPos) != 0)
                        {
                            if (++defaultCursors > 1)
                            {
                                throw new InvalidBoxStateException("More than one box has the DefaultCursorPos BehaviourAttribute set", b);
                            }
                        }
                    }

                    int screenBehaviour = (numCars != 0) ? 2 : 0;
                    if (metadata.BackLinkToPreviousScreen)
                    {
                        screenBehaviour = 3;
                    }
                    bw.Write((sbyte)metadata.ManufacturerID);
                    bw.Write((byte)screenBehaviour);
                    bw.Write((ushort)metadata.Music);
                    bw.Write(metadata.BackLink);
                    bw.Write((int)metadata.BackgroundIndex);
                    bw.Write(gmllData);
                    bw.Flush();

                    ms.Position = 0;
#if DEBUG
                    File.WriteAllBytes(Path.Combine(Globals.DebugOutputPath, "uncompressedgm.gm"), ms.ToArray());
#endif
                    MemoryStream compressed = Compress.GZipCompressStream(ms);
                    byte[] compressedBytes = compressed.ToArray();
                    File.WriteAllBytes(fileName, compressedBytes);
                }
            }
            catch (InvalidBoxStateException ibse)
            {
                MainForm.DisplayMsgBox(
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error,
                    ibse.Message
                );
            }
        }

        private static void ExportBoxGroup(BinaryWriter bw, List<IconImgBox> icons, List<Box> bigBoxes, int group)
        {
            IconImgBox[] groupIcons = icons.Where(box => box.Group == group).ToArray();
            Box[] groupBigBoxes = bigBoxes.Where(box => box.Group == group).ToArray();
            if (groupIcons.Any() || groupBigBoxes.Any())
            {
                bw.Write((ushort)groupIcons.Length);
                bw.Write((ushort)groupBigBoxes.Length);
                for (int j = 0; j < groupIcons.Length; ++j)
                {
                    groupIcons[j].Serialize(bw);
                }
                for (int j = 0; j < groupBigBoxes.Length; ++j)
                {
                    groupBigBoxes[j].Serialize(bw);
                }
            }
        }

        private static void ExportUngroupedBoxes(BinaryWriter bw, List<IconImgBox> icons, List<Box> bigBoxes)
        {
            IconImgBox[] groupIcons = icons.Where(box => box.Group == -1).ToArray();
            Box[] groupBigBoxes = bigBoxes.Where(box => box.Group == -1).ToArray();
            if (groupIcons.Any())
            {
                throw new InvalidBoxStateException("IconImg boxes must be in a group of 0 or higher", groupIcons.First());
            }
            else if (groupBigBoxes.Any())
            {
                bw.Write((uint)groupBigBoxes.Length);
                for (int j = 0; j < groupBigBoxes.Length; ++j)
                {
                    groupBigBoxes[j].Serialize(bw);
                }
            }
        }
    }
}
