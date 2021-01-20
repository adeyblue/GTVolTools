using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace GTMPConverter
{
    class PixelBuffer
    {
        public const int CANVAS_WIDTH = 512;
        public const int CANVAS_HEIGHT = 504;
        public const int TILE_WIDTH = 16;
        public const int TILE_HEIGHT = 8;

        private byte[] pixels;
        private int x;
        private int y;
        private int canvasWidth;
        private int tileWidth;
        private int writtenTiles;

        private delegate byte[] TileConverter(byte[] tileData);
        private TileConverter pixelConverter;

        public PixelBuffer(int bitsPerPixel)
        {
            int bytesRequired = (int)((CANVAS_WIDTH * CANVAS_HEIGHT) * (bitsPerPixel / 8.0f));
            pixels = new byte[bytesRequired];
            writtenTiles = 0;
            switch(bitsPerPixel)
            {
                case 4:
                {
                    pixelConverter = new TileConverter(ConvertTile4BPP);
                    canvasWidth = CANVAS_WIDTH / 2;
                    tileWidth = TILE_WIDTH / 2;
                }
                break;
                case 8:
                {
                    pixelConverter = new TileConverter(ConvertTile8BPP);
                    canvasWidth = CANVAS_WIDTH;
                    tileWidth = TILE_WIDTH;
                }
                break;
                default:
                {
                    Debug.Assert(false, "PixelBuffer constructed with unknown bits per pixel value");
                    throw new InvalidOperationException();
                }
            }
        }

        public void WriteTile(ImageSlice tile)
        {
            if (x == canvasWidth)
            {
                x = 0;
                y += TILE_HEIGHT;
                if (y >= CANVAS_HEIGHT)
                {
                    throw new InvalidOperationException("This image has too many unique tiles to write");
                }
            }
            int bufferCopyPos = (y * canvasWidth) + x;
            byte[] tilePixels = pixelConverter(tile.rect);
            for (int yPos = 0; yPos < TILE_HEIGHT; ++yPos)
            {
                Array.Copy(tilePixels, yPos * tileWidth, pixels, bufferCopyPos, tileWidth);
                bufferCopyPos += canvasWidth;
            }
            x += tileWidth;
            ++writtenTiles;
        }

        public int GetTilesWritten()
        {
            return writtenTiles;
        }

        private byte[] ConvertTile4BPP(byte[] tileData)
        {
            int tilePixels = tileData.Length;
            byte[] pixelData = new byte[tilePixels / 2];
            for (int i = 0, j = 0; i < tilePixels; i += 2, ++j)
            {
                byte firstPixel = tileData[i];
                byte secondPixel = tileData[i + 1];
                Debug.Assert(((firstPixel | secondPixel) & 0xF0) == 0, "Supposed 4bpp tile pixel has palette value greater than 15");
                pixelData[j] = (byte)(firstPixel | (secondPixel << 4));
            }
            return pixelData;
        }

        private byte[] ConvertTile8BPP(byte[] tileData)
        {
            // these are already in 8bpp
            return tileData;
        }

        public byte[] GetWrittenPixels()
        {
            // y full rows plus another TILE_HEIGHT full rows if we have a partially filled one
            bool hasPartialRows = (x > 0);
            int numPixels = (y * canvasWidth) + (hasPartialRows ? (TILE_HEIGHT * canvasWidth) : 0);
            // empty / solid backgrounds still have at least 0x1000 of pixel data
            // even if they don't have any pixels that aren't solid tiles
            int allocatedPixels = Math.Max(numPixels, 0x1000);
            byte[] writtenPixels = new byte[allocatedPixels];
            Array.Copy(pixels, 0, writtenPixels, 0, y * canvasWidth);
            if (hasPartialRows)
            {
                int startPoint = y * canvasWidth;
                for (int i = 0; i < TILE_HEIGHT; ++i)
                {
                    Array.Copy(pixels, startPoint, writtenPixels, startPoint, x);
                    startPoint += canvasWidth;
                }
            }
            return writtenPixels;
        }
    }

    public class ImageSlice : IEquatable<ImageSlice>
    {
        public byte[] rect;
        byte[] hash;

        public ImageSlice(byte[] tile)
        {
            rect = tile;
            SHA1Managed hasher = new SHA1Managed();
            hash = hasher.ComputeHash(rect);
        }

        public override bool Equals(object obj)
        {
            return Equals((ImageSlice)obj);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(hash, 0);
        }

        public bool Equals(ImageSlice other)
        {
            if (ReferenceEquals(this, other)) return true;
            int hashLen = hash.Length / 4;
            int res = 0;
            for (int i = 0; (i < hashLen) && (res == 0); ++i)
            {
                int xInt = BitConverter.ToInt32(hash, i * 4);
                int yInt = BitConverter.ToInt32(other.hash, i * 4);
                res = yInt - xInt;
            }
            return res == 0;
        }
    }
}
