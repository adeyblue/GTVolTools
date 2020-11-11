using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.GZip;

namespace GMCreator
{
    class Archiver : IDisposable
    {
        private class WriteData
        {
            public byte[] dataToWrite;
            public WriteData()
            {
                dataToWrite = null;
            }
        }

        private class ReadState
        {
            public FileStream file;
            public byte[] data;
            public int compressLevel;
            public int writeDataIndex;
        }

        private class WriteState
        {
            public FileStream archive;
            public GT2.RefCounter refCount;

            public WriteState(FileStream file, GT2.RefCounter count)
            {
                refCount = count;
                archive = file;
            }
        }

        private Thread writeThread;
        private Semaphore newWriteSignal;
        private ManualResetEvent exitEvent;
        private List<byte[]> writeData;
        private GT2.RefCounter outstandingReads;
        private string archiveFile;
        private int wasFinished;

        public Archiver(string outputFile, int fileAlignment)
        {
            archiveFile = outputFile;
            newWriteSignal = new Semaphore(0, Int32.MaxValue);
            exitEvent = new ManualResetEvent(false);
            writeData = new List<byte[]>(4000);
            outstandingReads = new GT2.RefCounter();
            wasFinished = 0;
            writeThread = new Thread(new ParameterizedThreadStart(WriteThread));
            writeThread.SetApartmentState(ApartmentState.STA);
#if DEBUG
            writeThread.Name = "Archiver Write Thread";
#endif
            writeThread.Start(--fileAlignment);
        }

        private void WriteThread(object o)
        {
            int fileAlignment = (int)o;
            string idxFile = Path.ChangeExtension(archiveFile, ".idx");
            MemoryStream memoryIdx = new MemoryStream(4000 * 4);
            int bytesWrittenSoFar = 0;
            int writeIndexSoFar = 0;
            GT2.RefCounter completeWriteCount = new GT2.RefCounter();
            FileStream archive = new FileStream(archiveFile, FileMode.Create, FileAccess.Write, FileShare.None, UInt16.MaxValue, true);
            using (BinaryWriter idxWriter = new BinaryWriter(memoryIdx))
            {
                WaitHandle[] waitables = new WaitHandle[2] {newWriteSignal, exitEvent};
                bool exitLoop = false;
                while (!exitLoop)
                {
                    int signalled = WaitHandle.WaitAny(waitables, Timeout.Infinite, false);
                    if (signalled == 0)
                    {
                        int count;
                        lock (writeData)
                        {
                            count = writeData.Count;
                        }
                        while (writeIndexSoFar < count)
                        {
                            // the reads can complete out of order, but we need to write the archive file
                            // in order. So if the alert we got wasn't for the next file, go back and wait
                            // this is also why it's a while loop, so that when we eventually get the signal
                            // for the next file, we can process the other ones we've aleady seen the signal for
                            byte[] nextData = writeData[writeIndexSoFar];
                            if (nextData != null)
                            {
                                WriteState ws = new WriteState(archive, completeWriteCount);
                                archive.Position = bytesWrittenSoFar;
                                archive.BeginWrite(nextData, 0, nextData.Length, new AsyncCallback(WriteComplete), ws);
                                int dataLen = nextData.Length;
                                int lengthWithAlignment = dataLen;
                                int padAmount = 0;
                                if (fileAlignment > 0)
                                {
                                    lengthWithAlignment = (lengthWithAlignment + fileAlignment) & ~fileAlignment;
                                    padAmount = lengthWithAlignment - dataLen;
                                }
                                idxWriter.Write(bytesWrittenSoFar | padAmount);
                                bytesWrittenSoFar += lengthWithAlignment;
                                ++writeIndexSoFar;
                            }
                        }
                    }
                    else
                    {
                        exitLoop = true;
                    }
                    Debug.Assert(signalled >= 0 && signalled <= 1, "Archive write thread got a signal other than 0 or 1!");
                }
            }
            bool cancelled = (Thread.VolatileRead(ref wasFinished) == 0);
            if (!cancelled)
            {
                using (BinaryWriter idxFileWriter = new BinaryWriter(new FileStream(idxFile, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    // how many
                    idxFileWriter.Write(writeIndexSoFar);
                    // the offsets
                    idxFileWriter.Write(memoryIdx.ToArray());
                    // file size
                    idxFileWriter.Write(bytesWrittenSoFar);
                }
            }
            while (!completeWriteCount.HasReached(writeIndexSoFar))
            {
                Thread.Sleep(100);
            }
            archive.Dispose();
            if (cancelled)
            {
                File.Delete(archiveFile);
            }
        }

        private void WriteComplete(IAsyncResult res)
        {
            WriteState ws = (WriteState)res.AsyncState;
            ws.refCount.Increment();
            ws.archive.EndWrite(res);
        }

        private void ReadComplete(IAsyncResult res)
        {
            ReadState rs = (ReadState)res.AsyncState;
            using (rs.file)
            {
                rs.file.EndRead(res);
                if (rs.compressLevel > 0)
                {
                    byte[] data = rs.data;
                    // don't compress things which are already compressed
                    if ((data.Length > 2) && ((data[0] != 0x1f) || (data[1] != 0x8b)))
                    {
                        MemoryStream compStream = new MemoryStream(rs.data.Length);
                        using (GZipOutputStream gzOut = new GZipOutputStream(compStream))
                        {
                            gzOut.SetLevel(rs.compressLevel);
                            gzOut.Write(rs.data, 0, rs.data.Length);
                            gzOut.Finish();
                            rs.data = compStream.ToArray();
                        }
                    }
                }
                lock (writeData)
                {
                    writeData[rs.writeDataIndex] = rs.data;
                    newWriteSignal.Release(1);
                }
            }
            outstandingReads.Increment();
        }

        public void AddFile(string file, int compressionLevel)
        {
            ReadState rs = new ReadState();
            rs.compressLevel = compressionLevel;
            FileStream fileStream = rs.file = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, UInt16.MaxValue, true);
            int fileLen = (int)fileStream.Length;
            byte[] data = rs.data = new byte[fileLen];
            lock(writeData)
            {
                rs.writeDataIndex = writeData.Count;
                writeData.Add(null);
            }
            fileStream.BeginRead(data, 0, data.Length, new AsyncCallback(ReadComplete), rs);
        }

        public void AddFiles(List<string> fileNames, int compressionLevel)
        {
            foreach (string s in fileNames)
            {
                AddFile(s, compressionLevel);
            }
        }

        public void Finish()
        {
            int numRequests;
            Interlocked.Increment(ref wasFinished);
            lock(writeData)
            {
                numRequests = writeData.Count;
            }
            while (!outstandingReads.HasReached(numRequests))
            {
                Thread.Sleep(100);
            }
            exitEvent.Set();
            writeThread.Join();
        }

        public void Dispose()
        {
            exitEvent.Set();
            if (writeThread.IsAlive)
            {
                writeThread.Join();
            }
            newWriteSignal.Close();
            exitEvent.Close();
        }
    }
}
