//#define USE_32BIT_SOURCE
// defining this will make the images load in 32bpp

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace GTMPConverter
{
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine(
                "Usage: GT2ImageConverter <inFile> <outFile> <profile>{0}" +
                "       <inFile> must be a {1}x{2} bmp/png/jpg to convert{0}" +
                "       <outFile> where to save the converted file{0}" +
                "       <profile> is either:{0}" +
                "           GTMP to convert to a background, or{0}" +
                "           GM to convert to a GMLL foreground image{0}" +
                "{0}" +
                "Notes:{0}" +
                "    1) GTMP files are limited to {3} palettes of {4} colours{0}"+ 
                "       ({5} total colours) and each 16x8 tile can only use 1 palette{0}" +
                "    2) GM files are limited to {6} palettes of {7} colours{0}" +
                "       ({8} total colours) and each 16x8 tile can only use 1 palette{0}" +
                "    3) For both, any 16x8 tile that is just one colour does not{0}" +
                "       take up any palette space{0}" + 
                "{0}" +
                "    If you get a 'too many colours' error, the input image will{0}"+
                "    have gone over one or more of these limits{0}" +
                "{0}" +
                "    Please note the GM profile only converts the image{0}" +
                "    data to the GMLL portion of a full GM file. It does{0}" +
                "    not create a fully valid GM file. Please use GMCreator{0}" +
                "    to create those.",
                Environment.NewLine,
                PixelBuffer.CANVAS_WIDTH,
                PixelBuffer.CANVAS_HEIGHT,
                Profiles.GTMP.NumPalettes, Profiles.GTMP.ColoursPerPalette,
                Profiles.GTMP.NumPalettes * Profiles.GTMP.ColoursPerPalette,
                Profiles.GM.NumPalettes, Profiles.GM.ColoursPerPalette,
                Profiles.GM.NumPalettes * Profiles.GM.ColoursPerPalette
            );
        }

        static void DisplayError(string format, params object[] p)
        {
            ConsoleColor fgColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, p);
            Console.ForegroundColor = fgColour;
        }

        static void PrintUsageAndExit()
        {
            PrintUsage();
            Environment.Exit(1);
        }

        static void PrintErrorAndExit(string message, params object[] p)
        {
            DisplayError(message, p);
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            if (
                (args.Length < 3) || 
                (!File.Exists(args[0]))
            )
            {
                PrintUsageAndExit();
            }
            string profile = args[2].ToLowerInvariant();
            if (profile[0] == '"' || profile[0] == '\'')
            {
                profile = profile.Substring(1, profile.Length - 2);
            }
            if ((profile != "gm") && (profile != "gtmp"))
            {
                PrintErrorAndExit("Invalid profile {0}, must be GTMP or GM", args[2]);
            }
            IConverterProfile profileToUse = (profile == "gm") ? Profiles.GM : Profiles.GTMP;
            Bitmap bm = new Bitmap(args[0]);
            try
            {
#if USE_32BIT_SOURCE
                using (Bitmap sourceFile = bm.Clone(new Rectangle(Point.Empty, bm.Size), System.Drawing.Imaging.PixelFormat.Format32bppRgb))
#else
                using (Bitmap sourceFile = bm.Clone(new Rectangle(Point.Empty, bm.Size), System.Drawing.Imaging.PixelFormat.Format16bppArgb1555))
#endif
                {
                    bm.Dispose();
                    Size imgSize = sourceFile.Size;
                    if ((imgSize.Width != PixelBuffer.CANVAS_WIDTH) || (imgSize.Height != PixelBuffer.CANVAS_HEIGHT))
                    {
                        PrintErrorAndExit("File is sized {0}x{1} instead of {2}x{3}", imgSize.Width, imgSize.Height, PixelBuffer.CANVAS_WIDTH, PixelBuffer.CANVAS_HEIGHT);
                    }
                    Convert(profileToUse, sourceFile, args[1]);
                }
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                PrintErrorAndExit("Conversion failed due to error: {0}", e.Message);
                Environment.ExitCode = 2;
            }
        }

        static void Convert(IConverterProfile profile, Bitmap source, string outFile)
        {
            const int totalPixels = PixelBuffer.CANVAS_WIDTH * PixelBuffer.CANVAS_HEIGHT;
#if USE_32BIT_SOURCE
            int[] originalPixels = new int[totalPixels];
            {
                Rectangle lockRect = new Rectangle(Point.Empty, source.Size);
                System.Drawing.Imaging.BitmapData sourceData = source.LockBits(lockRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                Debug.Assert(sourceData.Stride == (sourceData.Width * 4));
                System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, originalPixels, 0, originalPixels.Length);
                source.UnlockBits(sourceData);
            }
#else
            ushort[] originalPixels = new ushort[totalPixels];
            {
                Rectangle lockRect = new Rectangle(Point.Empty, source.Size);
                System.Drawing.Imaging.BitmapData sourceData = source.LockBits(lockRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format16bppArgb1555);
                Debug.Assert(sourceData.Stride == (sourceData.Width * 2));
                short[] tempPixels = new short[totalPixels];
                System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, tempPixels, 0, tempPixels.Length);
                source.UnlockBits(sourceData);
                Buffer.BlockCopy(tempPixels, 0, originalPixels, 0, tempPixels.Length * sizeof(ushort));
            }
#endif
            PaletteList palettes = new PaletteList(profile);
            List<PositionData> positions = new List<PositionData>(0x800);
            PixelBuffer convertedPixels = new PixelBuffer(profile.BitsPerPixel);
            ConvertAllTiles(originalPixels, convertedPixels, palettes, positions, profile);
            using(MemoryStream outputStream = new MemoryStream(50000))
            {
                using (BinaryWriter bw = new BinaryWriter(outputStream))
                {
                    profile.WriteFile(bw, positions, palettes, convertedPixels);
                }
                File.WriteAllBytes(outFile, outputStream.ToArray());
            }
        }

        struct TileInfo
        {
            public int tileIndex;
            public int paletteId;
            public TileInfo(int id, int palette)
            {
                tileIndex = id;
                paletteId = palette;
            }
        }

#if USE_32BIT_SOURCE
        static void ConvertAllTiles(int[] pixels, PixelBuffer convertedPixels, PaletteList palettes, List<PositionData> positions, IConverterProfile profile)
#else
        static void ConvertAllTiles(ushort[] pixels, PixelBuffer convertedPixels, PaletteList palettes, List<PositionData> positions, IConverterProfile profile)
#endif
        {
            List<ImageSlice> convertedTiles = new List<ImageSlice>();
            Dictionary<ImageSlice, TileInfo> uniqueTiles = new Dictionary<ImageSlice, TileInfo>();
            int tiles = 0;
            for (int y = 0; y < PixelBuffer.CANVAS_HEIGHT; y += PixelBuffer.TILE_HEIGHT)
            {
                for (int x = 0; x < PixelBuffer.CANVAS_WIDTH; x += PixelBuffer.TILE_WIDTH)
                {
#if USE_32BIT_SOURCE
                    int[] tile = GetTileData(pixels, x, y);
#else
                    ushort[] tile = GetTileData(pixels, x, y);
#endif
                    int usedPalette;
                    PositionData pd;
                    ImageSlice slice = palettes.NaturalizeTile(tile, out usedPalette);
                    if (slice != null)
                    {
                        ++tiles;
                        int uniqueTileId;
                        // see if this slice already exists
                        if (uniqueTiles.ContainsKey(slice))
                        {
                            TileInfo info = uniqueTiles[slice];
                            ImageSlice dup = convertedTiles[info.tileIndex];
                            Debug.Assert(AreArraysEqual(dup.rect, slice.rect), "In duplicate tile branch, but tiles aren't duplicates");
                            usedPalette = info.paletteId;
                            uniqueTileId = info.tileIndex;
                        }
                        else
                        {
                            // it's a unique slice, add it
                            uniqueTileId = convertedTiles.Count;
                            TileInfo ti = new TileInfo(uniqueTileId, usedPalette);
                            convertedTiles.Add(slice);
                            uniqueTiles.Add(slice, ti);
                            convertedPixels.WriteTile(slice);
                        }
                        //Debug.Assert(usedPalette < profile.NumPalettes);
                        pd = new PositionData(
                            (byte)(x / PixelBuffer.TILE_WIDTH),
                            (byte)(y / PixelBuffer.TILE_HEIGHT),
                            profile.MakeTileAndPaletteValue(uniqueTileId, usedPalette)
                        );
                        positions.Add(pd);
                    }
                    else if(usedPalette != PaletteList.DISCARD_TILE) // these are all the same colour
                    {
#if USE_32BIT_SOURCE
                        ushort singleColour = Palette.ConvertColourToBGR555(tile[0]);
#else
                        ushort singleColour = Palette.SwizzleColour(tile[0]);
#endif
                        pd = new PositionData(
                            (byte)(x / PixelBuffer.TILE_WIDTH),
                            (byte)(y / PixelBuffer.TILE_HEIGHT)
                        ).SetSolidColour(singleColour);
                        positions.Add(pd);
                    }
                    // else this is a tile containing only black and/or transparent colours
                    // and the converter profile said it shouldn't be included in the image file
                }
            }
            Console.WriteLine("Total tiles: {0}, unique tiles: {1}", tiles, convertedTiles.Count);
        }

#if USE_32BIT_SOURCE
        static int[] GetTileData(int[] pixels, int x, int y)
        {
            int[] tile = new int[PixelBuffer.TILE_HEIGHT * PixelBuffer.TILE_WIDTH];
#else
        static ushort[] GetTileData(ushort[] pixels, int x, int y)
        {
            ushort[] tile = new ushort[PixelBuffer.TILE_HEIGHT * PixelBuffer.TILE_WIDTH];
#endif
            int startPos = (y * PixelBuffer.CANVAS_WIDTH) + x;
            for (int i = 0; i < PixelBuffer.TILE_HEIGHT; ++i)
            {
                Array.Copy(pixels, startPos, tile, i * PixelBuffer.TILE_WIDTH, PixelBuffer.TILE_WIDTH);
                startPos += PixelBuffer.CANVAS_WIDTH;
            }
            return tile;
        }

        internal static bool AreArraysEqual<T>(T[] ar1, T[] ar2)
        {
            int arLen = ar1.Length;
            if (arLen != ar2.Length)
            {
                return false;
            }
            bool equal = true;
            for (int i = 0; (i < arLen) && equal; ++i)
            {
                equal = (ar1[i].Equals(ar2[i]));
            }
            return equal;
        }
    }
}
