using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;

namespace Importer.UI.ViewModels
{
    public class KeyValueEntry : BindableBase
    {
        private string _key;
        private string _value;
        public string Key { get => _key; set => SetProperty(ref _key, value); }
        public string Value { get => _value; set => SetProperty(ref _value, value); }
    }

    public class GenericSettingsViewModel : BindableBase
    {
        private const string SettingsDirRelative = "./Settings"; // relative to solution root / base dir
        private const string SettingsFileName = "Generic.Settings.json";
        private const string FieldMapFileName = "Generic.FieldMap.json";
        private const string BooleanValsFileName = "Generic.BooleanVals.json";

        private string _fieldDelimiter;
        private string _parser;
        private string _recordSeparator;

        public string FieldDelimiter { get => _fieldDelimiter; set => SetProperty(ref _fieldDelimiter, value); }
        public string Parser { get => _parser; set => SetProperty(ref _parser, value); }
        public string RecordSeparator { get => _recordSeparator; set => SetProperty(ref _recordSeparator, value); }

        public ObservableCollection<KeyValueEntry> FieldMapEntries { get; } = new ObservableCollection<KeyValueEntry>();
        public ObservableCollection<KeyValueEntry> BooleanValEntries { get; } = new ObservableCollection<KeyValueEntry>();

        private KeyValueEntry _selectedFieldMapEntry;
        public KeyValueEntry SelectedFieldMapEntry { get => _selectedFieldMapEntry; set => SetProperty(ref _selectedFieldMapEntry, value); }

        private KeyValueEntry _selectedBooleanValEntry;
        public KeyValueEntry SelectedBooleanValEntry { get => _selectedBooleanValEntry; set => SetProperty(ref _selectedBooleanValEntry, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ReloadCommand { get; }
        public DelegateCommand AddFieldMapEntryCommand { get; }
        public DelegateCommand RemoveSelectedFieldMapEntryCommand { get; }
        public DelegateCommand AddBooleanValEntryCommand { get; }
        public DelegateCommand RemoveSelectedBooleanValEntryCommand { get; }

        private string _baseSettingsDirectory;

        public GenericSettingsViewModel()
        {
            SaveCommand = new DelegateCommand(SaveAll);
            ReloadCommand = new DelegateCommand(LoadAll);
            AddFieldMapEntryCommand = new DelegateCommand(() => FieldMapEntries.Add(new KeyValueEntry()));
            RemoveSelectedFieldMapEntryCommand = new DelegateCommand(() =>
            {
                if (SelectedFieldMapEntry != null)
                {
                    FieldMapEntries.Remove(SelectedFieldMapEntry);
                    SelectedFieldMapEntry = null;
                }
            });
            AddBooleanValEntryCommand = new DelegateCommand(() => BooleanValEntries.Add(new KeyValueEntry()));
            RemoveSelectedBooleanValEntryCommand = new DelegateCommand(() =>
            {
                if (SelectedBooleanValEntry != null)
                {
                    BooleanValEntries.Remove(SelectedBooleanValEntry);
                    SelectedBooleanValEntry = null;
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
            LoadFieldMap();
            LoadBooleanVals();
        }

        private void LoadSettings()
        {
            try
            {
                var path = PathFor(SettingsFileName);
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var model = JsonConvert.DeserializeObject<GenericSettingsModel>(json);
                if (model == null) return;
                FieldDelimiter = model.FieldDelimiter;
                Parser = model.Parser;
                RecordSeparator = model.RecordSeparator;
            }
            catch { /* swallow for now or log */ }
        }

        private void LoadFieldMap()
        {
            FieldMapEntries.Clear();
            try
            {
                var path = PathFor(FieldMapFileName);
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                // Deserialize to dictionary-like object
                var dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(json);
                if (dict == null) return;
                foreach (var kvp in dict.Where(k => k.Key != null))
                {
                    FieldMapEntries.Add(new KeyValueEntry { Key = kvp.Key, Value = kvp.Value });
                }
            }
            catch { }
        }

        private void LoadBooleanVals()
        {
            BooleanValEntries.Clear();
            try
            {
                var path = PathFor(BooleanValsFileName);
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(json);
                if (dict == null) return;
                foreach (var kvp in dict.Where(k => k.Key != null))
                {
                    BooleanValEntries.Add(new KeyValueEntry { Key = kvp.Key, Value = kvp.Value });
                }
            }
            catch { }
        }

        private void SaveAll()
        {
            Directory.CreateDirectory(_baseSettingsDirectory);
            SaveSettings();
            SaveFieldMap();
            SaveBooleanVals();
        }

        private void SaveSettings()
        {
            try
            {
                var path = PathFor(SettingsFileName);
                var model = new GenericSettingsModel
                {
                    FieldDelimiter = FieldDelimiter,
                    Parser = Parser,
                    RecordSeparator = RecordSeparator
                };
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private void SaveFieldMap()
        {
            try
            {
                var path = PathFor(FieldMapFileName);
                var dict = FieldMapEntries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                    .GroupBy(e => e.Key)
                    .Select(g => g.Last())
                    .ToDictionary(e => e.Key, e => e.Value ?? string.Empty);
                var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private void SaveBooleanVals()
        {
            try
            {
                var path = PathFor(BooleanValsFileName);
                var dict = BooleanValEntries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                    .GroupBy(e => e.Key)
                    .Select(g => g.Last())
                    .ToDictionary(e => e.Key, e => e.Value ?? string.Empty);
                var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private class GenericSettingsModel
        {
            public string FieldDelimiter { get; set; }
            public string Parser { get; set; }
            public string RecordSeparator { get; set; }
        }
    }
}
