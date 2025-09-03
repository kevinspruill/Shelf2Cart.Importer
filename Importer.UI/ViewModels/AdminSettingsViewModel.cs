using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Importer.UI.ViewModels
{
    public class AdminSettingsViewModel : BindableBase
    {
        private const string SettingsDirRelative = "Settings"; // relative to base dir
        private const string SettingsFileName = "Admin.Settings.json";

        private string _baseSettingsDirectory;
        private string _settingsFileFullPath;

        private string _adminConsoleProcessingFolder;
        private string _adminConsoleFileName;

        public string AdminConsoleProcessingFolder { get => _adminConsoleProcessingFolder; set => SetProperty(ref _adminConsoleProcessingFolder, value); }
        public string AdminConsoleFileName { get => _adminConsoleFileName; set => SetProperty(ref _adminConsoleFileName, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ReloadCommand { get; }
        public DelegateCommand BrowseProcessingFolderCommand { get; }

        public AdminSettingsViewModel()
        {
            SaveCommand = new DelegateCommand(SaveSettings);
            ReloadCommand = new DelegateCommand(LoadSettings);
            BrowseProcessingFolderCommand = new DelegateCommand(BrowseProcessingFolder);

            ResolveSettingsDirectory();
            LoadSettings();
        }

        private void ResolveSettingsDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Typical locations: directly in executing directory under Settings, or one level up in project folder
            var candidateDirs = new[]
            {
                Path.Combine(baseDir, SettingsDirRelative),
                Path.GetFullPath(Path.Combine(baseDir, "..", SettingsDirRelative)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "Importer.Module.Admin", SettingsDirRelative))
            };
            _baseSettingsDirectory = candidateDirs.FirstOrDefault(Directory.Exists) ?? candidateDirs.Last();
            _settingsFileFullPath = Path.Combine(_baseSettingsDirectory, SettingsFileName);
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFileFullPath)) return;
                var json = File.ReadAllText(_settingsFileFullPath);
                var model = JsonConvert.DeserializeObject<AdminSettingsModel>(json);
                if (model == null) return;
                AdminConsoleProcessingFolder = model.AdminConsoleProcessingFolder;
                AdminConsoleFileName = model.AdminConsoleFileName;
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(_baseSettingsDirectory);
                var model = new AdminSettingsModel
                {
                    AdminConsoleProcessingFolder = AdminConsoleProcessingFolder,
                    AdminConsoleFileName = AdminConsoleFileName
                };
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(_settingsFileFullPath, json);
            }
            catch { }
        }

        private void BrowseProcessingFolder()
        {
            try
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.SelectedPath = !string.IsNullOrWhiteSpace(AdminConsoleProcessingFolder) && Directory.Exists(AdminConsoleProcessingFolder)
                        ? AdminConsoleProcessingFolder
                        : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    dlg.Description = "Select Admin Console Processing Folder";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        AdminConsoleProcessingFolder = dlg.SelectedPath;
                    }
                }
            }
            catch { }
        }

        private class AdminSettingsModel
        {
            public string AdminConsoleProcessingFolder { get; set; }
            public string AdminConsoleFileName { get; set; }
        }
    }
}
