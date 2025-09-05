using System;
using System.ComponentModel;

namespace Importer.Common.Models
{
    public class LocalEditFieldModel : INotifyPropertyChanged
    {
        private string _editField;
        private string _editable;
        private bool _isDirty;

        public string Edit_Field
        {
            get => _editField;
            set
            {
                if (_editField != value)
                {
                    _editField = value;
                    IsDirty = true;
                    OnPropertyChanged(nameof(Edit_Field));
                }
            }
        }
        public string Editable
        {
            get => _editable;
            set
            {
                if (_editable != value)
                {
                    _editable = value;
                    IsDirty = true;
                    OnPropertyChanged(nameof(Editable));
                    OnPropertyChanged(nameof(IsEditable));
                }
            }
        }
        public bool IsEditable
        {
            get => string.Equals(Editable, "Yes", StringComparison.OrdinalIgnoreCase);
            set => Editable = value ? "Yes" : "No";
        }
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }
        public void AcceptChanges()
        {
            IsDirty = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
