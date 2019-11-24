using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GT2Vol
{
    public static class MemoryMappedFile
    {
        private static uint PAGE_READONLY = 0x2;
        private static uint FILE_MAP_READ = 0x4;

        [DllImport("kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private extern static IntPtr CreateFileMappingW(
            IntPtr fileHandle,
            IntPtr sec,
            uint protect,
            uint maxHigh,
            uint maxLow,
            [MarshalAs(UnmanagedType.LPWStr)]
            string name
        );

        [DllImport("kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private extern static int UnmapViewOfFile(IntPtr pFile);

        [DllImport("kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private extern static IntPtr MapViewOfFileEx(
            IntPtr mapHandle,
            uint access,
            uint offLow,
            uint offHigh,
            IntPtr bytesToMap,
            IntPtr baseAddress
        );

        [DllImport("kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int CloseHandle(IntPtr handle);

        public static IntPtr Map(FileStream file)
        {
            IntPtr fileHandle = file.SafeFileHandle.DangerousGetHandle();
            IntPtr mapHandle = CreateFileMappingW(fileHandle, IntPtr.Zero, PAGE_READONLY, 0, 0, null);
            if (mapHandle != IntPtr.Zero)
            {
                IntPtr pFile = MapViewOfFileEx(mapHandle, FILE_MAP_READ, 0, 0, IntPtr.Zero, IntPtr.Zero);
                CloseHandle(mapHandle);
                mapHandle = pFile;
            }
            return mapHandle;
        }

        public static void UnMap(IntPtr pFile)
        {
            UnmapViewOfFile(pFile);
        }
    }
}
