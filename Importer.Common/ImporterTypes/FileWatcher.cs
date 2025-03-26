using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private DateTime lastEventTime;
        private readonly TimeSpan eventThreshold = TimeSpan.FromSeconds(1);

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
            // Check if the event is within the threshold
            if (DateTime.Now - lastEventTime > eventThreshold)
            {
                lastEventTime = DateTime.Now;
                // get the file path
                string filePath = e.FullPath;
                // Check if the file exists
                if (File.Exists(filePath))
                {
                    // Read the file content
                    string fileContent = ReadFileContent(filePath);
                    if (fileContent != null)
                    {
                        // send the filepath to the importer module
                        ImporterModule.ImporterTypeData = filePath;
                        // Trigger the Product Processor
                        ImporterModule.TriggerProcess();
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

    }
}
