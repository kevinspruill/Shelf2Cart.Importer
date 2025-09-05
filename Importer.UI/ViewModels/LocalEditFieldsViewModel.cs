using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using Importer.Common.Helpers;
using System.Linq;
using System.Windows.Input;
using System;
using Importer.Common.Models;

namespace Importer.UI.ViewModels
{
    public class LocalEditFieldsViewModel : BindableBase
    {
        private readonly DatabaseHelper _residentDb; // should be constructed with ResidentDatabase

        public ObservableCollection<LocalEditFieldModel> LocalEditFields { get; } = new ObservableCollection<LocalEditFieldModel>();

        private LocalEditFieldModel _selected;
        public LocalEditFieldModel SelectedLocalEditField
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public DelegateCommand<LocalEditFieldModel> ToggleProtectedCommand { get; }

        private string _newFieldName;
        public string NewFieldName
        {
            get => _newFieldName;
            set => SetProperty(ref _newFieldName, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public LocalEditFieldsViewModel()
        {
            _residentDb = new DatabaseHelper(DatabaseType.ResidentDatabase);    
            RefreshCommand = new DelegateCommand(Load);
            AddCommand = new DelegateCommand(Add);
            SaveCommand = new DelegateCommand(Save);
            DeleteCommand = new DelegateCommand(Delete, () => SelectedLocalEditField != null)
                .ObservesProperty(() => SelectedLocalEditField);
            ToggleProtectedCommand = new DelegateCommand<LocalEditFieldModel>(ToggleProtected);
            Load();
        }

        private void Load()
        {
            try
            {
                LocalEditFields.Clear();
                var items = _residentDb.GetLocalEditFields();
                foreach (var item in items.OrderBy(i => i.Edit_Field))
                {
                    LocalEditFields.Add(item);
                    item.AcceptChanges(); // mark as clean after initial load
                }
                Status = $"Loaded {LocalEditFields.Count} field(s).";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading LocalEditFields: {ex.Message}");
                Status = "Load failed.";
            }
        }

        private void Add()
        {
            if (string.IsNullOrWhiteSpace(NewFieldName))
            {
                Status = "Enter a field name first.";
                return;
            }
            if (LocalEditFields.Any(f => f.Edit_Field.Equals(NewFieldName, StringComparison.OrdinalIgnoreCase)))
            {
                Status = "Field already exists.";
                return;
            }
            var newItem = new LocalEditFieldModel { Edit_Field = NewFieldName.Trim(), Editable = "Yes" };
            LocalEditFields.Add(newItem);
            SelectedLocalEditField = newItem;
            NewFieldName = string.Empty;
            Status = "Added new field (not saved yet).";
        }

        private void Save()
        {
            try
            {
                foreach (var row in LocalEditFields)
                {
                    if (string.IsNullOrWhiteSpace(row.Edit_Field))
                        throw new Exception("Edit_Field cannot be empty.");
                }
                var dup = LocalEditFields.GroupBy(r => r.Edit_Field.ToLower()).FirstOrDefault(g => g.Count() > 1);
                if (dup != null)
                    throw new Exception($"Duplicate field name: {dup.Key}");

                var success = _residentDb.SaveLocalEditFields(LocalEditFields.ToList());
                if (success)
                {
                    foreach (var row in LocalEditFields) row.AcceptChanges();
                }
                Status = success ? "Saved successfully." : "Save failed.";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving LocalEditFields: {ex.Message}");
                Status = "Save failed.";
            }
            Load();
        }

        private void Delete()
        {
            if (SelectedLocalEditField == null) return;
            try
            {
                if (_residentDb.DeleteLocalEditField(SelectedLocalEditField.Edit_Field))
                {
                    LocalEditFields.Remove(SelectedLocalEditField);
                    Status = "Deleted.";
                }
                else
                {
                    Status = "Delete failed.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting LocalEditField: {ex.Message}");
                Status = "Delete failed.";
            }
        }

        private void ToggleProtected(LocalEditFieldModel row)
        {
            if (row == null)
                return;
            row.Editable = string.Equals(row.Editable, "Yes", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";
            Status = $"Toggled '{row.Edit_Field}' to {row.Editable}. (Save All to persist)";
        }
    }
}
