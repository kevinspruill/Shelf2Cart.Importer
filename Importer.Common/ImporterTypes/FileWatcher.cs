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

        private FileSystemWatcher fileWatcher;
        private DateTime lastEventTime;
        private readonly TimeSpan eventThreshold = TimeSpan.FromSeconds(1);

        public void InitializeFileWatcher()
        {
            fileWatcher = new FileSystemWatcher();
            fileWatcher.Path = FilePath;
            fileWatcher.Filter = FileName + FileFilter;
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fileWatcher.Changed += OnFileChangedOrCreated;
            fileWatcher.Created += OnFileChangedOrCreated;
        }
        public bool ToggleFileWatcher()
        {
            // Check if the file watcher is already enabled
            fileWatcher.EnableRaisingEvents = !fileWatcher.EnableRaisingEvents;

            return fileWatcher.EnableRaisingEvents;
        }
        public void ApplySettings(Dictionary<string, object> settings) 
        {
            Settings = settings;
        }
        public void OnFileChangedOrCreated(object sender, FileSystemEventArgs e)
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
