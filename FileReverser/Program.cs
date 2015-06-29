using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

            Reverse(fileName1M);
            Reverse(fileName10M);
            Reverse(fileName100M);
            Reverse(fileName500M);
            //Reverse(new FileInfo(fileName1G), DiskBufferSize);
            //Reverse(new FileInfo(fileName5G), DiskBufferSize);

            Console.WriteLine("Done. Press Enter to exit"); Console.ReadLine(); CleanFiles();
        }

        private static void Reverse(string fileName, int readBufferSize = DiskReadBufferSize, int writeBufferSize = DiskWriteBufferSize)
        {
            var fileInfo = new FileInfo(fileName);
            Console.WriteLine("Reversing file {0}", fileInfo.Name);
            var stopwatch = Stopwatch.StartNew();
            DoReverse(fileInfo, readBufferSize, writeBufferSize);
            Console.WriteLine("Took: {0}", stopwatch.Elapsed);
        }

        private static void DoReverse(FileInfo fileInfo, int readBufferSize = DiskReadBufferSize, int writeBufferSize = DiskWriteBufferSize)
        {
            var offsets = GetLineOffsets(fileInfo, readBufferSize);
            offsets.Reverse();
            
            var end = offsets.First();

            using (var reader = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, readBufferSize, FileOptions.SequentialScan))
            using (var writer = new FileStream(Path.Combine(fileInfo.DirectoryName, "reversed_" + fileInfo.Name), FileMode.Create, FileAccess.Write, FileShare.None, writeBufferSize))
            {
                for(var sIndex = 1; sIndex < offsets.Count; sIndex++)
                {
                    var start = offsets[sIndex];
                    var count = (int)(end - start);
                    var lineBuffer = new byte[count];
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
                var skipMultiPart = false;
                var lastWasNewLine = true;
                while ((value = reader.ReadByte()) > 0)
                {
                    if (skipMultiPart)
                    {
                        skipMultiPart = false;
                        continue;
                    }
                    var b = (byte)value;
                    if (b > 127)
                    {
                        skipMultiPart = true;
                        continue;
                    }
                    if (b == 10 || b == 13)
                    {
                        if (IncludeEmptyLines || !lastWasNewLine)
                        {
                            offsets.Add(reader.Position);
                        }
                        lastWasNewLine = true;
                        continue;
                    }
                    lastWasNewLine = false;
                }
            }
            offsets.Add(fileInfo.Length);
            return offsets;
        }

        private static void CreateFile(string fileName, int numberOfLines, int writeBuffer = DiskWriteBufferSize)
        {
            Console.WriteLine("Creating file {0}", fileName);
            var stopwatch = Stopwatch.StartNew();
            using (var fh = File.Create(fileName, writeBuffer))
            using (var writer = new StreamWriter(fh))
            {
                for (var i = 0; i < numberOfLines; i++)
                {
                    var letter = Letters[i % Letters.Length];
                    var line = string.Format("{0} - {1}", i, new string(letter, Random.Next(900, 1200)));
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