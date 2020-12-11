using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace GMCreator
{
    static class Images
    {
        internal enum ImageType
        {
            Bitmap = 1,
            GTMP = 2,
            GM = 3,
            GMLL = 4
        }

        internal class ImageLoadResult
        {
            public ImageType type;
            public Bitmap image;
            public GTMP.GMFile.GMFileInfo gmInfo;

            public ImageLoadResult(Bitmap bm)
                : this(bm, ImageType.Bitmap)
            {}

            public ImageLoadResult(Bitmap bm, ImageType imgType)
            {
                type = imgType;
                image = bm;
                gmInfo = null;
            }

            public ImageLoadResult(GTMP.GMFile.GMFileInfo info)
            {
                type = ImageType.GM;
                image = info.Image;
                gmInfo = info;
            }
        }

        static internal ImageLoadResult LoadFile(string fileName)
        {
            try
            {
                Bitmap bm = new Bitmap(fileName);
                return new ImageLoadResult(bm);
            }
            catch(Exception)
            {
                ;
            }
            MemoryStream streamToUse;
            using (FileStream fs = File.OpenRead(fileName))
            {
                streamToUse = Compress.DecompressStream(fs);
            }
            byte[] headerBytes = new byte[4];
            streamToUse.Read(headerBytes, 0, headerBytes.Length);
            streamToUse.Position = 0;
            string header = Encoding.ASCII.GetString(headerBytes);
            if (header == "GTMP")
            {
                Bitmap image = GTMP.GTMPFile.Parse(streamToUse);
                return new ImageLoadResult(image, ImageType.GTMP);
            }
            else if (header == "GM\x3\x0")
            {
                return new ImageLoadResult(GTMP.GMFile.Parse(streamToUse));
            }
            // also allow files that have been pre-converted by the GT2ImageConverter
            else if (header == "GMLL")
            {
                Bitmap bm = GTMP.GMFile.ParseGMLLData(streamToUse.ToArray(), 0, null);
                return new ImageLoadResult(bm, ImageType.GMLL);
            }
            throw new InvalidDataException("File type not recognized");
        }

        static internal byte[] GetBytes(Image bm, System.Drawing.Imaging.ImageFormat format)
        {
            MemoryStream ms = new MemoryStream(MainForm.CANVAS_WIDTH * MainForm.CANVAS_HEIGHT * 4);
            bm.Save(ms, format);
            return ms.ToArray();
        }

        static internal Bitmap FromBytes(byte[] pixelData)
        {
            MemoryStream ms = new MemoryStream(pixelData);
            try
            {
                // this gymnastics is required otherwise we have to keep the 
                // MemoryStream the Bitmap is created from alive as long as the Bitmap is
                Bitmap retBm;
                using(Bitmap temp = new Bitmap(ms))
                {
                    retBm = new Bitmap(temp.Width, temp.Height, temp.PixelFormat);
                    using(Graphics g = Graphics.FromImage(retBm))
                    {
                        g.DrawImageUnscaled(temp, Point.Empty);
                    }
                }
                return retBm;
            }
            catch (Exception)
            { }

            ms.Position = 0;
            Bitmap bm = GTMP.GTMPFile.Parse(ms);
            if (bm == null)
            {
                ms.Position = 0;
                GTMP.GMFile.GMFileInfo gmf = GTMP.GMFile.Parse(ms);
                if (gmf != null)
                {
                    bm = gmf.Image;
                }
                else
                {
                    bm = GTMP.GMFile.ParseGMLLData(pixelData, 0, null);
                }
            }
            return bm;
        }

        static private int FindGMLLOffset(byte[] data)
        {
            byte[] toFind = { (byte)'G', (byte)'M', (byte)'L', (byte)'L' };
            int toFindLength = toFind.Length;
            int nextStart = 0;
            int where = -1;
            bool keepGoing = true;
            while (keepGoing)
            {
                where = Array.FindIndex(data, nextStart, (x) => { return x == toFind[0]; });
                if (where == -1) break;
                int i = 1;
                for (; i < toFindLength; ++i)
                {
                    if (data[where + i] != toFind[i])
                    {
                        break;
                    }
                }
                keepGoing = (i < toFindLength);
                nextStart = where + i;
            }
            return where;
        }

        static internal byte[] TrimToGMLLData(byte[] gmFile)
        {
            byte[] retData = null;
            try
            {
                int gmllOffset = FindGMLLOffset(gmFile);
                if (gmllOffset > 0)
                {
                    int dataLen = gmFile.Length - gmllOffset;
                    Buffer.BlockCopy(gmFile, gmllOffset, gmFile, 0, dataLen);
                    Array.Resize(ref gmFile, dataLen);
                    retData = gmFile;
                }
                else if (gmllOffset == 0)
                {
                    retData = gmFile;
                }
            }
            catch (Exception e)
            {
                DebugLogger.Log("Images", "Caught exception trying to trim input to GMLL data - {0}", e.Message);
            }
            return retData;
        }

        static internal byte[] LoadGMLLData(string gmllFileName)
        {
            byte[] gmFileData = null;
            try
            {
                using (FileStream gmStream = File.OpenRead(gmllFileName))
                {
                    MemoryStream decompGMStream = Compress.DecompressStream(gmStream);
                    gmFileData = TrimToGMLLData(decompGMStream.ToArray());
                }
            }
            catch (Exception e)
            {
                DebugLogger.Log("Images", "Caught exception trying to load GMLL data - {0}", e.Message);
            }
            return gmFileData;
        }
    }
}
