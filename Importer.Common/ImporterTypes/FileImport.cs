using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.ImporterTypes
{
    public class FileImport
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }

        private FileSystemWatcher fileWatcher;
        private DateTime lastEventTime;
        private readonly TimeSpan eventThreshold = TimeSpan.FromSeconds(1);

        public FileImport(IImporterModule importerModule)
        {
            // determine if importerModule.TriggerValue is a file path or a directory
            if (Directory.Exists(importerModule.TriggerValue))
            {
                FilePath = importerModule.TriggerValue;
                FileName = "*";
                FileExtension = ".*";
            }
            else if (File.Exists(importerModule.TriggerValue))
            {
                FilePath = Path.GetDirectoryName(importerModule.TriggerValue);
                FileName = Path.GetFileNameWithoutExtension(importerModule.TriggerValue);
                FileExtension = Path.GetExtension(importerModule.TriggerValue);
            }
            else
            {
                throw new ArgumentException("TriggerValue must be a valid file path or directory.");
            }   

            InitializeFileWatcher();
        }

        private void InitializeFileWatcher()
        {
            fileWatcher = new FileSystemWatcher();
            fileWatcher.Path = FilePath;
            fileWatcher.Filter = FileName + FileExtension;
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fileWatcher.Changed += OnFileChangedOrCreated;
            fileWatcher.Created += OnFileChangedOrCreated;
        }

        public void StartWatching()
        {
            // Start the file watchers
            fileWatcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            // Stop the file watchers
            fileWatcher.EnableRaisingEvents = false;
        }

        private void OnFileChangedOrCreated(object sender, FileSystemEventArgs e)
        {
            // Check if the event is within the threshold
            if (DateTime.Now - lastEventTime > eventThreshold)
            {
                lastEventTime = DateTime.Now;
                // Handle the event
            }
        }

        public string ReadFileContent(string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                //throw new Exception($"Error reading file: {ex.Message}");
                return null;
            }
        }
    }
}
