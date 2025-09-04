using Importer.Common.Helpers;
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
        private const string _serviceName = "S2C_ImporterService";

        public MainWindowViewModel(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            Title = "Shelf 2 Cart Importer Manager";
            Icon = "pack://application:,,,/Importer.UI;component/Resources/Icons/merchandiser.ico";
            
            // service commands - TODO: Put into own Methods
            InstallServiceCommand = new DelegateCommand(OnInstallService).ObservesProperty(() => IsAdmin).ObservesProperty(() => IsServiceInstalled);
            StartServiceCommand = new DelegateCommand(OnStartService).ObservesProperty(() => IsAdmin);
            RestartServiceCommand = new DelegateCommand(OnRestartService).ObservesProperty(() => IsAdmin);
            StopServiceCommand = new DelegateCommand(OnStopService).ObservesProperty(() => IsAdmin);            
            ServiceControlCommand = new DelegateCommand<string>(OnToggleService).ObservesProperty(() => IsAdmin);

            CompactDatabaseCommand = new DelegateCommand<string>(OnCompactDatabase);
            ResetDatabasesCommand = new DelegateCommand(OnFlushDatabase);

            NavigateToContent = new DelegateCommand<string>(OnNavigateToContent);
            OpenLogFilesCommand = new DelegateCommand(OnOpenLogFiles);
            ProcessDatabaseCommand = new DelegateCommand(OnProcessDatabase);
            OpenInstallFolderCommand = new DelegateCommand(OnOpenInstallFolder);
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

            IsServiceInstalled = WindowsServiceHelper.IsServiceInstalled(_serviceName);
            ServiceStatus = WindowsServiceHelper.GetServiceStatus(_serviceName);

            CheckAdminRights();
        }

        public void OnCompactDatabase(string database)
        {
            try
            {
                // Cast string to database type enum
                var dbType = (DatabaseType)Enum.Parse(typeof(DatabaseType), database);

                var ImporterDatabase = new DatabaseHelper(dbType);
                ImporterDatabase.CompactDatabase();

                // message box to confirm completion
                MessageBox.Show($"{database} has been compacted.", "Compact Database", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start Compact Database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnFlushDatabase()
        {
            try
            {
                // message box to confirm
                var result = MessageBox.Show("Are you sure you want to flush the Importer database? This will delete all products.", "Confirm Flush Database", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                var ImporterDatabase = new DatabaseHelper(DatabaseType.ImportDatabase);
                ImporterDatabase.DeleteAllProducts();
                ImporterDatabase.CompactDatabase();

                // message box to confirm completion
                MessageBox.Show("Importer database has been flushed and Compacted.", "Flush Database", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start Flush Database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenInstallFolder()
        {
            try
            {
                string installFolderPath = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start("explorer.exe", installFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open install folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnProcessDatabase()
        {
            try
                {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "ExecuteProcessDatabase.exe",
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                };
                Process process = Process.Start(startInfo);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start Process Database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenLogFiles()
        {
            try
            {
                string logFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!System.IO.Directory.Exists(logFolderPath))
                {
                    System.IO.Directory.CreateDirectory(logFolderPath);
                }
                Process.Start("explorer.exe", logFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open log files folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnStopService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
                ServiceStatus = WindowsServiceHelper.GetServiceStatus(_serviceName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRestartService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
                ServiceStatus = WindowsServiceHelper.GetServiceStatus(_serviceName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restarting service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnStartService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                ServiceStatus = WindowsServiceHelper.GetServiceStatus(_serviceName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnInstallService()
        {
            if (IsServiceInstalled)
            {
                MessageBox.Show("Service is already installed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            WindowsServiceHelper.InstallService(_serviceName);
            IsServiceInstalled = WindowsServiceHelper.IsServiceInstalled(_serviceName);
            ServiceStatus = WindowsServiceHelper.GetServiceStatus(_serviceName);
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

        private bool _isServiceInstalled;
        public bool IsServiceInstalled
        {
            get { return _isServiceInstalled; }
            set { SetProperty(ref _isServiceInstalled, value); }
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
        // Commands
        public DelegateCommand ResetDatabasesCommand { get; set; }
        public DelegateCommand<string> CompactDatabaseCommand { get; set; }
        public DelegateCommand OpenInstallFolderCommand { get; set; }
        public DelegateCommand ProcessDatabaseCommand { get; set; }
        public DelegateCommand InstallServiceCommand { get; set; }
        public DelegateCommand StartServiceCommand { get; set; }
        public DelegateCommand RestartServiceCommand { get; set; }
        public DelegateCommand StopServiceCommand { get; set; }
        public DelegateCommand<string> ServiceControlCommand { get; set; }
        public DelegateCommand ExitCommand { get; set; }
        public DelegateCommand<string> NavigateToContent { get; set; }
        public DelegateCommand OpenLogFilesCommand { get; set; }

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
