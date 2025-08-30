using Importer.Common.Models;
using Importer.UI.ViewModels;
using System.Windows;

namespace Importer.UI.Views
{
    public partial class InstanceConfigView : Window
    {
        private readonly InstanceConfigViewModel _vm;
        public InstanceConfigView(ImporterInstance instance)
        {
            InitializeComponent();
            _vm = new InstanceConfigViewModel(instance);
            _vm.RequestClose += Vm_RequestClose;
            DataContext = _vm;
        }

        private void Vm_RequestClose(bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
