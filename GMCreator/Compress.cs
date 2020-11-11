using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;

namespace GMCreator
{
    static class Compress
    {
        public static MemoryStream DecompressStream(Stream compStream)
        {
            byte[] buffer = new byte[65536];
            MemoryStream ms = new MemoryStream((int)(compStream.Length * 2));
            StreamUtils.Copy(compStream, ms, buffer);
            ms.Position = 0;
            return ms;
        }

        public static MemoryStream ZipDecompress(Stream compStream)
        {
            ZipInputStream zis = new ZipInputStream(compStream);
            ZipEntry ze = zis.GetNextEntry();
            return DecompressStream(zis);
        }

        public static MemoryStream GZipDecompress(Stream compStream)
        {
            GZipInputStream gis = new GZipInputStream(compStream);
            return DecompressStream(gis);
        }

        public static MemoryStream ZipCompressStream(Stream decompStream)
        {
            MemoryStream ms = new MemoryStream((int)decompStream.Length);
            using (ZipOutputStream zos = new ZipOutputStream(ms))
            {
                zos.SetLevel(Globals.App.CompressionLevel);
                ZipEntry entry = new ZipEntry("1");
                entry.CompressionMethod = CompressionMethod.Deflated;
                zos.PutNextEntry(entry);
                byte[] buffer = new byte[65536];
                StreamUtils.Copy(decompStream, zos, buffer);
            }
            return ms;
        }

        public static MemoryStream GZipCompressStream(Stream decompStream)
        {
            MemoryStream ms = new MemoryStream((int)decompStream.Length);
            using (GZipOutputStream gzos = new GZipOutputStream(ms))
            {
                gzos.SetLevel(Globals.App.CompressionLevel);
                byte[] buffer = new byte[65536];
                StreamUtils.Copy(decompStream, gzos, buffer);
            }
            return ms;
        }

        public static MemoryStream GZipCompressString(string decompStr)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(decompStr));
            return GZipCompressStream(ms);
        }

        public static MemoryStream ZipCompressString(string decompStr)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(decompStr));
            return ZipCompressStream(ms);
        }
    }
}
