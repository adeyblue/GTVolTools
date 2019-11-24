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
    class GT3Vol
    {
        // header format
        // int RoFS; // the string "RoFS" but bitwise not-ted
        // int unk2; 
        // int headerSize;
        // int scrambledNameOffset; - names are scrambled using bitwise not ("bgm" (62 67 6d) = (9d 98 92))
        // int numEntries; // maybe
        // entries begin
        // high byte 2 = gzip compressed file (lower 3 bytes, name offset)
        //  first entry = file data sector (* 0x800 for byte position)
        //  second entry = decompressed size
        //  third entry = compressed size (ie the size of the data in the vol)
        // high byte 1 = directory (lower 3 bytes = absolute name offset)
        //  first directory entry = number of entries
        //  second directry entry = parent dir offset
        //  1...n entries = absolute offset of entries 
        // high byte 0 = file
        //  first entry = name offset
        //  second entry = file data sector (* 0x800 for byte position)
        //  third entry = file size
        //
        // The data in the VOL is divided into two layers.
        // First are the directories
        // Each directory is made up of n 4-byte values

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
        uint compressedFlag;
        uint directoryFlag;
        uint allFlags;
        uint turnOffFlags;
        bool isDemoVol;

        public GT3Vol(string file)
        {
            volFile = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] magic = new byte[4];
            volFile.Read(magic, 0, magic.Length);
            // if this is a demo vol
            if ((magic[0] == 'R') && (magic[1] == 'o') && (magic[2] == 'F') && (magic[3] == 'S'))
            {
                isDemoVol = true;
                compressedFlag = 0x2;
                directoryFlag = 0x1;
            }
            // this is 'RoFS' but bitwise not-ted
            else if ((magic[0] == 0xAD) && (magic[1] == 0x90) && (magic[2] == 0xB9) && (magic[3] == 0xAC))
            {
                isDemoVol = false;
                compressedFlag = 0x02000000;
                directoryFlag = 0x01000000;
            }
            else
            {
                volFile.Close();
                throw new FileLoadException("This isn't a GT3 VOL");
            }
            allFlags = compressedFlag | directoryFlag;
            turnOffFlags = ~allFlags;

            byte[] intArray = new byte[4];
            volFile.Seek(8, SeekOrigin.Begin);
            volFile.Read(intArray, 0, 4);
            uint headerSize = BitConverter.ToUInt32(intArray, 0);
            volFile.Seek(0, SeekOrigin.Begin);
            byte[] header = new byte[headerSize];
            volFile.Read(header, 0, (int)headerSize);
            MemoryStream ms = new MemoryStream(header, false);
            BinaryReader br = new BinaryReader(ms);
            br.BaseStream.Seek(0xc, SeekOrigin.Begin);
            if (!isDemoVol)
            {
                //volFile.Read(intArray, 0, 4);
                //uint namesOffset = BitConverter.ToUInt32(intArray, 0) + 1;
                uint namesOffset = br.ReadUInt32();

                uint namesSize = headerSize - namesOffset;
                for (uint i = namesOffset; i < headerSize; ++i)
                {
                    header[i] = (byte)~header[i];
                }
                br.BaseStream.Seek(0x14, SeekOrigin.Begin);
            }
            
            uint rootEntryData = br.ReadUInt32();
            if ((rootEntryData & directoryFlag) != directoryFlag)
            {
                Console.Error.WriteLine("{0} is an invalid VOL", file);
                volFile.Close();
                throw new FileLoadException("Don't recognize this type of GT3.VOL");
            }
            uint nameOffset = rootEntryData & turnOffFlags;
            rootVolEntry = new VolEntryInfo();
            rootVolEntry.size = 0;
            rootVolEntry.fileAddress = isDemoVol ? 0xcu : 0x14u;
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
            for(int i = 0; i < numEntries; ++i)
            {
                uint entryOffset = (uint)br.BaseStream.Position;
                uint entry = br.ReadUInt32();
                uint entryFlag = entry & allFlags;
                uint entryPointer = entry & turnOffFlags;
                // directory entries are offsets to data about the entries
                // the flags for directories etc are in this data, not the entries themselves
                Debug.Assert(entryFlag == 0);
                // parent entry
                if (entryPointer < currentFileOffset)
                {
                    continue;
                }
                uint offsetValue = GetValueAtOffset(br, entryPointer);
                uint offsetFlag = offsetValue & allFlags;
                uint offsetPosition = offsetValue & turnOffFlags;
                string entryName = ReadNullTermName(br, offsetPosition);
                VolEntryInfo volEntry = new VolEntryInfo();
                volEntry.children = null;
                volEntry.name = entryName;
                volEntry.isCompressed = false;
                volEntry.entryAddress = entryOffset;
                volEntry.fileInfoAddress = entryPointer;
                if(offsetFlag == directoryFlag)
                {
                    volEntry.size = 0;
                    long preParsePos = br.BaseStream.Position;
                    br.BaseStream.Seek(entryPointer + 4, SeekOrigin.Begin);
                    volEntry.children = ParseDirectory(br);
                    br.BaseStream.Seek(preParsePos, SeekOrigin.Begin);
                }
                else if(offsetFlag == compressedFlag) // compressed file (gzip)
                {
                    // high byte 2 = gzip compressed file (lower 3 bytes, name offset)
                    //  first entry = file data offset
                    //  second entry = decompressed size
                    //  third entry = compressed size (ie the size of the data in the vol)
                    volEntry.isCompressed = true;
                    long preParsePos = br.BaseStream.Position;
                    br.BaseStream.Seek(entryPointer + 4, SeekOrigin.Begin);
                    uint dataOffset = br.ReadUInt32() * 0x800;
                    volEntry.fileAddress = dataOffset;
                    br.ReadUInt32(); // decompressed size, not needed
                    volEntry.size = br.ReadUInt32();
                    br.BaseStream.Seek(preParsePos, SeekOrigin.Begin);
                }
                else // entry 
                {
                    Debug.Assert(offsetFlag == 0);
                    long preParsePos = br.BaseStream.Position;
                    br.BaseStream.Seek(entryPointer + 4, SeekOrigin.Begin);
                    uint dataOffset = br.ReadUInt32() * 0x800;
                    volEntry.fileAddress = dataOffset;
                    volEntry.size = br.ReadUInt32();
                    if (isDemoVol)
                    {
                        // the demo has 4 entries for both compressed and normal files
                        br.ReadUInt32();
                    }
                    br.BaseStream.Seek(preParsePos, SeekOrigin.Begin);
                }
                entryInfo.Add(volEntry);
            }
            return entryInfo;
        }

        private void ExtractDirectory(List<VolEntryInfo> dirEntries, string dirPath, bool decomp, VolFile.ExplodeProgressCallback callback)
        {
            string decompDir = Path.Combine(dirPath, VolFile.DecompDir);
            foreach (VolEntryInfo inf in dirEntries)
            {
                string localName = Path.Combine(dirPath, inf.name);
                if (inf.children != null)
                {
                    Directory.CreateDirectory(localName);
                    ExtractDirectory(inf.children, localName, decomp, callback);
                }
                else
                {
                    volFile.Seek(inf.fileAddress, SeekOrigin.Begin);
                    byte[] data = new byte[inf.size];
                    volFile.Read(data, 0, (int)inf.size);
                    callback(String.Format("Extracting {0} from {1:x}", inf.name, inf.fileAddress));
                    File.WriteAllBytes(localName, data);
                    if (inf.isCompressed && decomp)
                    {
                        Directory.CreateDirectory(decompDir);
                        string decompFileName = Path.Combine(decompDir, inf.name);
                        MemoryStream ms = new MemoryStream(data, false);
                        GZipInputStream gzIn = new GZipInputStream(ms);
                        callback(String.Format("Decompressing {0}", inf.name));
                        using (FileStream decompFile = new FileStream(decompFileName, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192];
                            StreamUtils.Copy(gzIn, decompFile, buffer);
                        }
                    }
                }
            }
        }

        public void Extract(string path, bool decomp, VolFile.ExplodeProgressCallback callback)
        {
            ExtractDirectory(rootVolEntry.children, path, decomp, callback);
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

        public static void Explode(string[] args, VolFile.ExplodeProgressCallback callback)
        {
            if (!File.Exists(args[1]))
            {
                Console.Error.WriteLine("VOL file '{0}' doesn't exist!", args[1]);
                return;
            }
            GT3Vol volFile = new GT3Vol(args[1]);
            volFile.Extract(args[2], args.Length >= 4, callback);
        }

        private class VolHeaderEntry
        {
            public uint entryOffset; // offset of the directory entry for this
            public uint nameOffset; // offset into stringbuilder string
            public uint fileInfoOffset; // Offset of the details (size, name, uncompressed size) of the file
            public uint dataOffset; // from start of data (first file is 0), this need the header size + padding added for true VOL offset
            public uint fileSize; // file size on disk / in the vol
            public uint decompFileSize; // file size after decompression
            public string filePath; // the path to the file on disk
            public VolHeaderEntry parent;
            public List<VolHeaderEntry> children;
        }

        private class VolHeader
        {
            public uint headerSize; // the size of the entire header: directories, file info & names
            public uint fileInfoOffset; // offset of the file info entries, IE the size of the directory entries
            public uint nameOffset; // offset of the bitwise-notted names, i.e. the size of the directory entries + file info entries
            public VolHeaderEntry volEntries;
            public byte[] nameString; // already bitwise notted

            public VolHeader()
            {
                volEntries = new VolHeaderEntry();
            }
        }

        private static void AppendVolHeaderFileName(BinaryWriter sb, string name, out uint offset)
        {
            sb.Flush();
            offset = (uint)sb.BaseStream.Position;
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
#if DEBUG
            foreach (byte b in nameBytes)
            {
                sb.Write(b);
            }
            sb.Write(0); // ~0
#else
            foreach (byte b in nameBytes)
            {
                sb.Write((byte)~b);
            }
            sb.Write((byte)0xFF); // ~0
#endif
        }

        private static List<VolHeaderEntry> DirectoryToHeaderEntries(
            string fsDirectory, 
            BinaryWriter sb, 
            ref uint nextDirStartOffset,
            ref uint fileDataRunningOffset,
            ref uint fileInfoSize
        )
        {
            string[] entries = Directory.GetFileSystemEntries(fsDirectory);
            for (int i = 0; i < entries.Length; ++i)
            {
                if (entries[i].EndsWith(GT2Vol.VolFile.DecompDir, StringComparison.InvariantCultureIgnoreCase))
                {
                    string thisEntry = entries[i];
                    string last = entries[entries.Length - 1];
                    entries[i] = last;
                    Array.Resize(ref entries, entries.Length - 1);
                    --i;
                    break;
                }
            }
            Array.Sort(entries);
            byte[] decompBuffer = new byte[65536];
            // the 3 is for:
            // the name entry for this directory
            // the count that comes after the name offset
            // the offset to the parent directory
            //
            // running dir offset is for entries in this directory
            // nextDirStartOffset is where the next child directory will start
            uint runningDirOffset = nextDirStartOffset + (3 * sizeof(uint));
            nextDirStartOffset += (uint)((entries.Length + 3) * sizeof(uint));
            List<VolHeaderEntry> dirEntries = new List<VolHeaderEntry>();
            char[] slashChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            foreach (string fileOrDir in entries)
            {
                int lastSlash = fileOrDir.LastIndexOfAny(slashChars);
                string lastPathPart = fileOrDir.Remove(0, lastSlash + 1);
                string fullFsPath = fileOrDir;
                VolHeaderEntry vhe = new VolHeaderEntry();
                vhe.entryOffset = runningDirOffset;
                vhe.filePath = fullFsPath;
                AppendVolHeaderFileName(sb, lastPathPart, out vhe.nameOffset);
                if (Directory.Exists(fullFsPath))
                {
                    vhe.dataOffset = vhe.fileInfoOffset = nextDirStartOffset;
                    vhe.children = DirectoryToHeaderEntries(
                        fullFsPath,
                        sb,
                        ref nextDirStartOffset,
                        ref fileDataRunningOffset,
                        ref fileInfoSize
                    );
                    foreach(VolHeaderEntry child in vhe.children)
                    {
                        child.parent = vhe;
                    }
                }
                else
                {
                    vhe.fileInfoOffset = fileInfoSize;
                    fileInfoSize += (sizeof(uint) * 2); // name offset + fileSize
                    vhe.dataOffset = fileDataRunningOffset;
                    long fileSize = 0;
                    uint decompSize = 0;
                    using (FileStream fs = new FileStream(fullFsPath, FileMode.Open, FileAccess.Read))
                    {
                        fileSize = fs.Length;
                        // try to decompress if it looks like a gzip file
                        if ((fs.ReadByte() == 0x1f) && (fs.ReadByte() == 0x8B))
                        {
                            try
                            {
                                using (GZipInputStream gzIn = new GZipInputStream(fs))
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    StreamUtils.Copy(gzIn, ms, decompBuffer);
                                    decompSize = (uint)ms.Position;
                                    fileInfoSize += sizeof(uint); // decomp size
                                }
                            }
                            catch (Exception)
                            {
                                ; // probably isn't a gzip file afterall
                            }
                        }
                    }
                    vhe.fileSize = (uint)fileSize;
                    vhe.decompFileSize = decompSize;
                    // round up to next sector multiple 
                    fileSize = (fileSize + 0x7FF) & ~0x7FF;
                    fileDataRunningOffset += (uint)fileSize;

                    vhe.children = null;
                }
                runningDirOffset += sizeof(uint);
                dirEntries.Add(vhe);
            }
            return dirEntries;
        }

        private static VolHeader BuildHeader(string directory)
        {
            VolHeader header = new VolHeader();
            MemoryStream stringStream = new MemoryStream();
            BinaryWriter nameString = new BinaryWriter(stringStream);
            VolHeaderEntry root = new VolHeaderEntry();
            root.entryOffset = 0x14;
            root.parent = null;
            AppendVolHeaderFileName(nameString, ".", out root.nameOffset);
            uint runningFileDataOffset = 0;
            uint dirEntryIter = 0x14;
            uint fileInfoSize = 0;
            root.dataOffset = 0x14;
            root.entryOffset = 0x14;
            root.children = DirectoryToHeaderEntries(directory, nameString, ref dirEntryIter, ref runningFileDataOffset, ref fileInfoSize);
            nameString.Flush();
#if DEBUG
            Console.WriteLine(
                "Sizes: fileInfoOffset = {0:x}, fileInfoSize = {1:x}, runningFDO = {2:x}, nameString Length {3:x}",
                dirEntryIter,
                fileInfoSize,
                runningFileDataOffset,
                stringStream.Position
            );
#endif
            header.nameString = stringStream.ToArray();
            header.fileInfoOffset = dirEntryIter;
            header.volEntries = root;
            header.nameOffset = header.fileInfoOffset + fileInfoSize;
            header.headerSize = header.nameOffset + (uint)header.nameString.Length;
            return header;
        }

        private static void WriteDirectory(
            BinaryWriter directoryWriter,
            BinaryWriter fileInfoWriter,
            uint fileInfoOffset,
            uint nameStringOffset,
            uint fileDataOffset,
            VolHeaderEntry dirEntry
        )
        {
            directoryWriter.Write(0x01000000 | (dirEntry.nameOffset + nameStringOffset));
            directoryWriter.Write(dirEntry.children.Count + 1); // +1 for the parent
            if(dirEntry.parent != null)
            {
                directoryWriter.Write(dirEntry.parent.entryOffset);
            }
            else
            {
                directoryWriter.Write(0);
            }
            List<MemoryStream> childDirDirectoryEntries = new List<MemoryStream>();
            foreach(VolHeaderEntry child in dirEntry.children)
            {
                if(child.children != null)
                {
                    directoryWriter.Write(child.dataOffset);
                    MemoryStream childDirEntries = new MemoryStream();
                    BinaryWriter childDirWriter = new BinaryWriter(childDirEntries);
                    WriteDirectory(childDirWriter, fileInfoWriter, fileInfoOffset, nameStringOffset, fileDataOffset, child);
                    childDirDirectoryEntries.Add(childDirEntries);
                }
                else
                {
                    directoryWriter.Write(child.fileInfoOffset + fileInfoOffset);
                    uint highByte = 0;
                    bool isCompressed = (child.decompFileSize != 0);
                    if(isCompressed)
                    {
                        highByte = 0x02000000;
                    }
                    fileInfoWriter.Write(highByte | (child.nameOffset + nameStringOffset));
                    fileInfoWriter.Write((fileDataOffset + child.dataOffset) / 0x800);
                    if(isCompressed)
                    {
                        fileInfoWriter.Write(child.decompFileSize);
                    }
                    fileInfoWriter.Write(child.fileSize);
                }
            }
            byte[] buffer = new byte[8192];
            directoryWriter.Flush();
            foreach (MemoryStream ms in childDirDirectoryEntries)
            {
                ms.Position = 0;
                StreamUtils.Copy(ms, directoryWriter.BaseStream, buffer);
            }
        }

        private static void WriteHeader(BinaryWriter volWriter, VolHeader volHead)
        {
            MemoryStream fileInfo = new MemoryStream();
            BinaryWriter fileInfoWriter = new BinaryWriter(fileInfo);
            WriteDirectory(
                volWriter,
                fileInfoWriter, 
                volHead.fileInfoOffset, 
                volHead.nameOffset,
                (uint)((volHead.headerSize + 0x7FF) & ~0x7FF),
                volHead.volEntries
            );
            volWriter.Flush();
            fileInfoWriter.Flush();
            fileInfoWriter.BaseStream.Position = 0;
            byte[] buffer = new byte[8192];
            StreamUtils.Copy(fileInfoWriter.BaseStream, volWriter.BaseStream, buffer);
        }

        public static void List(string[] args)
        {
            if (!File.Exists(args[1]))
            {
                Console.Error.WriteLine("File '{0}' doesn't exist!", args[1]);
            }
            GT3Vol vol = new GT3Vol(args[1]);
            vol.List();
        }
    }
}