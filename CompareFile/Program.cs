using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompareFile
{
    class Program
    {
        private const string PathToDirectory1 = @"C:\dir1\";
        private const string PathToDirectory2 = @"C:\dir2\";

        // Using for hashFunction
        private static readonly int _sizeHash = 5;

        static void Main(string[] args)
        {
            var dir1 = new DirectoryInfo(PathToDirectory1);
            var dir2 = new DirectoryInfo(PathToDirectory2);

            var filesDir1 = dir1.GetFiles("*", SearchOption.AllDirectories);
            var filesDir2 = dir2.GetFiles("*", SearchOption.AllDirectories);

            // Key - hash is first _sizeHash bytes from file, Value - FileInfo
            MultiValueDictionary<string, FileInfo> hashDictDir1;

            // Key - file name from dir1, Value - equals file's name from dir2
            MultiValueDictionary<string, string> dictFilesDir1;

            GetHashMapFiles(filesDir1, out hashDictDir1, out dictFilesDir1);

            Compare(hashDictDir1, filesDir2, dictFilesDir1);

            ProcessingOutputData(dictFilesDir1);
        }

        // Compare files from dir1 with files from dir2 using hashDict
        private static void Compare(MultiValueDictionary<string, FileInfo> hashDict, IEnumerable<FileInfo> filesDirToCompare, MultiValueDictionary<string, string> dictFilesDir1)
        {
            var hashBytes = new byte[_sizeHash];

            foreach (var fileInfo in filesDirToCompare)
            {
                using (var fsDirToCompare = new FileStream(fileInfo.FullName, FileMode.Open))
                {
                    fsDirToCompare.Read(hashBytes, 0, _sizeHash);

                    var hash = BitConverter.ToString(hashBytes);

                    if (hashDict.ContainsKey(hash))
                    {
                        var customFiles = hashDict.GetValues(hash, false);

                        CompareFileWithFiles(fsDirToCompare, fileInfo.Name, customFiles, dictFilesDir1);
                    }
                }
            }
        }

        // Compare file from dir2 with files from dir1 with equal hash
        private static void CompareFileWithFiles(FileStream fs, string fileName, HashSet<FileInfo> fileNamesToCompare, MultiValueDictionary<string, string> dictFilesDir1)
        {
            foreach (var fileNameToCompare in fileNamesToCompare)
            {
                using (var fsToCompare = new FileStream(fileNameToCompare.FullName, FileMode.Open))
                {
                    // Offset, because early read _sizeHash from fs
                    fsToCompare.Seek(_sizeHash, SeekOrigin.Begin);

                    if (EqualStreamsData(fs, fsToCompare))
                    {
                        dictFilesDir1.Add(fileNameToCompare.Name, fileName);
                    }

                    // Offset
                    fs.Seek(_sizeHash, SeekOrigin.Begin);
                }
            }
        }

        private static bool EqualStreamsData(FileStream fs, FileStream fsToCompare)
        {
            var isEqual = false;

            if (fsToCompare.Length == fs.Length)
            {
                int fileByte1, fileByte2;

                do
                {
                    // Read one byte from each file.
                    fileByte1 = fsToCompare.ReadByte();
                    fileByte2 = fs.ReadByte();
                } while ((fileByte1 == fileByte2) && (fileByte1 != -1));

                if ((fileByte1 - fileByte2) == 0)
                {
                    isEqual =  true;
                }
            }

            return isEqual;
        }


        // Get HashMap, where Key - hash first _sizeHash bytes from file, Value - FileInfo
        // Get Dictionary, where Key - file name from dir1, Value - equals file's name from dir2
        private static void GetHashMapFiles(IEnumerable<FileInfo> filesInfos, out MultiValueDictionary<string, FileInfo> filesDictionary, out MultiValueDictionary<string, string> filesDirectoryToCompare)
        {
            filesDictionary = new MultiValueDictionary<string, FileInfo>();
            filesDirectoryToCompare = new MultiValueDictionary<string, string>();

            var hashBytes = new byte[_sizeHash];

            foreach (var fileInfo in filesInfos)
            {
                using (var fs = new FileStream(fileInfo.FullName, FileMode.Open))
                {
                    fs.Read(hashBytes, 0, _sizeHash);
                }

                var hash = BitConverter.ToString(hashBytes);
                filesDirectoryToCompare.Add(fileInfo.Name, null);
                filesDictionary.Add(hash, fileInfo);
            }
        }


        // Output data
        private static void ProcessingOutputData(MultiValueDictionary<string, string> dataDictionary)
        {
            Console.WriteLine("Equals files:");

            foreach (
                var outputStr 
                in from data 
                in dataDictionary
                where data.Value.Count > 1
                let outputStr = $"1st directory: {data.Key}\r\n2nd directory: "
                select data.Value.Where(fileName => fileName != null).Aggregate(outputStr, (current, fileName) => current + $"{fileName} ") 
                into outputStr
                select outputStr + "\r\n\r\n"
                )
            {
                Console.Write(outputStr);
            }

            Console.WriteLine("Doesn't math files from 1st directory:");

            foreach (var data in dataDictionary.Where(data => data.Value.Count == 1))
            {
                Console.WriteLine(data.Key);
            }

            Console.ReadLine();
        }
    }
}
