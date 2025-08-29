using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Importer.UI.Views
{
    /// <summary>
    /// Interaction logic for ConsoleView.xaml
    /// </summary>
    public partial class ConsoleView : UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
            this.DataContextChanged += ConsoleView_DataContextChanged;
        }

        private void ConsoleView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var oldNotify = e.OldValue as INotifyPropertyChanged;
            if (oldNotify != null)
                oldNotify.PropertyChanged -= Notify_PropertyChanged;

            var notify = e.NewValue as INotifyPropertyChanged;
            if (notify != null)
                notify.PropertyChanged += Notify_PropertyChanged;
        }

        private void Notify_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ConsoleData")
            {
                var sv = this.FindName("ConsoleScrollViewer") as ScrollViewer;
                sv?.ScrollToEnd();
            }
        }
    }
}
