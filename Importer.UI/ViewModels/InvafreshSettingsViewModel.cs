using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;

namespace Importer.UI.ViewModels
{
    public class InvafreshKeyValueEntry : BindableBase
    {
        private string _key;
        private string _value;
        public string Key { get => _key; set => SetProperty(ref _key, value); }
        public string Value { get => _value; set => SetProperty(ref _value, value); }
    }

    public class InvafreshSettingsViewModel : BindableBase
    {
        private const string SettingsDirRelative = "./Settings"; // relative to base dir
        private const string SettingsFileName = "Invafresh.Settings.json";

        private int _tareDigits;
        private bool _useCustomMapping;
        private bool _useLegacyNutritionFormat;
        private string _customMapFileName;

        public int TareDigits { get => _tareDigits; set => SetProperty(ref _tareDigits, value); }
        public bool UseCustomMapping { get => _useCustomMapping; set { if (SetProperty(ref _useCustomMapping, value)) RaisePropertyChanged(nameof(IsCustomMapVisible)); } }
        public bool UseLegacyNutritionFormat { get => _useLegacyNutritionFormat; set => SetProperty(ref _useLegacyNutritionFormat, value); }
        public string CustomMapFileName { get => _customMapFileName; set => SetProperty(ref _customMapFileName, value); }

        public bool IsCustomMapVisible => UseCustomMapping;

        public ObservableCollection<InvafreshKeyValueEntry> CustomMapEntries { get; } = new ObservableCollection<InvafreshKeyValueEntry>();
        public InvafreshKeyValueEntry SelectedCustomMapEntry { get; set; }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ReloadCommand { get; }
        public DelegateCommand AddCustomMapEntryCommand { get; }
        public DelegateCommand RemoveSelectedCustomMapEntryCommand { get; }

        private string _baseSettingsDirectory;

        public InvafreshSettingsViewModel()
        {
            SaveCommand = new DelegateCommand(SaveAll);
            ReloadCommand = new DelegateCommand(LoadAll);
            AddCustomMapEntryCommand = new DelegateCommand(() => CustomMapEntries.Add(new InvafreshKeyValueEntry()));
            RemoveSelectedCustomMapEntryCommand = new DelegateCommand(() =>
            {
                if (SelectedCustomMapEntry != null)
                {
                    CustomMapEntries.Remove(SelectedCustomMapEntry);
                    SelectedCustomMapEntry = null;
                }
            });

            ResolveSettingsDirectory();
            LoadAll();
        }

        private void ResolveSettingsDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, SettingsDirRelative.Replace('/', Path.DirectorySeparatorChar)),
                Path.GetFullPath(Path.Combine(baseDir, "..", SettingsDirRelative.Replace('/', Path.DirectorySeparatorChar)))
            };
            _baseSettingsDirectory = candidates.FirstOrDefault(Directory.Exists) ?? candidates.Last();
        }

        private string PathFor(string fileName) => Path.Combine(_baseSettingsDirectory, fileName);

        private void LoadAll()
        {
            LoadSettings();
            LoadCustomMap();
        }

        private void LoadSettings()
        {
            try
            {
                var path = PathFor(SettingsFileName);
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var model = JsonConvert.DeserializeObject<InvafreshSettingsModel>(json);
                if (model == null) return;
                TareDigits = model.TareDigits;
                UseCustomMapping = model.UseCustomMapping;
                UseLegacyNutritionFormat = model.UseLegacyNutritionFormat;
                CustomMapFileName = model.CustomMapFileName;
            }
            catch { }
        }

        private void LoadCustomMap()
        {
            CustomMapEntries.Clear();
            try
            {
                if (!UseCustomMapping || string.IsNullOrWhiteSpace(CustomMapFileName)) return;
                var path = PathFor(CustomMapFileName);
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(json);
                if (dict == null) return;
                foreach (var kvp in dict.Where(k => k.Key != null))
                {
                    CustomMapEntries.Add(new InvafreshKeyValueEntry { Key = kvp.Key, Value = kvp.Value });
                }
            }
            catch { }
        }

        private void SaveAll()
        {
            Directory.CreateDirectory(_baseSettingsDirectory);
            SaveSettings();
            SaveCustomMap();
        }

        private void SaveSettings()
        {
            try
            {
                var path = PathFor(SettingsFileName);
                var model = new InvafreshSettingsModel
                {
                    TareDigits = TareDigits,
                    UseCustomMapping = UseCustomMapping,
                    UseLegacyNutritionFormat = UseLegacyNutritionFormat,
                    CustomMapFileName = CustomMapFileName
                };
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private void SaveCustomMap()
        {
            if (!UseCustomMapping || string.IsNullOrWhiteSpace(CustomMapFileName)) return;
            try
            {
                var path = PathFor(CustomMapFileName);
                var dict = CustomMapEntries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                    .GroupBy(e => e.Key)
                    .Select(g => g.Last())
                    .ToDictionary(e => e.Key, e => e.Value ?? string.Empty);
                var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private class InvafreshSettingsModel
        {
            public int TareDigits { get; set; }
            public bool UseCustomMapping { get; set; }
            public bool UseLegacyNutritionFormat { get; set; }
            public string CustomMapFileName { get; set; }
        }
    }
}
