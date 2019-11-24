using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// This was an attempt at displaying GM pictures from the EuroDemo53
// vol, it doesn't work

namespace GTMP
{
    static class ED53GTMPFile
    {
        class Palette
        {
            public Color[] colours;
            public Palette()
            {
                colours = new Color[256];
            }
        }

        class ImageSlice
        {
            public byte[] rect;
            public ImageSlice(int x, int y)
            {
                rect = new byte[8 * 8];
            }
        }

        class PositionData
        {
            public int x;
            public int y;
            public ushort tile;
            public byte palette;
        }

        private static Color MakeColorFromBGR555(ushort colour)
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

        private static List<ImageSlice> ParseSlices(byte[] imageData)
        {
            // tiles are 8 x 8 in ed53
            // stride is always 512 bytes
            List<ImageSlice> slices = new List<ImageSlice>();
            int numBytes = imageData.Length;
            const int stride = 512;
            const int xTiles = stride / 8;
            int yTiles = numBytes / stride / 8;
            for (int yTile = 0; yTile < yTiles; ++yTile)
            {
                int yPos = yTile * 8;
                for (int xTile = 0; xTile < xTiles; xTile++)
                {
                    int xPos = (xTile * 8);
                    ImageSlice s = new ImageSlice(xPos, yPos);
                    for (int localIter = 0; localIter < (8 * 8); ++localIter)
                    {
                        int localX = localIter % 8;
                        int localY = localIter / 8;
                        Debug.Assert((localY >= 0) && (localY < 8));
                        int yPixOffset = (yPos + localY) * stride;
                        s.rect[(localY * 8) + localX] = imageData[yPixOffset + xPos + localX];
                    }
                    slices.Add(s);
                }
            }
            //slices.Sort();
            return slices;
        }

        private static List<PositionData> ParsePositionData(BinaryReader br)
        {
            // max of 0x1f80 bytes of position data
            // 4 is the size of the pos struct
            //struct TileInfo
            //{
            //	unsigned char x; // * 16 for pixel pos
            //	unsigned char y; // * 8 for pixel pos
            //	unsigned short tileIndex; // & 0x0FFF = which tile, & 0xF000 >> 12 = which palette
            //};
            int maxNumOfDataStructs = 0x3f00 / 4;
            List<PositionData> pdList = new List<PositionData>();
            int zeroDataSeen = 0;
            while (maxNumOfDataStructs > 0)
            {
                const ushort paletteMask = 0xF000;
                PositionData pd = new PositionData();
                byte x = br.ReadByte();
                byte y = br.ReadByte();
                // hack
                byte xPos = (byte)(x & 0x1F);
                byte otherXData = (byte)((x & 0xE0) >> 5);
                //pd.x = (x > 32) ? x : (x * 16);
                pd.x = (x * 16) % 512;
                pd.y = y * 8;
                ushort tileAndPal = br.ReadUInt16();
                pd.tile = (ushort)(tileAndPal & 0x0FFF);
                pd.palette = (byte)((tileAndPal & paletteMask) >> 12);
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
            int byteStride = bd.Stride;
            ImageSlice previous = null;
            foreach (PositionData pd in posData)
            {
                int[] colourData = new int[8 * 8];
                // hack - what to do if pd.tile > slices.count
                if (pd.tile < slices.Count)
                {
                    ImageSlice tile = slices[pd.tile];
                    // also hack
                    if (tile != previous)
                    {
                        Palette p = palettes[pd.palette];

                        for (int i = 0; i < (8 * 8); ++i)
                        {
                            colourData[i] = p.colours[tile.rect[i]].ToArgb();
                        }
                    }
                    previous = tile;
                }
                // end of hack                
                IntPtr scanLineIter = new IntPtr(bd.Scan0.ToInt64() + ((pd.y * byteStride) + (pd.x * 4)));
                for (int i = 0; i < 8; ++i)
                {
                    Marshal.Copy(colourData, i * 8, scanLineIter, 8);
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
                const int pixelDataOffset = 0x5f08;
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
                List<ImageSlice> slices = ParseSlices(imageArray);
                // second 512 = hack, was numImageBytes / 512
                Bitmap bm = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 512, 512));
                }
                List<PositionData> pd = ParsePositionData(br);
                ArrangeSlices(bm, slices, paletteList, pd);
                return bm;
            }
        }

        private static List<PositionData> ParsePositionDataED53(BinaryReader br)
        {
            // max of 0x1f80 bytes of position data
            // 4 is the size of the pos struct
            //struct TileInfo
            //{
            //	unsigned char x; // * 16 for pixel pos
            //	unsigned char y; // * 8 for pixel pos
            //	unsigned short tileIndex; // & 0x0FFF = which tile, & 0xF000 >> 12 = which palette
            //};
            int maxNumOfDataStructs = 0x1f80 / 4;
            List<PositionData> pdList = new List<PositionData>();
            int zeroDataSeen = 0;
            while (maxNumOfDataStructs > 0)
            {
                const ushort paletteMask = 0xF000;
                PositionData pd = new PositionData();
                byte x = br.ReadByte();
                byte y = br.ReadByte();
                // hack
                byte xPos = (byte)(x & 0x1F);
                byte otherXData = (byte)((x & 0xE0) >> 5);
                //pd.x = (x > 32) ? x : (x * 16);
                pd.x = (x * 16) % 512;
                pd.y = y * 8;
                ushort tileAndPal = br.ReadUInt16();
                pd.tile = (ushort)(tileAndPal & 0x0FFF);
                pd.palette = (byte)((tileAndPal & paletteMask) >> 12);
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

        public static Bitmap ParseED53Image(string file)
        {
            List<Palette> paletteList = new List<Palette>();
            using (FileStream fileData = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                int numImageBytes = (int)(fileData.Length - 0x5f04);
                BinaryReader br = new BinaryReader(fileData);
                byte[] header = br.ReadBytes(4);
                if (Encoding.ASCII.GetString(header, 0, 4) != "GTMP")
                {
                    return null;
                }
                br.BaseStream.Seek(0x5f04, SeekOrigin.Begin);
                byte[] imageArray = br.ReadBytes(numImageBytes);
                br.BaseStream.Seek(0x3f04, SeekOrigin.Begin);
                for (int i = 0; i < 16; ++i)
                {
                    Palette p = new Palette();
                    for (int j = 0; j < p.colours.Length; ++j)
                    {
                        // colors are in BGR format
                        ushort color = br.ReadUInt16();
                        Color rgbColor = MakeColorFromBGR555(color);
                        p.colours[j] = rgbColor;
                    }
                    paletteList.Add(p);
                }
                br.BaseStream.Seek(0x3a9c, SeekOrigin.Begin);
                List<ImageSlice> slices = ParseSlices(imageArray);
                // second 512 = hack, was numImageBytes / 512
                Bitmap bm = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bm))
                {
                    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 512, 512));
                }
                List<PositionData> pd = ParsePositionDataED53(br);
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
                Bitmap bm = Parse(file);
                string name = Path.GetFileName(file);
                string outputFile = Path.Combine(outDir, name + ".png");
                bm.Save(outputFile, ImageFormat.Png);
                bm.Dispose();
            }
        }
    }
}