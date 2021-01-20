using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GTMPConverter
{
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

        /// <summary>
        /// These colours need swizzling!!
        /// </summary>
        /// <returns>Unswizzled colours</returns>
        public ushort[] GetColours()
        {
            return palColours.ToArray();
        }

        public ImageSlice AddColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
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
                if ((colour & 0x8000) == 0)
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
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
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
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
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

        public static bool AreAllTransparent(ushort[] colours)
        {
            int numColours = colours.Length;
            for (int i = 0; i < numColours; ++i)
            {
                // transparent is 0 alpha
                if ((colours[i] & 0x8000) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static void FlattenTransparencies(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
            int numColours = colours.Length;
            for (int i = 0; i < numColours; ++i)
            {
                if ((colours[i] & 0xFF000000) != 0xFF000000)
                {
                    colours[i] = 0;
                }
            }
        }

        public static void FlattenTransparencies(ushort[] colours)
        {
            int numColours = colours.Length;
            for (int i = 0; i < numColours; ++i)
            {
                if ((colours[i] & 0x8000) == 0)
                {
                    colours[i] = 0;
                }
            }
        }

        public static int CountUniqueColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
            return CountUniqueColours(convertedColours);
        }

        public static int CountUniqueColours(ushort[] colours)
        {
            List<ushort> unique = RemoveDuplicateColours(colours);
            return unique.Count;
        }

        public bool CanAddColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
            return CanAddColours(convertedColours);
        }

        public bool CanAddColours(ushort[] colours)
        {
            List<ushort> uniqueColours = RemoveDuplicateColours(colours);
            RemoveExistingColours(uniqueColours);
            return (palColours.Count + uniqueColours.Count) < allowedColours;
        }

        private static ushort[] ConvertAllColoursToRGB555(int[] colours)
        {
            ushort[] converted = new ushort[colours.Length];
            for (int i = 0; i < colours.Length; ++i)
            {
                converted[i] = ConvertColourToRGB555(colours[i]);
            }
            return converted;
        }

        private static void GetRGBComponents(int colour, out int r, out int g, out int b)
        {
            r = colour & 0xff;
            g = (colour >> 8) & 0xff;
            b = (colour >> 16) & 0xff;
        }

        public static ushort ConvertColourToRGB555(int colour)
        {
            int r, g, b;
            GetRGBComponents(colour, out r, out g, out b);
            b = ((int)((b * 0x1f) / (float)0xff)) & 0x1f;
            g = ((int)((g * 0x1f) / (float)0xff)) & 0x1f;
            r = ((int)((r * 0x1f) / (float)0xff)) & 0x1f;
            ushort highBit;
            if ((colour & 0xFF000000) == 0xFF000000)
            {
                highBit = 0x8000;
            }
            else highBit = 0;
            return (ushort)(highBit | (r | (g << 5) | (b << 10)));
        }

        public bool ContainsAllColours(int[] colours)
        {
            ushort[] convertedColours = ConvertAllColoursToRGB555(colours);
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
            bool areAllTransparent = Palette.AreAllTransparent(tile);
            if (convertProfile.FilterOutTransparentBlocks && areAllTransparent)
            {
                paletteId = DISCARD_TILE;
                return null;
            }
            // if this is a solid colour tile, it doesn't require pixels
            // or palette data, except if this is a tranparent block, which does
            else if (Palette.AreAllSameColour(tile) && !areAllTransparent)
            {
                paletteId = -1;
                return null;
            }
            Palette.FlattenTransparencies(tile);
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
            Console.WriteLine("Image uses {0} palette(s)", palettes.Count);
            int i = 0;
            ushort[] paletteColours = new ushort[convertProfile.ColoursPerPalette];
            ushort[] defaultPalette = new ushort[paletteColours.Length];
#if DEBUG
            for (int p = 0; p < defaultPalette.Length; ++p)
            {
                // set the default palette to magenta
                // this will show up in the image if any pixel
                // has an out of bounds palette colour index
                defaultPalette[p] = 0x7C1F;
            }
#endif
            foreach (Palette p in palettes)
            {
                Array.Copy(defaultPalette, paletteColours, defaultPalette.Length);
                ushort[] rgbColours = p.GetColours();
                ushort[] bgrColours = Array.ConvertAll(rgbColours, new Converter<ushort, ushort>(convertProfile.SwizzlePaletteColour));
                Array.Copy(bgrColours, 0, paletteColours, 0, bgrColours.Length);
                Console.WriteLine("Palette {0}: {1} colours", i++, bgrColours.Length);
                foreach (ushort c in paletteColours)
                {
                    bw.Write(c);
                }
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
        private bool notSetTile;

        public PositionData(byte xPos, byte yPos, ushort tileAndPaletteValue)
        {
            x = xPos;
            y = yPos;
            tileAndPalette = tileAndPaletteValue;
            notSetTile = false;
        }

        public PositionData(byte xPos, byte yPos)
        {
            x = xPos;
            y = yPos;
            tileAndPalette = 0xffff;
            notSetTile = true;
        }

        public PositionData(PositionData other)
        {
            x = other.x;
            y = other.y;
            tileAndPalette = other.tileAndPalette;
            notSetTile = other.notSetTile;
        }

        public PositionData SetSolidColour(ushort colour)
        {
            tileAndPalette = colour;
            x = (byte)(x | (1 << 7));
            notSetTile = false;
            return this;
        }

        public void Write(BinaryWriter bw)
        {
            if (notSetTile)
            {
                throw new InvalidDataException("PositionData didn't have tile & palette value set before being written");
            }
            bw.Write(x);
            bw.Write(y);
            bw.Write(tileAndPalette);
        }
    }
}
