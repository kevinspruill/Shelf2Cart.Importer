using Importer.Common.Models;
using Importer.Common.Models.TypeSettings;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Importer.UI.ViewModels
{
    public class InstanceConfigViewModel : BindableBase
    {
        private readonly ImporterInstance _model; // original reference we edit in place on Save/Save&Close

        public InstanceConfigViewModel(ImporterInstance instance)
        {
            _model = instance ?? new ImporterInstance();

            // Initialize editable fields from model
            _name = _model.Name;
            _description = _model.Description;
            _importerModule = string.IsNullOrWhiteSpace(_model.ImporterModule) ? "Generic" : _model.ImporterModule;
            _customerProcess = _model.CustomerProcess;
            _enabled = _model.Enabled;

            EnsureTypeSettingsForModule(_importerModule);
            LoadFromTypeSettings();
            LoadCustomerProcesses();

            LoadTemplates();
            Modules = new ObservableCollection<string>(new[] { "Generic", "Invafresh", "Upshop", "Parsley", "Admin", "GrocerySignage" });

            // Commands
            LoadTemplateCommand = new DelegateCommand(LoadTemplate, () => SelectedTemplate != null);
            SaveTemplateCommand = new DelegateCommand(SaveAsTemplate, () => !string.IsNullOrWhiteSpace(Name));
            ExportConfigCommand = new DelegateCommand(ExportConfig, () => true);
            ValidateCommand = new DelegateCommand(Validate, () => true);
            TestConfigCommand = new DelegateCommand(TestConfiguration, () => true);
            SaveCommand = new DelegateCommand(SaveOnly, CanSave);
            SaveCloseCommand = new DelegateCommand(SaveAndClose, CanSave);
            CancelCommand = new DelegateCommand(Cancel);
            BrowseForTargetCommand = new DelegateCommand(OnBrowseForTargetFolder);
        }

        private void LoadCustomerProcesses()
        {
            // read dlls and get all ICustomerProcess types
            var processes = new List<string> { "" }; // allow empty
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dllFiles = Directory.GetFiles(baseDir, "*.CustomerProcess.dll");
                foreach (var dll in dllFiles)
                {
                    try
                    {
                        var assembly = System.Reflection.Assembly.LoadFrom(dll);
                        var types = assembly.GetTypes().Where(t => typeof(Importer.Common.Interfaces.ICustomerProcess).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                        foreach (var type in types)
                        {
                            // get the Name property value
                            var nameProp = type.GetProperty("Name");
                            if (nameProp != null && nameProp.PropertyType == typeof(string))
                            {
                                var instance = Activator.CreateInstance(type);
                                var nameValue = nameProp.GetValue(instance) as string;
                                if (!string.IsNullOrWhiteSpace(nameValue))
                                {
                                    processes.Add(nameValue);
                                }
                            }
                        }
                    }
                    catch (Exception) { /* swallow individual dll load errors */ }
                }

                // assign to property
                CustomerProcesses = processes.Distinct().OrderBy(s => s).ToList();
            }
            catch (Exception) { /* swallow */ }
        }

        private void OnBrowseForTargetFolder()
        {
            // Browse for folder dialog - set TargetPath if selected in try/catch
            try
            {
                using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dlg.Description = "Select Target Folder";
                    dlg.ShowNewFolderButton = true;
                    // if current path valid, set as selected path
                    if (Directory.Exists(TargetPath))
                    {
                        dlg.SelectedPath = TargetPath;
                    }

                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        TargetPath = dlg.SelectedPath;
                    }
                }
            }
            catch (Exception) { /* swallow */ }

        }

        #region Basic Fields
        private string _name; public string Name { get => _name; set { SetProperty(ref _name, value); SaveCommand.RaiseCanExecuteChanged(); SaveCloseCommand.RaiseCanExecuteChanged(); } }
        private string _description; public string Description { get => _description; set => SetProperty(ref _description, value); }
        private string _importerModule; public string ImporterModule { get => _importerModule; set { if (SetProperty(ref _importerModule, value)) { EnsureTypeSettingsForModule(value); LoadFromTypeSettings(); RaiseModuleVisibility(); } } }
        private string _customerProcess; public string CustomerProcess { get => _customerProcess; set => SetProperty(ref _customerProcess, value); }
        private bool _enabled; public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }

        private List<string> _customerProcesses;
        public List<string> CustomerProcesses
        {
            get { return _customerProcesses; }
            set { SetProperty(ref _customerProcesses, value); }
        }
        #endregion

        #region FileMonitor Settings
        private string _targetPath; public string TargetPath { get => _targetPath; set => SetProperty(ref _targetPath, value); }
        private int _pollIntervalMs; public int PollIntervalMs { get => _pollIntervalMs; set => SetProperty(ref _pollIntervalMs, value); }
        private string _allowedExtensionsText; public string AllowedExtensionsText { get => _allowedExtensionsText; set => SetProperty(ref _allowedExtensionsText, value); }
        private bool _isAdminFile; public bool IsAdminFile { get => _isAdminFile; set => SetProperty(ref _isAdminFile, value); }
        private bool _useLogin; public bool UseLogin { get => _useLogin; set { if (SetProperty(ref _useLogin, value)) RaisePropertyChanged(nameof(ShowLoginSettings)); } }
        private string _loginUsername; public string LoginUsername { get => _loginUsername; set => SetProperty(ref _loginUsername, value); }
        private string _loginPassword; public string LoginPassword { get => _loginPassword; set => SetProperty(ref _loginPassword, value); }
        private string _loginDomain; public string LoginDomain { get => _loginDomain; set => SetProperty(ref _loginDomain, value); }
        #endregion

        #region Scheduler/API Settings
        private string _apiEndpoint; public string ApiEndpoint { get => _apiEndpoint; set => SetProperty(ref _apiEndpoint, value); }
        private string _apiKey; public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
        private int _apiPollIntervalHours = 1; public int ApiPollIntervalHours { get => _apiPollIntervalHours; set => SetProperty(ref _apiPollIntervalHours, value); } // currently not persisted
        #endregion

        #region Visibility Helpers
        public bool IsFileMonitorSelected => !string.Equals(ImporterModule, "Parsley", StringComparison.OrdinalIgnoreCase);
        public bool IsApiSelected => string.Equals(ImporterModule, "Parsley", StringComparison.OrdinalIgnoreCase);
        public bool ShowLoginSettings => UseLogin && IsFileMonitorSelected;

        private void RaiseModuleVisibility()
        {
            RaisePropertyChanged(nameof(IsFileMonitorSelected));
            RaisePropertyChanged(nameof(IsApiSelected));
            RaisePropertyChanged(nameof(ShowLoginSettings));
        }
        #endregion

        #region Templates
        public class TemplateEntry
        {
            public string DisplayName { get; set; }
            public ImporterInstance Instance { get; set; }
        }

        public ObservableCollection<TemplateEntry> Templates { get; } = new ObservableCollection<TemplateEntry>();
        private TemplateEntry _selectedTemplate; public TemplateEntry SelectedTemplate { get => _selectedTemplate; set { SetProperty(ref _selectedTemplate, value); LoadTemplateCommand.RaiseCanExecuteChanged(); } }
        public ObservableCollection<string> Modules { get; }

        private void LoadTemplates()
        {
            Templates.Clear();
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidateDirs = new[]
                {
                    Path.Combine(baseDir, "InstanceTemplates"),
                    Path.Combine(baseDir, "Importer.Common", "InstanceTemplates"),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "Importer.Common", "InstanceTemplates"))
                };
                var dir = candidateDirs.FirstOrDefault(Directory.Exists);
                if (dir == null) return;
                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    var json = File.ReadAllText(file);
                    var list = JsonConvert.DeserializeObject<List<ImporterInstance>>(json);
                    if (list == null) continue;
                    foreach (var inst in list)
                    {
                        Templates.Add(new TemplateEntry { DisplayName = inst.Name, Instance = inst });
                    }
                }
            }
            catch (Exception) { /* swallow */ }
        }

        private void LoadTemplate()
        {
            if (SelectedTemplate == null) return;
            var temp = SelectedTemplate.Instance;
            Name = temp.Name;
            Description = temp.Description;
            ImporterModule = temp.ImporterModule;
            Enabled = temp.Enabled;
            _model.TypeSettings = temp.TypeSettings; // copy reference ok
            LoadFromTypeSettings();
        }

        private void SaveAsTemplate()
        {
            try
            {
                var inst = BuildInstanceFromFields();
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var templateDir = Path.Combine(baseDir, "InstanceTemplates");
                Directory.CreateDirectory(templateDir);
                var file = Path.Combine(templateDir, SanitizeFileName(inst.ImporterModule) + ".custom.json");
                var json = JsonConvert.SerializeObject(new[] { inst }, Formatting.Indented);
                File.WriteAllText(file, json);
                LoadTemplates();
            }
            catch (Exception) { }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }
        #endregion

        #region Commands
        public DelegateCommand LoadTemplateCommand { get; }
        public DelegateCommand SaveTemplateCommand { get; }
        public DelegateCommand ExportConfigCommand { get; }
        public DelegateCommand ValidateCommand { get; }
        public DelegateCommand TestConfigCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand SaveCloseCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand BrowseForTargetCommand { get; }

        public event Action<bool?> RequestClose; // bool? dialog result

        private void ExportConfig()
        {
            try
            {
                var inst = BuildInstanceFromFields();
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = inst.Name + ".json",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    var json = JsonConvert.SerializeObject(inst, Formatting.Indented);
                    File.WriteAllText(dlg.FileName, json);
                }
            }
            catch (Exception) { }
        }

        private void Validate() { /* placeholder for future validation */ }
        private void TestConfiguration() { /* placeholder */ }

        private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

        private void SaveOnly()
        {
            CommitToModel();
            // stay open
        }

        private void SaveAndClose()
        {
            CommitToModel();
            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }
        #endregion

        #region Helpers
        private void EnsureTypeSettingsForModule(string module)
        {
            if (string.Equals(module, "Parsley", StringComparison.OrdinalIgnoreCase))
            {
                if (!(_model.TypeSettings is SchedulerServiceSettings))
                {
                    _model.TypeSettings = new SchedulerServiceSettings();
                }
            }
            else
            {
                if (!(_model.TypeSettings is FileMonitorSettings))
                {
                    _model.TypeSettings = new FileMonitorSettings();
                }
            }
        }

        private void LoadFromTypeSettings()
        {
            if (_model.TypeSettings is FileMonitorSettings fm)
            {
                TargetPath = fm.TargetPath;
                PollIntervalMs = fm.PollIntervalMilliseconds;
                AllowedExtensionsText = string.Join(",", fm.AllowedExtensions ?? new HashSet<string>());
                IsAdminFile = fm.IsAdminFile;
                UseLogin = fm.UseLogin;
                LoginUsername = fm.LoginUsername;
                LoginPassword = fm.LoginPassword;
                LoginDomain = fm.LoginDomain;
            }
            else if (_model.TypeSettings is SchedulerServiceSettings ss)
            {
                ApiEndpoint = ss.Endpoint;
                ApiKey = ss.ApiKey;
            }
        }

        private ImporterInstance BuildInstanceFromFields()
        {
            var inst = new ImporterInstance
            {
                Name = Name,
                Description = Description,
                ImporterModule = ImporterModule,
                CustomerProcess = CustomerProcess,
                Enabled = Enabled
            };
            if (IsFileMonitorSelected)
            {
                var fm = new FileMonitorSettings
                {
                    TargetPath = TargetPath ?? string.Empty,
                    PollIntervalMilliseconds = PollIntervalMs,
                    AllowedExtensions = new HashSet<string>((AllowedExtensionsText ?? string.Empty)
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => s.StartsWith(".")) ),
                    IsAdminFile = IsAdminFile,
                    UseLogin = UseLogin,
                    LoginUsername = LoginUsername,
                    LoginPassword = LoginPassword,
                    LoginDomain = LoginDomain
                };
                inst.TypeSettings = fm;
            }
            else
            {
                var ss = new SchedulerServiceSettings
                {
                    Endpoint = ApiEndpoint,
                    ApiKey = ApiKey
                };
                inst.TypeSettings = ss;
            }
            return inst;
        }

        private void CommitToModel()
        {
            _model.Name = Name;
            _model.Description = Description;
            _model.ImporterModule = ImporterModule;
            _model.CustomerProcess = CustomerProcess;
            _model.Enabled = Enabled;

            if (IsFileMonitorSelected)
            {
                var fm = _model.TypeSettings as FileMonitorSettings ?? new FileMonitorSettings();
                fm.TargetPath = TargetPath ?? string.Empty;
                fm.PollIntervalMilliseconds = PollIntervalMs;
                fm.AllowedExtensions = new HashSet<string>((AllowedExtensionsText ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => s.StartsWith(".")) );
                fm.IsAdminFile = IsAdminFile;
                fm.UseLogin = UseLogin;
                fm.LoginUsername = LoginUsername;
                fm.LoginPassword = LoginPassword;
                fm.LoginDomain = LoginDomain;
                _model.TypeSettings = fm;
            }
            else
            {
                var ss = _model.TypeSettings as SchedulerServiceSettings ?? new SchedulerServiceSettings();
                ss.Endpoint = ApiEndpoint;
                ss.ApiKey = ApiKey;
                _model.TypeSettings = ss;
            }
        }
        #endregion
    }
}
