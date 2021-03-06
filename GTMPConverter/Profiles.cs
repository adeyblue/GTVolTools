﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GTMPConverter
{
    interface IConverterProfile
    {
        int NumPalettes
        {
            get;
        }

        int ColoursPerPalette
        {
            get;
        }

        int BitsPerColour
        {
            get;
        }

        int BitsPerPixel
        {
            get;
        }

        bool FilterOutTransparentBlocks
        {
            get;
        }

        void WriteFile(BinaryWriter bw, List<PositionData> positionData, PaletteList palettes, PixelBuffer pixels);

        ushort MakeTileAndPaletteValue(int tileIndex, int paletteIndex);

        ushort SwizzleSolidColour(ushort colour);

        ushort SwizzlePaletteColour(ushort colour);
    }

    internal class GTMPProfile : IConverterProfile
    {
        public int NumPalettes
        {
            get
            {
                return 16;
            }
        }

        public int ColoursPerPalette
        {
            get
            {
                return 256;
            }
        }

        public int BitsPerColour
        {
            get
            {
                return 16;
            }
        }

        public int BitsPerPixel
        {
            get
            {
                return 8;
            }
        }

        public bool FilterOutTransparentBlocks
        {
            get
            {
                return false;
            }
        }

        public void WriteFile(BinaryWriter bw, List<PositionData> positionData, PaletteList palettes, PixelBuffer pixelBuffer)
        {
            Encoding ascii = Encoding.ASCII;
            byte[] gtmpMarker = ascii.GetBytes("GTMP");
            positionData.Reverse(); // these are written backwards
            bw.Write(gtmpMarker);
            foreach (PositionData pd in positionData)
            {
                pd.Write(bw);
            }
            bw.BaseStream.Position = 0x1f84;
            palettes.Write(bw);
            bw.BaseStream.Position = 0x3f84;
            // 0x3f84 - there is some data here, looks to be tile count
            // which is written twice
            int numTiles = Math.Max(1, pixelBuffer.GetTilesWritten());
            bw.Write(numTiles); // 0x3f84
            bw.Write(numTiles); // 0x3f88
            // 0x3f8c - 0x4000, there is some data here, but it doesnt seem to be read
            // or acted upon by the game, so idk
            bw.BaseStream.Position = 0x4000;
            byte[] pixels = pixelBuffer.GetWrittenPixels();
            bw.Write(pixels, 0, pixels.Length);
        }

        public ushort MakeTileAndPaletteValue(int tileIndex, int paletteIndex)
        {
            Debug.Assert(((tileIndex >= 0) && (tileIndex < 0x1000)), "GTMP conversion got tile with index greater than 0x1000");
            Debug.Assert(((paletteIndex >= 0) && (paletteIndex < NumPalettes)), "GTMP conversion got palette index greater than 16");
            return (ushort)((tileIndex & 0x0fff) | ((paletteIndex & 0xF) << 12));
        }

        public ushort SwizzleSolidColour(ushort colour)
        {
            int blue = colour & 0x1f;
            int green = (colour & (0x1f << 5));
            int red = (colour & (0x1f << 10)) >> 10;
            // Background solid tile colours also never have the high bit set
            // so turn it off
            int highBit = 0;
            return (ushort)(highBit | (blue << 10) | green | red);
        }

        public ushort SwizzlePaletteColour(ushort colour)
        {
            int blue = colour & 0x1f;
            int green = (colour & (0x1f << 5));
            int red = (colour & (0x1f << 10)) >> 10;
            // Backgrounds palette colours always have the high bit set
            // except palette colour 0x0 (ARGB0000) is transparent even in backgrounds
            // so convert every transparent pixel to that colour
            int highBit = colour & 0x8000;
            if ((highBit & 0x8000) == 0)
            {
                // these should have already been flattened
                Debug.Assert(colour == 0);
            }
            return (ushort)(highBit | (blue << 10) | green | red);
        }
    }

    internal class GMProfile : IConverterProfile
    {
        public int NumPalettes
        {
            get
            {
                return 32;
            }
        }

        public int ColoursPerPalette
        {
            get
            {
                return 16;
            }
        }

        public int BitsPerColour
        {
            get
            {
                return 16;
            }
        }

        public int BitsPerPixel
        {
            get
            {
                return 4;
            }
        }

        public bool FilterOutTransparentBlocks
        {
            get
            {
                return true;
            }
        }

        public void WriteFile(BinaryWriter bw, List<PositionData> positionData, PaletteList palettes, PixelBuffer pixels)
        {
            // GMLL header
            bw.Write(Encoding.ASCII.GetBytes("GMLL"));
            // number of tile positions
            bw.Write(positionData.Count);
            positionData.Reverse();
            // the tile data
            foreach (PositionData pd in positionData)
            {
                pd.Write(bw);
            }
            // then the palettes
            palettes.Write(bw);
            // space is included for any unused palettes
            int usedPalettes = palettes.Count;
            int paddingPalettes = NumPalettes - usedPalettes;
            int padBytes = paddingPalettes * (ColoursPerPalette * (BitsPerColour / 8));
            bw.Seek(padBytes, SeekOrigin.Current);
            // then the number of pixel tiles, since there can be solid colour tiles
            // this can be less than the tile positions written above
            byte[] pixelData = pixels.GetWrittenPixels();
            int actualNumTiles = pixels.GetTilesWritten();
            bw.Write(actualNumTiles);
            // finally the actual pixel data
            bw.Write(pixelData);
        }

        public ushort MakeTileAndPaletteValue(int tileIndex, int paletteIndex)
        {
            Debug.Assert(((tileIndex >= 0) && (tileIndex < 0x800)), "GM conversion got tile with index greater than 0x800");
            //Debug.Assert(((paletteIndex >= 0) && (paletteIndex < NumPalettes)), "GM conversion got palette index greater than 32");
            return (ushort)((tileIndex & 0x07ff) | ((paletteIndex & 0x1F) << 11));
        }

        public ushort SwizzleSolidColour(ushort colour)
        {
            int red = colour & 0x1f;
            int green = (colour & (0x1f << 5));
            int blue = (colour & (0x1f << 10));
            // GM game colours never have the high-bit set
            // The exception is that black (ARGB0000) is treated as transparent by the game
            // except when it has the high bit set (ARGB1000), which makes it opaque
            // so we leave the top bits for both blacks alone and toggle all the rest
            // since in PC land images, the high bits are set for opaque colours
            // this could perhaps be left alone, but without the bit is how the game does it
            // so it's how we're doing it
            if ((colour & 0x7fff) != 0)
            {
                colour ^= 0x8000;
            }
            int highBit = colour & 0x8000;
            return (ushort)(highBit | ((red << 10) | green | (blue >> 10)));
        }

        public ushort SwizzlePaletteColour(ushort colour)
        {
            return SwizzleSolidColour(colour);
        }
    }

    static class Profiles
    {
        static public IConverterProfile GTMP
        {
            get
            {
                return new GTMPProfile();
            }
        }

        static internal IConverterProfile GM
        {
            get
            {
                return new GMProfile();
            }
        }
    }
}
