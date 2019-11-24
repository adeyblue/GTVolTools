using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;

// tex format
// header
// char[4] = Tex1
// int unk // 0
// int unk2 // 0
// int fileSize // 
// // gap
// short width; (0x60)
// short height; (0x62)
// int paletteOffset; // 4 bytes per colour (0x64)

// imgs format (might be gzipped)
// char[4] = IMGS
// int zero
// int numEntries
// 

namespace GT2Vol
{
    class GT3DemoVol
    {
        // Demo header format
        // RoFS string
        // short unk
        // short unk2 // maybe version? This is 1 here, but 2 in full game vol
        // int headerSize
        // directoryEntriesBegin - each value is 4 bytes
        //   nameOffset (value & 1 = this is a directory, otherwise a normal file, name is null terminated)
        //   numberOfEntries (including parent) 
        //   absoluteOffsets[numOfEntries]// backwards = parent
        // File entries - every file has 4 entries unlike full game vol
        //   nameOffset (value & 2 = compressed, otherwise normal file)
        //   sectorAddress (* 0x800 for absolute offset)
        //   uncompressed size
        //   compressed size

        struct Flags
        {
            internal const uint Directory = 1;
            internal const uint Compressed = 2;
            internal const uint AllFlags = Directory | Compressed;
            internal const uint TurnOffFlags = ~AllFlags;
        }

        class VolEntryInfo
        {
            public uint entryAddress; // the address of the data that is parsed to fill this struct
            public uint fileInfoAddress;
            public uint fileAddress; // where the data for this entry lives
            public uint size; // how big the data is
            public string name;
            public bool isCompressed;
            public List<VolEntryInfo> children; // if this is null, this is a file, otherwise a directory
        }

        const string listFormat = "{0}\t{1}\t{2:x}\t{3:x}\t{4:x}";
        FileStream volFile;
        VolEntryInfo rootVolEntry;

        public GT3DemoVol(string file)
        {
            volFile = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] intArray = new byte[4];
            volFile.Seek(8, SeekOrigin.Begin);
            volFile.Read(intArray, 0, 4);
            uint headerSize = BitConverter.ToUInt32(intArray, 0);
            volFile.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[headerSize];
            volFile.Read(header, 0, (int)headerSize);
            Debug.Assert((header[0] == 'R') && (header[1] == 'o') && (header[2] == 'F') && (header[3] == 'S'));

            MemoryStream ms = new MemoryStream(header, false);
            ms.Seek(0xc, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(ms, Encoding.UTF8);
            uint rootEntryData = br.ReadUInt32();
            if ((rootEntryData & Flags.Directory) != Flags.Directory)
            {
                Console.Error.WriteLine("{0} is an invalid VOL", file);
                volFile.Close();
                return;
            }
            uint nameOffset = (rootEntryData & Flags.TurnOffFlags);
            rootVolEntry = new VolEntryInfo();
            rootVolEntry.size = 0;
            rootVolEntry.fileAddress = 0xc;
            rootVolEntry.name = ReadNullTermName(br, nameOffset);
            rootVolEntry.children = ParseDirectory(br);
        }

        private string ReadNullTermName(BinaryReader br, uint offset)
        {
            long savedPos = br.BaseStream.Position;
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            StringBuilder sb = new StringBuilder();
            byte b = 0;
            while ((b = br.ReadByte()) != 0)
            {
                sb.Append((char)b);
            }
            br.BaseStream.Seek(savedPos, SeekOrigin.Begin);
            return sb.ToString();
        }

        private uint GetValueAtOffset(BinaryReader br, uint offset)
        {
            long savedPos = br.BaseStream.Position;
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            uint offsetValue = br.ReadUInt32();
            br.BaseStream.Seek(savedPos, SeekOrigin.Begin);
            return offsetValue;
        }

        private List<VolEntryInfo> ParseDirectory(BinaryReader br)
        {
            int numEntries = br.ReadInt32();
            List<VolEntryInfo> entryInfo = new List<VolEntryInfo>(numEntries);
            long currentFileOffset = br.BaseStream.Position;
            for (int i = 0; i < numEntries; ++i)
            {
                uint entryOffset = (uint)br.BaseStream.Position;
                uint entry = br.ReadUInt32();
                uint entryFlag = (entry & Flags.AllFlags);
                uint entryPointer = entry & ~Flags.TurnOffFlags;
                // directory entries are offsets to data about the entries
                // the flags for directories etc are in this data, not the entries themselves
                Debug.Assert(entryFlag == 0);
                // parent entry
                if (entryPointer < currentFileOffset)
                {
                    continue;
                }
                uint offsetValue = GetValueAtOffset(br, entryPointer);
                uint offsetFlag = offsetValue & Flags.AllFlags;
                uint offsetPosition = offsetValue & Flags.TurnOffFlags;
                string entryName = ReadNullTermName(br, offsetPosition);
                VolEntryInfo volEntry = new VolEntryInfo();
                volEntry.children = null;
                volEntry.name = entryName;
                volEntry.isCompressed = false;
                volEntry.entryAddress = entryOffset;
                volEntry.fileInfoAddress = entryPointer;
                switch (offsetFlag)
                {
                    case 1: // directory
                        {
                            volEntry.size = 0;
                            long preParsePos = br.BaseStream.Position;
                            br.BaseStream.Seek(entryPointer + 4, SeekOrigin.Begin);
                            volEntry.children = ParseDirectory(br);
                            br.BaseStream.Seek(preParsePos, SeekOrigin.Begin);
                        }
                        break;
                    case 2: // compressed file (gzip)
                    case 0: // uncompressed file
                        {
                            // files -
                            //  first entry = file data offset
                            //  second entry = decompressed size
                            //  third entry = compressed size (ie the size of the data in the vol)
                            volEntry.isCompressed = (offsetFlag == 2);
                            long preParsePos = br.BaseStream.Position;
                            br.BaseStream.Seek(entryPointer + 4, SeekOrigin.Begin);
                            uint dataOffset = br.ReadUInt32() * 0x800;
                            volEntry.fileAddress = dataOffset;
                            br.ReadUInt32(); // decompressed size, not needed
                            volEntry.size = br.ReadUInt32();
                            br.BaseStream.Seek(preParsePos, SeekOrigin.Begin);
                        }
                        break;
                    default:
                        {
                            Debug.Assert(false);
                        }
                        break;
                }
                entryInfo.Add(volEntry);
            }
            return entryInfo;
        }

        private void ExtractDirectory(List<VolEntryInfo> dirEntries, string dirPath, bool decomp)
        {
            string decompDir = Path.Combine(dirPath, VolFile.DecompDir);
            foreach (VolEntryInfo inf in dirEntries)
            {
                string localName = Path.Combine(dirPath, inf.name);
                if (inf.children != null)
                {
                    Directory.CreateDirectory(localName);
                    ExtractDirectory(inf.children, localName, decomp);
                }
                else
                {
                    volFile.Seek(inf.fileAddress, SeekOrigin.Begin);
                    byte[] data = new byte[inf.size];
                    volFile.Read(data, 0, (int)inf.size);
                    Console.WriteLine("Extracting {0}", inf.name);
                    File.WriteAllBytes(localName, data);
                    if (inf.isCompressed && decomp)
                    {
                        Directory.CreateDirectory(decompDir);
                        string decompFileName = Path.Combine(decompDir, inf.name);
                        MemoryStream ms = new MemoryStream(data, false);
                        GZipInputStream gzIn = new GZipInputStream(ms);
                        Console.WriteLine("Decompressing {0}", inf.name);
                        using (FileStream decompFile = new FileStream(decompFileName, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192];
                            StreamUtils.Copy(gzIn, decompFile, buffer);
                        }
                    }
                }
            }
        }

        public void Extract(string path, bool decomp)
        {
            ExtractDirectory(rootVolEntry.children, path, decomp);
        }

        // const string listFormat = "{0}\t{1}\t{2:x}\t{3:x}\t{4:x}";
        private void ListDirectory(List<VolEntryInfo> dirEntries, ref int numDirs, ref int numFiles, string pathSoFar)
        {
            foreach (VolEntryInfo inf in dirEntries)
            {
                Console.WriteLine(listFormat, Path.Combine(pathSoFar, inf.name), inf.size, inf.entryAddress, inf.fileInfoAddress, inf.fileAddress);
                if (inf.children != null)
                {
                    ++numDirs;
                    ListDirectory(inf.children, ref numDirs, ref numFiles, Path.Combine(pathSoFar, inf.name));
                }
                else
                {
                    ++numFiles;
                }
            }
        }

        public void List()
        {
            Console.WriteLine("Name\tSize\tDir Entry Position\tFile Info Position\tFile Data Position");
            Console.WriteLine(listFormat, rootVolEntry.name, 0, rootVolEntry.entryAddress, rootVolEntry.fileInfoAddress, 0);
            int numFiles = 0;
            int numDirs = 0;
            ListDirectory(rootVolEntry.children, ref numDirs, ref numFiles, String.Empty);
            Console.WriteLine("Totals: {0} directories, {1} files", numDirs, numFiles);
        }

        public static void Explode(string[] args)
        {
            if (!File.Exists(args[1]))
            {
                Console.Error.WriteLine("VOL file '{0}' doesn't exist!", args[1]);
                return;
            }
            GT3DemoVol volFile = new GT3DemoVol(args[1]);
            volFile.Extract(args[2], args.Length >= 4);
        }

        public static void List(string[] args)
        {
            if (!File.Exists(args[1]))
            {
                Console.Error.WriteLine("File '{0}' doesn't exist!", args[1]);
            }
            GT3DemoVol vol = new GT3DemoVol(args[1]);
            vol.List();
        }
    }
}