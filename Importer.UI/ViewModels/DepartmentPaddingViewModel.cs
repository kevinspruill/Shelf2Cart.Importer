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
    public class DepartmentPaddingViewModel : BindableBase
    {
        private readonly DatabaseHelper _db;

        public ObservableCollection<DepartmentPadding> DepartmentPaddings { get; } = new ObservableCollection<DepartmentPadding>();

        private DepartmentPadding _selected;
        public DepartmentPadding SelectedDepartmentPadding
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DepartmentPaddingViewModel(DatabaseHelper db)
        {
            _db = db;

            RefreshCommand = new DelegateCommand(Load);
            AddCommand = new DelegateCommand(Add);
            SaveCommand = new DelegateCommand(Save);
            DeleteCommand = new DelegateCommand(Delete, () => SelectedDepartmentPadding != null)
                .ObservesProperty(() => SelectedDepartmentPadding);

            Load();
        }

        private void Load()
        {
            try
            {
                DepartmentPaddings.Clear();
                var items = _db.GetDepartmentPadding();
                foreach (var item in items.OrderBy(i => i.DataDeptNum))
                    DepartmentPaddings.Add(item);
                Status = $"Loaded {DepartmentPaddings.Count} row(s).";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading Department Padding: {ex.Message}");
                Status = "Load failed.";
            }
        }

        private void Add()
        {
            var nextId = GetNextDeptNum();
            var newItem = new DepartmentPadding
            {
                DataDeptNum = nextId,
                ValueToAddToPLU = 0
            };
            DepartmentPaddings.Add(newItem);
            SelectedDepartmentPadding = newItem;
        }

        private string GetNextDeptNum()
        {
            var existing = DepartmentPaddings
                .Select(d => d.DataDeptNum)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s =>
                {
                    int n;
                    return int.TryParse(s, out n) ? n : -1;
                })
                .Where(n => n >= 0)
                .OrderBy(n => n)
                .ToList();

            int candidate = 1;
            foreach (var n in existing)
            {
                if (n == candidate) candidate++;
                else if (n > candidate) break;
            }
            return candidate.ToString();
        }

        private void Save()
        {
            try
            {
                // Basic validation
                foreach (var row in DepartmentPaddings)
                {
                    if (string.IsNullOrWhiteSpace(row.DataDeptNum))
                        throw new Exception("DataDeptNum cannot be empty.");
                    if (!int.TryParse(row.DataDeptNum, out _))
                        throw new Exception($"DataDeptNum '{row.DataDeptNum}' must be numeric.");
                }

                var success = _db.SaveDepartmentPadding(DepartmentPaddings.ToList());
                Status = success ? "Saved successfully." : "Save failed.";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving Department Padding: {ex.Message}");
                Status = "Save failed.";
            }
            Load(); // reload from DB to reflect canonical ordering
        }

        private void Delete()
        {
            if (SelectedDepartmentPadding == null) return;
            try
            {
                if (_db.DeleteDepartmentPadding(SelectedDepartmentPadding.DataDeptNum))
                {
                    DepartmentPaddings.Remove(SelectedDepartmentPadding);
                    Status = "Deleted.";
                }
                else
                {
                    Status = "Delete failed.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting Department Padding: {ex.Message}");
                Status = "Delete failed.";
            }
        }
    }
}