using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Importer.Common.ImporterTypes
{
    public class FileWatcher : IImporterType
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileFilter { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
        public IImporterModule ImporterModule { get; set; } = null;

        private FileSystemWatcher fileWatcher;
        private Dictionary<string, DateTime> fileLastEventTimes = new Dictionary<string, DateTime>();
        private readonly TimeSpan eventThreshold = TimeSpan.FromSeconds(1);
        
        // Add a queue for file processing and processing state
        private Queue<string> fileProcessingQueue = new Queue<string>();
        private bool isProcessing = false;
        private object queueLock = new object();

        public FileWatcher(IImporterModule importerModule)
        {
            ImporterModule = importerModule;
        }

        public void InitializeFileWatcher()
        {
            fileWatcher = new FileSystemWatcher();
            fileWatcher.Path = FilePath;
            fileWatcher.Filter = FileName + FileFilter;
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fileWatcher.Changed += OnFileChangedOrCreated;
            fileWatcher.Created += OnFileChangedOrCreated;
            fileWatcher.Renamed += OnFileChangedOrCreated;
        }
        public bool ToggleFileWatcher()
        {
            // Check if the file watcher is already enabled
            fileWatcher.EnableRaisingEvents = !fileWatcher.EnableRaisingEvents;

            // Log the status
            Logger.Info($"File watcher {(fileWatcher.EnableRaisingEvents ? "started" : "stopped")} for path: {FilePath}");

            return fileWatcher.EnableRaisingEvents;
        }
        public void ApplySettings(Dictionary<string, object> settings) 
        {
            Settings = settings;

            // Apply settings to the file watcher
            if (settings.ContainsKey("FilePath"))
            {
                FilePath = settings["FilePath"].ToString();
            }
            if (settings.ContainsKey("FileName"))
            {
                FileName = settings["FileName"].ToString();
            }
            if (settings.ContainsKey("FileFilter"))
            {
                FileFilter = settings["FileFilter"].ToString();
            }
        }
        
        public void OnFileChangedOrCreated(object sender, FileSystemEventArgs e)
        {
            // get the file path
            string filePath = e.FullPath;
            
            // Check if this file has been recently processed
            bool shouldProcess = true;
            if (fileLastEventTimes.ContainsKey(filePath))
            {
                // Check if the event is within the threshold for this specific file
                if (DateTime.Now - fileLastEventTimes[filePath] <= eventThreshold)
                {
                    shouldProcess = false;
                }
            }
            
            if (shouldProcess)
            {
                // Update the last event time for this file
                fileLastEventTimes[filePath] = DateTime.Now;
                
                // Check if the file exists
                if (File.Exists(filePath))
                {
                    // Read the file content
                    string fileContent = ReadFileContent(filePath);
                    if (fileContent != null)
                    {
                        // Add the file to the processing queue
                        EnqueueFile(filePath);
                    }
                    else
                    {
                        // Handle error reading file
                        Logger.Error($"Error reading file: {filePath}");
                    }
                }
                else
                {
                    // Handle file not found
                    Logger.Error($"File not found: {filePath}");
                }
            }
        }
        
        // Add file to processing queue and start processing if not already active
        private void EnqueueFile(string filePath)
        {
            lock (queueLock)
            {
                // Add the file to the queue
                fileProcessingQueue.Enqueue(filePath);
                Logger.Info($"Added file to processing queue: {filePath}. Queue size: {fileProcessingQueue.Count}");
                
                // If we're not currently processing, start processing the queue
                if (!isProcessing)
                {
                    isProcessing = true;
                    // Start processing the queue asynchronously
                    Task.Run(() => ProcessQueue());
                }
            }
        }
        
        // Process files in the queue one at a time
        private void ProcessQueue()
        {
            while (true)
            {
                string fileToProcess = null;
                
                // Get the next file to process from the queue under a lock
                lock (queueLock)
                {
                    if (fileProcessingQueue.Count > 0)
                    {
                        fileToProcess = fileProcessingQueue.Dequeue();
                        Logger.Info($"Dequeued file for processing: {fileToProcess}. Remaining in queue: {fileProcessingQueue.Count}");
                    }
                    else
                    {
                        // No more files to process, exit the processing loop
                        isProcessing = false;
                        Logger.Info("File processing queue is empty, stopping processor");
                        break;
                    }
                }
                
                // Process the file outside the lock
                if (fileToProcess != null)
                {
                    try
                    {
                        Logger.Info($"Processing file: {fileToProcess}");
                        // send the filepath to the importer module
                        ImporterModule.ImporterTypeData = fileToProcess;
                        // Trigger the Product Processor
                        ImporterModule.TriggerProcess();
                        Logger.Info($"Completed processing file: {fileToProcess}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error processing file {fileToProcess}: {ex.Message}");
                    }
                }
            }
        }
        
        public string ReadFileContent(string filePath)
        {
            int retryCount = 0;
            const int maxRetries = 5;
            const int retryDelayMs = 500;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Try to open the file with exclusive access
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        // If we got here, we have exclusive access
                        return reader.ReadToEnd();
                    }
                }
                catch (IOException ex) when ((ex is IOException) &&
                    (ex.Message.Contains("being used by another process") ||
                     ex.Message.Contains("access is denied")))
                {
                    // File is in use, retry after a short delay
                    retryCount++;
                    Logger.Info($"File {filePath} is in use. Retry attempt {retryCount} of {maxRetries}.");

                    if (retryCount < maxRetries)
                    {
                        // Wait before retrying
                        System.Threading.Thread.Sleep(retryDelayMs);
                    }
                    else
                    {
                        Logger.Error($"Failed to access file {filePath} after {maxRetries} attempts. File may be locked by another process.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Logger.Error($"Error reading file {filePath}: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        public int GetQueuedFileCount()
        {
            lock (queueLock)
            {
                return fileProcessingQueue.Count;
            }
        }
    }
}
