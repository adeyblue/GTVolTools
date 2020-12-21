using System;
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

        bool FilterOutBlackAndTransparentBlocks
        {
            get;
        }

        void WriteFile(BinaryWriter bw, List<PositionData> positionData, PaletteList palettes, PixelBuffer pixels);

        ushort MakeTileAndPaletteValue(int tileIndex, int paletteIndex);
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

        public bool FilterOutBlackAndTransparentBlocks
        {
            get
            {
                return false;
            }
        }

        public void WriteFile(BinaryWriter bw, List<PositionData> positionData, PaletteList palettes, PixelBuffer pixelBuffer)
        {
            byte[] gtmpMarker = { (byte)'G', (byte)'T', (byte)'M', (byte)'P' };
            bw.Write(gtmpMarker, 0, gtmpMarker.Length);
            foreach (PositionData pd in positionData)
            {
                pd.Write(bw);
            }
            bw.BaseStream.Position = 0x1f84;
            palettes.Write(bw);
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

        public bool FilterOutBlackAndTransparentBlocks
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
            int writtenPixels = pixelData.Length * (8 / BitsPerPixel);
            int numTiles = writtenPixels / (PixelBuffer.TILE_HEIGHT * PixelBuffer.TILE_WIDTH);
            int actualNumTiles = pixels.GetTilesWritten();
            bw.Write(actualNumTiles);
            // finally the actual pixel data
            bw.Write(pixelData);
        }

        public ushort MakeTileAndPaletteValue(int tileIndex, int paletteIndex)
        {
            Debug.Assert(((tileIndex >= 0) && (tileIndex < 0x800)), "GM conversion got tile with index greater than 0x800");
            Debug.Assert(((paletteIndex >= 0) && (paletteIndex < NumPalettes)), "GM conversion got palette index greater than 32");
            return (ushort)((tileIndex & 0x07ff) | ((paletteIndex & 0x1F) << 11));
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
