using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace Importer.UI.ViewModels
{
    public class ImporterSettingsViewModel : BindableBase
    {
        private const string SettingsFileRelativePath = "Importer.Common/Settings/settings.json"; // adjust if needed

        // Backing fields
        private bool _testingEnabled;
        private string _deptColName;
        private bool _buttonSameAsDesc;
        private bool _flush;
        private bool _properCase;
        private bool _departmentPLU;
        private bool _descriptionCleanse;
        private int _holdDataFilesDays;

        private string _properCaseFieldsText;
        private string _allCapsFieldsText;
        private string _findReplaceFieldsText;

        public bool TestingEnabled { get => _testingEnabled; set => SetProperty(ref _testingEnabled, value); }
        public string DeptColName { get => _deptColName; set => SetProperty(ref _deptColName, value); }
        public bool ButtonSameAsDesc { get => _buttonSameAsDesc; set => SetProperty(ref _buttonSameAsDesc, value); }
        public bool Flush { get => _flush; set => SetProperty(ref _flush, value); }
        public bool ProperCase { get => _properCase; set => SetProperty(ref _properCase, value); }
        public bool DepartmentPLU { get => _departmentPLU; set => SetProperty(ref _departmentPLU, value); }
        public bool DescriptionCleanse { get => _descriptionCleanse; set => SetProperty(ref _descriptionCleanse, value); }
        public int HoldDataFilesDays { get => _holdDataFilesDays; set => SetProperty(ref _holdDataFilesDays, value); }

        public string ProperCaseFieldsText { get => _properCaseFieldsText; set => SetProperty(ref _properCaseFieldsText, value); }
        public string AllCapsFieldsText { get => _allCapsFieldsText; set => SetProperty(ref _allCapsFieldsText, value); }
        public string FindReplaceFieldsText { get => _findReplaceFieldsText; set => SetProperty(ref _findReplaceFieldsText, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ReloadCommand { get; }

        private string _settingsFileFullPath;

        public ImporterSettingsViewModel()
        {
            SaveCommand = new DelegateCommand(SaveSettings);
            ReloadCommand = new DelegateCommand(LoadSettings);
            ResolveSettingsPath();
            LoadSettings();
        }

        private void ResolveSettingsPath()
        {
            // Attempt to build an absolute path based on current domain base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Try typical relative locations
            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "Settings", "settings.json"),
                Path.Combine(baseDir, SettingsFileRelativePath.Replace('/', Path.DirectorySeparatorChar)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "Importer.Common", "Settings", "settings.json"))
            };

            _settingsFileFullPath = candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths.Last();
        }

        private void LoadSettings()
        {
            if (!File.Exists(_settingsFileFullPath))
            {
                return;
            }

            var json = File.ReadAllText(_settingsFileFullPath);
            var model = JsonConvert.DeserializeObject<RootSettings>(json);
            if (model == null) return;

            TestingEnabled = model.TestingEnabled;
            DeptColName = model.DeptColName;
            ButtonSameAsDesc = model.ButtonSameAsDesc;
            Flush = model.Flush;
            ProperCase = model.ProperCase;
            DepartmentPLU = model.DepartmentPLU;
            DescriptionCleanse = model.DescriptionCleanse;
            HoldDataFilesDays = model.HoldDataFilesDays;
            ProperCaseFieldsText = string.Join(Environment.NewLine, model.ProperCaseFields ?? new List<string>());
            AllCapsFieldsText = string.Join(Environment.NewLine, model.AllCapsFields ?? new List<string>());
            FindReplaceFieldsText = string.Join(Environment.NewLine, model.FindReplaceFields ?? new List<string>());
        }

        private void SaveSettings()
        {
            var model = new RootSettings
            {
                TestingEnabled = TestingEnabled,
                DeptColName = DeptColName,
                ButtonSameAsDesc = ButtonSameAsDesc,
                Flush = Flush,
                ProperCase = ProperCase,
                DepartmentPLU = DepartmentPLU,
                DescriptionCleanse = DescriptionCleanse,
                HoldDataFilesDays = HoldDataFilesDays,
                ProperCaseFields = SplitLines(ProperCaseFieldsText),
                AllCapsFields = SplitLines(AllCapsFieldsText),
                FindReplaceFields = SplitLines(FindReplaceFieldsText)
            };

            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFileFullPath));
            File.WriteAllText(_settingsFileFullPath, json);
        }

        private List<string> SplitLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();
        }

        private class RootSettings
        {
            public bool TestingEnabled { get; set; }
            public string DeptColName { get; set; }
            public bool ButtonSameAsDesc { get; set; }
            public bool Flush { get; set; }
            public bool ProperCase { get; set; }
            public bool DepartmentPLU { get; set; }
            public bool DescriptionCleanse { get; set; }
            public int HoldDataFilesDays { get; set; }
            public List<string> ProperCaseFields { get; set; }
            public List<string> AllCapsFields { get; set; }
            public List<string> FindReplaceFields { get; set; }
        }
    }
}
