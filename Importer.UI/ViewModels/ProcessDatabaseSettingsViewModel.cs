using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace Importer.UI.ViewModels
{
    public class ProcessDatabaseSettingsViewModel : BindableBase
    {
        private const string SettingsFileRelativePath = "./Settings/ProcessDatabaseSettings.json";
        private string _settingsFileFullPath;

        private string _residentDatabase;
        private string _processDatabase;
        private string _importDatabase;
        private string _adminConsoleDatabase;
        private string _baseDatabase;

        private bool _useAdminConsoleDatabase;
        private bool _useBaseDatabase;
        private bool _importTables;
        private bool _importLocalEdits;
        private bool _keepLocalItems;
        private bool _deleteOrphanItems;
        private bool _useLegacy;

        public string ResidentDatabase { get => _residentDatabase; set => SetProperty(ref _residentDatabase, value); }
        public string ProcessDatabase { get => _processDatabase; set => SetProperty(ref _processDatabase, value); }
        public string ImportDatabase { get => _importDatabase; set => SetProperty(ref _importDatabase, value); }
        public string AdminConsoleDatabase { get => _adminConsoleDatabase; set => SetProperty(ref _adminConsoleDatabase, value); }
        public string BaseDatabase { get => _baseDatabase; set => SetProperty(ref _baseDatabase, value); }

        public bool UseAdminConsoleDatabase { get => _useAdminConsoleDatabase; set => SetProperty(ref _useAdminConsoleDatabase, value); }
        public bool UseBaseDatabase { get => _useBaseDatabase; set => SetProperty(ref _useBaseDatabase, value); }
        public bool ImportTables { get => _importTables; set => SetProperty(ref _importTables, value); }
        public bool ImportLocalEdits { get => _importLocalEdits; set => SetProperty(ref _importLocalEdits, value); }
        public bool KeepLocalItems { get => _keepLocalItems; set => SetProperty(ref _keepLocalItems, value); }
        public bool DeleteOrphanItems { get => _deleteOrphanItems; set => SetProperty(ref _deleteOrphanItems, value); }
        public bool UseLegacy { get => _useLegacy; set =>SetProperty(ref _useLegacy, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ReloadCommand { get; }
        public DelegateCommand BrowseResidentCommand { get; }
        public DelegateCommand BrowseProcessCommand { get; }
        public DelegateCommand BrowseImportCommand { get; }
        public DelegateCommand BrowseAdminConsoleCommand { get; }
        public DelegateCommand BrowseBaseDbCommand { get; }

        public ProcessDatabaseSettingsViewModel()
        {
            SaveCommand = new DelegateCommand(SaveSettings);
            ReloadCommand = new DelegateCommand(LoadSettings);
            BrowseResidentCommand = new DelegateCommand(() => ResidentDatabase = BrowseForPath(ResidentDatabase));
            BrowseProcessCommand = new DelegateCommand(() => ProcessDatabase = BrowseForPath(ProcessDatabase));
            BrowseImportCommand = new DelegateCommand(() => ImportDatabase = BrowseForPath(ImportDatabase));
            BrowseAdminConsoleCommand = new DelegateCommand(() => AdminConsoleDatabase = BrowseForPath(AdminConsoleDatabase));
            BrowseBaseDbCommand = new DelegateCommand(() => BaseDatabase = BrowseForPath(BaseDatabase));
            ResolveSettingsPath();
            LoadSettings();
        }

        private void ResolveSettingsPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "Settings", "ProcessDatabaseSettings.json"),
                Path.Combine(baseDir, SettingsFileRelativePath.Replace('/', Path.DirectorySeparatorChar)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "Importer.Common", "Settings", "ProcessDatabaseSettings.json"))
            };
            _settingsFileFullPath = candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths.Last();
        }

        private void LoadSettings()
        {
            if (!File.Exists(_settingsFileFullPath)) return;
            var json = File.ReadAllText(_settingsFileFullPath);
            var model = JsonConvert.DeserializeObject<ProcessDbSettingsModel>(json);
            if (model == null) return;

            ResidentDatabase = model.ResidentDatabase;
            ProcessDatabase = model.ProcessDatabase;
            ImportDatabase = model.ImportDatabase;
            AdminConsoleDatabase = model.AdminConsoleDatabase;
            BaseDatabase = model.BaseDatabase;
            UseAdminConsoleDatabase = model.UseAdminConsoleDatabase;
            UseBaseDatabase = model.UseBaseDatabase;
            ImportTables = model.ImportTables;
            ImportLocalEdits = model.ImportLocalEdits;
            DeleteOrphanItems = model.DeleteOrphanItems;
            KeepLocalItems = model.KeepLocalItems;

            UseLegacy = model.UseLegacy;
        }

        private void SaveSettings()
        {
            var model = new ProcessDbSettingsModel
            {
                ResidentDatabase = ResidentDatabase,
                ProcessDatabase = ProcessDatabase,
                ImportDatabase = ImportDatabase,
                AdminConsoleDatabase = AdminConsoleDatabase,
                BaseDatabase = BaseDatabase,
                UseAdminConsoleDatabase = UseAdminConsoleDatabase,
                UseBaseDatabase = UseBaseDatabase,
                ImportTables = ImportTables,
                ImportLocalEdits = ImportLocalEdits,
                KeepLocalItems = KeepLocalItems,
                DeleteOrphanItems = DeleteOrphanItems,
                UseLegacy = UseLegacy
            };
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFileFullPath));
            File.WriteAllText(_settingsFileFullPath, json);
        }

        private string BrowseForPath(string currentValue)
        {
            var dlg = new OpenFileDialog
            {
                CheckFileExists = false,
                FileName = string.IsNullOrWhiteSpace(currentValue) ? string.Empty : Path.GetFileName(currentValue),
                InitialDirectory = TryGetInitialDir(currentValue),
                Filter = "Database Files (*.mdb;*.accdb;*.db)|*.mdb;*.accdb;*.db|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                return dlg.FileName;
            }
            return currentValue;
        }

        private string TryGetInitialDir(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    var dir = Path.GetDirectoryName(path);
                    if (Directory.Exists(dir)) return dir;
                }
            }
            catch { }
            return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        private class ProcessDbSettingsModel
        {
            public string ResidentDatabase { get; set; }
            public string ProcessDatabase { get; set; }
            public string ImportDatabase { get; set; }
            public string AdminConsoleDatabase { get; set; }
            public string BaseDatabase { get; set; }
            public bool UseAdminConsoleDatabase { get; set; }
            public bool UseBaseDatabase { get; set; }
            public bool ImportTables { get; set; }
            public bool ImportLocalEdits { get; set; }
            public bool KeepLocalItems { get; set; }
            public bool DeleteOrphanItems { get; set; }
            public bool UseLegacy { get; set; }
        }
    }
}
