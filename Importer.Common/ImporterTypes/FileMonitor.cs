using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models.TypeSettings;
using Microsoft.Win32.SafeHandles;
using SimpleImpersonation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Importer.Common.ImporterTypes
{
    public class FileMonitor : IImporterType, IDisposable
    {
        public string Name { get; set; } = "FileMonitor";

        public FileMonitorSettings Settings = new FileMonitorSettings();
        public IImporterModule ImporterModule { get; set; } = null;

        private bool _isDirectory;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ConcurrentDictionary<string, FileSnapshot> _fileSnapshots = new ConcurrentDictionary<string, FileSnapshot>();
        
        private const int MaxRetryCount = 3;
        private const int RetryDelayMilliseconds = 200;
        private const int RecoveryDelayMilliseconds = 5000; // Wait time before retrying after a failure

        // --- fields for queuing ---
        // Using Tuple<string, string> where Item1=filePath and Item2=fileHash.
        private readonly ConcurrentQueue<Tuple<string, string>> fileProcessingQueue = new ConcurrentQueue<Tuple<string, string>>();
        private bool isProcessing = false;
        private readonly object queueLock = new object();

        private readonly string _queuedFolder;
        private readonly string _processingFolder;
        private readonly string _archiveFolder;

        public FileMonitor(IImporterModule importerModule)
        {
            ImporterModule = importerModule;

            ApplySettings();

            _isDirectory = Directory.Exists(Settings.TargetPath);
            
            // Determine the base path. If monitoring a single file, use its parent directory.
            string basePath = _isDirectory ? Settings.TargetPath : Path.GetDirectoryName(Settings.TargetPath);

            if (!Settings.IsAdminFile)
            {
                // Create subdirectories for handling different processing stages.
                _queuedFolder = Path.Combine(basePath, "Queued");
                _processingFolder = Path.Combine(basePath, "Processing");
                _archiveFolder = Path.Combine(basePath, "Archive");

                Directory.CreateDirectory(_queuedFolder);
                Directory.CreateDirectory(_processingFolder);
                Directory.CreateDirectory(_archiveFolder);
            }
            else
            {
                // For Admin files, use the base path directly for all folders. We don't want to move files around in this case.
                _queuedFolder = basePath;
                _processingFolder = basePath;
                _archiveFolder = basePath;
            }

            InitializeDatabase(basePath);
        }

        public void Start()
        {
            // if network credienals are configured, run using SimpleImpersanation to impersonate the user
            if (Settings.UseLogin)
            {
                UserCredentials credentials = new UserCredentials(Settings.LoginDomain, Settings.LoginUsername, Settings.LoginPassword);

                using (SafeAccessTokenHandle userHandle = credentials.LogonUser(LogonType.NewCredentials))
                {
                    WindowsIdentity.RunImpersonated(userHandle, () =>
                    {
                        // First, check and enqueue any files that are already in the queued folder.
                        if (!Settings.IsAdminFile)
                            Task.Run(() => EnqueueExistingQueuedFilesAsync(_cts.Token));
                        
                        // Then, start the main monitoring loop.
                        Task.Run(() => MonitorLoopAsync(_cts.Token));
                    });
                }
                            
            }
            else
            {
                // First, check and enqueue any files that are already in the queued folder.
                if (!Settings.IsAdminFile)
                    Task.Run(() => EnqueueExistingQueuedFilesAsync(_cts.Token));

                // Then, start the main monitoring loop.
                Task.Run(() => MonitorLoopAsync(_cts.Token));
            }

        }


        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            Logger.Info($"Started monitoring {Settings.TargetPath}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_isDirectory)
                    {


                        var files = Directory.GetFiles(Settings.TargetPath)
                           .Where(path => IsFileExtensionAllowed(path)) // Add filter here
                           .Select(path => new { Path = path, LastModified = File.GetLastWriteTimeUtc(path) })
                           .Where(fileInfo =>
                           {
                               try
                               {
                                   // Try to open the file with exclusive access
                                   using (var stream = new FileStream(fileInfo.Path, FileMode.Open, FileAccess.Read, FileShare.None))
                                   using (StreamReader reader = new StreamReader(stream))
                                   {
                                       return true; // File is not shared
                                   }
                               }
                               catch (IOException)
                               {
                                   Logger.Error($"File in use, skipping for now: {fileInfo.Path}");
                                   return false; // File is being shared
                               }
                           })
                           .OrderBy(fileInfo => fileInfo.LastModified) // Orders in ascending order (oldest first)
                           .Select(fileInfo => fileInfo.Path)
                           .ToList();

                        var tasks = files.Select(file => CheckFileAsync(file, token)).ToArray();
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        // For single file monitoring, check if extension is allowed
                        if (IsFileExtensionAllowed(Settings.TargetPath))
                        {
                            await CheckFileAsync(Settings.TargetPath, token);
                        }
                    }

                    await Task.Delay(Settings.PollIntervalMilliseconds);

                    // Logger.Trace(Name + " is alive."); // Debug Heartbeat log for monitoring

                }
                catch (DirectoryNotFoundException)
                {
                    Logger.Error($"Directory not found: {Settings.TargetPath}. Attempting recovery in {RecoveryDelayMilliseconds}ms");
                    await Task.Delay(RecoveryDelayMilliseconds);
                }
                catch (IOException ex)
                {
                    Logger.Error($"IO exception encountered: {ex.Message}. Attempting recovery in {RecoveryDelayMilliseconds}ms");
                    await Task.Delay(RecoveryDelayMilliseconds);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Monitor loop error: {ex.Message}");
                    await Task.Delay(RecoveryDelayMilliseconds);
                }
            }

            Logger.Info("Stopped.");
        }

        private bool IsFileExtensionAllowed(string filePath)
        {
            if (Settings.AllowedExtensions == null || !Settings.AllowedExtensions.Any())
                return true; // No restrictions

            if (Settings.AllowedExtensions.Contains("*.*"))
                return true; // All extensions allowed

            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            return Settings.AllowedExtensions.Contains(fileExtension);
        }

        private async Task CheckFileAsync(string filePath, CancellationToken token)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                // Optionally filter by allowed extensions.  
                if (Settings.AllowedExtensions != null &&
                   !Settings.AllowedExtensions.Contains("*.*") &&
                   !Settings.AllowedExtensions.Count.Equals(0) &&
                   !Settings.AllowedExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
                    return;

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                using (StreamReader reader = new StreamReader(stream))
                {
                    Logger.Trace("File is not Locked, continuing checking file...");
                }

                var info = new FileInfo(filePath);
                var snapshot = new FileSnapshot(info.Length, info.LastWriteTimeUtc);

                if (_fileSnapshots.TryGetValue(filePath, out FileSnapshot lastSnapshot) && lastSnapshot.Equals(snapshot))
                {
                    return; // No changes detected.
                }

                _fileSnapshots[filePath] = snapshot;

                // Compute the hash with retry logic.
                string hash = await RetryAsync(() => ComputeSha256HashAsync(filePath, token), MaxRetryCount, RetryDelayMilliseconds, token);

                if (IsHashInDatabase(hash))
                {
                    Logger.Trace($"Already processed: {filePath}");
                    return;
                }                

                if (Settings.IsAdminFile)
                {
                    // Admin file processing:
                    // 1. Insert hash (original filePath).
                    // 2. Process directly from original path.
                    // 3. Mark hash as processed.
                    // No moving, no queueing.

                    // Use a windows temporary file path for admin processing.
                    string _adminProcessingFilePath = Path.Combine(Path.GetTempPath(), "AdminProcessing", Path.GetFileName(GenerateUniqueFileName(filePath)));
                    
                    if (!Directory.Exists(Path.GetDirectoryName(_adminProcessingFilePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(_adminProcessingFilePath));

                    try
                    {
                        File.Copy(filePath, _adminProcessingFilePath, true);
                        Logger.Info($"Admin file '{filePath}' copied to static processing path '{_adminProcessingFilePath}'.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to copy admin file '{filePath}' to static path '{_adminProcessingFilePath}': {ex.Message}");
                        return; // Don't proceed if copy fails
                    }

                    InsertHashIntoDatabase(hash, filePath); // filePath is the original path
                    Logger.Info($"Processing admin file directly: {filePath}");

                    await ProcessFileAsync(_adminProcessingFilePath, token); // Process from the admin processing path
                    
                    MarkHashAsProcessed(hash); // Mark as processed after successful direct processing
                    Logger.Info($"Completed processing admin file: {filePath}");
                }
                else
                {
                    // Non-admin file processing (existing logic):
                    // Move to queued, insert hash, enqueue.

                    string currentFilePath = filePath; // Use a new variable to hold the potentially moved path
                    string currentDir = Path.GetDirectoryName(currentFilePath);

                    // Only if the file is not already in one of the processing subfolders,
                    // generate one unique name to be used throughout.
                    if (currentDir != _queuedFolder && currentDir != _processingFolder && currentDir != _archiveFolder)
                    {
                        string originalFileName = Path.GetFileName(currentFilePath);
                        // Generate a unique filename with a single timestamp.
                        string uniqueFileName = GenerateUniqueFileName(originalFileName);
                        string queuedFilePath = Path.Combine(_queuedFolder, uniqueFileName);
                        try
                        {
                            File.Move(currentFilePath, queuedFilePath);
                            Logger.Info($"Moved file to queued folder: {queuedFilePath}");
                            currentFilePath = queuedFilePath; // Update currentFilePath to the new path in _queuedFolder
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to move file to queued folder: {filePath} - {ex.Message}");
                            return; // Exit if move fails
                        }
                    }

                    // Insert the hash into the database using the file path (which might be in _queuedFolder).
                    InsertHashIntoDatabase(hash, currentFilePath);
                    Logger.Info($"Queuing file for processing: {currentFilePath}");
                    // Enqueue the file along with its computed hash.
                    EnqueueFile(currentFilePath, hash);
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"File in use, skipping for now: {filePath} ({ex.Message})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing {filePath}: {ex.Message}");
                Logger.Error(ex.InnerException?.Message);
            }
        }

        /// <summary>
        /// Enqueues the file for later processing.
        /// </summary>
        private void EnqueueFile(string filePath, string hash)
        {
            fileProcessingQueue.Enqueue(Tuple.Create(filePath, hash));
            Logger.Info($"Added to processing queue: {filePath}. Queue size: {fileProcessingQueue.Count}");

            // Ensure only one processing task runs at a time
            lock (queueLock)
            {
                if (!isProcessing)
                {
                    isProcessing = true;
                    Task.Run(() => ProcessQueue());
                }
            }
        }

        private async Task EnqueueExistingQueuedFilesAsync(CancellationToken token)
        {
            // Retrieve all files from the queued folder and order them by LastWriteTimeUtc (oldest first)
            var queuedFiles = Directory.GetFiles(_queuedFolder)
                                .Select(path => new FileInfo(path))
                                .OrderBy(fileInfo => fileInfo.LastWriteTimeUtc)
                                .ToList();

            foreach (var fileInfo in queuedFiles)
            {
                try
                {
                    // Compute the hash for each file (using the existing async Blake3 hash method).
                    string hash = await RetryAsync(() => ComputeSha256HashAsync(fileInfo.FullName, token),
                                                     MaxRetryCount, RetryDelayMilliseconds, token);

                    // Optionally, you could check if the hash already exists in the database
                    // to prevent reprocessing, but if the file is in Queued it likely hasn't been processed yet.
                    EnqueueFile(fileInfo.FullName, hash);
                    Logger.Info($"Enqueued pre-existing file: {fileInfo.FullName}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing existing queued file {fileInfo.FullName}: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// Processes files one at a time from the queue.
        /// </summary>
        private async Task ProcessQueue()
        {
            while (!_cts.IsCancellationRequested && fileProcessingQueue.TryDequeue(out Tuple<string, string> queuedItem))
            {
                string fileToProcess = queuedItem.Item1;
                string hash = queuedItem.Item2;
                try
                {
                    Logger.Info($"Processing queued file: {fileToProcess}");

                    // Use the same unique file name for the Processing folder.
                    string uniqueFileName = Path.GetFileName(fileToProcess);
                    string processingFilePath = Path.Combine(_processingFolder, uniqueFileName);
                    try
                    {
                        File.Move(fileToProcess, processingFilePath);
                        Logger.Info($"Moved file to processing folder: {processingFilePath}");
                        fileToProcess = processingFilePath;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to move file to processing folder: {fileToProcess} - {ex.Message}");
                        continue;
                    }

                    // Process the file.
                    await ProcessFileAsync(fileToProcess, _cts.Token);

                    // After processing, move the file to the Archive folder using the same unique filename.
                    string archiveFilePath = Path.Combine(_archiveFolder, uniqueFileName);
                    try
                    {
                        File.Move(fileToProcess, archiveFilePath);
                        Logger.Info($"Moved file to archive folder: {archiveFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to move file to archive folder: {fileToProcess} - {ex.Message}");
                    }

                    // Mark the file's hash as processed.
                    MarkHashAsProcessed(hash);
                    Logger.Info($"Completed processing queued file: {archiveFilePath}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing queued file {fileToProcess}: {ex.Message}");
                }
            }

            lock (queueLock)
            {
                isProcessing = false;
            }
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries, int delayMilliseconds, CancellationToken token)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (IOException)
                {
                    if (attempt == maxRetries) throw;
                    await Task.Delay(delayMilliseconds);
                }
            }

            throw new IOException("Operation failed after maximum retry attempts.");
        }

        /// <summary>Computes a streaming SHA‑256 hash without loading the whole file into memory.</summary>
        private static async Task<string> ComputeSha256HashAsync(string filePath, CancellationToken token)
        {
            // 128 KB is a good compromise between I/O and memory on spinning or SSD drives.
            const int BufferSize = 128 * 1024;

            using (var sha256 = SHA256.Create())
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                                               FileShare.Read, BufferSize, useAsync: true))
            {
                var buffer = new byte[BufferSize];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();
            }
        }

        private bool IsHashInDatabase(string hash)
        {
            return false;

            using (var connection = new SQLiteConnection($"Data Source={Settings.DatabaseFile}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM FileHashes WHERE Hash = $hash";
                command.Parameters.AddWithValue("$hash", hash);

                var count = (long)command.ExecuteScalar();
                return count > 0;
            }
        }

        private void InsertHashIntoDatabase(string hash, string filePath)
        {
            using (var connection = new SQLiteConnection($"Data Source={Settings.DatabaseFile}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                INSERT INTO FileHashes (Hash, FilePath, Processed)
                VALUES ($hash, $filePath, 0);";

                command.Parameters.AddWithValue("$hash", hash);
                command.Parameters.AddWithValue("$filePath", filePath);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Marks a file’s hash as processed after the file has been handled.
        /// </summary>
        private void MarkHashAsProcessed(string hash)
        {
            using (var connection = new SQLiteConnection($"Data Source={Settings.DatabaseFile}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE FileHashes SET Processed = 1 WHERE Hash = $hash";
                command.Parameters.AddWithValue("$hash", hash);
                command.ExecuteNonQuery();
            }
        }

        private void InitializeDatabase(string basepath)
        {

            // if Settings.DatabaseFile is null or empty, set it to the default value
            if (string.IsNullOrEmpty(Settings.DatabaseFile))
            {
                Settings.DatabaseFile = "FileHistory.db";
            }

            // create a folder in the basepath for the database file if it does not exist
            basepath = Path.Combine(basepath, "FileHistory");
            if (!Directory.Exists(basepath))
            {
                Directory.CreateDirectory(basepath);
            }

            Settings.DatabaseFile = Path.Combine(basepath, Settings.DatabaseFile);

            if (!File.Exists(Settings.DatabaseFile))
            {
                using (var connection = new SQLiteConnection($"Data Source={Settings.DatabaseFile}"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS FileHashes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Hash TEXT NOT NULL,
                        FilePath TEXT,
                        Processed INTEGER DEFAULT 0,
                        ModifiedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );";

                    command.ExecuteNonQuery();
                }
            }
        }
        private string GenerateUniqueFileName(string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName);
            string baseName = Path.GetFileNameWithoutExtension(originalFileName);
            // Generate a single timestamp (with milliseconds for increased granularity)
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            return $"{baseName}_{timestamp}{extension}";
        }

        /// <summary>
        /// The processing method for an individual file.
        /// </summary>
        private Task ProcessFileAsync(string filePath, CancellationToken token)
        {
            try
            {
                Logger.Info($"Processing file: {filePath}");
                // send the filepath to the importer module
                ImporterModule.ImporterTypeData = filePath;
                // Trigger the Product Processor
                ImporterModule.TriggerProcess();
                Logger.Info($"Completed processing file: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing file {filePath}: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        public void ApplySettings()
        {
            var typeSettings = ImporterModule.ImporterInstance.TypeSettings;
            Settings = typeSettings as FileMonitorSettings;
        }

        public int GetQueuedFileCount()
        {
            // Get the count of files in the processing queue
            return fileProcessingQueue.Count;
        }

        private class FileSnapshot
        {
            public long Length { get; private set; }
            public DateTime LastWriteTimeUtc { get; private set; }

            public FileSnapshot(long length, DateTime lastWriteTimeUtc)
            {
                Length = length;
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            public override bool Equals(object obj)
            {
                var other = obj as FileSnapshot;
                if (other == null) return false;
                return Length == other.Length && LastWriteTimeUtc == other.LastWriteTimeUtc;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + Length.GetHashCode();
                    hash = hash * 23 + LastWriteTimeUtc.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
