﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GT2
{
    internal class Palette
    {
        public ushort[] colours;
        public Palette(int numColours)
        {
            colours = new ushort[numColours];
        }

        public static ushort GMSwizzleColour(ushort colour)
        {
            int blue = colour & 0x1f;
            int green = (colour & (0x1f << 5));
            int red = (colour & (0x1f << 10)) >> 10;
            // The game colours never have the high-bit set
            // The exception is that black (ARGB0000) is treated as transparent by the game
            // except when it has the high bit set, which makes it opaque
            // so we leave the top bits for both blacks alone and toggle all the rest
            if ((colour & 0x7fff) != 0)
            {
                colour ^= 0x8000;
            }
            int highBit =  colour & 0x8000;
            return (ushort)(highBit | (blue << 10) | green | red);
        }

        public static ushort GTMPSwizzleColour(ushort colour)
        {
            int blue = colour & 0x1f;
            int green = (colour & (0x1f << 5));
            int red = (colour & (0x1f << 10)) >> 10;
            // Backgrounds palette colours always have the high bit set, except transparent black
            // Background solid tiles never have the high bit set
            int highBit = colour & 0x8000;
            return (ushort)(highBit | (blue << 10) | green | red);
        }

        public void GMSwizzleColours()
        {
            for (int i = 0; i < colours.Length; ++i)
            {
                colours[i] = GMSwizzleColour(colours[i]);
            }
        }

        public void GTMPSwizzleColours()
        {
            for (int i = 0; i < colours.Length; ++i)
            {
                colours[i] = GTMPSwizzleColour(colours[i]);
            }
        }
    }

    internal class ImageSlice
    {
        public byte[] rect;
        public ImageSlice(byte[] data)
        {
            rect = data;
        }
        public ImageSlice()
        {
            rect = new byte[16 * 8];
        }
    }

    internal class PositionData
    {
        public int x;
        public int y;
        public ushort tile;
        public byte palette;
        public PositionData()
        {
            x = y = 0;
            tile = 0;
            palette = 0;
        }

        public PositionData(PositionData other)
        {
            x = other.x;
            y = other.y;
            tile = other.tile;
            palette = other.palette;
        }
    }

    public class RefCounter
    {
        int number;
        public RefCounter()
        {
            number = 0;
        }

        public int Increment()
        {
            return System.Threading.Interlocked.Increment(ref number);
        }

        public bool HasReached(int target)
        {
            return System.Threading.Thread.VolatileRead(ref number) == target;
        }
    }

    static class Common
    {
        public static void ArrangeSlices(
            Bitmap bm, 
            List<ImageSlice> slices, 
            List<Palette> palettes, 
            List<PositionData> posData
        )
        {
            Rectangle lockRect = new Rectangle(0, 0, bm.Width, bm.Height);
            BitmapData bd = bm.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
            int byteStride = bd.Stride;
            const int numColours = 16 * 8;
            short[] tempColours = new short[numColours];
            ushort[] colourData = new ushort[numColours];
            foreach (PositionData pd in posData)
            {
                Array.Clear(colourData, 0, colourData.Length);
                if (pd.palette != 0xFF)
                {
                    ImageSlice tile = slices[pd.tile];
                    Palette p = palettes[pd.palette];

                    for (int i = 0; i < numColours; ++i)
                    {
                        ushort pixelColour = p.colours[tile.rect[i]];
                        colourData[i] = pixelColour;
                    }
                }
                else
                {
                    ushort colour = (ushort)pd.tile;
                    for (int i = 0; i < numColours; ++i)
                    {
                        colourData[i] = colour;
                    }
                }
                Buffer.BlockCopy(colourData, 0, tempColours, 0, colourData.Length * sizeof(ushort));
                // find the starting position for this position data
                IntPtr scanLineIter = new IntPtr(bd.Scan0.ToInt64() + ((pd.y * byteStride) + (pd.x * sizeof(ushort))));
                for (int i = 0; i < 8; ++i)
                {
                    // copy the pixels for this row
                    Marshal.Copy(tempColours, i * 16, scanLineIter, 16);
                    // go to next row
                    scanLineIter = new IntPtr(scanLineIter.ToInt64() + bd.Stride);
                }
            }
            bm.UnlockBits(bd);
        }
    }
}