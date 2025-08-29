using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Importer.UI.Helpers
{
    public static class WindowsServiceHelper
    {
        public static void InstallService(string servicePath)
        {
            try
            {
                // use Topshelf to install the service
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{servicePath}\" install",
                    Verb = "runas", // Run as administrator
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string ToggleService(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    else if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                return GetServiceStatus(serviceName);
            }
            catch (Exception ex)
            {
                return $"Error controlling service: {ex.Message}";
            }
        }

        public static string RestartService(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
                return GetServiceStatus(serviceName);
            }
            catch (Exception ex)
            {
                return $"Error restarting service: {ex.Message}";
            }
        }

        public static string StopService(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
                return GetServiceStatus(serviceName);
            }
            catch (Exception ex)
            {
                return $"Error stopping service: {ex.Message}";
            }
        }

        public static string StartService(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                return GetServiceStatus(serviceName);
            }
            catch (Exception ex)
            {
                return $"Error starting service: {ex.Message}";
            }
        }

        public static bool IsServiceInstalled(string serviceName)
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == serviceName);
        }

        public static string GetServiceStatus(string serviceName)
        {
            if (!IsServiceInstalled(serviceName))
            {
                return "Not Installed";
            }
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    return sc.Status == ServiceControllerStatus.Running ? "Running" : "Stopped";
                }
            }
            catch (Exception ex)
            {
                return "Error";
            }
        }
    }
}
