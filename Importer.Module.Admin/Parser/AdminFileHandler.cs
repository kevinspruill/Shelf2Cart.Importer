using Importer.Common.Helpers;
using Importer.Module.Admin.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Admin.Parser
{
    public class AdminFileHandler
    {
        // Unzips the file to the specified directory
        public void UnzipFile(string zipFilePath, string destinationDirectory)
        {
            if (string.IsNullOrEmpty(zipFilePath) || string.IsNullOrEmpty(destinationDirectory))
            {
                throw new ArgumentException("Zip file path and destination directory cannot be null or empty.");
            }
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException("The specified zip file does not exist.", zipFilePath);
            }

            // Ensure the destination directory exists
            Directory.CreateDirectory(destinationDirectory);

            // Use ZipArchive with FileStream for older versions
            using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    // Skip directory entries
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    string destinationPath = Path.Combine(destinationDirectory, entry.FullName);

                    // Create directory if needed
                    string directoryPath = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Extract file with overwrite capability
                    using (var entryStream = entry.Open())
                    using (var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                    {
                        entryStream.CopyTo(outputStream);
                    }
                }
            }
        }

        /// <summary>
        /// Synchronizes files and subfolders from a source directory to a destination directory.
        /// It copies files from the source to the destination if the file doesn't exist in the destination,
        /// or if the source file is newer than the destination file.
        /// This is a one-way synchronization: source to destination.
        /// </summary>
        /// <param name="sourceDirectoryPath">The path to the source directory.</param>
        /// <param name="destinationDirectoryPath">The path to the destination directory. It will be created if it doesn't exist.</param>
        /// <exception cref="ArgumentNullException">Thrown if source or destination paths are null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if the source directory does not exist.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs during synchronization.</exception>
        public void SyncFolders(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            if (string.IsNullOrEmpty(sourceDirectoryPath))
            {
                throw new ArgumentNullException(nameof(sourceDirectoryPath), "Source directory path cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(destinationDirectoryPath))
            {
                throw new ArgumentNullException(nameof(destinationDirectoryPath), "Destination directory path cannot be null or empty.");
            }

            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirectoryPath);
            DirectoryInfo destDir = new DirectoryInfo(destinationDirectoryPath);

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir.FullName}");
            }

            if (!destDir.Exists)
            {
                Directory.CreateDirectory(destDir.FullName);
            }

            // Copy files
            foreach (FileInfo fileInSource in sourceDir.GetFiles())
            {
                // Skip "Thumbs.db" files, case-insensitively
                if (string.Equals(fileInSource.Name, "Thumbs.db", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string targetFilePath = Path.Combine(destDir.FullName, fileInSource.Name);
                FileInfo destFile = new FileInfo(targetFilePath);

                // Copy if destination file doesn't exist or source file is newer
                if (!destFile.Exists || fileInSource.LastWriteTime > destFile.LastWriteTime)
                {
                    fileInSource.CopyTo(targetFilePath, true); // true to overwrite
                }
            }

            // Recursively sync subdirectories
            foreach (DirectoryInfo sourceSubDir in sourceDir.GetDirectories())
            {
                string destinationSubDirPath = Path.Combine(destDir.FullName, sourceSubDir.Name);
                SyncFolders(sourceSubDir.FullName, destinationSubDirPath); // Recursive call
            }
        }

        // Determine if file is a zip or mdb file based on extension
        public bool IsZipFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Logger.Fatal(filePath + " is null or empty. Cannot determine if it is a zip file.");
                return false;
            }

            return Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        }


    }
}
