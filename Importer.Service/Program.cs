using Importer.Common.Helpers;
using Importer.Core.Modes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Importer.Service
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Logger.Info("Application starting");
                bool testingEnabled = IsTestingEnabled();

                if (testingEnabled)
                {
                    Logger.Info("Running in test mode");
                    await RunTestMode();
                }
                else
                {
                    Logger.Info("Running in service mode");
                    RunServiceMode();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occurred in the main program", ex);
                Console.WriteLine("An error occurred. Check the log file for details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                Logger.Info("Application shutting down");
            }
        }


        private static bool IsTestingEnabled()
        {
            // Check if the TestingEnabled setting is set to true in the settings file

            SettingsLoader settingsLoader = new SettingsLoader();
            return settingsLoader.TestingEnabled;
        }

        private static async Task RunTestMode()
        {

            try
            {
                Logger.Info("Starting test mode");
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

        private static void RunServiceMode()
        {
            try
            {
                HostFactory.Run(x =>
                {
                    x.Service<ServiceMode>(s =>
                    {
                        s.ConstructUsing(name => new ServiceMode());
                        s.WhenStarted(async tc => await tc.Start());
                        s.WhenStopped(async tc => await tc.Stop());
                    });
                    x.RunAsLocalSystem();
                    x.SetDescription("This service will import data a Data Host into Shelf 2 Cart Merchandiser");
                    x.SetDisplayName("Shelf 2 Cart Importer Service");
                    x.SetServiceName("S2C_ImporterService");

                    x.OnException(ex =>
                    {
                        Logger.Error("An error occurred in the service", ex);
                    });
                });
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
}
