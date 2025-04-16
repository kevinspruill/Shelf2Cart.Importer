using Data.HashFunction.Blake3;
using Importer.Common.Interfaces;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public FilePollMonitor(string targetPath, TimeSpan pollInterval, string databaseFile = "filehashes.db", IEnumerable<string> allowedExtensions = null)
        {
            _targetPath = targetPath;
            _pollInterval = pollInterval != TimeSpan.Zero ? pollInterval : TimeSpan.FromMilliseconds(150);
            _databaseFile = databaseFile;
            _isDirectory = Directory.Exists(targetPath);
            _allowedExtensions = allowedExtensions != null ? new HashSet<string>(allowedExtensions.Select(e => e.ToLowerInvariant())) : null;

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
            Console.WriteLine($"[Monitor] Started monitoring {_targetPath}");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_isDirectory)
                    {
                        var files = Directory.GetFiles(_targetPath);
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
                    Console.WriteLine($"Directory not found: {_targetPath}. Attempting recovery in {RecoveryDelayMilliseconds}ms");
                    await Task.Delay(RecoveryDelayMilliseconds);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"IO exception encountered: {ex.Message}. Attempting recovery in {RecoveryDelayMilliseconds}ms");
                    await Task.Delay(RecoveryDelayMilliseconds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitor loop error: {ex.Message}");
                    await Task.Delay(RecoveryDelayMilliseconds);
                }
            }

            Console.WriteLine("Stopped.");
        }

        private async Task CheckFileAsync(string filePath, CancellationToken token)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                if (_allowedExtensions != null && !_allowedExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()))
                    return;

                var info = new FileInfo(filePath);
                var snapshot = new FileSnapshot(info.Length, info.LastWriteTimeUtc);

                FileSnapshot lastSnapshot;
                if (_fileSnapshots.TryGetValue(filePath, out lastSnapshot) && lastSnapshot.Equals(snapshot))
                {
                    return; // No changes detected
                }

                _fileSnapshots[filePath] = snapshot;

                string hash = await RetryAsync(() => ComputeBlake3HashAsync(filePath, token), MaxRetryCount, RetryDelayMilliseconds, token);

                if (IsHashInDatabase(hash))
                {
                    Console.WriteLine($"Already processed: {filePath}");
                    return;
                }

                InsertHashIntoDatabase(hash, filePath);

                Console.WriteLine($"Processing file: {filePath}");
                await ProcessFileAsync(filePath, token);
                MarkHashAsProcessed(hash);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File in use, skipping for now: {filePath} ({ex.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {filePath}: {ex.Message}");
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

            // Read file in sync inside Task.Run
            byte[] data = await Task.Run(() => File.ReadAllBytes(filePath), token);

            // Compute hash synchronously
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
            throw new NotImplementedException();
        }

        public List<string> GetSettingNames()
        {
            throw new NotImplementedException();
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
