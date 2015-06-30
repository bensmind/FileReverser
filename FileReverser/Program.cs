using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FileReverser
{
    static class Program
    {
        private const string OutputDir = @"C:\FileReverser";
        private const int NumberOfLinesfor1MFile = 1000;
        private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly Random Random = new Random();
        private static readonly bool IncludeEmptyLines = false;
        private const int DiskReadBufferSize = 16384;//8192;
        private const int DiskWriteBufferSize = 8192;//4096;
        private static readonly int utf8Mask = 1 << 7;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Press Enter to start"); Console.ReadLine();

            Directory.CreateDirectory(OutputDir);
            
            var filePrefix = string.Format("SampleFile_{0:yyyyMMdd_hhmmss}", DateTime.Now);

            var fileName1M = Path.Combine(OutputDir, filePrefix + "_1m.txt");
            var fileName10M = Path.Combine(OutputDir, filePrefix + "_10m.txt");
            var fileName100M = Path.Combine(OutputDir, filePrefix + "_100m.txt");
            var fileName500M = Path.Combine(OutputDir, filePrefix + "_500m.txt");
            //var fileName1G = Path.Combine(OutputDir, filePrefix + "_1g.txt");
            //var fileName5G = Path.Combine(OutputDir, filePrefix + "_5g.txt");

            CreateFile(fileName1M, NumberOfLinesfor1MFile);
            CreateFile(fileName10M, NumberOfLinesfor1MFile * 10);
            CreateFile(fileName100M, NumberOfLinesfor1MFile * 100);
            CreateFile(fileName500M, NumberOfLinesfor1MFile * 500);
            //CreateFile(fileName1G, NumberOfLinesfor1MFile * 1000);
            //CreateFile(fileName5G, NumberOfLinesfor1MFile * 5000);
            Console.WriteLine();

            Reverse(new FileInfo(fileName1M));
            Reverse(new FileInfo(fileName10M));
            Reverse(new FileInfo(fileName100M));
            Reverse(new FileInfo(fileName500M));
            //Reverse(new FileInfo(fileName1G));
            //Reverse(new FileInfo(fileName5G));

            Console.WriteLine("Done. Press Enter to exit"); Console.ReadLine(); CleanFiles();
        }

        private static void Reverse(FileInfo fileInfo, int readBufferSize = DiskReadBufferSize, int writeBufferSize = DiskWriteBufferSize)
        {
            Console.WriteLine("Reversing file {0}", fileInfo.Name);
            var stopwatch = Stopwatch.StartNew();
            List<long> offsets = GetLineOffsets(fileInfo, readBufferSize);
            DoReverse(fileInfo, offsets, readBufferSize, writeBufferSize);
            Console.WriteLine("Took: {0}", stopwatch.Elapsed);
        }

        private static void DoReverse(FileInfo fileInfo, List<long> offsets, int readBufferSize = DiskReadBufferSize, int writeBufferSize = DiskWriteBufferSize)
        {
            offsets.Reverse();
            
            var end = offsets.First();

            using (var reader = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, readBufferSize, FileOptions.SequentialScan))
            using (var writer = new FileStream(Path.Combine(fileInfo.DirectoryName, "reversed_" + fileInfo.Name), FileMode.Create, FileAccess.Write, FileShare.None, writeBufferSize))
            {
                for(var sIndex = 1; sIndex < offsets.Count; sIndex++)
                {
                    long start = offsets[sIndex];
                    int count = (int)(end - start);
                    byte[] lineBuffer = new byte[count];
                    reader.Seek(start, SeekOrigin.Begin);
                    reader.Read(lineBuffer, 0, count);
                    writer.Write(lineBuffer, 0, count);
                    end = start;
                }
            }
        }

        private static List<long> GetLineOffsets(FileInfo fileInfo, int bufferSize)
        {
            var offsets = new List<long> {0};

            using (var reader = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, FileOptions.RandomAccess))
            {
                int value;
                byte last = 0;
                while ((value = reader.ReadByte()) > 0)
                {
                    byte b = (byte)value;
                    if ((b & utf8Mask) != 0) //this is the flag for multibyte codes, all bytes of a multibyte code have the highest bit set
                    {
                        continue;
                    }
                    if (b == 10 || (b == 13 && last != 10))
                    {
                        offsets.Add(reader.Position);
                    }
                    last = b;
                }
            }
            offsets.Add(fileInfo.Length);
            return offsets;
        }

        private static void CreateFile(string fileName, int numberOfLines, int writeBuffer = DiskWriteBufferSize)
        {
            Console.WriteLine("Creating file {0}", fileName);
            var stopwatch = Stopwatch.StartNew();
            using (var fileStream = File.Create(fileName, writeBuffer))
            using (var writer = new StreamWriter(fileStream))
            {
                for (var i = 0; i < numberOfLines; i++)
                {
                    char letter = Letters[i % Letters.Length];
                    string line = String.Format("{0} - {1}", i, new string(letter, Random.Next(900, 1200)));
                    writer.Write(line);
                    writer.WriteLine();
                }
            }
            Console.WriteLine("Took: {0}", stopwatch.Elapsed);
        }

        private static void CleanFiles()
        {
            if (Directory.Exists(OutputDir))
            {
                Directory.Delete(OutputDir, true);
            }
        }
    }
}
