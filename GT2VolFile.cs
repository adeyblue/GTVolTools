using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;

namespace GT2Vol
{
    public class EmbeddedFileInfo
    {
        public int dateTime;
        public uint fileAddress;
        public uint size;
        public IntPtr pFileStart;
        public string name;
    }

    class VolFile : IDisposable
    {
        private List<EmbeddedFileInfo> embeddedFiles;
        private List<WaitHandle> outstandingRequests;
        private ManualResetEvent[] explodeEvents;
        private long volMappingAddrAsNum;
        private bool disposed;
        public static string DecompDir = "decomp";
        private enum ExplodeEvents
        {
            NotRunning = 0,
            Stop = 1
        }

        public delegate void TocFileNotify(EmbeddedFileInfo efi);

        public VolFile(string name)
        {
            disposed = false;
            TotalEntries = Files = 0;
            explodeEvents = new ManualResetEvent[2] { new ManualResetEvent(true), new ManualResetEvent(false) };
            embeddedFiles = new List<EmbeddedFileInfo>();
            outstandingRequests = new List<WaitHandle>();
            DecompDir = "decomp";
            using (FileStream theVol = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IntPtr volMapping = MemoryMappedFile.Map(theVol);
                volMappingAddrAsNum = volMapping.ToInt64();
            }
        }

#region IDisposable
        ~VolFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                StopExploding();
                if (disposing)
                {
                    if (volMappingAddrAsNum != 0)
                    {
                        MemoryMappedFile.UnMap(new IntPtr(volMappingAddrAsNum));
                        volMappingAddrAsNum = 0;
                    }
                }
                disposed = true;
            }
        }

#endregion

        public bool CheckAndCacheHeaderDetails()
        {
            byte[] buffer = new byte[12];
            Marshal.Copy(new IntPtr(volMappingAddrAsNum), buffer, 0, 12);
            bool check = (Encoding.ASCII.GetString(buffer).Substring(0, 8) == "GTFS\0\0\0\0");
            Files = BitConverter.ToInt16(buffer, 8);
            TotalEntries = BitConverter.ToInt16(buffer, 10);
            return check && Files > 0 && TotalEntries > 0 && TotalEntries > Files;
        }

        static uint SanitiseOffset(uint mangled, out uint bytesFromLastSector)
        {
            // items embedded within are aligned to 2K boundaries
            bytesFromLastSector = mangled & 0x7FF;
            return mangled & 0xFFFFF800;
        }

        static void ReadEmbeddedFileData(
            List<EmbeddedFileInfo> files,
            short numFiles,
            BinaryReader src,
            uint[] fileOffsets,
            TocFileNotify notifyFn,
            long volStartAddress
        )
        {
            List<string> directories = new List<string>();
            string dirBeingParsed = String.Empty;
            EmbeddedFileInfo lastFile = null;
            int dirIndex = 0, dirInsertIndex = 0;
            short i = 0;
            for(; i < numFiles; ++i)
            {
                EmbeddedFileInfo efi = new EmbeddedFileInfo();
                efi.dateTime = src.ReadInt32();
                int offsetIndex = src.ReadInt16();
                byte flags = src.ReadByte();

                uint bytesFromLastSector = 0;
                // flags
                // 0x00 = regular file
                // 0x01 = directory
                // 0x80 = last entry for this directory
                if (offsetIndex == 0 || (flags & 0x1) == 0x1)
                {
                    // '..' entries and directories don't have an offset
                    efi.fileAddress = 0;
                    efi.pFileStart = IntPtr.Zero;
                }
                else
                {
                    efi.fileAddress = SanitiseOffset(fileOffsets[offsetIndex], out bytesFromLastSector);
                    efi.pFileStart = new IntPtr(volStartAddress + efi.fileAddress);
                }

                // cache this here for now, it's not the actual size, but it's used in its calculation
                efi.size = bytesFromLastSector;
                efi.name = Path.Combine(dirBeingParsed, Encoding.ASCII.GetString(src.ReadBytes(25)).TrimEnd('\0'));

                if ((flags & 0x1) != 0)
                {
                    // '..' don't need saving
                    if (!efi.name.EndsWith(".."))
                    {
                        // if we're parsing one, this is a child directory
                        // its' contents are after the one being parsed, not at the end of the list
                        if (dirBeingParsed != String.Empty)
                        {
                            directories.Insert(dirInsertIndex++, efi.name);
                        }
                        else
                        {
                            directories.Add(efi.name);
                        }
                    }
                }
                else
                {
                    if (lastFile != null)
                    {
                        uint realSize = efi.fileAddress - lastFile.fileAddress - lastFile.size;
                        // realsize can be 0 if files are 0 bytes (everything in the replay dir)
                        lastFile.size = realSize;
                        if (notifyFn != null)
                        {
                            notifyFn(lastFile);
                        }
                    }
                    lastFile = efi;
                }
                // if this is the last entry for this directory
                // change which dir we use as the parent
                if ((flags & 0x80) != 0)
                {
                    // directories are listed in order of first encounter
                    // so a simple iteration will suffice
                    dirBeingParsed = 
                        (dirIndex < directories.Count) ? directories[dirIndex++] : String.Empty;
                    dirInsertIndex = dirIndex;
                }
                // '..' entries don't need to be saved
                if (offsetIndex != 0)
                {
                    files.Add(efi);
                }
            }
            // the last fileOffset is the length of the whole file
            lastFile.size = fileOffsets[fileOffsets.Length - 1] - lastFile.fileAddress - lastFile.size;
            lastFile.size = (lastFile.size > 0) ? lastFile.size : 0x800;
            if (notifyFn != null)
            {
                notifyFn(lastFile);
            }
        }

        public void ParseToc(TocFileNotify notifyFn)
        {
            uint[] offsets = new uint[Files];
            // this merry dance is because unsigned things are second-class
            // citizens when it comes to marshalling
            byte[] offsetBytes = new byte[offsets.Length * sizeof(uint)];
            // offsets start at 0x10
            IntPtr offStart = new IntPtr(volMappingAddrAsNum + 0x10);
            Marshal.Copy(offStart, offsetBytes, 0, offsetBytes.Length);
            Buffer.BlockCopy(offsetBytes, 0, offsets, 0, offsetBytes.Length);

            // offsets[0] = offset of the offsets
            // offsets[1] = toc
            // offsets[2...n] = files
            uint unusedLastSectorBytes = 0;
            uint tocOffset = SanitiseOffset(offsets[1], out unusedLastSectorBytes);

            uint notNeeded = 0;
            uint firstFileOffset = SanitiseOffset(offsets[2], out notNeeded);

            uint tocLen = firstFileOffset - tocOffset - unusedLastSectorBytes;
            byte[] toc = new byte[tocLen];
            IntPtr tocStart = new IntPtr(volMappingAddrAsNum + tocOffset);
            Marshal.Copy(tocStart, toc, 0, (int)tocLen); 

            embeddedFiles.Capacity = TotalEntries;
            BinaryReader reader = new BinaryReader(new MemoryStream(toc, false));
            ReadEmbeddedFileData(embeddedFiles, TotalEntries, reader, offsets, notifyFn, volMappingAddrAsNum);
        }

        //public void DumpToc(TocFileNotify tfn)
        //{
        //    uint[] offsets = new uint[Files];
        //    // this merry dance is because unsigned things are second-class
        //    // citizens when it comes to marshalling
        //    byte[] offsetBytes = new byte[offsets.Length * sizeof(uint)];
        //    // offsets start at 0x10
        //    IntPtr offStart = new IntPtr(volMappingAddrAsNum + 0x10);
        //    Marshal.Copy(offStart, offsetBytes, 0, offsetBytes.Length);
        //    Buffer.BlockCopy(offsetBytes, 0, offsets, 0, offsetBytes.Length);

        //    // offsets[0] = offset of the offsets
        //    // offsets[1] = toc
        //    // offsets[2...n] = files
        //    uint unusedLastSectorBytes = 0;
        //    uint tocOffset = SanitiseOffset(offsets[1], out unusedLastSectorBytes);

        //    uint notNeeded = 0;
        //    uint firstFileOffset = SanitiseOffset(offsets[2], out notNeeded);

        //    uint tocLen = firstFileOffset - tocOffset - unusedLastSectorBytes;
        //    byte[] toc = new byte[tocLen];
        //    IntPtr tocStart = new IntPtr(volMappingAddrAsNum + tocOffset);
        //    Marshal.Copy(tocStart, toc, 0, (int)tocLen);

        //    embeddedFiles.Capacity = TotalEntries;
        //    BinaryReader reader = new BinaryReader(new MemoryStream(toc, false));
        //    for(int i = 0; i < TotalEntries; ++i)
        //    {
        //        EmbeddedFileInfo efi = new EmbeddedFileInfo();
        //        efi.dateTime = reader.ReadInt32();
        //        short offset = reader.ReadInt16();
        //        byte flags = reader.ReadByte();
        //        bool isDir = ((flags & 1) == 1);
        //        efi.fileAddress = isDir ? (uint)offset : offsets[offset];
        //        efi.name = String.Format("{0}{1}", Encoding.ASCII.GetString(reader.ReadBytes(25)).TrimEnd('\0'), isDir ? " (dir)" : String.Empty);
        //        tfn(efi);
        //    }
        //}

        class WorkUnit
        {
            public EmbeddedFileInfo efi;
            public FileStream destFile;
            public ManualResetEvent finishedHandle;
            public long fileVolAddress;
            public bool shouldDecompAfter;
            public ExplodeProgressCallback callback;

            public WorkUnit(
                string dir, 
                EmbeddedFileInfo efiParam, 
                VolFile volFile, 
                bool decompGZ, 
                ManualResetEvent compEvent,
                ExplodeProgressCallback epc
            )
            {
                efi = efiParam;
                destFile = new FileStream(
                    Path.Combine(dir, efi.name),
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    8192,
                    false
                );
                fileVolAddress = volFile.volMappingAddrAsNum + efi.fileAddress;
                shouldDecompAfter = (decompGZ && efi.name.EndsWith(".gz"));
                finishedHandle = compEvent;
                callback = epc;
            }
        }

        public delegate void ExplodeProgressCallback(string action);

        public void Explode(string dir, bool decompGZ, ExplodeProgressCallback epc)
        {
            explodeEvents[(int)ExplodeEvents.NotRunning].Reset();
            Directory.CreateDirectory(decompGZ ? Path.Combine(dir, VolFile.DecompDir) : dir);
            outstandingRequests.Capacity = embeddedFiles.Count;
            int minNumThread, minNumCompThread;
            // we can handle more threads since we're mainly IO bound
            ThreadPool.GetMinThreads(out minNumThread, out minNumCompThread);
            ThreadPool.SetMinThreads(minNumThread * 2, minNumCompThread);
            foreach (EmbeddedFileInfo efi in embeddedFiles)
            {
                if (efi.fileAddress == 0)
                {
                    string dirName = Path.Combine(dir, efi.name);
                    if (decompGZ)
                    {
                        dirName += Path.DirectorySeparatorChar + VolFile.DecompDir;
                    }
                    DirectoryInfo di = Directory.CreateDirectory(dirName);
                    DateTime dirDate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(efi.dateTime);
                    di.LastAccessTime = di.LastWriteTime = di.CreationTime = dirDate;
                    //Directory.SetCreationTime(dirName, dirDate);
                    //Directory.SetLastWriteTime(dirName, dirDate);
                    //Directory.SetLastAccessTime(dirName, dirDate);
                }
                else if (!explodeEvents[(int)ExplodeEvents.Stop].WaitOne(0))
                {
                    ExtractSingleFile(efi, dir, decompGZ, false, epc);
                }
                else break;
            }
            ThreadPool.SetMinThreads(minNumThread, minNumCompThread);
            explodeEvents[(int)ExplodeEvents.NotRunning].Set();
        }

        private void ExtractSingleFile(EmbeddedFileInfo efi, string dir, bool decompGZ, bool waitForCompletion, ExplodeProgressCallback epc)
        {
            ManualResetEvent finEvent = new ManualResetEvent(false);
            if (!waitForCompletion)
            {
                outstandingRequests.Add(finEvent);
            }
            WorkUnit unit = new WorkUnit(dir, efi, this, decompGZ, finEvent, epc);
            if (!ThreadPool.QueueUserWorkItem(new WaitCallback(ExtractCallback), unit))
            {
                throw new ThreadInterruptedException("No Threadpool Threads");
            }
            if (waitForCompletion)
            {
                finEvent.WaitOne();
                finEvent.Close();
            }
        }

        public void ExtractSingleFile(EmbeddedFileInfo efi, string dir, bool decompGZ)
        {
            ExtractSingleFile(efi, dir, decompGZ, true, null);
        }

        private void ExtractCallback(object ctx)
        {
            WorkUnit unit = (WorkUnit)ctx;
            EmbeddedFileInfo efi = unit.efi;
            byte[] buffer = new byte[efi.size];
            IntPtr fileMemAddress = new IntPtr(unit.fileVolAddress);
            Marshal.Copy(fileMemAddress, buffer, 0, buffer.Length);
            if (unit.callback != null)
            {
                unit.callback(String.Format("Extracted {0} from VOL", efi.name));
            }

            // I tried async-ing this write in the decomp case, but it was magnitudes slower
            unit.destFile.Write(buffer, 0, buffer.Length);
            if (unit.callback != null)
            {
                unit.callback(String.Format("Finished writing {0} bytes of {1}", buffer.Length, efi.name));
            }
            if (unit.shouldDecompAfter)
            {
                // create a mem stream on the buffer so we can decomp without having to hit the filesystem
                // on both the read and the write
                MemoryStream memGz = new MemoryStream(buffer, false);
                using (FileStream destFile = GetUnzippedFileFromOrig(unit.destFile.Name))
                using (GZipInputStream srcStream = new GZipInputStream(memGz))
                {
                    byte[] readBuf = new byte[0x8000];
                    StreamUtils.Copy(srcStream, destFile, readBuf);
                    if (unit.callback != null)
                    {
                        unit.callback(String.Format("Ungzipped {0} bytes to {1} for {2}", memGz.Length, destFile.Position, efi.name));
                    }
                }
            }
            unit.finishedHandle.Set();
            string fileName = unit.destFile.Name;
            unit.destFile.Close();
            DateTime fileTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(unit.efi.dateTime);
            File.SetCreationTime(fileName, fileTime);
            File.SetLastWriteTime(fileName, fileTime);
            File.SetLastAccessTime(fileName, fileTime);
        }

        public void StopExploding()
        {
            WaitHandle.SignalAndWait(explodeEvents[(int)ExplodeEvents.Stop], explodeEvents[(int)ExplodeEvents.NotRunning]);
            explodeEvents[(int)ExplodeEvents.Stop].Reset();
            int numReqs = outstandingRequests.Count;
            int doneSoFar = 0;
            WaitHandle[] reqHandles = outstandingRequests.ToArray();
            WaitHandle[] part = new WaitHandle[63];
            while (numReqs > 0)
            {
                int toCopy = Math.Min(numReqs, 63);
                outstandingRequests.CopyTo(doneSoFar, part, 0, toCopy);
                numReqs -= toCopy;
                doneSoFar += toCopy;
                WaitHandle.WaitAll(part);
            }
            outstandingRequests.Clear();
        }

        static FileStream GetUnzippedFileFromOrig(string fileName)
        {
            char dirSep = Path.DirectorySeparatorChar;
            string newDir = Path.GetDirectoryName(fileName) + dirSep + VolFile.DecompDir;
            string ungzFileName = newDir + dirSep + Path.GetFileNameWithoutExtension(fileName);
            return new FileStream(ungzFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, false);
        }

        public short Files
        {
            get;
            private set;
        }

        public short TotalEntries
        {
            get;
            private set;
        }
    }
}
