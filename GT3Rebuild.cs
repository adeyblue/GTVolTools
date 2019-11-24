using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;

// This doesn't work
// There seems to be more than one collection of file entries in the vol
// so they go
// Header
// DirectoryData1
// DirectoryData2
// FileDataForDir1
// FileDataForDir2
// DirectoryData3
// DirectoryData4
// FileDataForDir3
// FileDataForDir4
// etc
//
// Taking it apart is fine, since we just follow offsets
// Putting it back together, I've no idea how it knows when to split DirData3/4 from 1/2
// And putting them back together in the DirData1/2/3/4 then FileData1/2/3/4 order doesn't work

namespace GT2Vol
{
    class GT3Rebuild
    {
        private static uint g_compressedFlag;
        private static uint g_directoryFlag;
        private static bool g_demoVol;

        // how many files in the rebuilding are compressed
        private static int g_numCompressed;
        // these are so we can know when all the decompression jobs have finished
        private static int g_jobsStarted;
        private static int g_jobsFinished;

        [Serializable()]
        class HeaderEntry
        {
            public string fullFileName;
            public bool isCompressed;
            public uint fileSize;
            public uint decompSize;
            // absolute offset in the output vol where this file should be written
            public uint fileDataPosition;
            public List<HeaderEntry> children;
        };

        private class AsyncDecompressArgs
        {
            public HeaderEntry Entry {get; private set;}
            public FileStream File {get; private set;}
            public AsyncDecompressArgs(HeaderEntry he, FileStream file)
            {
                Entry = he;
                File = file;
            }

        }

        private static void GetDecompressedSize(object o)
        {
            AsyncDecompressArgs ada = (AsyncDecompressArgs)o;
            const int bufSize = 128 * 1024;
            byte[] decompBuffer = new byte[bufSize];
            using (ada.File)
            using (GZipInputStream gzIn = new GZipInputStream(ada.File))
            using (MemoryStream ms = new MemoryStream(bufSize * 2))
            {
                try
                {
                    StreamUtils.Copy(gzIn, ms, decompBuffer);
                    ada.Entry.isCompressed = true;
                    ada.Entry.decompSize = (uint)ms.Position;
                    Interlocked.Increment(ref g_numCompressed);
                }
                catch (Exception)
                {
                    ; // probably isn't a gzip file afterall
                }
                Interlocked.Increment(ref g_jobsFinished);
            }
        }

        private static HeaderEntry HeaderEntryForFile(string file)
        {
            HeaderEntry he = new HeaderEntry();
            he.fullFileName = file;
            byte[] buffer = new byte[2];
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 32768);
            FileStream fsToDispose = fs;
            {
                he.fileSize = (uint)fs.Length;
                if (he.fileSize > 2)
                {
                    // try to decompress if it looks like a gzip file
                    fs.Read(buffer, 0, 2);
                    if ((buffer[0] == 0x1f) && (buffer[1] == 0x8B))
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        AsyncDecompressArgs ada = new AsyncDecompressArgs(he, fs);
                        fsToDispose = null;
                        ++g_jobsStarted;
#if !DEBUG
                        try
                        {
                            ThreadPool.QueueUserWorkItem(new WaitCallback(GetDecompressedSize), ada);
                        }
                        catch(Exception) 
                        {
#endif
                            GetDecompressedSize(ada);
#if !DEBUG
                            Interlocked.Increment(ref g_jobsFinished);
                        }                        
#endif
                    }
                }
            }
            if (fsToDispose != null)
            {
                fsToDispose.Dispose();
            }
            fs = null;
            return he;
        }

        private static void AppendVolHeaderFileName(BinaryWriter sb, string name, out uint offset)
        {
            sb.Flush();
            offset = (uint)sb.BaseStream.Position;
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            
            if (g_demoVol)
            {
                sb.Write(nameBytes);
                // the demo pads names so they start at the next multiple of 4
                // if a name length is divisble by 4, 4 pad bytes are used
                int nameLen = nameBytes.Length;
                int padBytes = 4 - (nameLen % 4);
                while(padBytes-- > 0)
                {
                    sb.Write((byte)0);
                }
            }
            else
            {
                foreach (byte b in nameBytes)
                {
                    sb.Write((byte)~b);
                }
                sb.Write((byte)0xff);
            }
        }

        private static List<HeaderEntry> BuildEntryList(string directory, ref uint numDirs, ref uint numFiles, out uint namesLength)
        {
            string[] subDirs = Directory.GetFileSystemEntries(directory);
            List<HeaderEntry> thisDir = new List<HeaderEntry>(subDirs.Length);
            namesLength = 0;
            foreach (string thing in subDirs)
            {
                uint nameLen = (uint)Path.GetFileName(thing).Length;
                // demo names are padded to start at the next multiple of four
                if (g_demoVol)
                {
                    uint padBytes = 4 - (nameLen % 4);
                    nameLen += padBytes;
                }
                else
                {
                    ++nameLen;
                }
                if (File.Exists(thing))
                {
                    HeaderEntry he = HeaderEntryForFile(thing);
                    ++numFiles;
                    namesLength += nameLen;
                    thisDir.Add(he);
                }
                else if (!thing.EndsWith(GT2Vol.VolFile.DecompDir, StringComparison.InvariantCultureIgnoreCase))
                {
                    HeaderEntry he = new HeaderEntry();
                    he.fullFileName = thing;
                    ++numDirs;
                    namesLength += nameLen;
                    uint subDirLen = 0;
                    he.children = BuildEntryList(thing, ref numDirs, ref numFiles, out subDirLen);
                    namesLength += subDirLen;
                    thisDir.Add(he);
                }
            }
            return thisDir;
        }

        class DirInfo
        {
            public uint offset;
            public uint parentOffset;
            public HeaderEntry entry;
            public DirInfo(uint inOffset, uint inParentOffset, HeaderEntry inEntry)
            {
                offset = inOffset;
                parentOffset = inParentOffset;
                entry = inEntry;
            }
        }

        private static uint GetCurrentStreamPosition(BinaryWriter bw)
        {
            return (uint)bw.BaseStream.Position;
        }

        private static void SetCurrentStreamPosition(BinaryWriter bw, uint pos)
        {
            bw.BaseStream.Position = pos;
        }

        private static List<DirInfo> WriteNewDirectory(
            HeaderEntry directory,
            uint parentDirPos,
            ref uint nextDirInfoPos, // for the directory contents
            ref uint nextFileInfoPos, // for the file info
            ref uint nextFileDataPos, // for the file data
            uint startOfNamesOffset,
            BinaryWriter headerWriter,
            BinaryWriter names,
            List<DirInfo> directories
        )
        {
            uint thisDirPos = GetCurrentStreamPosition(headerWriter);
            // write the name
            uint dirNameOffset = 0;
            AppendVolHeaderFileName(names, Path.GetFileName(directory.fullFileName), out dirNameOffset);
            // fix it up and write it
            dirNameOffset += startOfNamesOffset;
            headerWriter.Write(dirNameOffset | g_directoryFlag);
            List<HeaderEntry> contents = directory.children;
            // then how many entries
            headerWriter.Write(contents.Count + 1);
            // then the parent pos
            headerWriter.Write(parentDirPos);
            // then the entries
            bool doLoop = (directories == null);
            if (directories == null)
            {
                directories = new List<DirInfo>();
            }
            for (int i = 0; i < contents.Count; ++i)
            {
                HeaderEntry he = contents[i];
                if(he.children != null && he.children.Count > 0)
                {
                    // the format of the vol is 
                    //                 root
                    //    DirA         DirB        DirC
                    // DirAA DirAB DirBA DirBB DirCA DirCB
                    // 
                    // All these go in file order so root->DirA->DirB->DirC->DirAA rather than root->DirA->DirAA->DirAB->DirB
                    // so don't recurse here, or that's what we'd get
                    //
                    headerWriter.Write(nextDirInfoPos);
                    directories.Add(new DirInfo(nextDirInfoPos, thisDirPos, he));
                    // update the position for the next dir
                    nextDirInfoPos += (uint)((3 + he.children.Count) * 4);
                }
                else
                {
                    // next file pos
                    headerWriter.Write(nextFileInfoPos);
                    uint curPos = GetCurrentStreamPosition(headerWriter);
                    SetCurrentStreamPosition(headerWriter, nextFileInfoPos);
                    uint nameOffset = 0;
                    // write the name and fix up its position from name-stream relative
                    // to vol header relative
                    AppendVolHeaderFileName(names, Path.GetFileName(he.fullFileName), out nameOffset);
                    nameOffset += startOfNamesOffset;
                    // save these so we know where to dump the file data in the vol
                    // without having to recalculate stuff
                    he.fileDataPosition = nextFileDataPos;
                    if(he.isCompressed)
                    {
                        headerWriter.Write(nameOffset | g_compressedFlag);
                        headerWriter.Write(nextFileDataPos / 0x800);
                        // this seems to be ignored (if it's incorrect and more than actually required) nothing happens
                        // smaller than required not tested
                        headerWriter.Write(he.decompSize);
                        headerWriter.Write(he.fileSize);
                        nextFileInfoPos += (4 * 4);
                    }
                    else
                    {
                        headerWriter.Write(nameOffset);
                        headerWriter.Write(nextFileDataPos / 0x800);
                        headerWriter.Write(he.fileSize);
                        nextFileInfoPos += (3 * 4);
                        // demo always has four entries per file info, compressed or not
                        if (g_demoVol)
                        {
                            headerWriter.Write(he.fileSize);
                            nextFileInfoPos += 4;
                        }
                    }
                    // reset file pointer for next entry
                    SetCurrentStreamPosition(headerWriter, curPos);
                    // round the file data position to the start of the next sector
                    nextFileDataPos = ((nextFileDataPos + he.fileSize + 0x7ffu) & ~0x7ffu);
                }
            }
            if (!doLoop)
            {
                return directories;
            }
            while(directories.Count > 0)
            {
                DirInfo dir = directories[0];
                SetCurrentStreamPosition(headerWriter, dir.offset);
                List<DirInfo> childDirs = new List<DirInfo>();
                WriteNewDirectory(
                     dir.entry,
                     dir.parentOffset,
                     ref nextDirInfoPos,
                     ref nextFileInfoPos,
                     ref nextFileDataPos,
                     startOfNamesOffset,
                     headerWriter,
                     names,
                     childDirs
                 );
                directories.RemoveAt(0);
                directories.AddRange(childDirs);
            }
            return null;
        }

        private static byte[] BuildHeaderFromList(byte[] headerString, List<HeaderEntry> files, uint numFolders, uint numFiles, uint namesLength)
        {
            uint startPos = g_demoVol ? 0xcu : 0x14u;
            // first phase of the vol, offsets to actual info
            // one for each file, plus three for a directory
            // (its name offset, number of entries, the parent location)
            //uint fileInfoOffset = startPos + ((numFiles + (numFolders * 3)) * 4);
            // first phase of the vol, offsets to actual info
            // one for each file system entry (either dir or file), plus three for a directory
            // (its name offset, number of entries, the parent location)
            uint fileInfoOffset = startPos + (((numFiles + numFolders) + (numFolders * 3)) * 4);
            //
            // The second set of data is three/four (for the demo) entries per normal file
            // and four per compressed file
            //
            uint numCompressedUint = (uint)g_numCompressed;
            uint nonCompressedFileEntries = g_demoVol ? 4u : 3u;
            uint nameOffset = fileInfoOffset + (((numCompressedUint * 4) + ((numFiles - numCompressedUint) * nonCompressedFileEntries)) * 4);
            // The names are aligned to a 0x10 boundary, so do that
            nameOffset = (nameOffset + 0xfu) & ~0xfu;
            // the file data is aligned to sectors, so do that
            uint fileDataOffset = nameOffset + namesLength;
            fileDataOffset = (fileDataOffset + 0x7ffu) & ~0x7ffu;
            
            MemoryStream headerStream = new MemoryStream((int)fileDataOffset);
            BinaryWriter headerWriter = new BinaryWriter(headerStream);

            // rofs string
            headerWriter.Write(headerString);
            // always 2
            headerWriter.Write((ushort)2);
            // version? field, 1 for demo, 2 for release
            ushort ver = 1;
            // not demo
            if (!g_demoVol)
            {
                ver = 2;
            }
            headerWriter.Write(ver);
            // whole header size
            headerWriter.Write(nameOffset + namesLength + 1);
            if (!g_demoVol)
            {
                // names offset, this is weird as in the demo, the value is for the byte /after/ the names start
                // whereas in the full game, it's for the byte /before/
                headerWriter.Write(g_demoVol ? nameOffset + 1 : nameOffset - 1);
                // full game has an extra field
                // don't know what it is though
                headerWriter.Write((uint)0x3532u);
            }

            MemoryStream nameStream = new MemoryStream((int)(fileDataOffset - nameOffset));
            BinaryWriter names = new BinaryWriter(nameStream);
            startPos += (uint)((3 + files[0].children.Count) * 4);
            WriteNewDirectory(files[0], 0, ref startPos, ref fileInfoOffset, ref fileDataOffset, nameOffset, headerWriter, names, null);
            headerWriter.Flush();
            names.Flush();

            byte[] buffer = new byte[4096];

            headerStream.Seek(nameOffset, SeekOrigin.Begin);
            nameStream.Seek(0, SeekOrigin.Begin);
            StreamUtils.Copy(nameStream, headerStream, buffer);

            return headerStream.ToArray();
        }

#if DEBUG
        // scaffolding to save the directory listing to disk. 
        // With all the decompression required it takes for-ever to build the list
        [Serializable()]
        class EntryListInfo
        {
            public uint numCompressed;
            public uint numFiles;
            public uint numFolders;
            public uint namesLength;
            public List<HeaderEntry> root;
        };

        private static EntryListInfo DeserializeEntryList(string filename)
        {
            EntryListInfo eli = null;
            try
            {
                using(FileStream fs = new FileStream(filename + ".dat", FileMode.Open, FileAccess.Read, FileShare.Read, 32768))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    eli = (EntryListInfo)bf.Deserialize(fs);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(
                    String.Format("ERROR: Couldn't deserialize {0}.dat because of exception:\n{1}", filename, e.Message)
                );
            }
            return eli;
        }

        private static void SerializeEntryList(string filename, EntryListInfo eli)
        {            
            try
            {
                using (FileStream fs = new FileStream(filename + ".dat", FileMode.Create, FileAccess.Write, FileShare.None, 32768))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, eli);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(
                    String.Format("ERROR: Couldn't serialize {0}.dat because of exception:\n{1}", filename, e.Message)
                );
            }
        }
#endif

        public static void Rebuild(string[] args)
        {
            if (!Directory.Exists(args[1]))
            {
                Console.Error.WriteLine("Directory '{0}' doesn't exist!", args[1]);
            }
            args[1] = Path.GetFullPath(args[1]);
            // demo vol
            byte[] rofsHeader = new byte[4]{(byte)'R', (byte)'o', (byte)'F', (byte)'S'};
            byte[] headerString = null;
            if (args.Length > 3)
            {
                g_compressedFlag = 0x2;
                g_directoryFlag = 0x1;
                headerString = rofsHeader;
                g_demoVol = true;
            }
            else
            {
                g_compressedFlag = 0x02000000;
                g_directoryFlag = 0x01000000;
                headerString = BitConverter.GetBytes(~BitConverter.ToUInt32(rofsHeader, 0));
                g_demoVol = false;
            }
            uint numFolders = 0, numFiles = 0, namesLength = 0;
            List<HeaderEntry> rootDir = null;
#if DEBUG
            // building the entry list takes for freaking ever due to all the decompressing
            // so when debugging we do it once, then save the results to a file
            // then on the next run, we don't have to wait for the same info to be recollected
            string serializeName = Path.GetFileName(args[1]);
            EntryListInfo eli = DeserializeEntryList(serializeName);
            //Console.WriteLine("Press enter to continue");
            //Console.ReadLine();
            if (eli == null)
            {
#endif
                //DateTime start = DateTime.Now;
                List<HeaderEntry> fileHeaders = BuildEntryList(args[1], ref numFolders, ref numFiles, out namesLength);
                // The dot<nul> for the root entry needs accounting for
                namesLength += g_demoVol ? 4u : 2u;
                rootDir = new List<HeaderEntry>(1);
                HeaderEntry rootEntry = new HeaderEntry();
                rootDir.Add(rootEntry);
                rootEntry.children = fileHeaders;
                rootEntry.fullFileName = ".";
                // wait for all the decompression jobs to finish before continuing
                while (Interlocked.CompareExchange(ref g_jobsFinished, g_jobsStarted, g_jobsStarted) != g_jobsStarted)
                {
                    Thread.Sleep(100);
                }
                //DateTime end = DateTime.Now;
                //TimeSpan dur = end - start;
                //Console.WriteLine("Building entry list took {0}ms/{1}s", dur.TotalMilliseconds, dur.TotalSeconds);
#if DEBUG
                // still in the if(eli == null) case
                eli = new EntryListInfo();
                eli.namesLength = namesLength;
                eli.numCompressed = (uint)g_numCompressed;
                eli.numFiles = numFiles;
                eli.numFolders = numFolders;
                eli.root = rootDir;
                SerializeEntryList(serializeName, eli);
            } // end of eli == null
            else
            {
                g_numCompressed = (int)eli.numCompressed;
                numFiles = eli.numFiles;
                numFolders = eli.numFolders;
                namesLength = eli.namesLength;
                rootDir = eli.root;
            }
#endif
            byte[] volHead = BuildHeaderFromList(headerString, rootDir, numFolders, numFiles, namesLength);
            using (FileStream fs = new FileStream(args[2], FileMode.Create, FileAccess.Write))
            {
                byte[] blank = new byte[8191];
                fs.Write(volHead, 0, volHead.Length);
                Stack<List<HeaderEntry>> entryStack = new Stack<List<HeaderEntry>>();
                List<HeaderEntry> curIterList = rootDir;
                while (true)
                {
                    if (curIterList.Count == 0)
                    {
                        if (entryStack.Count == 0)
                        {
                            break;
                        }
                        curIterList = entryStack.Pop();
                        continue;
                    }
                    int last = curIterList.Count - 1;
                    HeaderEntry entry = curIterList[last];
                    curIterList.RemoveAt(last);
                    if (entry.children == null)
                    {
                        fs.Seek(entry.fileDataPosition, SeekOrigin.Begin);
                        Console.WriteLine("Reading {0} to write to the vol at {1:x}", Path.GetFileName(entry.fullFileName), entry.fileDataPosition);
                        byte[] data = File.ReadAllBytes(entry.fullFileName);
                        fs.Write(data, 0, data.Length);
                        fs.Write(blank, 0, 0x800 - (data.Length % 0x800));
                    }
                    else
                    {
                        entryStack.Push(curIterList);
                        Console.WriteLine("Moving to directory {0}", Path.GetFileName(entry.fullFileName));
                        curIterList = entry.children;
                    }
                }
            }
        }
    }
}
