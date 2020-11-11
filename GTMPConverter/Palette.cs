using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GTMPConverter
{
    class OrigPalette
    {
        SortedList<int, int> extantColours;
        Dictionary<int, ushort> palColours;
        private int allowedColours;

        public OrigPalette(int id, int numColours)
        {
            allowedColours = numColours;
            extantColours = new SortedList<int, int>(numColours);
            palColours = new Dictionary<int, ushort>(numColours);
            Id = id;
        }

        public int Id
        {
            get;
            private set;
        }

        public ushort[] GetColours()
        {
            List<ushort> bgrCols = new List<ushort>(palColours.Values);
            return bgrCols.ToArray();
        }

        public ImageSlice AddColours(int[] colours)
        {
            List<int> uniqueColours = RemoveDuplicateColours(colours);
            Debug.Assert(uniqueColours.Count > 1);
            RemoveExistingColours(uniqueColours);
            int newColourIndex = palColours.Count;
            foreach (int c in uniqueColours)
            {
                palColours.Add(c, ConvertPaletteColourToBGR555(c));
                extantColours.Add(c, newColourIndex++);
            }
            return ColoursToPixels(colours);
        }

        public ImageSlice ColoursToPixels(int[] colours)
        {
            byte[] pixels = new byte[colours.Length];
            Debug.Assert(palColours.Count <= allowedColours);
            Debug.Assert(ContainsAllColours(colours));
            for (int i = 0; i < colours.Length; ++i)
            {
                pixels[i] = (byte)extantColours[colours[i]];
            }
            return new ImageSlice(pixels);
        }

        private static List<int> RemoveDuplicateColours(int[] colours)
        {
            List<int> uniqueColours = new List<int>(colours);
            uniqueColours.Sort();
            for (int i = 1; i < uniqueColours.Count; ++i)
            {
                if (uniqueColours[i] == uniqueColours[i - 1])
                {
                    uniqueColours.RemoveAt(i);
                    --i;
                }
            }
            return uniqueColours;
        }

        private void RemoveExistingColours(List<int> colours)
        {
            for (int i = 0; i < colours.Count; ++i)
            {
                if (palColours.ContainsKey(colours[i]))
                {
                    colours.RemoveAt(i);
                    --i;
                }
            }
        }

        public static bool AreAllSameColour(int[] colours)
        {
            return RemoveDuplicateColours(colours).Count == 1;
        }

        public static bool AreAllBlackOrTransparent(int[] colours)
        {
            int numColours = colours.Length;
            for (int i = 0; i < numColours; ++i)
            {
                if (((colours[i] & 0xffffff) != 0) && (colours[i] != -1))
                {
                    return false;
                }
            }
            return true;
        }

        public static int CountUniqueColours(int[] colours)
        {
            List<int> unique = RemoveDuplicateColours(colours);
            return unique.Count;
        }

        public bool CanAddColours(int[] colours)
        {
            List<int> uniqueColours = RemoveDuplicateColours(colours);
            RemoveExistingColours(uniqueColours);
            return (palColours.Count + uniqueColours.Count) < allowedColours;
        }

        private static ushort[] ConvertAllColoursToBGR555(int[] colours)
        {
            ushort[] converted = new ushort[colours.Length];
            for (int i = 0; i < colours.Length; ++i)
            {
                converted[i] = ConvertPaletteColourToBGR555(colours[i]);
            }
            return converted;
        }

        private static void GetRGBComponents(int colour, out int r, out int g, out int b)
        {
            r = colour & 0xff;
            g = (colour >> 8) & 0xff;
            b = (colour >> 16) & 0xff;
        }

        private static ushort ConvertPaletteColourToBGR555(int colour)
        {
            // all the palette colours seem to have the high bit set,
            // while the static colour tiles don't
            return (ushort)((1 << 15) | ConvertColourToBGR555(colour));
        }

        public static ushort ConvertColourToBGR555(int colour)
        {
            const float mulFactor = 0x1f / (float)0xff;
            int r, g, b;
            GetRGBComponents(colour, out r, out g, out b);
            g = ((int)(g * mulFactor)) & 0x1f;
            b = ((int)(b * mulFactor)) & 0x1f;
            r = ((int)(r * mulFactor)) & 0x1f;
            return (ushort)(b | (g << 5) | (r << 10));
        }

        public bool ContainsAllColours(int[] colours)
        {
            return Array.TrueForAll(colours, palColours.ContainsKey);
        }
    }

    class Palette
    {
        // colour - index
        SortedList<int, byte> extantColours;
        // orig colour to converted pal colour
        List<ushort> palColours;
        // max colours allowed in the palette
        private int allowedColours;

        public Palette(int id, int numColours)
        {
            allowedColours = numColours;
            extantColours = new SortedList<int, byte>(numColours);
            palColours = new List<ushort>(numColours);
            Id = id;
        }

        public int Id
        {
            get;
            private set;
        }

        public static ushort SwizzleColour(ushort colour)
        {
            int red = colour & 0x1f;
            int green = (colour & (0x1f << 5));
            int blue = (colour & (0x1f << 10)) >> 10;
            int highBit = colour & 0x8000;
            return (ushort)(highBit | (red << 10) | green | blue);
        }

        public ushort[] GetColours()
        {
            return palColours.ConvertAll<ushort>(SwizzleColour).ToArray();
        }

        public ImageSlice AddColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToBGR555(colours);
            return AddColours(convertedColours);
        }

        public ImageSlice AddColours(ushort[] colours)
        {
            List<ushort> uniqueColours = RemoveDuplicateColours(colours);
            Debug.Assert(uniqueColours.Count > 1);
            RemoveExistingColours(uniqueColours);
            byte newColourIndex = (byte)palColours.Count;
            foreach (ushort c in uniqueColours)
            {
                palColours.Add(c);
                extantColours.Add(c, newColourIndex++);
            }
            return ColoursToPixelsInternal(colours);
        }

        // the colour values need swizzling
        private ImageSlice ColoursToPixelsInternal(ushort[] colours)
        {
            byte[] pixels = new byte[colours.Length];
            for (int i = 0; i < colours.Length; ++i)
            {
                ushort colour = colours[i];
                if((colour & 0x8000) == 0)
                {
                    pixels[i] = extantColours[0];
                }
                else
                {
                    pixels[i] = extantColours[colour];
                }

            }
            return new ImageSlice(pixels);
        }

        public ImageSlice ColoursToPixels(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToBGR555(colours);
            return ColoursToPixels(convertedColours);
        }

        public ImageSlice ColoursToPixels(ushort[] colours)
        {
            Debug.Assert(palColours.Count <= allowedColours);
            Debug.Assert(ContainsAllConvertedColours(colours));
            return ColoursToPixelsInternal(colours);
        }

        private static List<ushort> RemoveDuplicateColours(ushort[] colours)
        {
            List<ushort> uniqueColours = new List<ushort>(colours);
            uniqueColours.Sort();
            bool hasSeenTransparent = false;
            for (int i = 0; i < uniqueColours.Count; ++i)
            {
                // transform sll transparent colours to 0
                // the game renders this as transparent
                if ((uniqueColours[i] & 0x8000) == 0)
                {
                    if (hasSeenTransparent)
                    {
                        uniqueColours.RemoveAt(i);
                        --i;
                    }
                    else
                    {
                        hasSeenTransparent = true;
                        uniqueColours[i] = 0;
                    }
                }
                else if ((i >= 1) && (uniqueColours[i] == uniqueColours[i - 1]))
                {
                    uniqueColours.RemoveAt(i);
                    --i;
                }
            }
            return uniqueColours;
        }

        private void RemoveExistingColours(List<ushort> colours)
        {
            for (int i = 0; i < colours.Count; ++i)
            {
                if (palColours.Contains(colours[i]))
                {
                    colours.RemoveAt(i);
                    --i;
                }
            }
        }

        public static bool AreAllSameColour(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToBGR555(colours);
            return AreAllSameColour(convertedColours);
        }

        public static bool AreAllSameColour(ushort[] colours)
        {
            return RemoveDuplicateColours(colours).Count == 1;
        }

        public static bool AreAllBlackOrTransparent(int[] colours)
        {
            int transparent = System.Drawing.Color.Transparent.A;
            int numColours = colours.Length;
            for (int i = 0; i < numColours; ++i)
            {
                // black is RGB 0,0,0, transparent is 0xff alpha
                if (((colours[i] & 0xffffff) != 0) || ((colours[i] >> 24) != transparent))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool AreAllBlackOrTransparent(ushort[] colours)
        {
            int numColours = colours.Length;
            for (int i = 0; i < numColours; ++i)
            {
                // black is RGB 0,0,0, transparent is 0 alpha
                if (((colours[i] & 0x7fff) != 0) && ((colours[i] >> 15) != 0))
                {
                    return false;
                }
            }
            return true;
        }

        public static int CountUniqueColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToBGR555(colours);
            return CountUniqueColours(convertedColours);
        }

        public static int CountUniqueColours(ushort[] colours)
        {
            List<ushort> unique = RemoveDuplicateColours(colours);
            return unique.Count;
        }

        public bool CanAddColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToBGR555(colours);
            return CanAddColours(convertedColours);
        }

        public bool CanAddColours(ushort[] colours)
        {
            List<ushort> uniqueColours = RemoveDuplicateColours(colours);
            RemoveExistingColours(uniqueColours);
            return (palColours.Count + uniqueColours.Count) < allowedColours;
        }

        private static ushort[] ConvertAllColoursToBGR555(int[] colours)
        {
            ushort[] converted = new ushort[colours.Length];
            for (int i = 0; i < colours.Length; ++i)
            {
                converted[i] = ConvertPaletteColourToBGR555(colours[i]);
            }
            return converted;
        }

        private static void GetRGBComponents(int colour, out int r, out int g, out int b)
        {
            r = colour & 0xff;
            g = (colour >> 8) & 0xff;
            b = (colour >> 16) & 0xff;
        }

        private static ushort ConvertPaletteColourToBGR555(int colour)
        {
            // all the palette colours seem to have the high bit set,
            // while the static colour tiles don't
            return (ushort)((1 << 15) | ConvertColourToBGR555(colour));
        }

        public static ushort ConvertColourToBGR555(int colour)
        {
            //const float mulFactor = 0x1f / (float)0xff;
            int r, g, b;
            GetRGBComponents(colour, out r, out g, out b);
            b = ((int)((b * 0x1f) / (float)0xff)) & 0x1f;
            g = ((int)((g * 0x1f) / (float)0xff)) & 0x1f;
            r = ((int)((r * 0x1f) / (float)0xff)) & 0x1f;
            //g = ((int)(g * mulFactor)) & 0x1f;
            //b = ((int)(b * mulFactor)) & 0x1f;
            //r = ((int)(r * mulFactor)) & 0x1f;
            return (ushort)(b | (g << 5) | (r << 10));
        }

        public bool ContainsAllColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToBGR555(colours);
            return ContainsAllColours(convertedColours);
        }

        public bool ContainsAllColours(ushort[] colours)
        {
            return ContainsAllConvertedColours(colours);
        }

        private bool ContainsAllConvertedColours(ushort[] colours)
        {
            return Array.TrueForAll(colours, palColours.Contains);
        }
    }
    
    class PaletteList
    {
        private List<Palette> palettes;
        private int nextPaletteId;
        private IConverterProfile convertProfile;

        public PaletteList(IConverterProfile profile)
        {
            convertProfile = profile;
            palettes = new List<Palette>(profile.NumPalettes);
            nextPaletteId = 0;
            AddNewPalette();
        }

        public const int DISCARD_TILE = -2;

#if USE_32BIT_SOURCE
        public ImageSlice NaturalizeTile(int[] tile, out int paletteId)
#else
        public ImageSlice NaturalizeTile(ushort[] tile, out int paletteId)
#endif
        {
            if (convertProfile.FilterOutBlackAndTransparentBlocks && Palette.AreAllBlackOrTransparent(tile))
            {
                paletteId = DISCARD_TILE;
                return null;
            }
            // if this is a solid colour tile, it doesn't require pixels
            // or palette data
            else if (Palette.AreAllSameColour(tile))
            {
                paletteId = -1;
                return null;
            }            
            int uniqueColours = Palette.CountUniqueColours(tile);
            if (uniqueColours > convertProfile.ColoursPerPalette)
            {
                throw new InvalidDataException(
                    String.Format(
                        "Image has a {0}x{1} tile with more colours ({2}) than are allowed in a palette ({3}). Simplify the image and try again",
                        PixelBuffer.TILE_WIDTH,
                        PixelBuffer.TILE_HEIGHT,
                        uniqueColours,
                        convertProfile.ColoursPerPalette
                    )
                );
            }
            foreach (Palette p in palettes)
            {
                if (p.ContainsAllColours(tile))
                {
                    paletteId = p.Id;
                    return p.ColoursToPixels(tile);
                }
                if (p.CanAddColours(tile))
                {
                    paletteId = p.Id;
                    return p.AddColours(tile);
                }
            }
            // no space and we don't have these colours, make another palette to contain them
            Palette newPalette = AddNewPalette();
            paletteId = newPalette.Id;
            return newPalette.AddColours(tile);
        }

        public void Write(BinaryWriter bw)
        {
            if (palettes.Count > convertProfile.NumPalettes)
            {
                ThrowTooManyColours();
            }
#if DEBUG
            Console.WriteLine("Image uses {0} palette(s)", palettes.Count);
            int i = 0;
#endif
            byte[] blacks = new byte[512];
            foreach (Palette p in palettes)
            {
                ushort[] bgrColours = p.GetColours();
#if DEBUG
                Console.WriteLine("Palette {0}: {1} colours", i++, bgrColours.Length);
#endif
                foreach (ushort c in bgrColours)
                {
                    bw.Write(c);
                }
                int paddingColours = convertProfile.ColoursPerPalette - bgrColours.Length;
                bw.Write(blacks, 0, paddingColours * sizeof(ushort));
            }
        }

        public int Count
        {
            get
            {
                return palettes.Count;
            }
        }

        private Palette AddNewPalette()
        {
            Palette newPal = new Palette(nextPaletteId++, convertProfile.ColoursPerPalette);
            palettes.Add(newPal);
            return newPal;
        }

        private void ThrowTooManyColours()
        {
            throw new InvalidDataException(
                String.Format(
                    "This image requires {0} palettes and {1} total colours, which is more than the allowed {2} palettes or {3} total colours. Reduce the number of colours in the image and try again",
                    nextPaletteId,
                    nextPaletteId * convertProfile.ColoursPerPalette,
                    convertProfile.NumPalettes,
                    convertProfile.NumPalettes * convertProfile.ColoursPerPalette
                )
            );
        }
    }

    public struct PositionData
    {
        public byte x;
        public byte y;
        public ushort tileAndPalette;

        public PositionData(byte xPos, byte yPos, int tileIndex, int paletteIndex)
        {
            x = xPos;
            y = yPos;
            Debug.Assert(tileIndex < 0x1000);
            tileAndPalette = (ushort)((tileIndex & 0x0fff) | ((paletteIndex & 0xF) << 12));
        }

        public PositionData(byte xPos, byte yPos, ushort solidColour)
        {
            x = xPos;
            y = yPos;
            tileAndPalette = solidColour;
            x = (byte)(x | (1 << 7));
        }

        public PositionData(PositionData other)
        {
            x = other.x;
            y = other.y;
            tileAndPalette = other.tileAndPalette;
        }

        public void SetSolidColour(ushort colour)
        {
            tileAndPalette = colour;
            x = (byte)(x | 1 << 7);
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(x);
            bw.Write(y);
            bw.Write(tileAndPalette);
        }
    }
}
