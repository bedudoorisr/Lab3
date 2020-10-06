using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;


namespace ImageProcessorApp
{
    internal class Program
    {
        private static readonly Dictionary<byte[], Func<BinaryReader, Size>> myImageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Size>>
        {
            {new byte[] {0x42, 0x4D}, DecodeBitmap},
            {new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}, DecodePng}
        };

        private static void Main()
        {
            string temp;
            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(new string('=', 100));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{"",40} Image Processor App");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(new string('=', 100));
                Thread.Sleep(300);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Enter full path of the image file :");
                ProcessData(Console.ReadLine());

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
                Console.WriteLine("Press 1 to try again");
                Console.WriteLine("Press any other key to exit ...");
                temp = Console.ReadKey().KeyChar.ToString().Split('\'')[0];
            } while (temp == "1");

            Environment.Exit(0);
        }

        private static void ProcessData(string file)
        {
            if (!File.Exists(file))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("File not found !");
                return;
            }

            byte[] bytes;
            using (var fs = new FileStream(file, FileMode.Open))
            {
                var len = (int) fs.Length;
                bytes = new byte[len];
                fs.Read(bytes, 0, len);
            }

            var imageFormat = GetImageFormat(bytes);
            if (imageFormat == ImageFormat.None) return;
            var size = GetDimensions(file);
            WriteToFile(size, file);
        }

        private static void WriteToFile(Size size, string file)
        {
            string temp;
            do
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
                Console.WriteLine("Press 1 to write the dimensions to given file or folder");
                Console.WriteLine("Press 2 to write the dimensions automatically to file in same folder as the image file");
                Console.WriteLine("Press 3 to not write to a file");
                temp = Console.ReadKey().KeyChar.ToString().Split('\'')[0];
                Console.WriteLine("");
            } while (temp != "1" && temp != "2" && temp != "3");

            switch (temp)
            {
                case "1":
                {
                    WriteDimensionToGivenFile(size, file);
                    break;
                }
                case "2":
                    WriteDimension(size, file);
                    break;
            }
        }

        private static void WriteDimension(Size size, string imgFile)
        {
            Console.WriteLine("");
            var fullFile = Path.Combine(Path.GetDirectoryName(imgFile) ?? throw new InvalidOperationException(),
                $"{Path.GetFileNameWithoutExtension(imgFile)}_{DateTime.Now:MMddyyyy_HHmmss}.txt");
            using (var streamWriter = new StreamWriter(fullFile, true))
            {
                streamWriter.WriteLine($"{size.Width}x{size.Height}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Data written to file - {fullFile}!");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void WriteDimensionToGivenFile(Size size, string imgFile)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            Console.WriteLine("Enter full path of the file or folder to which the dimensions must be written to :");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Note : In case of folder, filename will be automatically generated.");
            Console.WriteLine("Note : In case of file, dimensions are written to it without any other validation.");
            Console.ForegroundColor = ConsoleColor.White;
            bool flag;
            do
            {
                var file = Console.ReadLine();
                if (File.Exists(file))
                {
                    using (var streamWriter = new StreamWriter(file ?? throw new InvalidOperationException(), true))
                    {
                        streamWriter.WriteLine($"{size.Width}x{size.Height}");
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Data written to file - {file}!");
                    Console.ForegroundColor = ConsoleColor.White;
                    flag = false;
                }
                else if (Directory.Exists(file))
                {
                    var fullFile = Path.Combine(file, $"{Path.GetFileNameWithoutExtension(imgFile)}_{DateTime.Now:MMddyyyy_HHmmss}.txt");
                    using (var streamWriter = new StreamWriter(fullFile, true))
                    {
                        streamWriter.WriteLine($"{size.Width}x{size.Height}");
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Data written to file - {fullFile}!");
                    Console.ForegroundColor = ConsoleColor.White;
                    flag = false;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid File or Folder path. Enter valid data!");
                    Console.ForegroundColor = ConsoleColor.White;
                    flag = true;
                }
            } while (flag);
        }

        private static ImageFormat GetImageFormat(byte[] bytes)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            var bmp = new byte[] {0x42, 0x4D};
            var png = new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};
            if (bmp.SequenceEqual(bytes.Take(bmp.Length))) return ImageFormat.Bmp;

            if (png.SequenceEqual(bytes.Take(png.Length))) return ImageFormat.Png;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("This is not a valid .Bmp or .Png file!");
            return ImageFormat.None;
        }

        private static Size GetDimensions(string path)
        {
            using (var binaryReader = new BinaryReader(File.OpenRead(path)))
            {
                return GetDimensions(binaryReader);
            }
        }

        private static Size GetDimensions(BinaryReader binaryReader)
        {
            var maxMagicBytesLength = myImageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;
            var magicBytes = new byte[maxMagicBytesLength];
            for (var i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();
                foreach (var kvPair in myImageFormatDecoders.Where(kvPair => StartsWith(magicBytes, kvPair.Key)))
                    return kvPair.Value(binaryReader);
            }

            return new Size(0, 0);
        }

        private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
        {
            for (var i = 0; i < thatBytes.Length; i += 1)
                if (thisBytes[i] != thatBytes[i])
                    return false;

            return true;
        }

        private static int ReadLittleEndInt32(BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(int)];
            for (var i = 0; i < sizeof(int); i += 1) bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            var width = binaryReader.ReadInt32();
            var height = binaryReader.ReadInt32();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"This is a .bmp image. Resolution: {width}x{height} pixels.");
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            var width = ReadLittleEndInt32(binaryReader);
            var height = ReadLittleEndInt32(binaryReader);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"This is a .png image. Resolution: {width}x{height} pixels.");
            return new Size(width, height);
        }
    }

    public enum ImageFormat
    {
        Bmp,
        Png,
        None
    }
}