using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// this file has the code to display a decompressed GM "menu" gt2 image file. 
// These are the ones in the \gtmenu\<lang>\gtmenudat.dat.
// That file needs exploding into its constituent gzip archives and the archives need decompressing
// before being dragged here

namespace GTMP
{
    // Gm file format
    // struct 
    // {
    //    byte[4] sig; // GM\x3\x0
    //    ushort numOfUnk1; // these are 0x31 big?. 
    //    // When numOfUnk1 is 1 and unk2 is 0, cursor areas start at 0x68; when unk1 is 2/3/4 and unk2 = 0, cursor areas start at 0x88
    //    ushort numOfUnk2; // 
    //    ushort numOfCursorAreas; // These are 0x4c big
    //    ushort numOfUnk3; // these are 0xf1 big?
    //    Unk1Structs[numOfUnk1]; // then these
    //    Unk2Structs[numOfUnk2]; // 
    //    CursorAreas[numOfCursorAreas]; // these always start at 0x88 if there are any of the first two structs, otherwise not
    //    Unk3Structs[numOfUnk3];
    //    byte[0xc] unk4; // then 12 bytes
    //    byte[4] gmllString; // "GMLL"
    //    int numOfPositionEntries;
    //    int[numOfPositionEntries] positionEntries; // variable sized array of position entries, same as those in the GTMP files
    //    short[16][16] palettes; // 16 palettes of 16 BGR555 colours
    //    byte[] padding; // until 0x404 bytes after the start of the palettes
    //    byte[] pixels; // to the end of the file, 4bpp meaning two pixels per byte
    // }
    // struct CursorArea
    // {
    //    short x1;
    //    short y1; // these are the top left corner of the bounding box, in pixels
    //    short x2;
    //    short y2; // these are the bottom right corner, in pixels
    //    byte[3] unk;
    //    byte unk2; // seems to indicate either direction, or which type of picture to place there?
    //    short index; // looks like a picture index, or which screen to visit when clicked on
    //    byte[0x4c - 0xe] unk3; // rest have always been 0
    // }
    static class GMFile
    {
        class Palette
        {
            public Color[] colours;
            public Palette()
            {
                colours = new Color[16];
            }
        }

        class ImageSlice
        {
            public byte[] rect;
            public ImageSlice()
            {
                rect = new byte[16 * 8];
            }
        }

        #region File Search Stuff
        // file search stuff, from http://stackoverflow.com/questions/283456/byte-array-pattern-search
        static readonly int[] Empty = new int[0];

        public static int[] FindPattern(byte[] haystack, byte[] needle, int startingPoint)
        {
            if (IsEmptyLocate(haystack, needle))
                return Empty;

            List<int> list = new List<int>();

            for (int i = startingPoint; i < haystack.Length; i++)
            {
                if (!IsMatch(haystack, i, needle))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] haystack, int position, byte[] needle)
        {
            if (needle.Length > (haystack.Length - position))
                return false;

            for (int i = 0; i < needle.Length; i++)
                if (haystack[position + i] != needle[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }
        #endregion

        private static List<ImageSlice> ParseSlices(byte[] imageData)
        {
            // tiles are 16 x 8 but 4bpp, so each byte is 2 pixels, so 8x8 in bytes
            // stride is always 512 bytes
            List<ImageSlice> slices = new List<ImageSlice>();
            int numBytes = imageData.Length;
            const int stride = 256; // in bytes, not pixels
            const int xTiles = stride / 8;
            int yTiles = numBytes / stride / 8;
            for (int yTile = 0; yTile < yTiles; ++yTile)
            {
                int yPos = yTile * 8;
                for (int xTile = 0; xTile < xTiles; xTile++)
                {
                    int xPos = xTile * 8; // also used as byte index
                    ImageSlice s = new ImageSlice();
                    for (int localIter = 0; localIter < (16 * 8); localIter += 2)
                    {
                        int localX = localIter % 16;
                        int localY = localIter / 16;
                        Debug.Assert((localY >= 0) && (localY < 8));
                        int yPixOffset = (yPos + localY) * stride;
                        byte twoPix = imageData[yPixOffset + xPos + (localX / 2)];
                        s.rect[((localY * 16) + localX) + 1] = (byte)((twoPix & 0xF0) >> 4);
                        s.rect[((localY * 16) + localX)] = (byte)(twoPix & 0xF);
                    }
                    slices.Add(s);
                }
            }
            return slices;
        }

        public static List<GTMPFile.PositionData> ParsePositionData(byte[] positionData, int maxDataSize)
        {
            // max of 0x1f80 bytes of position data
            // 4 is the size of the pos struct
            //struct TileInfo
            //{
            //	unsigned char x; // * 16 for pixel pos
            //	unsigned char y; // * 8 for pixel pos
            //	unsigned short tileIndex; // & 0x0FFF = which tile, & 0xF000 >> 12 = which palette
            //};
            int maxNumOfDataStructs = maxDataSize / 4;
            List<GTMPFile.PositionData> pdList = new List<GTMPFile.PositionData>();
            int posDataIter = 0;
            while (maxNumOfDataStructs > 0)
            {
                // these non-background pictures have 32 palettes instead of 16
                const ushort paletteMask = 0xF800;
                GTMPFile.PositionData pd = new GTMPFile.PositionData();
                byte x = positionData[posDataIter++];
                byte y = positionData[posDataIter++];
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
                ushort tileAndPal = BitConverter.ToUInt16(positionData, posDataIter);
                posDataIter += 2;
                if (isSolidColourFlag == 0)
                {
                    pd.tile = (ushort)(tileAndPal & 0x03FF);
                    pd.palette = (byte)((tileAndPal & paletteMask) >> 11);
                }
                else
                {
                    pd.tile = tileAndPal;
                    pd.palette = 0xFF;
                }
                pdList.Add(pd);
                --maxNumOfDataStructs;
            }
            return pdList;
        }

        private static void ArrangeSlices(Bitmap bm, List<ImageSlice> slices, List<Palette> palettes, List<GTMPFile.PositionData> posData)
        {
            Rectangle lockRect = new Rectangle(0, 0, bm.Width, bm.Height);
            BitmapData bd = bm.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            // docs say this is pixel width, it's a dirty liar. It's the byte width
            int byteStride = bd.Stride;
            foreach (GTMPFile.PositionData pd in posData)
            {
                int[] colourData = new int[16 * 8];
                bool makeTopLeftBlack = false;
                ushort actualTile = pd.tile;
                //if ((pd.tile & 0x0800) != 0)
                //{
                //    actualTile = (ushort)(pd.tile & ~0x800);
                //    makeTopLeftBlack = true;
                //}
                if (pd.palette != 0xFF)
                {
                    ImageSlice tile = slices[actualTile];
                    Palette p = palettes[pd.palette];
                    Color topLeft = p.colours[tile.rect[0]];

                    for (int i = 0; i < (16 * 8); ++i)
                    {
                        byte colourIndex = tile.rect[i];
                        Color pixelColour = p.colours[colourIndex];
                        if (makeTopLeftBlack && (pixelColour == topLeft))
                        {
                            colourData[i] = Color.Black.ToArgb(); //(int)(pixelColour.ToArgb() & 0x00FFFFFF);
                        }
                        else colourData[i] = pixelColour.ToArgb();
                    }
                }
                else
                {
                    Color blockColour = GTMPFile.MakeColorFromBGR555(actualTile);
                    int colourRGB = blockColour.ToArgb();
                    for (int i = 0; i < (16 * 8); ++i)
                    {
                        colourData[i] = colourRGB;
                    }
                }
                IntPtr scanLineIter = new IntPtr(bd.Scan0.ToInt64() + ((pd.y * byteStride) + (pd.x * 4)));
                for (int i = 0; i < 8; ++i)
                {
                    Marshal.Copy(colourData, i * 16, scanLineIter, 16);
                    scanLineIter = new IntPtr(scanLineIter.ToInt64() + bd.Stride);
                }
            }
            bm.UnlockBits(bd);
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        struct CursorPos
        {
            public ushort x1;
            public ushort y1;
            public ushort x2;
            public ushort y2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
            public byte[] unk;
            public byte unk2;
            public ushort index;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (0x4c-0xe))]
            public byte[] unk3;
        }

        static List<Rectangle> FindClickRects(byte[] fileData, int imageDataStartOffset)
        {
            List<Rectangle> rects = new List<Rectangle>();
            int numOfUnk1 = BitConverter.ToInt32(fileData, 4);
            ushort numOfCursorAreas = BitConverter.ToUInt16(fileData, 8);
            ushort numOfUnk2 = BitConverter.ToUInt16(fileData, 0xa);
            int startPos = 0xc + (0x10 * numOfUnk1);
            if (numOfUnk1 == 2)
            {
                startPos += 0x10;
            }
            bool lookForNonZeroAfterFirstSeries = false;
            if (numOfUnk2 > 0)
            {
                lookForNonZeroAfterFirstSeries = true;
            }
            byte[] data = new byte[0x4c];
            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr dataPtr = dataHandle.AddrOfPinnedObject();
            int sizeofCursorPos = Marshal.SizeOf(typeof(CursorPos));
            int timesThrough = 0;
            int nextIncrementAddition = 0;
            while((startPos + sizeofCursorPos) < imageDataStartOffset)
            {
                Buffer.BlockCopy(fileData, startPos, data, 0, data.Length);
                CursorPos cp = (CursorPos)Marshal.PtrToStructure(dataPtr, typeof(CursorPos));
                if (cp.x1 < 0x10 && startPos > 0x80)
                {
                    startPos += 0x14;
                    continue;
                }
                Rectangle r = new Rectangle(cp.x1, cp.y1, cp.x2 - cp.x1, cp.y2 - cp.y1);
                rects.Add(r);
                startPos += sizeofCursorPos;
                //if (nextIncrementAddition > 0)
                //{
                //    startPos += nextIncrementAddition;
                //    nextIncrementAddition = 0;
                //}
                //if (lookForNonZeroAfterFirstSeries)
                //{
                //    ++timesThrough;
                //    if (timesThrough == numOfCursorAreas)
                //    {
                //        nextIncrementAddition = 0x14;
                //        //while (fileData[startPos] != 0)
                //        //{
                //        //    --startPos;
                //        //}
                //        //++startPos;
                //        lookForNonZeroAfterFirstSeries = false;
                //    }
                //}
            }
            dataHandle.Free();
            return rects;
        }

        public static Bitmap Parse(string file)
        {
            List<Palette> paletteList = new List<Palette>();
            byte[] fileData = File.ReadAllBytes(file);
            if (Encoding.ASCII.GetString(fileData, 0, 3) != "GM\x3")
            {
                return null;
            }
            int[] gmllPositions = FindPattern(fileData, Encoding.ASCII.GetBytes("GMLL"), 4);
            if (gmllPositions.Length == 0)
            {
                return null;
            }
            int gmllPos1 = gmllPositions[0];
            List<Rectangle> clickablePos = FindClickRects(fileData, gmllPos1);
            int numPositionsEntries = BitConverter.ToInt32(fileData, gmllPos1 + 4);
            // first +4 for GMLL, second 4 for the number of entries
            int paletteOffset = gmllPos1 + 4 + 4 + (numPositionsEntries * 4);
            int pixelDataOffset = paletteOffset + 0x404;
            int numImageBytes = (int)(fileData.Length - pixelDataOffset);
            byte[] imageArray = new byte[numImageBytes];
            Array.Copy(fileData, pixelDataOffset, imageArray, 0, numImageBytes);

            int palettePosIter = paletteOffset;
            for(int i = 0; i < 32; ++i)
            {
                Palette p = new Palette();
                for (int j = 0; j < p.colours.Length; ++j, palettePosIter += 2)
                {
                    // colours are in BGR format
                    ushort color = BitConverter.ToUInt16(fileData, palettePosIter);
                    Color rgbColor = GTMPFile.MakeColorFromBGR555(color);
                    p.colours[j] = rgbColor;
                }
                paletteList.Add(p);
            }
            List<ImageSlice> slices = ParseSlices(imageArray);
            // image positiioning seems to be on an absolute 512x504 canvas even if there's
            // only enough image data in the file to cover 512x8
            Bitmap bm = new Bitmap(512, 504, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 512, 504));
            }
            byte[] positionArray = new byte[numPositionsEntries * 4];
            Array.Copy(fileData, gmllPos1 + 8, positionArray, 0, positionArray.Length);
            List<GTMPFile.PositionData> pd = ParsePositionData(positionArray, positionArray.Length);
            ArrangeSlices(bm, slices, paletteList, pd);
            if (clickablePos.Count > 0)
            {
                using (Graphics g = Graphics.FromImage(bm))
                {
                    foreach(Rectangle r in clickablePos)
                    {
                        g.DrawRectangle(Pens.YellowGreen, r);
                    }
                }
            }
            return bm;
        }

        private static int GetTileCount(string file)
        {
            byte[] fileData = File.ReadAllBytes(file);
            if (Encoding.ASCII.GetString(fileData, 0, 3) != "GM\x3")
            {
                return 0;
            }
            int[] gmllPositions = FindPattern(fileData, Encoding.ASCII.GetBytes("GMLL"), 4);
            if (gmllPositions.Length == 0)
            {
                return 0;
            }
            int gmllPos1 = gmllPositions[0];
            int numPositionsEntries = BitConverter.ToInt32(fileData, gmllPos1 + 4);
            // first +4 for GMLL, second 4 for the number of entries
            int paletteOffset = gmllPos1 + 4 + 4 + (numPositionsEntries * 4);
            int pixelDataOffset = paletteOffset + 0x404;
            int numImageBytes = (int)(fileData.Length - pixelDataOffset);
            byte[] imageArray = new byte[numImageBytes];
            Array.Copy(fileData, pixelDataOffset, imageArray, 0, numImageBytes);
            List<ImageSlice> slices = ParseSlices(imageArray);
            return slices.Count;
        }

        public static void DumpGMDir(string dir, string outDir)
        {
            string[] files = Directory.GetFiles(dir);
            Directory.CreateDirectory(outDir);
            // max tiles in a file is 0x400, least is 0x24
            foreach (string file in files)
            {
                string justName = Path.GetFileName(file);
                Console.WriteLine("Processing '{0}'", justName);
                using (Bitmap bm = Parse(file))
                {
                    string name = Path.GetFileName(file);
                    string outputFile = Path.Combine(outDir, name + ".png");
                    bm.Save(outputFile, ImageFormat.Png);
                }
            }
        }
    }
}