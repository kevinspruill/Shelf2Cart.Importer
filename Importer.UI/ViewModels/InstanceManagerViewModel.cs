using Importer.Common.Helpers;
using Importer.Common.Models;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Importer.UI.Views;

namespace Importer.UI.ViewModels
{
    public class InstanceManagerViewModel : BindableBase
    {
        private const string InstancesFileName = "ImporterInstances.json"; // lives in Settings folder next to exe
        private string _instancesFileFullPath;

        public ObservableCollection<ImporterInstance> Instances { get; } = new ObservableCollection<ImporterInstance>();

        private ImporterInstance _selectedInstance;
        public ImporterInstance SelectedInstance
        {
            get => _selectedInstance;
            set
            {
                SetProperty(ref _selectedInstance, value);
                UpdateDetailFields(value);
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                ExportCommand.RaiseCanExecuteChanged();
            }
        }

        // Detail fields
        private string _detailName; public string DetailName { get => _detailName; set => SetProperty(ref _detailName, value); }
        private string _detailModule; public string DetailModule { get => _detailModule; set => SetProperty(ref _detailModule, value); }
        private string _detailStatus; public string DetailStatus { get => _detailStatus; set => SetProperty(ref _detailStatus, value); }
        private string _detailEnabled; public string DetailEnabled { get => _detailEnabled; set => SetProperty(ref _detailEnabled, value); }
        private string _detailCustomer; public string DetailCustomer { get => _detailCustomer; set => SetProperty(ref _detailCustomer, value); }
        private string _detailDescription; public string DetailDescription { get => _detailDescription; set => SetProperty(ref _detailDescription, value); }
        private string _detailLastRun; public string DetailLastRun { get => _detailLastRun; set => SetProperty(ref _detailLastRun, value); }
        private string _detailFilesProcessed; public string DetailFilesProcessed { get => _detailFilesProcessed; set => SetProperty(ref _detailFilesProcessed, value); }
        private string _detailRecordsImported; public string DetailRecordsImported { get => _detailRecordsImported; set => SetProperty(ref _detailRecordsImported, value); }

        // Commands
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ImportCommand { get; }
        public DelegateCommand ExportCommand { get; }

        public InstanceManagerViewModel()
        {
            RefreshCommand = new DelegateCommand(LoadInstances);
            AddCommand = new DelegateCommand(AddInstance);
            EditCommand = new DelegateCommand(EditInstance, () => SelectedInstance != null);
            DeleteCommand = new DelegateCommand(DeleteInstance, () => SelectedInstance != null);
            ImportCommand = new DelegateCommand(ImportInstances);
            ExportCommand = new DelegateCommand(ExportInstance, () => SelectedInstance != null);

            ResolveInstancesPath();
            LoadInstances();
        }

        private void ResolveInstancesPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidate = Path.Combine(baseDir, "Settings", InstancesFileName);
            _instancesFileFullPath = candidate;
        }

        private void LoadInstances()
        {
            Instances.Clear();
            try
            {
                var list = InstanceLoader.LoadInstances();
                foreach (var inst in list.OrderBy(i => i.Name))
                    Instances.Add(inst);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load instances: {ex.Message}");
            }
        }

        private void SaveInstances()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_instancesFileFullPath));
                var json = JsonConvert.SerializeObject(Instances, Formatting.Indented);
                File.WriteAllText(_instancesFileFullPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save instances: {ex.Message}");
            }
        }

        private void AddInstance()
        {
            var newInst = new ImporterInstance
            {
                Name = "New Instance",
                ImporterModule = "Generic",
                Enabled = true,
                Description = string.Empty
            };
            Instances.Add(newInst);
            SelectedInstance = newInst;
            // Open editor
            var win = new InstanceConfigView(newInst);
            if (win.ShowDialog() == true)
            {
                SaveInstances();
                UpdateDetailFields(newInst);
            }
            else
            {
                // If cancelled and still default name and maybe unused allow removal
                if (string.IsNullOrWhiteSpace(newInst.Name) || newInst.Name == "New Instance")
                {
                    Instances.Remove(newInst);
                }
            }
        }

        private void EditInstance()
        {
            if (SelectedInstance == null) return;
            var win = new InstanceConfigView(SelectedInstance);
            if (win.ShowDialog() == true)
            {
                SaveInstances();
                UpdateDetailFields(SelectedInstance);
                // Refresh grid
                var ordered = Instances.OrderBy(i => i.Name).ToList();
                Instances.Clear();
                foreach (var inst in ordered) Instances.Add(inst);
            }
        }

        private void DeleteInstance()
        {
            if (SelectedInstance == null) return;
            var toRemove = SelectedInstance;
            SelectedInstance = null;
            Instances.Remove(toRemove);
            SaveInstances();
        }

        private void ImportInstances()
        {
            // Simple import using file open dialog
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dlg.FileName);
                    var imported = JsonConvert.DeserializeObject<ImporterInstance[]>(json) ?? new ImporterInstance[0];
                    foreach (var inst in imported)
                    {
                        var existing = Instances.FirstOrDefault(i => i.Name == inst.Name);
                        if (existing == null)
                            Instances.Add(inst);
                        else
                        {
                            // optional: overwrite
                            existing.Description = inst.Description;
                            existing.ImporterModule = inst.ImporterModule;
                            existing.CustomerProcess = inst.CustomerProcess;
                            existing.Enabled = inst.Enabled;
                            existing.TypeSettings = inst.TypeSettings;
                        }
                    }
                    var ordered = Instances.OrderBy(i => i.Name).ToList();
                    Instances.Clear();
                    foreach (var inst in ordered) Instances.Add(inst);
                    SaveInstances();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Import failed: {ex.Message}");
                }
            }
        }

        private void ExportInstance()
        {
            if (SelectedInstance == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = SelectedInstance.Name + ".json",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(SelectedInstance, Formatting.Indented);
                    File.WriteAllText(dlg.FileName, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
                }
            }
        }

        private void UpdateDetailFields(ImporterInstance inst)
        {
            DetailName = inst?.Name;
            DetailModule = inst?.ImporterModule;
            DetailEnabled = inst == null ? null : (inst.Enabled ? "Yes" : "No");
            DetailCustomer = inst?.CustomerProcess;
            DetailDescription = inst?.Description;
        }
    }
}
