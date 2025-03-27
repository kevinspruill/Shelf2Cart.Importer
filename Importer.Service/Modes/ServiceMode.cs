using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Importer.Core.Modes
{
    public static class ServiceMode
    {
        // TopShelf Service Mode Intialization
        public static void RunServiceMode(string[] args)
        {
            try
            {
                // Use Host.Run to pass args automatically, it will handle install, uninstall, etc.
                var exitCode = HostFactory.Run(x =>
                {
                    x.Service<ImporterService>(s =>
                    {
                        s.ConstructUsing(name => new ImporterService());
                        s.WhenStarted(async tc => await tc.Start());
                        s.WhenStopped(async tc => await tc.Stop());
                    });
                    
                    x.RunAsLocalSystem();
                    x.SetDescription("This service will import data into The Shelf 2 Cart Merchandiser");
                    x.SetDisplayName("Shelf 2 Cart Importer Service");
                    x.SetServiceName("S2C_ImporterService");
                    x.OnException(ex =>
                    {
                        Logger.Error("An error occurred in the service", ex);
                    });
                    
                    // If args were provided, TopShelf will use them automatically
                });
                
                // Check TopShelf exit code and log it
                int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
                Logger.Info($"Service exited with code {exitCodeValue}");
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while setting up the service", ex);
                Console.WriteLine($"An error occurred while setting up the service: {ex.Message}");
                Console.WriteLine("Check the log file for more details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

    public class ImporterService
    {
        public async Task Start()
        {
            try
            {
                // new line in the logger with the Date and Time in the middle
                Logger.Info("--------------------------------------------------");
                Logger.Info($"Service started at: {DateTime.Now}");
                Logger.Info("--------------------------------------------------");

                IImporterModule importerModule = null;
                Dictionary<string, object> typeSettings = new Dictionary<string, object>();

                var configuredInstances = InstanceLoader.LoadInstances();
                foreach (var instance in configuredInstances)
                {
                    if (instance.Enabled)
                    {
                        Logger.Info($"Loading module: {instance.ImporterModule} for instance: {instance.Name}");
                        importerModule = InstanceLoader.GetImporterModule(instance.ImporterModule);
                        importerModule.InitModule(instance);
                        importerModule.StartModule();
                        Logger.Info($"Instance: {instance.Name} is enabled is loaded.");
                        Logger.Info("--------------------------------------------------");
                        Logger.Info($"Awaiting Data...");
                        Logger.Info("--------------------------------------------------");
                    }
                    else
                    {
                        Logger.Debug($"Instance {instance.Name} is not enabled, and will not be loaded.");
                    }
                }

                if (importerModule == null)
                {
                    Logger.Error("No enabled importer module found.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error starting service", ex);
            }

            await Task.CompletedTask;
        }

        public async Task Stop()
        {
            await Task.CompletedTask;
            Logger.Info("--------------------------------------------------");
            Logger.Info($"Service Stopped at: {DateTime.Now}");
            Logger.Info("--------------------------------------------------");
        }
    }
}
