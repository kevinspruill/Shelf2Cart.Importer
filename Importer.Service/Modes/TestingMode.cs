using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Module.Invafresh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Core.Modes
{
    public static class TestingMode
    {
        public static async Task RunTestMode(string filePath = "")
        {
            try
            {
                Logger.Info("Starting test mode");

                IImporterModule importerModule = null;
                ICustomerProcess customerProcess = null;
                Dictionary<string, object> typeSettings = new Dictionary<string, object>();

                var configuredInstances = InstanceLoader.LoadInstances();
                foreach (var instance in configuredInstances)
                {
                    if (instance.Enabled)
                    {
                        importerModule = InstanceLoader.GetImporterModule(instance.ImporterModule);
                        importerModule.InitModule(instance);
                        importerModule.StartModule();
                        Logger.Info($"Loading module: {instance.ImporterModule} for instance: {instance.Name}");
                    }

                    Logger.Debug($"Configured instance: {instance.Name}");
                }

                if (importerModule == null)
                {
                    Logger.Error("No enabled importer module found.");
                    return;
                }

                // Keep the application running until user wants to exit
                Console.WriteLine("File watcher is now running. Press 'Q' to quit...");
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            Logger.Info("User requested to quit the application");
                            if (importerModule != null)
                            {
                                importerModule.StopModule();
                            }
                            break;
                        }
                    }
                    await Task.Delay(100); // Small delay to prevent high CPU usage
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred during test mode", ex);
                Console.WriteLine($"An error occurred during test mode: {ex.Message}");
                Console.WriteLine("Check the log file for more details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
