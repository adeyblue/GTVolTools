using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;

// GT2 VOL File layout, other GT games and Tourist Trophy are different
// Header layout
// GTFS\0\0\0\0 - first 8 bytes (0x0)
// 0x???? - 2 bytes, number of files (0x8)
// 0x???? - 2 bytes, total number of entries including those for directories (0xa)
// 0x00000000 - 4 bytes of 0 (0xc)
// 0x???????? - n 4 byte offsets (n = number of files from 0x8 above) (0x10)
//    Each offset comprises two parts:
//    offset & 0xFFFFF800 = the file address of the start of the file, each file is aligned to 2K sectors
//    offset & 0x7FF = the number of pad bytes in the last 0x800 sized sector of the file (essentially 0x800 - (fileSize % 0x800))
//       The file size is computed as ((nextOffset & 0xFFFFF800) - (thisOffset & 0xFFFF800) - (thisOffset & 0x7FF))
//    offsets[0] = offset of the start of the file (essentially just the count of pad bytes)
//    offsets[1] = toc
//    offsets[2...n - 1] = files
//    offsets[n - 1] = vol file size
//
// TOC entries are stored starting from the root directory in alphabetical order
// Files and directories are mixed together (alphabetical is the only ordering)
// Contents of child directories come after the contents of the current directory.
// at the end of each directory (except the last) there's an entry for '..'
// '..' entries have an offset of 0x0000 and the directory flag set
// e.g. abridged root dir
// .carcolor - file
// .text - dir
// .usedcar - file
// arcade - dir (end of root entries)
// ..
// .data.txd - file (in .text dir, only file)
// ..
// arc_carlogo - file (in arcade dir)
// 
// TOC format = 1 for each entry including directories
// struct TOCEntry //0x20 in size
// { 
//   int date; // file time stamp
//   // offsetIndex - for files: the index into the offset list which points to where the data for this file is
//   // For directories: the index into the toc list where the files for this directory start.
//   // This applies to .. directory entries too. The majority of these will be 0, since the next dir
//   // up is the root dir. For th
//   short offsetIndex;
//   byte flags; // various stuff. 0 = normal file, 1 = directory entry, 0x80 = last file for this dir
//   byte name[25]; // name, padded with null for shorter names
// };
//
// File entries are just the file data, padded with 0 till they're a multiple of 0x800

namespace GT2Vol
{
    class Rebuilder
    {
        const byte TOCFlags_File = 0;
        const byte TOCFlags_Directory = 1;
        const byte TOCFlags_LastEntry = 0x80;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TocEntry
        {
            public int dateTime;
            public short offsetIndex;
            public byte flags; // TOCFlags_ values / bitfield
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=25, ArraySubType=UnmanagedType.I1)]
            public byte[] name;
        }

        public class FSEntry : IComparable<FSEntry>
        {
            public string name;
            public string diskFile; // if we're copying it as is from the disk, this is where it lives
            public uint size; // this is the size of the above file
            public byte[] compressedFile; // if we recompressed it, this is the compressed file otherwise null
            public TocEntry tocEntry;

            public int CompareTo(FSEntry other)
            {
                return String.CompareOrdinal(name, other.name);
            }
        }

        public class FixupPair
        {
            private FSEntry pairKey;
            private FSEntry pairValue;

            public FSEntry Key
            {
                get { return pairKey; }
            }

            public FSEntry Value
            {
                get { return pairValue; }
                set { pairValue = value; } 
            }

            public FixupPair(FSEntry inKey, FSEntry inValue)
            {
                pairKey = inKey;
                pairValue = inValue;
            }
        }

        private static readonly DateTime dtEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private int GetFileCreationTimeInSeconds(string file)
        {
            return (int)(File.GetCreationTime(file) - dtEpoch).TotalSeconds;
        }

        private int GetDirectoryCreationTimeInSeconds(string file)
        {
            return (int)(File.GetCreationTime(file) - dtEpoch).TotalSeconds;
        }

        private List<FSEntry> RecompressDir(string dir)
        {
            string[] files = Directory.GetFiles(dir);
            List<FSEntry> entries = new List<FSEntry>(files.Length);
            byte[] compBuffer = new byte[8192];
            
            foreach (string f in files)
            {
                FSEntry fe = new FSEntry();
                fe.tocEntry = new TocEntry();
                fe.tocEntry.dateTime = GetFileCreationTimeInSeconds(f);
                fe.tocEntry.flags = TOCFlags_File;
                fe.tocEntry.offsetIndex = 0;
                fe.tocEntry.name = new byte[25];
                fe.diskFile = f;
                using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read))
                {
                    MemoryStream ms = new MemoryStream((int)fs.Length);
                    GZipOutputStream gzOut = new GZipOutputStream(ms);
                    gzOut.SetLevel(2);
                    StreamUtils.Copy(fs, gzOut, compBuffer);
                    byte[] compData = ms.GetBuffer();
                    Array.Resize(ref compData, (int)ms.Position);
                    fe.compressedFile = compData;
                }
                string gzName = Path.GetFileName(f) + ".gz";
                fe.name = gzName;
                entries.Add(fe);
            }
            return entries;
        }

        public List<FSEntry> ScanAndCompressDir(string dir, List<FixupPair> dirOffsetsToFixup, bool recomp)
        {
            string[] entries = Directory.GetFileSystemEntries(dir);
            string lowerDecompDir = VolFile.DecompDir.ToLower();
            List<FSEntry> curDirEntries = new List<FSEntry>();
            List<FSEntry> subDirEntries = new List<FSEntry>();
            FSEntry startOfDirEntry = new FSEntry();
            startOfDirEntry.tocEntry = new TocEntry();
            startOfDirEntry.tocEntry.dateTime = (int)(DateTime.UtcNow - dtEpoch).TotalSeconds;
            startOfDirEntry.tocEntry.flags = TOCFlags_Directory;
            startOfDirEntry.tocEntry.offsetIndex = 0;
            startOfDirEntry.compressedFile = null;
            startOfDirEntry.name = "..";
            startOfDirEntry.size = 0;
            curDirEntries.Add(startOfDirEntry);
            foreach (string e in entries)
            {
                string justFileName = Path.GetFileName(e);
                string lowerFileName = justFileName.ToLower();
                FSEntry fe = new FSEntry();
                fe.name = justFileName;
                fe.tocEntry = new TocEntry();
                fe.tocEntry.flags = TOCFlags_File;
                fe.tocEntry.offsetIndex = 0;
                fe.tocEntry.name = new byte[25];
                if (!Directory.Exists(e))
                {
                    Debug.Assert(File.Exists(e));
                    fe.size = 0;
                    fe.diskFile = e;
                    fe.tocEntry.dateTime = GetFileCreationTimeInSeconds(e);
                }
                else if (lowerFileName != lowerDecompDir)
                {
                    fe.tocEntry.dateTime = GetDirectoryCreationTimeInSeconds(e);
                    fe.tocEntry.flags = TOCFlags_Directory;
                    fe.diskFile = null;
                    FixupPair dirFixup = new FixupPair(fe, null);
                    dirOffsetsToFixup.Add(dirFixup);
                    List<FSEntry> childContents = ScanAndCompressDir(e, dirOffsetsToFixup, recomp);
                    dirFixup.Value = childContents[0];
                    dirOffsetsToFixup.Add(new FixupPair(childContents[0], curDirEntries[0]));
                    subDirEntries.AddRange(childContents);
                }
                else continue; // we don't want an entry for the decomp dir
                curDirEntries.Add(fe);
            }
            curDirEntries.Sort();
            string decompDir = Path.Combine(dir, VolFile.DecompDir);
            if (recomp && Directory.Exists(decompDir))
            {
                bool added = false;
                List<FSEntry> recompressedEntries = RecompressDir(decompDir);
                foreach (FSEntry r in recompressedEntries)
                {
                    int where = curDirEntries.BinarySearch(r);
                    if (where > 0)
                    {
                        curDirEntries[where] = r;
                    }
                    else
                    {
                        added = true;
                        curDirEntries.Add(r);
                    }
                }
                if (added)
                {
                    curDirEntries.Sort();
                }
            }
            // last file in the dir has this set
            // there aren't any empty directories in the real vol, so I don't know what will
            // happen if you include one. Setting the LastEntry flag on the .. entry might
            // make it work, or it might break it, I've no idea. But I've attempted to support
            // it anyway
            curDirEntries[curDirEntries.Count - 1].tocEntry.flags |= TOCFlags_LastEntry;
            curDirEntries.AddRange(subDirEntries);
            return curDirEntries;
        }

        public List<FSEntry> ScanAndCompress(string flatDir, bool recomp)
        {
            List<FixupPair> dirOffsetsToFixup = new List<FixupPair>();
            List<FSEntry> entries = ScanAndCompressDir(flatDir, dirOffsetsToFixup, recomp);
            if (entries.Count != 0)
            {
                // there is no .. entry at the start of the list
                entries.RemoveAt(0);
            }
            int runningIndex = 0;
            foreach (FixupPair pair in dirOffsetsToFixup)
            {
                bool isDotDotKey = pair.Key.name == "..";
                int startIndex = isDotDotKey ? 0 : runningIndex;
                int i = startIndex;
                for (; i < entries.Count; ++i)
                {
                    FSEntry listEntry = entries[i];
                    if (Object.ReferenceEquals(listEntry, pair.Value))
                    {
                        pair.Key.tocEntry.offsetIndex = (short)i;
                        break;
                    }
                }
                if (!isDotDotKey) runningIndex = i + 1;
            }
            return entries;
        }

        public class HeaderInfo
        {
            // GTFS\0\0\0\0<numEntries><numFiles>
            public byte[] header;
            // first two already have their final values, the rest are blank
            public int[] offsets;
            // already has offset indexes filled in in order of file appearance in entries
            // The data inside is just the toc, no padding to a 0x800 multiple has been performed
            public MemoryStream toc;
            // absolute offset to where the first file can be stored
            public int fileDataStartPosition;
        }

        public HeaderInfo BuildHeader(List<FSEntry> entries)
        {
            short numEntries = (short)entries.Count;
            // header + offsets size is 0x10 + ((numFiles + 2) * sizeof(int))
            // TOC size is numFiles * sizeof(TOCEntry)
            MemoryStream tocStream = new MemoryStream(entries.Count * 0x20);
            BinaryWriter bw = new BinaryWriter(tocStream);
            short numOffsets = 2; // 0 = start of file, 1 = toc
            foreach (FSEntry f in entries)
            {
                TocEntry te = f.tocEntry;
                if ((te.flags & TOCFlags_Directory) == 0)
                {
                    te.offsetIndex = numOffsets++; // 2+ = files
                    //++numFiles;
                }
                bw.Write(te.dateTime);
                bw.Write(te.offsetIndex);
                bw.Write(te.flags);
                byte[] name = Encoding.ASCII.GetBytes(f.name);
                Array.Resize(ref name, 25);
                bw.Write(name, 0, 25);
            }
            bw.Flush();
            ++numOffsets; // last one = file size

            HeaderInfo hi = new HeaderInfo();

            MemoryStream header = new MemoryStream(16);
            bw = new BinaryWriter(header);
            bw.Write(Encoding.ASCII.GetBytes("GTFS\0\0\0\0"));
            bw.Write(numOffsets);
            bw.Write(numEntries);
            bw.Write((int)0);
            bw.Flush();
            byte[] headerBuffer = header.GetBuffer();
            Array.Resize(ref headerBuffer, 16);
            hi.header = headerBuffer;

            int[] offsets = new int[numOffsets];
            // first the size of the file header + offset list
            int offsetSectionSize = 0x10 + (offsets.Length * sizeof(int));
            int offsetToTocPadding = 0x800 - (offsetSectionSize % 0x800);

            // this always starts at file address 0, so the offset portion of the value is always 0
            // leaving just the padding value
            offsets[0] = offsetToTocPadding;
            Debug.Assert((offsets[0] & 0xFFFFF800) == 0);
            Debug.Assert((offsets[0] & 0x7FF) != 0);

            long tocSize = tocStream.Position;
            long tocPadding = 0x800 - (tocSize % 0x800);

            int tocStartPos = offsetSectionSize + offsetToTocPadding;

            offsets[1] = (tocStartPos | (int)tocPadding);
            
            hi.offsets = offsets;
            hi.toc = tocStream;
            hi.fileDataStartPosition = tocStartPos + (int)(tocSize + tocPadding);
            Debug.Assert((hi.fileDataStartPosition & 0x7FF) == 0);
            return hi;
        }

        public void WriteNewVol(string newVol, HeaderInfo hi, List<FSEntry> entries, VolFile.ExplodeProgressCallback callback)
        {
            // the Vol file is massive, about 500 MB
            // I don't want to have to write out the file data section to an actual file and have to read it back in
            // to copy it to the final file. So ~500MB of memory it is
            using (MemoryStream fileData = new MemoryStream(500000000))
            {
                int[] offsets = hi.offsets;
                int nextOffset = hi.fileDataStartPosition;
                byte[] padBytes = new byte[0x7ff];
                BinaryWriter bw = new BinaryWriter(fileData);
                int fileCount = 0;
                foreach (FSEntry entry in entries)
                {
                    int size = 0;
                    if ((entry.tocEntry.flags & TOCFlags_Directory) == 0)
                    {
                        // if we recompressed the file, just write its bytes out
                        if (entry.compressedFile != null)
                        {
                            size = entry.compressedFile.Length;
                            bw.Write(entry.compressedFile);
                        }
                        else
                        {
                            // otherwise read it in and write it to the memory
                            byte[] thisFileData = File.ReadAllBytes(entry.diskFile);
                            size = thisFileData.Length;
                            bw.Write(thisFileData);
                        }
                        // write the pad bytes
                        int remainder = 0x800 - (size % 0x800);
                        if (remainder == 0x800) remainder = 0;
                        bw.Write(padBytes, 0, remainder);
                        callback(String.Format("Wrote {0} at 0x{1:x}, padded with 0x{2:x} bytes", entry.name, nextOffset, remainder));
                        offsets[2 + fileCount] = nextOffset | remainder;
                        nextOffset += size + remainder;
                        Debug.Assert((nextOffset & 0x7FF) == 0);
                        ++fileCount;
                    }
                }
                bw.Flush();
                fileData.Position = 0;
                Debug.Assert((2 + fileCount) == (offsets.Length - 1));
                offsets[2 + fileCount] = nextOffset; // last one is the file size
                using (FileStream newVolFile = new FileStream(newVol, FileMode.Create, FileAccess.Write))
                {
                    byte[] offBytes = new byte[offsets.Length * sizeof(int)];
                    Buffer.BlockCopy(offsets, 0, offBytes, 0, offBytes.Length);
                    // header
                    newVolFile.Write(hi.header, 0, hi.header.Length);
                    // offsets
                    newVolFile.Write(offBytes, 0, offBytes.Length);
                    // padding for offsets
                    newVolFile.Write(padBytes, 0, offsets[0] & 0x7ff);
                    // toc
                    hi.toc.Position = 0;
                    StreamUtils.Copy(hi.toc, newVolFile, offBytes);
                    // toc padding
                    newVolFile.Write(padBytes, 0, offsets[1] & 0x7ff);
                    // file data
                    StreamUtils.Copy(fileData, newVolFile, offBytes);
                }
            }
        }

        static public void RebuildGT2(string[] args)
        {
            Rebuilder r = new Rebuilder();
            List<FSEntry> entries = r.ScanAndCompress(args[1], args.Length >= 4);
            HeaderInfo hi = r.BuildHeader(entries);
            r.WriteNewVol(args[2], hi, entries, new VolFile.ExplodeProgressCallback(Console.WriteLine));
        }
    }
}
