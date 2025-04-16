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
    public class FilePollMonitor : IDisposable, IImporterType
    {
        public string Name { get; set; } = "FilePollMonitor";
        public Dictionary<string, object> Settings { get; set; }

        private readonly string _targetPath;
        private readonly bool _isDirectory;
        private readonly TimeSpan _pollInterval;
        private readonly string _databaseFile;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, FileSnapshot> _fileSnapshots = new ConcurrentDictionary<string, FileSnapshot>();
        private readonly HashSet<string> _allowedExtensions;
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

        public FilePollMonitor(string targetPath, TimeSpan pollInterval, string databaseFile = "filehashes.db", IEnumerable<string> allowedExtensions = null)
        {
            _targetPath = targetPath;
            _pollInterval = pollInterval != TimeSpan.Zero ? pollInterval : TimeSpan.FromMilliseconds(150);
            _databaseFile = databaseFile;
            _isDirectory = Directory.Exists(targetPath);
            _allowedExtensions = allowedExtensions != null ? new HashSet<string>(allowedExtensions.Select(e => e.ToLowerInvariant())) : null;

            // Determine the base path. If monitoring a single file, use its parent directory.
            string basePath = _isDirectory ? _targetPath : Path.GetDirectoryName(_targetPath);

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
            Task.Run(() => MonitorLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            Logger.Info($"Started monitoring {_targetPath}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_isDirectory)
                    {
                        var files = Directory.GetFiles(_targetPath)
                            .Select(path => new { Path = path, LastModified = File.GetLastWriteTimeUtc(path) })
                            .OrderBy(fileInfo => fileInfo.LastModified) // Orders in ascending order (oldest first)
                            .Select(fileInfo => fileInfo.Path)
                            .ToList();
                        var tasks = files.Select(file => CheckFileAsync(file, token)).ToArray();
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        await CheckFileAsync(_targetPath, token);
                    }

                    await Task.Delay(_pollInterval);
                }
                catch (DirectoryNotFoundException ex)
                {
                    Logger.Error($"Directory not found: {_targetPath}. Attempting recovery in {RecoveryDelayMilliseconds}ms");
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
                if (_allowedExtensions != null && !_allowedExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
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
            using (var connection = new SqliteConnection($"Data Source={_databaseFile}"))
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
            using (var connection = new SqliteConnection($"Data Source={_databaseFile}"))
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
            using (var connection = new SqliteConnection($"Data Source={_databaseFile}"))
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
            if (!File.Exists(_databaseFile))
            {
                using (var connection = new SqliteConnection($"Data Source={_databaseFile}"))
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
        /// The processing method for an individual file. Adjust the file processing logic here as needed.
        /// </summary>
        private Task ProcessFileAsync(string filePath, CancellationToken token)
        {
            Console.WriteLine($"Processing: {filePath}");
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        public void ApplySettings(Dictionary<string, object> settings)
        {
            // Apply settings if needed
        }

        public List<string> GetSettingNames()
        {
            return new List<string>();
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
