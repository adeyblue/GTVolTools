using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GT2Vol
{
    static class GT2K
    {
        class GT2KVolEntry
        {
            public string name; // prefixed with a single byte of length
            public int offset; // multiples of 0x800, big endian
            public int length; // in bytes, big endian
        }

        static public void Explode2KVol(string[] args)
        {
            try
            {
                using (FileStream vol2k = new FileStream(args[1], FileMode.Open, FileAccess.Read))
                {
                    List<GT2KVolEntry> files = new List<GT2KVolEntry>();
                    BinaryReader br = new BinaryReader(vol2k);
                    string name = br.ReadString();
                    while (!String.IsNullOrEmpty(name))
                    {
                        GT2KVolEntry file = new GT2KVolEntry();
                        file.name = name;
                        byte[] offsetArray = br.ReadBytes(4);
                        byte[] sizeArray = br.ReadBytes(4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(offsetArray);
                            Array.Reverse(sizeArray);
                        }
                        file.offset = BitConverter.ToInt32(offsetArray, 0) * 0x800;
                        file.length = BitConverter.ToInt32(sizeArray, 0);
                        files.Add(file);
                        name = br.ReadString();
                    }
                    int num = files.Count;
                    char[] pathSeps = new char[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar };
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < num; ++i)
                    {
                        vol2k.Seek(files[i].offset, SeekOrigin.Begin);
                        byte[] fileData = br.ReadBytes(files[i].length);
                        string fileName = files[i].name;

                        string[] pathParts = fileName.Split(pathSeps);
                        sb.AppendFormat("{0}{1}", args[2], Path.DirectorySeparatorChar);
                        if (pathParts.Length > 1)
                        {
                            sb.AppendFormat("{0}{1}", pathParts[0], Path.DirectorySeparatorChar);
                            Directory.CreateDirectory(sb.ToString());
                            fileName = pathParts[1];
                        }
                        sb.Append(fileName);
                        File.WriteAllBytes(sb.ToString(), fileData);
                        sb.Remove(0, sb.Length);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("VOL file doesn't exist or can't be accessed!");
            }
        }

        static public void List2KVol(string[] args)
        {
            try
            {
                using (FileStream vol2k = new FileStream(args[1], FileMode.Open, FileAccess.Read))
                {
                    List<GT2KVolEntry> files = new List<GT2KVolEntry>();
                    BinaryReader br = new BinaryReader(vol2k);
                    string name = br.ReadString();
                    Console.WriteLine("Name\tSize\tFile Data Position");
                    while (!String.IsNullOrEmpty(name))
                    {
                        byte[] offsetArray = br.ReadBytes(4);
                        byte[] sizeArray = br.ReadBytes(4);
                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(offsetArray);
                            Array.Reverse(sizeArray);
                        }
                        int offset = BitConverter.ToInt32(offsetArray, 0) * 0x800;
                        int length = BitConverter.ToInt32(sizeArray, 0);
                        Console.WriteLine("{0}\t{1}(0x{1:x})\t{2}(0x{2:x})", name, length, offset);
                        name = br.ReadString();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("VOL file doesn't exist or can't be accessed!");
            }
        }
    }
}