using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileReverser
{
    class Program
    {
            /// <summary>
        ///efficiently reverse the lines of a file.  As an example, if this is the input file
    /// line 1
    /// line 2
    /// line 3
    /// line 4
    /// line 5
    ///
    /// The expected output file you should create should be:
    /// line 5
    /// line 4
    /// line 3
    /// line 2
    /// line 1
    /// </summary>

        private const string OutputDir = @"C:\FileReverser";
        private const int NumberOfLinesfor1MFile = 1000;
        private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly Random Random = new Random();
        private static readonly char[] NewlineChars = new[] {'\r', '\n'};
        private static bool IncludeEmptyLines = false;
        private const int DiskBufferSize = 4096*16;

        static void Main(string[] args)
        {
            Console.WriteLine("Press Enter to start"); Console.ReadLine();

            Directory.CreateDirectory(OutputDir);
            var fileName1M = Path.Combine(OutputDir, string.Format("File_{0:yyyyMMdd_hhmmss}_1m.txt", DateTime.Now));
            var fileName10M = Path.Combine(OutputDir, string.Format("File_{0:yyyyMMdd_hhmmss}_10m.txt", DateTime.Now));
            var fileName100M = Path.Combine(OutputDir, string.Format("File_{0:yyyyMMdd_hhmmss}_100m.txt", DateTime.Now));
            var fileName500M = Path.Combine(OutputDir, string.Format("File_{0:yyyyMMdd_hhmmss}_500m.txt", DateTime.Now));
            //var fileName1G = Path.Combine(OutputDir, string.Format("File_{0:yyyyMMdd_hhmmss}_1g.txt", DateTime.Now));
            //var fileName5G = Path.Combine(OutputDir, string.Format("File_{0:yyyyMMdd_hhmmss}_5g.txt", DateTime.Now));

            CreateFile(fileName1M, NumberOfLinesfor1MFile);
            CreateFile(fileName10M, NumberOfLinesfor1MFile * 10);
            CreateFile(fileName100M, NumberOfLinesfor1MFile * 100);
            CreateFile(fileName500M, NumberOfLinesfor1MFile * 500);
            //CreateFile(fileName1G, NumberOfLinesfor1MFile * 1000);
            //CreateFile(fileName5G, NumberOfLinesfor1MFile * 5000);

            Reverse(new FileInfo(fileName1M), DiskBufferSize);
            Reverse(new FileInfo(fileName10M), DiskBufferSize);
            Reverse(new FileInfo(fileName100M), DiskBufferSize);
            Reverse(new FileInfo(fileName500M), DiskBufferSize);
            //Reverse(new FileInfo(fileName1G), DiskBufferSize);
            //Reverse(new FileInfo(fileName5G), DiskBufferSize);

            Console.WriteLine("Done. Press Enter to exit");Console.ReadLine();CleanFiles();
        }

        private static void Reverse(FileInfo fileInfo, int bufferSize)
        {
            Console.WriteLine("Reversing file {0}", fileInfo.Name);
            var stopwatch = Stopwatch.StartNew();
            var offsets = GetLineOffsets(fileInfo, bufferSize);
            DoReverse(fileInfo, offsets, bufferSize);
            Console.WriteLine(stopwatch.Elapsed);
        }

        private static void DoReverse(FileInfo fileInfo, List<long> offsets, int bufferSize)
        {
            offsets.Reverse();
            long end = offsets.First();
            using (
                FileStream reader = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None,
                    bufferSize, FileOptions.RandomAccess))
            {
                using (FileStream writer = new FileStream(Path.Combine(fileInfo.DirectoryName, "reversed_" + fileInfo.Name),FileMode.Create,FileAccess.Write,FileShare.None,bufferSize))
                {
                    foreach (long start in offsets.Skip(1))
                    {
                        byte[] lineBuffer = new byte[end - start];
                        reader.Seek(start, SeekOrigin.Begin);
                        reader.Read(lineBuffer, 0, (int) (end - start));
                        writer.Write(lineBuffer, 0, (int)(end - start));
                        end = start;
                    }
                }
            }
        }

        private static List<long> GetLineOffsets(FileInfo fileInfo, int bufferSize)
        {
            List<long> offsets = new List<long>();
            offsets.Add(0);

            using (FileStream reader = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, FileOptions.SequentialScan))
            {
                byte b;
                int value;
                bool skipMultiPart = false;
                bool lastWasNewLine = true;
                while ((value = reader.ReadByte()) > 0)
                {
                    if (skipMultiPart)
                    {
                        skipMultiPart = false;
                        continue;
                    }
                    b = (byte) value;
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

        private static void CreateFile(string fileName, int numberOfLines)
        {
            Console.WriteLine("Creating file {0}", fileName);
            var stopwatch = Stopwatch.StartNew();
            using (var fh = File.Create(fileName))
            using(var writer = new StreamWriter(fh))
            {
                for (var i = 0; i < numberOfLines; i++)
                {
                    var letter = Letters[i%Letters.Length];
                    var line = string.Format("{0} - {1}", i, new string(letter, Random.Next(900, 1200)));
                    writer.Write(line);
                    writer.WriteLine();
                }
            }
            Console.WriteLine(stopwatch.Elapsed);
        }

        private static void CleanFiles()
        {
            if (Directory.Exists(OutputDir))
            {
                Directory.Delete(OutputDir,true);
            }
        }
    }
}
