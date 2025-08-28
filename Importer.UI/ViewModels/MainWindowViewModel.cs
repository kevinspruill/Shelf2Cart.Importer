using Importer.UI.Helpers;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Importer.UI.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            Title = "Shelf 2 Cart Importer Manager";
            Icon = "pack://application:,,,/Importer.UI;component/Resources/Icons/merchandiser.ico";

            NavigateToContent = new DelegateCommand<string>(OnNavigateToContent);
            ServiceControlCommand = new DelegateCommand<string>(OnToggleService).ObservesProperty(() => IsAdmin);
            ServiceStatus = WindowsServiceHelper.GetServiceStatus("S2C_ImporterService");
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

        }

        private string _icon;
        public string Icon
        {
            get { return _icon; }
            set { SetProperty(ref _icon, value); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _serviceStatus;
        public string ServiceStatus
        {
            get { return _serviceStatus; }
            set 
            { 
                SetProperty(ref _serviceStatus, value);
                ServiceButtonText = _serviceStatus == "Running" ? "Stop Service" : "Start Service";
            }
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get { return _isAdmin; }
            set { SetProperty(ref _isAdmin, value); }
        }

        private string _serviceButtonText;
        public string ServiceButtonText
        {
            get { return _serviceButtonText; }
            set { SetProperty(ref _serviceButtonText, value); }
        }

        public DelegateCommand<string> ServiceControlCommand { get; set; }
        public DelegateCommand ExitCommand { get; set; }
        public DelegateCommand<string> NavigateToContent { get; set; }

        // Command Methods
        private void OnToggleService(string serviceName)
        {
            ServiceStatus = WindowsServiceHelper.ToggleService(serviceName);
        }

        private void OnNavigateToContent(string viewName)
        {
            _regionManager.RequestNavigate("ContentRegion", viewName);
        }

        private void CheckAdminRights()
        {
            IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (!IsAdmin)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show(
                    "This application requires administrative privileges to control the service. " +
                    "Would you like to restart the application as administrator?",
                    "Administrator Rights Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    RestartAsAdmin();
                }
            }
        }

        private void RestartAsAdmin()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas" // This triggers the UAC prompt
                };

                Process.Start(startInfo);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Win32Exception ex)
            {
                // User cancelled the UAC prompt
                System.Windows.MessageBox.Show(
                    "The application will continue to run with limited functionality. Service control operations will not be available.",
                    "Limited Functionality",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to restart application as administrator: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
