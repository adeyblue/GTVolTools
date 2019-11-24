using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

// this file contains the code to displays a GT3 Tex1 format image
//
// tex format
// header
// char[4] = Tex1
// int unk // 0
// int unk2 // 0
// int fileSize // 0xc
// short unk3; // 0 - 0x10
// short unk4; // changes for all - 0x12
// short ver/type? // mostly 1, but some have 2. And 2 have larger headers, upto c0
// // gap
// short otherFlags; (0x5e) //0x01 = little endian pixels?
// short width; (0x60)
// short height; (0x62)
// int paletteOffset; // 4 bytes per colour (0x64)
// short unk 0x68
// short unk2 0x6a
// short bpp? (0x6c) (0x10 = 8bpp, 0x8 = 4bpp)
// short flags (0x6e) (0x10 = tiled palette)

namespace GTMP
{
    class GT3Tex
    {
        static int SwizzleBGR32(int bgrColour)
        {
            int r = bgrColour & 0xFF;
            int g = (bgrColour >> 8) & 0xFF;
            int b = (bgrColour >> 16) & 0xFF;
            int a = ((bgrColour >> 24) & 0xFF);
            return ((a << 24) | (r << 16) | (g << 8) | b);
        }

        static List<int> FindPalette(byte[] fileData, int startingOffset, int fileSize)
        {
            List<int> palette = new List<int>((fileSize - startingOffset) / 4);
            while (startingOffset < fileSize)
            {
                // colours are in bgr32 format, this changes them to rgb which is what GDI+ expects
                int rgbColour = SwizzleBGR32(BitConverter.ToInt32(fileData, startingOffset));
                palette.Add(rgbColour);
                startingOffset += 4;
            }
            return palette;
        }

        static List<int> GenerateSequentialPixels(int endNum)
        {
            List<int> pixels = new List<int>(endNum);
            for (int i = 0; i < endNum; ++i)
            {
                pixels.Add(i);
            }
            return pixels;
        }

        static List<int> ParseIndexes<T>(ushort width, ushort height, int startPoint, byte[] data)
        {
            int numBytesToRead = width * height;
            T[] indexes = new T[numBytesToRead / Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(data, startPoint, indexes, 0, numBytesToRead);
            return new List<int>(Array.ConvertAll(indexes, (x) => { return Convert.ToInt32(x); }));
        }

        static byte ReverseBits(byte b)
        {
            int rev = (b >> 4) | ((b & 0xf) << 4);
            rev = ((rev & 0xcc) >> 2) | ((rev & 0x33) << 2);
            rev = ((rev & 0xaa) >> 1) | ((rev & 0x55) << 1);
            return (byte)rev;
        }

        static List<int> Parse8bppIndexes(ushort width, ushort height, int startPoint, byte[] data)
        {
            int numBytesToRead = width * height;
            byte[] indexes = new byte[numBytesToRead];
            Buffer.BlockCopy(data, startPoint, indexes, 0, numBytesToRead);
            for (int i = 0; i < indexes.Length; ++i)
            {
                indexes[i] = ReverseBits(indexes[i]);
            }
            return new List<int>(Array.ConvertAll(indexes, (x) => { return Convert.ToInt32(x); }));
        }

        static List<int> Parse4bppIndexes(ushort width, ushort height, int startPoint, byte[] data)
        {
            int numBytesToRead = width * height;
            byte[] indexBytes = new byte[numBytesToRead / 2];
            Buffer.BlockCopy(data, startPoint, indexBytes, 0, numBytesToRead / 2);
            List<int> indexes = new List<int>(numBytesToRead * 2);
            foreach (byte b in indexBytes)
            {
                indexes.Add(b & 0xf);
                indexes.Add((b & 0xf0) >> 4);
            }
            return indexes;
        }

        static int BlendColours(int c1, int c2, int percentOfFirst)
        {
            int b1 = c1 & 0xff;
            int g1 = (c1 & 0xff00) >> 8;
            int r1 = (c1 & 0xff0000) >> 16;
            int b2 = c2 & 0xff;
            int g2 = (c2 & 0xff00) >> 8;
            int r2 = (c2 & 0xff0000) >> 16;
            int percentOfSecond = 100 - percentOfFirst;
            int resB = ((b1 * percentOfFirst) + (b2 * percentOfSecond)) / 100;
            int resG = ((g1 * percentOfFirst) + (g2 * percentOfSecond)) / 100;
            int resR = ((r1 * percentOfFirst) + (r2 * percentOfSecond)) / 100;
            //int resB = (b1 + b2) / 2;
            //int resG = (g1 + g2) / 2;
            //int resR = (r1 + r2) / 2;
            return (0xFF << 24) | (resR << 16) | (resG << 8) | resB;
        }

        static void PlotBitmap(BitmapData bmData, List<int> pixels, List<int> palette)
        {
            IntPtr pData = bmData.Scan0;
            int width = bmData.Width;
            int numPixels = pixels.Count;
            List<int> linePixels = new List<int>(bmData.Width);
            int paletteEntries = palette.Count;
            int lastColour = 0;
            for (int i = 0; i < numPixels; ++i)
            {
                int paletteIndex = pixels[i];
                bool justColour = false;
                if (paletteIndex > paletteEntries)
                {
                    Debug.WriteLine(String.Format("Found palette index of {0:x}, there are only {1:x} in the palette", paletteIndex, paletteEntries));
                    paletteIndex = GTMPFile.MakeColorFromBGR555((ushort)paletteIndex).ToArgb();
                    justColour = true;
                }

                if ((linePixels.Count > 1) && ((linePixels.Count % bmData.Width) == 0))
                {
                    Marshal.Copy(linePixels.ToArray(), 0, pData, linePixels.Count);
                    pData = new IntPtr(pData.ToInt64() + (linePixels.Count * 4));
                    linePixels.Clear();
                }
                //int palColour = palette[paletteIndex];
                //int alphaVal = (int)((palColour & 0xFF000000) >> 24);
                //if (alphaVal != 0x80)
                //{
                //    int percentOfThisColour = (int)((alphaVal / 128.0f) * 100);
                //    int blended = BlendColours(palColour, lastColour, percentOfThisColour);
                //    palColour = blended;
                //}
                //else
                //{
                //    lastColour = palColour;
                //}
                linePixels.Add(justColour ? paletteIndex : palette[paletteIndex]);
            }
            Marshal.Copy(linePixels.ToArray(), 0, pData, linePixels.Count);
        }

        private static int[] ReadAndConvertBGR24Pixels(byte[] fileData, int totalColours, int startOffset, int endianFlag)
        {
            int[] colours = new int[totalColours];
            byte[] channels = new byte[4];
            for (int i = 0; i < totalColours; ++i, startOffset += 3)
            {
                // built in swizzle!
                if (endianFlag == 0)
                {
                    channels[0] = fileData[startOffset + 2];
                    channels[1] = fileData[startOffset + 1];
                    channels[2] = fileData[startOffset];
                    channels[3] = 0xff;
                }
                else
                {
                    Buffer.BlockCopy(fileData, startOffset, channels, 0, 4);
                    channels[0] = 0xff;
                }
                colours[i] = BitConverter.ToInt32(channels, 0);
            }
            return colours;
        }

        public static Bitmap Parse(string file)
        {
            byte[] fileData = File.ReadAllBytes(file);
            if ((fileData.Length < 4) || (Encoding.ASCII.GetString(fileData, 0, 4) != "Tex1"))
            {
                Debug.WriteLine(String.Format("{0} is not a Tex1 File!", file));
                return null;
            }
            int fileSize = BitConverter.ToInt32(fileData, 0xc);
            //if (fileData.Length != fileSize)
            //{
            //    Debug.WriteLine(String.Format("Read {0} bytes from file, header says it has {1}", fileData.Length, fileSize));
            //    return null;
            //}
            bool isVer2 = BitConverter.ToUInt16(fileData, 0x14) == 2;
            int pixelOffset = isVer2 ? 0xa0 : 0x70;
            int widthOffset = isVer2 ? 0x88 : 0x60;
            int heightOffset = isVer2 ? 0x8a : 0x62;
            int paletteOffsetLoc = isVer2 ? 0x8c : 0x64;
            int bppOffset = isVer2 ? 0x94 : 0x6c;
            int pixelFlagOffset = isVer2 ? 0x86 : 0x5e;
            int dataBytes = (fileSize - pixelOffset);
            ushort pixelFlags = BitConverter.ToUInt16(fileData, pixelFlagOffset);
            ushort width = BitConverter.ToUInt16(fileData, widthOffset);
            ushort height = BitConverter.ToUInt16(fileData, heightOffset);
            if ((width != 0) && (height == 0))
            {
                height = (ushort)(dataBytes / width / 4);
            }
            else if ((width == 0) && (height != 0))
            {
                width = (ushort)(dataBytes / height / 4);
            }
            int paletteOffset = BitConverter.ToInt32(fileData, paletteOffsetLoc);
            ushort bpp = BitConverter.ToUInt16(fileData, bppOffset);
            List<int> palette = null;
            if (paletteOffset == 0)
            {
                int[] paletteData = null;
                //if (BitConverter.ToUInt16(fileData, 0x5e) == 0x101)
                if(BitConverter.ToUInt16(fileData, 0x6e) == 0)
                {
                    // seems to be 24-bit BGR
                    int totalColours = width * height;
                    if ((totalColours * 3) > (dataBytes))
                    {
                        totalColours = (dataBytes / 3);
                    }
                    paletteData = ReadAndConvertBGR24Pixels(fileData, totalColours, pixelOffset, pixelFlags & 0x1);
                }
                else
                {
                    //int totalColours = width * height;
                    //if ((totalColours * 4) > (dataBytes))
                    //{
                    //    totalColours = (dataBytes / 4);
                    //}
                    //int[] paletteData = new int[totalColours];
                    //Buffer.BlockCopy(fileData, 0x70, paletteData, 0, totalColours * 4);
                    paletteData = new int[dataBytes / 4];
                    palette = new List<int>(paletteData);
                    Buffer.BlockCopy(fileData, pixelOffset, paletteData, 0, dataBytes);
                    for (int i = 0; i < paletteData.Length; ++i)
                    {
                        paletteData[i] = SwizzleBGR32(paletteData[i]);
                    }
                }
                palette = new List<int>(paletteData);
            }
            else
            {
                palette = FindPalette(fileData, paletteOffset, fileSize);
                short flagAt6e = BitConverter.ToInt16(fileData, 0x6e);
                // tiled palette
                if (flagAt6e == 0x10)
                {
                    for (int i = 0; i < palette.Count; i += 32)
                    {
                        if ((palette.Count - i) < 32) break;
                        for (int j = 0; j < 8; ++j)
                        {
                            int temp = palette[i + 8 + j];
                            palette[i + 8 + j] = palette[i + 16 + j];
                            palette[i + 16 + j] = temp;
                        }
                    }
                }
            }
            List<int> pixels = null;
            while (true)
            {
                try
                {
                    switch (bpp / 2)
                    {
                        case 0:
                            pixels = GenerateSequentialPixels(palette.Count);
                            break;
                        case 4:
                            pixels = Parse4bppIndexes(width, height, pixelOffset, fileData);
                            break;
                        case 8:
                            pixels = ParseIndexes<byte>(width, height, pixelOffset, fileData);
                            //pixels = Parse8bppIndexes(width, height, 0x70, fileData);
                            break;
                        case 16:
                            pixels = ParseIndexes<ushort>(width, height, pixelOffset, fileData);
                            break;
                        default:
                            {
                                Debug.Assert(false, "Unknown bpp pixel value!");
                                System.Windows.Forms.MessageBox.Show("Unknown BPP Value!", "GTMP");
                                return null;
                            }
                            break;
                    }
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught exception {0}\n{1}", e.Message, e.StackTrace);
                    if (bpp != 4)
                    {
                        bpp /= 2;
                    }
                    else bpp = 0;
                }
            }
            PixelFormat pf = (bpp != 8) ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
            //PixelFormat pf = PixelFormat.Format32bppArgb;
            Rectangle r = new Rectangle(0, 0, width, height);
            Bitmap bm = new Bitmap(width, height, pf);
            BitmapData bmData = bm.LockBits(r, ImageLockMode.WriteOnly, pf);
            PlotBitmap(bmData, pixels, palette);
            bm.UnlockBits(bmData);
            //if (bpp != 8)
            {
                Bitmap bm2 = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bm2))
                {
                    //g.FillRectangle(Brushes.Black, 0, 0, width, height);
                    //g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    //g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawImage(bm, 0, 0);
                }
                bm.Dispose();
                bm = bm2;
            }
            return bm;
        }

        static public void DumpTexDir(string inDir, string outDir)
        {
            string[] files = Directory.GetFiles(inDir);
            Directory.CreateDirectory(outDir);
            // max tiles in a file is 0x400, least is 0x24
            foreach (string file in files)
            {
                string justName = Path.GetFileName(file);
                string outputFile = Path.Combine(outDir, justName + ".png");
                //if (!File.Exists(outputFile))
                {
                    Console.WriteLine("Processing '{0}'", justName);
                    using (Bitmap bm = Parse(file))
                    {
                        if (bm != null)
                        {
                           bm.Save(outputFile, ImageFormat.Png);
                        }
                    }
                }
            }
        }
    }
}