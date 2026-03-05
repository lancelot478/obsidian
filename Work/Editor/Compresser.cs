
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class Compresser {
    public static void CompressDirectory(string _filesDirectory, string _zipFilePath) {
        var files = new List<string>();

        void ProcessDirectory(string targetDirectory) {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries) {
                files.Add(fileName.Replace(_filesDirectory, ""));
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries) {
                ProcessDirectory(subdirectory);
            }
        }

        ProcessDirectory(_filesDirectory);
        using (FileStream zipToOpen = new FileStream(_zipFilePath, FileMode.OpenOrCreate)) {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create)) {
                foreach (var file in files) {
                    ZipArchiveEntry entry = archive.CreateEntry(file);
                    using (Stream entryStream = entry.Open()) {
                        var filePath = Path.Combine(_filesDirectory, file);
                        var fromFileStream = File.OpenRead(filePath);
                        fromFileStream.CopyTo(entryStream);
                    }
                }
            }
        }
    }

    public static void ExtractZipToDirectory(string _zipFilePath, string _outDirectory) {
        using (FileStream zipToOpen = new FileStream(_zipFilePath, FileMode.Open)) {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read)) {
                var entries = archive.Entries;
                foreach (var entry in entries) {
                    using (Stream entryStream = entry.Open()) {
                        var fileInfo = new FileInfo(Path.Combine(_outDirectory, entry.FullName));
                        if (fileInfo.Directory.Exists == false) {
                            fileInfo.Directory.Create();
                        }

                        entryStream.CopyTo(File.OpenWrite(fileInfo.FullName));
                    }
                }
            }
        }
    }
}

