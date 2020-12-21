using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GMCreator
{
    static class Hardcoded
    {
        static internal class IconImages
        {
            private static List<IconImgEntry> images;

            public static IconImgEntry Get(int id)
            {
                return images[id];
            }

            public static Dictionary<string, int> GetNamesAndIndexes()
            {
                Dictionary<string, int> names = new Dictionary<string, int>();
                int i = 0;
                foreach (IconImgEntry e in images)
                {
                    names.Add(e.Name, i);
                    ++i;
                }
                return names;
            }

            public static IconImgEntry FindFromData(int imgMapX, int imgMapY)
            {
                Point topLeft = new Point(imgMapX, imgMapY);
                foreach (IconImgEntry entry in images)
                {
                    if (entry.ImageLocation == topLeft)
                    {
                        return entry;
                    }
                }
                return null;
            }

            internal static void Dispose()
            {
                foreach (IconImgEntry entry in images)
                {
                    entry.Image.Dispose();
                }
                images = null;
            }

            private static Color MakeColorFromBGR555(ushort colour)
            {
                int green = (colour & (0x1f << 5)) >> 5;
                int blue = colour & 0x1f;
                int red = (colour & (0x1f << 10)) >> 10;
                const float mulFactor = (float)0xff / 0x1f;
                green = (int)(green * mulFactor);
                blue = (int)(blue * mulFactor);
                red = (int)(red * mulFactor);
                return Color.FromArgb(0xff, blue, green, red);
            }

            internal static void Refresh(IconImgData imageData, IconImgType version, string directory)
            {
                Debug.Assert(version != IconImgType.Invalid);
                DebugLogger.Log("Hardcoded", "Refreshing IconImages of version {0}", version);
                int stride = imageData.ByteStride;
                IconImgFile iconFileInfo = imageData.Versions[(int)version];
                string iconImgFile = Path.Combine(directory, version.ToString() + "Icon.dat");
                byte[] fileData = File.ReadAllBytes(iconImgFile);
                IconImgEntry[] imageEntries = iconFileInfo.Entries;
                int id = 0;
                images = new List<IconImgEntry>();
                foreach (IconImgEntry entry in imageEntries)
                {
                    Size imageSize = entry.ImageSize;
                    Bitmap bm = new Bitmap(imageSize.Width, imageSize.Height, System.Drawing.Imaging.PixelFormat.Format4bppIndexed);
                    System.Drawing.Imaging.ColorPalette imgPalette = bm.Palette;
                    Color[] entries = imgPalette.Entries;
                    for (int i = 0, colourOffset = entry.PaletteLocation; i < 16; ++i, colourOffset += sizeof(ushort))
                    {
                        ushort colour = BitConverter.ToUInt16(fileData, colourOffset);
                        entries[i] = ((colour & 0x7fff) == 0) ? Color.Transparent : MakeColorFromBGR555(colour);
                    }
                    bm.Palette = imgPalette;
                    System.Drawing.Imaging.BitmapData bmData = bm.LockBits(new Rectangle(Point.Empty, bm.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format4bppIndexed);
                    // These are 4bpp, so the 1 X byte = 2 X pixels, so we half it
                    int lineBytes = imageSize.Width / 2;
                    int height = imageSize.Height;
                    int lockDataStride = bmData.Stride;
                    byte[] imageBuffer = new byte[lockDataStride * height];
                    int fileIter = (entry.ImageLocation.Y * stride) + (entry.ImageLocation.X / 2);
                    int imageBufferIter = 0;
                    // the image buffer in the file is Big endian, like this (pixel numbers within each byte)
                    // [2, 1], [4, 3], [6, 5]
                    // we need it in little endian so this copies the data while swizzling it into
                    // [1, 2], [3, 4], [5, 6]
                    // 
                    for (int y = 0; y < height; ++y, fileIter += stride, imageBufferIter += lockDataStride)
                    {
                        Buffer.BlockCopy(fileData, fileIter, imageBuffer, imageBufferIter, lineBytes);
                        for (int x = 0; x < lineBytes; ++x)
                        {
                            byte pixels = imageBuffer[imageBufferIter + x];
                            byte swizzedPixels = (byte)(((pixels << 4) & 0xF0) | (pixels >> 4));
                            imageBuffer[imageBufferIter + x] = swizzedPixels;
                        }
                    }
                    // blit it to the real bitmap data
                    System.Runtime.InteropServices.Marshal.Copy(imageBuffer, 0, bmData.Scan0, imageBuffer.Length);
                    bm.UnlockBits(bmData);
                    entry.Image = bm;
                    entry.Id = id++;
                    images.Add(entry);
                }
            }
        }

        static public CarListMeasurements UsedCarListMeasurements;
        static public CarListMeasurements GarageCarListMeasurements;
        static public CarPartsListMeasurements EquippedPartsListMeasurements;
        static public Brush UpgradeHPTextColour;
        static public Brush DealershipPriceColour;
        static public Brush StandardFontColour;
        static public Brush PartFontColour;
        static public Brush EquippedPartFontColour;
        static public Size ColourSwatchSize;
        static public Font StandardFont;
        static public Font BigFont;
        static public Font PartFont;
        static public Bitmap SmallLicenseGraphic;
        static public Bitmap LargeLicenseGraphic;
        static public Bitmap LicenseTrophyGraphic;
        static public Bitmap CarPicture;
        static public Bitmap CarLogo;
        static public Bitmap DrivetrainGraphic;

        static private string GetExePath()
        {
            string codeLoc = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            string localUrlPath = new Uri(codeLoc).LocalPath;
            return Path.GetDirectoryName(localUrlPath);
        }

        [Conditional("DEBUG")]
        static private void DumpFontInfo(string name, Font font)
        {
            string toMeasure = "Suspension - Fully Customized Service";
            StringBuilder deets = new StringBuilder(name);
            deets.AppendLine();
            deets.AppendLine("-------------");
            deets.AppendFormat("Name: {0}, Line Height: {1}, Points: {2}", font.Name, font.Height, font.SizeInPoints);
            deets.AppendLine();
            using(Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                using(StringFormat def = new StringFormat(StringFormat.GenericDefault))
                using(StringFormat defType = new StringFormat(StringFormat.GenericTypographic))
                {
                    CharacterRange[] range = new CharacterRange[2] {
                        new CharacterRange(0, 6),
                        new CharacterRange(9, 9)
                    };
                    def.SetMeasurableCharacterRanges(range);
                    defType.SetMeasurableCharacterRanges(range);
                    RectangleF rf = new RectangleF(32, 160, MainForm.CANVAS_WIDTH - 32, MainForm.CANVAS_HEIGHT - 160);
                    Region[] regs = g.MeasureCharacterRanges(toMeasure, font, rf, def);
                    DisplayRegionInfo("Default", deets, g, regs);
                    regs = g.MeasureCharacterRanges(toMeasure, font, rf, defType);
                    DisplayRegionInfo("DefaultTypographic", deets, g, regs);
                }
            }
            deets.AppendLine();
            Debug.WriteLine(deets.ToString());
        }

        [Conditional("DEBUG")]
        static private void DisplayRegionInfo(string which, StringBuilder deets, Graphics g, Region[] regs)
        {
            int rCount = 1;
            foreach (Region r in regs)
            {
                RectangleF regBounds = r.GetBounds(g);
                deets.AppendFormat("{0} Region {1}: {2}", which, rCount, regBounds.ToString());
                deets.AppendLine();
                r.Dispose();
                ++rCount;
            }
        }

        static public void Refresh(string appDir)
        {
            DebugLogger.Log("Hardcoded", "Refreshing hardcoded data");
            string dataDir = Path.Combine(appDir, "data") + Path.DirectorySeparatorChar;
            string dataFile = dataDir + "hardcoded.json";
            string fileText = File.ReadAllText(dataFile);
            Measurements m = JsonConvert.DeserializeObject<Measurements>(fileText);
            if (UpgradeHPTextColour != null)
            {
                DebugLogger.Log("Hardcoded", "Disposing previously loaded data");
                UsedCarListMeasurements.TextFont.Dispose();
                GarageCarListMeasurements.TextFont.Dispose();
                UpgradeHPTextColour.Dispose();
                DealershipPriceColour.Dispose();
                StandardFontColour.Dispose();
                PartFontColour.Dispose();
                EquippedPartFontColour.Dispose();
                StandardFont.Dispose();
                BigFont.Dispose();
                PartFont.Dispose();
                SmallLicenseGraphic.Dispose();
                LargeLicenseGraphic.Dispose();
                CarPicture.Dispose();
                CarLogo.Dispose();
                LicenseTrophyGraphic.Dispose();
                DrivetrainGraphic.Dispose();
                IconImages.Dispose();
            }
            UsedCarListMeasurements = m.usedCarList;
            GarageCarListMeasurements = m.garageCarList;
            EquippedPartsListMeasurements = m.installedCarPartsList;
            UpgradeHPTextColour = new SolidBrush(m.upgradeHPColour);
            DealershipPriceColour = new SolidBrush(m.dealershipPriceColour);
            StandardFontColour = new SolidBrush(m.standardFontColour);
            PartFontColour = new SolidBrush(m.partFontColour);
            EquippedPartFontColour = new SolidBrush(m.installedCarPartsList.TextColour);
            ColourSwatchSize = m.colourSwatchSize;
            StandardFont = m.standardFont;
            BigFont = m.bigFont;
            PartFont = m.partNameFont;
            DumpFontInfo("Standard Font", StandardFont);
            DumpFontInfo("Big Font", BigFont);
            DumpFontInfo("Part Font", PartFont);
            DumpFontInfo("Installed Part Font", m.installedCarPartsList.TextFont);
            SmallLicenseGraphic = new Bitmap(dataDir + "smalllicense.png");
            LargeLicenseGraphic = new Bitmap(dataDir + "biglicense.png");
            CarPicture = new Bitmap(dataDir + "carpicture.png");
            CarLogo = MakeBlackTransparentAndDispose(new Bitmap(dataDir + "carlogo.png"));
            LicenseTrophyGraphic = MakeBlackTransparentAndDispose(new Bitmap(dataDir + "licensetrophy.png"));
            DrivetrainGraphic = MakeBlackTransparentAndDispose(new Bitmap(dataDir + "drivetrain.png"));
            IconImages.Refresh(m.iconImageData, Globals.App.GT2Version, dataDir);
        }

        public static Bitmap MakeColourTransparent(Bitmap bm, Color makeTransparent)
        {
            Bitmap newImage = new Bitmap(bm.Width, bm.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorKey(makeTransparent, makeTransparent);
                g.DrawImage(bm, new Rectangle(Point.Empty, newImage.Size), 0, 0, newImage.Width, newImage.Height, GraphicsUnit.Pixel, ia);
            }
            return newImage;
        }

        public static Bitmap MakeBlackTransparentAndDispose(Bitmap bm)
        {
            Bitmap newIm = MakeColourTransparent(bm, Color.Black);
            bm.Dispose();
            return newIm;
        }

        // silence the 'never assigned to' warning
#pragma warning disable 0649
        // The serialised class
        private class Measurements
        {
            public CarListMeasurements usedCarList;
            public CarListMeasurements garageCarList;
            public CarPartsListMeasurements installedCarPartsList;
            public Color upgradeHPColour;
            public Color dealershipPriceColour;
            public Color standardFontColour;
            public Color partFontColour;
            public Size colourSwatchSize;
            public Font standardFont;
            public Font bigFont;
            public Font partNameFont;
            public IconImgData iconImageData;
        }
#pragma warning restore 0649
    }
}
