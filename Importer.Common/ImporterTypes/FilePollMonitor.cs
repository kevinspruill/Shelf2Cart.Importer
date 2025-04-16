using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.HashFunction.Blake3;
using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Microsoft.Data.Sqlite;

namespace Importer.Common.ImporterTypes
{
    public class FilePollMonitor : IDisposable
    {
        public string Name { get; set; } = "FilePollMonitor";
        public FilePollMonitorSettings Settings { get; set; } = new FilePollMonitorSettings();
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

        public FilePollMonitor(IImporterModule importerModule)
        {
            ImporterModule = importerModule;

            ApplySettings();

            _isDirectory = Directory.Exists(Settings.TargetPath);
            
            // Determine the base path. If monitoring a single file, use its parent directory.
            string basePath = _isDirectory ? Settings.TargetPath : Path.GetDirectoryName(Settings.TargetPath);

            // Create subdirectories for handling different processing stages.
            _queuedFolder = Path.Combine(basePath, "Queued");
            _processingFolder = Path.Combine(basePath, "Processing");
            _archiveFolder = Path.Combine(basePath, "Archive");

            Directory.CreateDirectory(_queuedFolder);
            Directory.CreateDirectory(_processingFolder);
            Directory.CreateDirectory(_archiveFolder);

            InitializeDatabase();
        }

        public void Start()
        {
            // First, check and enqueue any files that are already in the queued folder.
            Task.Run(() => EnqueueExistingQueuedFilesAsync(_cts.Token));

            // Then, start the main monitoring loop.
            Task.Run(() => MonitorLoopAsync(_cts.Token));
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
                            .Select(path => new { Path = path, LastModified = File.GetLastWriteTimeUtc(path) })
                            .OrderBy(fileInfo => fileInfo.LastModified) // Orders in ascending order (oldest first)
                            .Select(fileInfo => fileInfo.Path)
                            .ToList();
                        var tasks = files.Select(file => CheckFileAsync(file, token)).ToArray();
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        await CheckFileAsync(Settings.TargetPath, token);
                    }

                    await Task.Delay(Settings.PollIntervalMilliseconds);
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

        private async Task CheckFileAsync(string filePath, CancellationToken token)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                // Optionally filter by allowed extensions.
                if (Settings.AllowedExtensions != null && !Settings.AllowedExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
                    return;

                var info = new FileInfo(filePath);
                var snapshot = new FileSnapshot(info.Length, info.LastWriteTimeUtc);

                if (_fileSnapshots.TryGetValue(filePath, out FileSnapshot lastSnapshot) && lastSnapshot.Equals(snapshot))
                {
                    return; // No changes detected.
                }

                _fileSnapshots[filePath] = snapshot;

                // Compute the hash with retry logic.
                string hash = await RetryAsync(() => ComputeBlake3HashAsync(filePath, token), MaxRetryCount, RetryDelayMilliseconds, token);

                if (IsHashInDatabase(hash))
                {
                    Logger.Trace($"Already processed: {filePath}");
                    return;
                }

                // Only if the file is not already in one of the processing subfolders,
                // generate one unique name to be used throughout.
                string currentDir = Path.GetDirectoryName(filePath);
                if (currentDir != _queuedFolder && currentDir != _processingFolder && currentDir != _archiveFolder)
                {
                    string originalFileName = Path.GetFileName(filePath);
                    // Generate a unique filename with a single timestamp.
                    string uniqueFileName = GenerateUniqueFileName(originalFileName);
                    string queuedFilePath = Path.Combine(_queuedFolder, uniqueFileName);
                    try
                    {
                        File.Move(filePath, queuedFilePath);
                        Logger.Info($"Moved file to queued folder: {queuedFilePath}");
                        filePath = queuedFilePath;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to move file to queued folder: {filePath} - {ex.Message}");
                        return;
                    }
                }

                // Insert the hash into the database using the file path with the unique name.
                InsertHashIntoDatabase(hash, filePath);
                Logger.Info($"Queuing file for processing: {filePath}");
                // Enqueue the file along with its computed hash.
                EnqueueFile(filePath, hash);
            }
            catch (IOException ex)
            {
                Logger.Error($"File in use, skipping for now: {filePath} ({ex.Message})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing {filePath}: {ex.Message}");
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
                    string hash = await RetryAsync(() => ComputeBlake3HashAsync(fileInfo.FullName, token),
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

        private async Task<string> ComputeBlake3HashAsync(string filePath, CancellationToken token)
        {
            var blake3 = Blake3Factory.Instance.Create();

            // Read file synchronously inside Task.Run
            byte[] data = await Task.Run(() => File.ReadAllBytes(filePath), token);

            var hashResult = blake3.ComputeHash(data);
            return BitConverter.ToString(hashResult.Hash).Replace("-", "").ToLowerInvariant();
        }

        private bool IsHashInDatabase(string hash)
        {
            using (var connection = new SqliteConnection($"Data Source={Settings.DatabaseFile}"))
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
            using (var connection = new SqliteConnection($"Data Source={Settings.DatabaseFile}"))
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
            using (var connection = new SqliteConnection($"Data Source={Settings.DatabaseFile}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "UPDATE FileHashes SET Processed = 1 WHERE Hash = $hash";
                command.Parameters.AddWithValue("$hash", hash);
                command.ExecuteNonQuery();
            }
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(Settings.DatabaseFile))
            {
                using (var connection = new SqliteConnection($"Data Source={Settings.DatabaseFile}"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS FileHashes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Hash TEXT NOT NULL UNIQUE,
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
            // Apply settings to the file poll monitor

            Settings.PollIntervalMilliseconds = ApplySetting<int>("PollIntervalMilliseconds");
            Settings.TargetPath = ApplySetting<string>("TargetPath");
            Settings.DatabaseFile = ApplySetting<string>("DatabaseFile");

            // Convert List<string> to HashSet<string> to fix the type mismatch
            var allowedExtensionsList = ApplySetting<List<string>>("AllowedExtensions");
            Settings.AllowedExtensions = allowedExtensionsList != null
                ? new HashSet<string>(allowedExtensionsList)
                : null;
        }

        public T ApplySetting<T>(string key)
        {
            return jsonLoader.GetSetting<T>(key, ImporterModule.ImporterInstance.TypeSettings);
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
