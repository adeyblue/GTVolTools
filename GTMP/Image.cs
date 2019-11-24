using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// this file contains the code for displaying a 'GTMP' format GT2 background image.
// These are the ones in \gtmenu\commonpic.dat. That file needs exploding into its constituent
// GTMP files 

namespace GTMP
{
    static class GTMPFile
    {
        class Palette
        {
            public Color[] colours;
            public Palette()
            {
                colours = new Color[256];
            }
        }

        public class ImageSlice
        {
            public byte[] rect;
            public ImageSlice()
            {
                rect = new byte[16 * 8];
            }
        }

        public class PositionData
        {
            public int x;
            public int y;
            public ushort tile;
            public byte palette;
        }

        public static Color MakeColorFromBGR555(ushort colour)
        {
            int green = (colour & (0x1f << 5)) >> 5;
            int red = colour & 0x1f;
            int blue = (colour & (0x1f << 10)) >> 10;
            float mulFactor = (float)0xff / 0x1f;
            green = (int)(green * mulFactor);
            red = (int)(red * mulFactor);
            blue = (int)(blue * mulFactor);
            return Color.FromArgb(0xff, red, green, blue);
        }

        public static List<ImageSlice> ParseSlices(byte[] imageData)
        {
            // tiles are 16 x 8
            // stride is always 512 bytes
            List<ImageSlice> slices = new List<ImageSlice>();
            int numBytes = imageData.Length;
            const int stride = 512;
            const int xTiles = stride / 16;
            int yTiles = numBytes / stride / 8;
            for (int yTile = 0; yTile < yTiles; ++yTile)
            {
                int yPos = yTile * 8;
                for(int xTile = 0; xTile < xTiles; xTile++)
                {
                    int xPos = (xTile * 16);
                    ImageSlice s = new ImageSlice();
                    for (int localIter = 0; localIter < (16 * 8); ++localIter)
                    {
                        int localX = localIter % 16;
                        int localY = localIter / 16;
                        Debug.Assert((localY >= 0) && (localY < 8));
                        int yPixOffset = (yPos + localY) * stride;
                        s.rect[(localY * 16) + localX] = imageData[yPixOffset + xPos + localX];
                    }
                    slices.Add(s);
                }
            }
            //slices.Sort();
            return slices;
        }

        public static List<PositionData> ParsePositionData(BinaryReader br, int maxDataSize)
        {
            // max of 0x1f80 bytes of position data
            // 4 is the size of the pos struct
            //struct TileInfo
            //{
            //	unsigned char x; // * 16 for pixel pos
            //	unsigned char y; // * 8 for pixel pos
            //	unsigned short tileIndex; // & 0x0FFF = which tile, & 0xF000 >> 12 = which palette
            //};
            int dataSize = maxDataSize == 0 ? 0x1f80 : maxDataSize;
            int maxNumOfDataStructs = dataSize / 4;
            List<PositionData> pdList = new List<PositionData>();
            int zeroDataSeen = 0;
            while (maxNumOfDataStructs > 0)
            {
                const ushort paletteMask = 0xF000;
                PositionData pd = new PositionData();
                byte x = br.ReadByte();
                byte y = br.ReadByte();
                // the x value is technically two bits of data
                // the low 5 bits are the x coordinate
                // the upper three (really just the high bit) are a flag
                // if this flag is set, then the tile index is actually a BGR555 solid colour
                // and is used for all 16 x 8 pixels instead of a tile
                byte xPos = (byte)(x & 0x1F);
                byte isSolidColourFlag = (byte)((x & 0xE0) >> 5);
                // turn these into pixel coordinates instead of tile coordinates
                // to make it easier later
                pd.x = xPos * 16;
                pd.y = y * 8;
                ushort tileAndPal = br.ReadUInt16();
                if (isSolidColourFlag == 0)
                {
                    pd.tile = (ushort)(tileAndPal & 0x0FFF);
                    pd.palette = (byte)((tileAndPal & paletteMask) >> 12);
                }
                else
                {
                    pd.tile = tileAndPal;
                    pd.palette = 0xFF;
                }
                if ((pd.x + pd.y + pd.palette + pd.tile) == 0)
                {
                    if (++zeroDataSeen == 2)
                    {
                        pdList.RemoveAt(pdList.Count - 1);
                        break;
                    }
                }
                else zeroDataSeen = 0;
                pdList.Add(pd);
                --maxNumOfDataStructs;
            }
            return pdList;
        }

        private static void ArrangeSlices(Bitmap bm, List<ImageSlice> slices, List<Palette> palettes, List<PositionData> posData)
        {
            Rectangle lockRect = new Rectangle(0, 0, bm.Width, bm.Height);
            BitmapData bd = bm.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            // docs say this is pixel width, it's a dirty liar. It's the byte width
            int byteStride = bd.Stride;
            foreach(PositionData pd in posData)
            {
                int[] colourData = new int[16 * 8];
                if (pd.palette != 0xFF)
                {
                    ImageSlice tile = slices[pd.tile];
                    Palette p = palettes[pd.palette];

                    for (int i = 0; i < (16 * 8); ++i)
                    {
                        colourData[i] = p.colours[tile.rect[i]].ToArgb();
                    }
                }
                else
                {
                    Color blockColour = MakeColorFromBGR555(pd.tile);
                    int colourRGB = blockColour.ToArgb();
                    for (int i = 0; i < (16 * 8); ++i)
                    {
                        colourData[i] = colourRGB;
                    }
                }
                // find the starting position for this position data
                IntPtr scanLineIter = new IntPtr(bd.Scan0.ToInt64() + ((pd.y * byteStride) + (pd.x * 4)));
                for(int i = 0; i < 8; ++i)
                {
                    // copy the pixels for this row
                    Marshal.Copy(colourData, i * 16, scanLineIter, 16);
                    // go to next row
                    scanLineIter = new IntPtr(scanLineIter.ToInt64() + bd.Stride);
                }
            }
            bm.UnlockBits(bd);
        }

        public static Bitmap Parse(string file)
        {
            List<Palette> paletteList = new List<Palette>();
            using (FileStream fileData = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                // these are the same for all
                const int pixelDataOffset = 0x4000;
                const int paletteOffset = 0x1f84;
                int numImageBytes = (int)(fileData.Length - pixelDataOffset);
                BinaryReader br = new BinaryReader(fileData);
                byte[] header = br.ReadBytes(4);
                if (Encoding.ASCII.GetString(header, 0, 4) != "GTMP")
                {
                    return null;
                }
                br.BaseStream.Seek(pixelDataOffset, SeekOrigin.Begin);
                // read the raw 16x8 squares and make them into their rects
                byte[] imageArray = br.ReadBytes(numImageBytes);
                List<ImageSlice> slices = ParseSlices(imageArray);
                // read the palettes
                br.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);
                for(int i = 0; i < 16; ++i)
                {
                    Palette p = new Palette();
                    for (int j = 0; j < p.colours.Length; ++j)
                    {
                        // colours are in BGR format
                        ushort color = br.ReadUInt16();
                        Color rgbColor = MakeColorFromBGR555(color);
                        p.colours[j] = rgbColor;
                    }
                    paletteList.Add(p);
                }
                br.BaseStream.Seek(4, SeekOrigin.Begin);
                // image positiioning seems to be on an absolute 512x504 canvas even if there's
                // only enough image data in the file to cover 512x8
                Bitmap bm = new Bitmap(512, 504, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 512, 504));
                }
                List<PositionData> pd = ParsePositionData(br, 0);
                ArrangeSlices(bm, slices, paletteList, pd);
                return bm;
            }
        }

        private static byte FindNearestColourIndex(Color blockColour, List<Palette> palettes)
        {
            byte nearestColour = 0;
            int nearestDiff = Int32.MaxValue;

            for (int i = 0; i < palettes.Count; ++i)
            {
                Color[] p = palettes[i].colours;
                for (int j = 0; j < p.Length; ++j)
                {
                    if (p[j] == blockColour)
                    {
                        return (byte)((i * 16) + j);
                    }
                    else
                    {
                        Color pColour = p[j];
                        int currentDiffR = blockColour.R - pColour.R;
                        int currentDiffG = blockColour.G - pColour.G;
                        int currentDiffB = blockColour.B - pColour.B;
                        int curDiff = currentDiffR + currentDiffG + currentDiffB;
                        if (curDiff < nearestDiff)
                        {
                            nearestColour = (byte)((i * 16) + j);
                            nearestDiff = curDiff;
                        }
                    }
                }
            }
            return nearestColour;
        }

        private static void ArrangeSlicesIndexed(Bitmap bm, List<ImageSlice> slices, List<Palette> palettes, List<PositionData> posData)
        {
            Rectangle lockRect = new Rectangle(0, 0, bm.Width, bm.Height);
            BitmapData bd = bm.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            // docs say this is pixel width, it's a dirty liar. It's the byte width
            int byteStride = bd.Stride;
            foreach (PositionData pd in posData)
            {
                byte[] colourData = new byte[16 * 8];
                if (pd.palette != 0xFF)
                {
                    ImageSlice tile = slices[pd.tile];
                    Palette p = palettes[pd.palette];

                    for (int i = 0; i < (16 * 8); ++i)
                    {
                        colourData[i] = (byte)((pd.palette * 16) + tile.rect[i]);
                    }
                }
                else
                {
                    Color blockColour = MakeColorFromBGR555(pd.tile);
                    byte index = FindNearestColourIndex(blockColour, palettes);
                    for (int i = 0; i < (16 * 8); ++i)
                    {
                        colourData[i] = index;
                    }
                }
                // find the starting position for this position data
                IntPtr scanLineIter = new IntPtr(bd.Scan0.ToInt64() + ((pd.y * byteStride) + (pd.x)));
                for (int i = 0; i < 8; ++i)
                {
                    // copy the pixels for this row
                    Marshal.Copy(colourData, i * 16, scanLineIter, 16);
                    // go to next row
                    scanLineIter = new IntPtr(scanLineIter.ToInt64() + bd.Stride);
                }
            }
            bm.UnlockBits(bd);
        }

        public static Bitmap ParseTo8BppBmp(string file)
        {
            List<Palette> paletteList = new List<Palette>();
            using (FileStream fileData = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                // these are the same for all
                const int pixelDataOffset = 0x4000;
                const int paletteOffset = 0x1f84;
                int numImageBytes = (int)(fileData.Length - pixelDataOffset);
                BinaryReader br = new BinaryReader(fileData);
                byte[] header = br.ReadBytes(4);
                if (Encoding.ASCII.GetString(header, 0, 4) != "GTMP")
                {
                    return null;
                }
                br.BaseStream.Seek(pixelDataOffset, SeekOrigin.Begin);
                // read the raw 16x8 squares and make them into their rects
                byte[] imageArray = br.ReadBytes(numImageBytes);
                List<ImageSlice> slices = ParseSlices(imageArray);
                // read the palettes
                br.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);
                for (int i = 0; i < 16; ++i)
                {
                    Palette p = new Palette();
                    for (int j = 0; j < p.colours.Length; ++j)
                    {
                        // colours are in BGR format
                        ushort color = br.ReadUInt16();
                        Color rgbColor = MakeColorFromBGR555(color);
                        p.colours[j] = rgbColor;
                    }
                    paletteList.Add(p);
                }
                br.BaseStream.Seek(4, SeekOrigin.Begin);
                // image positiioning seems to be on an absolute 512x504 canvas even if there's
                // only enough image data in the file to cover 512x8
                Bitmap bm = new Bitmap(512, 504, PixelFormat.Format8bppIndexed);
                Color[] paletteEntries = bm.Palette.Entries;
                for(int i = 0; i < paletteList.Count; ++i)
                {
                    for(int j = 0; j < paletteList[i].colours.Length; ++j)
                    {
                        paletteEntries[i * 16 + j] = paletteList[i].colours[j];
                    }
                }
                //using (Graphics g = Graphics.FromImage(bm))
                //{
                //    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 512, 504));
                //}
                List<PositionData> pd = ParsePositionData(br, 0);
                ArrangeSlicesIndexed(bm, slices, paletteList, pd);
                return bm;
            }
        }

        public static List<PositionData> ParsePositionDataDemo(BinaryReader br, int maxDataSize)
        {
            // max of 0x1f80 bytes of position data
            // 4 is the size of the pos struct
            //struct TileInfo
            //{
            //	unsigned char x; // * 16 for pixel pos
            //	unsigned char y; // * 8 for pixel pos
            //	unsigned short tileIndex; // & 0x0FFF = which tile, & 0xF000 >> 12 = which palette
            //};
            int dataSize = maxDataSize == 0 ? 0x1f80 : maxDataSize;
            int maxNumOfDataStructs = dataSize / 4;
            List<PositionData> pdList = new List<PositionData>();
            int zeroDataSeen = 0;
            while (maxNumOfDataStructs > 0)
            {
                const ushort paletteMask = 0xF800;
                PositionData pd = new PositionData();
                byte x = br.ReadByte();
                byte y = br.ReadByte();
                // the x value is technically two bits of data
                // the low 5 bits are the x coordinate
                // the upper three (really just the high bit) are a flag
                // if this flag is set, then the tile index is actually a BGR555 solid colour
                // and is used for all 16 x 8 pixels instead of a tile
                byte xPos = (byte)(x & 0x1F);
                byte isSolidColourFlag = (byte)((x & 0xE0) >> 5);
                // turn these into pixel coordinates instead of tile coordinates
                // to make it easier later
                pd.x = xPos * 16;
                pd.y = y * 8;
                ushort tileAndPal = br.ReadUInt16();
                if (isSolidColourFlag == 0)
                {
                    pd.tile = (ushort)(tileAndPal & 0x04FF);
                    pd.palette = (byte)((tileAndPal & paletteMask) >> 11);
                }
                else
                {
                    pd.tile = tileAndPal;
                    pd.palette = 0xFF;
                }
                if ((pd.x + pd.y + pd.palette + pd.tile) == 0)
                {
                    if (++zeroDataSeen == 2)
                    {
                        pdList.RemoveAt(pdList.Count - 1);
                        break;
                    }
                }
                else zeroDataSeen = 0;
                pdList.Add(pd);
                --maxNumOfDataStructs;
            }
            return pdList;
        }

        public static Bitmap ParseDemoBG(string file)
        {
            List<Palette> paletteList = new List<Palette>();
            using (FileStream fileData = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                const int pixelDataOffset = 0x5f04;
                const int paletteOffset = 0x3f04;
                int numImageBytes = (int)(fileData.Length - pixelDataOffset);
                BinaryReader br = new BinaryReader(fileData);
                byte[] header = br.ReadBytes(4);
                if (Encoding.ASCII.GetString(header, 0, 4) != "GTMP")
                {
                    return null;
                }
                br.BaseStream.Seek(pixelDataOffset, SeekOrigin.Begin);
                byte[] imageArray = br.ReadBytes(numImageBytes);
                // for final game
                br.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);
                for (int i = 0; i < 32; ++i)
                {
                    Palette p = new Palette();
                    for (int j = 0; j < p.colours.Length; ++j)
                    {
                        // colours are in BGR format
                        ushort color = br.ReadUInt16();
                        Color rgbColor = MakeColorFromBGR555(color);
                        p.colours[j] = rgbColor;
                    }
                    paletteList.Add(p);
                }
                br.BaseStream.Seek(4, SeekOrigin.Begin);
                List<ImageSlice> slices = ParseSlices(imageArray);
                // image positiioning seems to be on an absolute 512x504 canvas even if there's
                // only enough image data in the file to cover 512x8
                Bitmap bm = new Bitmap(512, 504, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 512, 504));
                }
                List<PositionData> pd = ParsePositionDataDemo(br, 0x3f00);
                ArrangeSlices(bm, slices, paletteList, pd);
                return bm;
            }
        }

        public static void DumpGTMPDir(string dir, string outDir)
        {
            string[] files = Directory.GetFiles(dir);
            Directory.CreateDirectory(outDir);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                Console.WriteLine("Processing '{0}'", name);
                using (Bitmap bm = ParseTo8BppBmp(file))
                {
                    string outputFile = Path.Combine(outDir, name + ".bmp");
                    bm.Save(outputFile, ImageFormat.Bmp);
                }
            }
        }
    }
}